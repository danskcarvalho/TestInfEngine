namespace InfEngine.Engine;

public partial class Solver
{
    public const long MaxRecursion = 100;
    public const long MaxIterations = 50000;
    
    private List<EqGoal> _eqGoals = new();
    private List<RecImplGoalChain> _implGoals = new();
    private Dictionary<ProvenImplGoal, List<string>> _provenImplGoals = new();
    private List<Clause> _clauses = new();
    private TermMatch? _match;
    private Dictionary<string, Instatiation> _instatiations = new();
    private static long _goalSeed = 0;
    private readonly IterationCount _iterations;

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
        this._implGoals.AddRange(goals.OfType<ImplGoal>().Select((x, i) => new RecImplGoalChain(x, new ProofChain(), 0)));
        this._clauses.AddRange(clauses);
        this._iterations = new IterationCount();
    }

    private Solver(IterationCount iterationCount)
    {
        this._iterations = iterationCount;
        this._iterations.Increment();
    }

    private Solver? InternalRun()
    {
        if (this._iterations.Overflown())
        {
            return null;
        }
        
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
        
        var candidates = GetCandidates(implGoal.Value);

        foreach (var candidate in candidates)
        {
            var solver = candidate.InternalRun();
            if (solver != null)
                return solver;
        }

        return null;
    }

    private List<Solver> GetCandidates(RecImplGoalChain implGoal)
    {
        List<Solver> candidates = new();
        foreach (var implClause in this._clauses.OfType<ImplClause>())
        {
            var newVars = implClause.TyParams.Select(x => (BoundVar: x, FreeVar: FreeVar.New())).ToDictionary(x => x.BoundVar, x => x.FreeVar);
            var trait = implClause.Trait.Replace<BoundVar>(b => newVars[b]);
            var target = implClause.Target.Replace<BoundVar>(b => newVars[b]);
            
            var subs = Term.TryMatch(new App("S", [target, trait]), new App("S", [implGoal.Goal.Target, implGoal.Goal.Trait]));
            if (subs == null)
            {
                continue;
            }

            if (newVars.Keys.All(k => subs.Substitutions.ContainsKey(newVars[k])))
            {
                var candidate = BuildCandidate(subs, newVars, implClause, implGoal);
                if (candidate != null)
                    candidates.Add(candidate);
            }
        }

        return candidates;
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
                                   .Select(x => (ImplGoal: (RecImplGoalChain?)x, CountFreeVars: x.Goal.CountFreeVars()))
                                   .OrderBy(x => x.CountFreeVars)
                                   .FirstOrDefault();

        if (nonGenericTarget.ImplGoal != null)
        {
            return nonGenericTarget.ImplGoal;
        }

        var implGoalWithLeastVars = this._implGoals
                                    .Select(x => (ImplGoal: (RecImplGoalChain?)x, CountFreeVars: x.Goal.CountFreeVars(), x.RecursionDepth))
                                    .OrderBy(x => x.CountFreeVars)
                                    .FirstOrDefault();

        return implGoalWithLeastVars.ImplGoal;
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
                this._implGoals[i] = new RecImplGoalChain(this._implGoals[i].Goal.Substitute(this._match), this._implGoals[i].Chain, this._implGoals[i].RecursionDepth);
            }

            var newProvenImplGoals = new Dictionary<ProvenImplGoal, List<string>>();
            foreach (var (goal, constraintsNames) in this._provenImplGoals)
            {
                var newGoal = goal.Substitute(this._match);
                if (newProvenImplGoals.ContainsKey(newGoal))
                {
                    newProvenImplGoals[newGoal].AddRange(constraintsNames);
                }
                else
                {
                    newProvenImplGoals[newGoal] = constraintsNames.ToList();
                }
            }
            this._provenImplGoals = newProvenImplGoals;
            
            foreach (var instName in this._instatiations.Keys.ToList())
            {
                this._instatiations[instName] = this._instatiations[instName].Substitute(this._match);
            }
        }

        return true;
    }

    public record struct RecImplGoalChain(ImplGoal Goal, ProofChain Chain, long RecursionDepth);

    public class IterationCount
    {
        private int _iterations;

        public void Increment()
        {
            this._iterations++;
        }

        public bool Overflown()
        {
            return this._iterations > MaxIterations;
        }
    }
}