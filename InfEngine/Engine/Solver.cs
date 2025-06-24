namespace InfEngine.Engine;

public class Solver
{
    public const long MaxRecursion = 1000;
    
    private List<EqGoal> _eqGoals = new();
    private List<RecImplGoalChain> _implGoals = new();
    private HashSet<ImplGoalChain> _implGoalStack = new();
    private List<Clause> _clauses = new();
    private TermMatch? _match;
    private Dictionary<string, Instatiation> _instatiations = new();
    private static long _goalSeed = 0;

    public SolverResult? Run()
    {
        var solver = this.InternalRun();
        if (solver == null)
        {
            return null;
        }

        return new SolverResult(solver._instatiations, solver._match ?? new TermMatch(new Dictionary<FreeVar, Term>()));
    }

    public Solver(List<Goal> goals, List<Clause> clauses)
    {
        this._eqGoals.AddRange(goals.OfType<EqGoal>());
        this._implGoals.AddRange(goals.OfType<ImplGoal>().Select((x, i) => new RecImplGoalChain(x, i + 1, 0)));
        this._clauses.AddRange(clauses);
    }

    private Solver()
    {
        
    }

    private Solver? InternalRun()
    {
        if (!this.HandleEqGoals())
        {
            return null;
        }

        return this.HandleImplGoals();
    }

    private Solver? HandleImplGoals()
    {
        var implGoal = this.RetrieveImplGoal();
        if (implGoal == null)
        {
            return this;
        }
        
        var candidates = GetCandidates(implGoal.Value.Goal, implGoal.Value.ChainId, implGoal.Value.RecursionDepth);

        foreach (var candidate in candidates)
        {
            var solver = candidate.InternalRun();
            if (solver != null)
                return solver;
        }

        return null;
    }

    private List<Solver> GetCandidates(ImplGoal implGoal, long chainId, long recursionDepth)
    {
        List<Solver> candidates = new();
        foreach (var implClause in this._clauses.OfType<ImplClause>())
        {
            var newVars = implClause.TyParams.Select(x => (BoundVar: x, FreeVar: FreeVar.New())).ToDictionary(x => x.BoundVar, x => x.FreeVar);
            var trait = implClause.Trait.Replace<BoundVar>(b => newVars[b]);
            var target = implClause.Target.Replace<BoundVar>(b => newVars[b]);
            
            var subs = Term.TryMatch(new App("S", [target, trait]), new App("S", [implGoal.Target, implGoal.Trait]));
            if (subs == null)
            {
                continue;
            }

            if (newVars.Keys.All(k => subs.Substitutions.ContainsKey(newVars[k])))
            {
                var candidate = BuildCandidate(subs, newVars, implClause, new RecImplGoalChain(implGoal, chainId, recursionDepth));
                if (candidate != null)
                    candidates.Add(candidate);
            }
        }

        return candidates;
    }

    private Solver? BuildCandidate(TermMatch substitutions, 
                                   Dictionary<BoundVar, FreeVar> varMap,
                                   ImplClause clause, 
                                   RecImplGoalChain implGoalChain)
    {
        var eqGoals = new List<EqGoal>();
        var implGoals = this._implGoals.ToList();
        implGoals.Remove(implGoalChain);
        var implGoalStack = this._implGoalStack.ToHashSet();

        // infinite recursion
        if (!implGoalStack.Add(new ImplGoalChain(implGoalChain.Goal, implGoalChain.ChainId)))
        {
            return null;
        }
        
        // test if the goal has already been solved
        if (this._implGoalStack.Any(x => x.Goal.Trait == implGoalChain.Goal.Trait && x.Goal.Target == implGoalChain.Goal.Target))
        {
            var goal = this._implGoalStack.First(x => x.Goal.Trait == implGoalChain.Goal.Trait && x.Goal.Target == implGoalChain.Goal.Target);
            var newInstantiations = this._instatiations.ToDictionary(entry => entry.Key, entry => entry.Value);
            newInstantiations[implGoalChain.Goal.ResolvesTo] = newInstantiations[goal.Goal.ResolvesTo];
            
            // reuse proof
            return new Solver()
            {
                _implGoals = implGoals,
                _match = this._match,
                _clauses = this._clauses,
                _eqGoals = eqGoals,
                _instatiations = newInstantiations,
                _implGoalStack = implGoalStack
            };
        }

        // possibly infinite recursion
        if (implGoalChain.RecursionDepth > MaxRecursion)
        {
            return null;
        }
        
        var instantiations = this._instatiations.ToDictionary(
            entry => entry.Key,
            entry => entry.Value);
        
        var inst = new Instatiation(
            clause.Name, 
            clause.TyParams.Select(p => substitutions.Substitutions[varMap[p]]).ToList(), 
            clause.Constraints.Select(_ => $"$g{++_goalSeed}".ToString()).ToList());
        
        var substConstraints = clause.TyParams.ToDictionary(x => x, x => substitutions.Substitutions[varMap[x]]);
        instantiations[implGoalChain.Goal.ResolvesTo] = inst;

        foreach (var s in substitutions.Substitutions)
        {
            eqGoals.Add(new EqGoal(s.Key, s.Value));
        }

        for (int i = 0; i < clause.Constraints.Length; i++)
        {
            var c = clause.Constraints[i];
            var n = inst.Constraints[i];
            implGoals.Add(implGoalChain with { Goal = new ImplGoal(c.Target.Substitute(substConstraints), c.Trait.Substitute(substConstraints), n), RecursionDepth = implGoalChain.RecursionDepth + 1 });
        }

        var newSolver = new Solver()
        {
            _implGoals = implGoals,
            _match = this._match,
            _clauses = this._clauses,
            _eqGoals = eqGoals,
            _instatiations = instantiations,
            _implGoalStack = implGoalStack
        };
        return newSolver;
    }

    private RecImplGoalChain? RetrieveImplGoal()
    {
        if (this._implGoals.Count == 0)
            return null;
        
        var nonGenImplGoals = this._implGoals.FirstOrDefault(g => g.Goal.IsNonGeneric());
        if (nonGenImplGoals.Goal != null)
        {
            return nonGenImplGoals;
        }

        var nonGenericTarget = this._implGoals.Where(x => !x.Goal.Target.Any<FreeVar>())
                                   .Select(x => (ImplGoal: x, CountFreeVars: x.Goal.CountFreeVars(), x.ChainId))
                                   .OrderBy(x => x.CountFreeVars)
                                   .FirstOrDefault();

        if (nonGenericTarget.ChainId != 0)
        {
            return nonGenericTarget.ImplGoal;
        }

        var implGoalWithLeastVars = this._implGoals
                                    .Select(x => (ImplGoal: x, CountFreeVars: x.Goal.CountFreeVars(), x.ChainId, x.RecursionDepth))
                                    .OrderBy(x => x.CountFreeVars)
                                    .FirstOrDefault();

        return implGoalWithLeastVars.ChainId == 0 ? null : new RecImplGoalChain(implGoalWithLeastVars.ImplGoal.Goal, implGoalWithLeastVars.ChainId, implGoalWithLeastVars.RecursionDepth);
    }

    private bool HandleEqGoals()
    {
        if(this._eqGoals.Count == 0)
            return true;

        while(this._eqGoals.Count != 0)
        {
            var eqGoal = _eqGoals[^1];
            _eqGoals.RemoveAt(_eqGoals.Count - 1);
            
            var match = Term.TryMatch(eqGoal.Left, eqGoal.Right);
            if (match == null)
            {
                return false;
            }
            this._match = this._match == null ? match : this._match.Merge(match);

            for (int i = 0; i < _eqGoals.Count; i++)
            {
                _eqGoals[i] = _eqGoals[i].Substitute(match);
            }
        }

        if (this._match != null)
        {
            for (int i = 0; i < this._implGoals.Count; i++)
            {
                this._implGoals[i] = new RecImplGoalChain(this._implGoals[i].Goal.Substitute(this._match), this._implGoals[i].ChainId, this._implGoals[i].RecursionDepth);
            }

            this._implGoalStack = new HashSet<ImplGoalChain>(this._implGoalStack.Select(i => i.Substitute(this._match))).ToHashSet();
            
            foreach (var instName in this._instatiations.Keys.ToList())
            {
                this._instatiations[instName] = this._instatiations[instName].Substitute(this._match);
            }
        }

        return true;
    }
    
    public readonly record struct ImplGoalChain(ImplGoal Goal, long ChainId)
    {
        public ImplGoalChain Substitute(TermMatch match)
        {
            return new ImplGoalChain(new ImplGoal(Goal.Target.Substitute(match), Goal.Trait.Substitute(match), Goal.ResolvesTo), ChainId);
        }
    }

    public record struct RecImplGoalChain(ImplGoal Goal, long ChainId, long RecursionDepth);
}