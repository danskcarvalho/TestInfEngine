using InfEngine.Engine.Clauses;
using InfEngine.Engine.Goals;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine;

public partial class Solver
{
    private Solver? BuildCandidate(TermMatch substitutions,
                                   Dictionary<BoundVar, FreeVar> varMap,
                                   ImplClause clause,
                                   RecImplGoalChain implGoalChain)
    {
        var eqGoals = new List<EqGoal>(substitutions.LateEqGoals);
        AddAssocTraitGoals(eqGoals, implGoalChain.Goal);
        var implGoals = this._implGoals.ToList();
        implGoals.Remove(implGoalChain);
        var normGoals = this._normGoals.ToList();
        var provenGoals = this._provenImplGoals.ToDictionary(x => x.Key, x => x.Value);
        // we create normalization goals specifically here so we can chain the proofs so we have a well-defined
        // recursion chain
        CreateNormalizationGoals(normGoals, eqGoals, implGoalChain.Chain, implGoalChain.RecursionDepth);

        // infinite recursion
        if (!TryAddProvenImplGoal(implGoalChain, provenGoals, clause, substitutions, varMap))
        {
            return null;
        }

        // test if the goal has already been solved
        var existing = this.TryReuseExistingProof(implGoalChain, implGoals, normGoals, eqGoals);
        if (existing != null)
        {
            return existing;
        }

        // possibly infinite recursion
        if (implGoalChain.RecursionDepth > MaxRecursion)
        {
            return null;
        }

        // add new instantiation
        var instantiations = this.AddNewInstantiation(substitutions, varMap, clause, implGoalChain, out var inst);

        // add substitutions as eq goals
        AddSubstitutionsAsEqGoals(substitutions, eqGoals);

        // Add requirements of the found instance to the solver
        AddRequirementsFromInstantiation(substitutions, varMap, clause, implGoalChain, inst, implGoals);

        var newSolver = new Solver(this._iterations)
        {
            _implGoals = implGoals,
            _match = this._match,
            _clauses = this._clauses,
            _eqGoals = eqGoals,
            _instatiations = instantiations,
            _provenImplGoals =  provenGoals,
            _normGoals = normGoals
        };
        return newSolver;
    }

    private void AddAssocTraitGoals(List<EqGoal> eqGoals, ImplGoal goal)
    {
        foreach (var goalAssocConstraint in goal.AssocConstraints)
        {
            var left = new Alias(goal.Target, goal.Trait, goalAssocConstraint.Key);
            var right = goalAssocConstraint.Value;
            eqGoals.Add(new EqGoal(left, right));   
        }
    }

    private static void AddRequirementsFromInstantiation(TermMatch substitutions, Dictionary<BoundVar, FreeVar> varMap,
                                                         ImplClause clause,
                                                         RecImplGoalChain implGoalChain, Instatiation inst,
                                                         List<RecImplGoalChain> implGoals)
    {
        var substConstraints = clause.TyParams.ToDictionary(x => x, x => substitutions.Substitutions[varMap[x]]);
        for (int i = 0; i < clause.Constraints.Length; i++)
        {
            var c = clause.Constraints[i];
            var n = inst.Constraints[i];
            implGoals.Add(new RecImplGoalChain(
                new ImplGoal(
                    c.Target.Substitute(substConstraints), 
                    c.Trait.Substitute(substConstraints),
                    c.AssocConstraints.ToDictionary(x => x.Key, x => x.Value.Substitute(substConstraints)),
                    n), 
                new ProofChain(implGoalChain.Chain),
                implGoalChain.RecursionDepth + 1));
        }
    }

    private static void AddSubstitutionsAsEqGoals(TermMatch substitutions, List<EqGoal> eqGoals)
    {
        foreach (var s in substitutions.Substitutions)
        {
            eqGoals.Add(new EqGoal(s.Key, s.Value));
        }
    }

    private Dictionary<string, Instatiation> AddNewInstantiation(TermMatch substitutions,
                                                                 Dictionary<BoundVar, FreeVar> varMap,
                                                                 ImplClause clause,
                                                                 RecImplGoalChain implGoalChain, out Instatiation inst)
    {
        var instantiations = this._instatiations.ToDictionary(
            entry => entry.Key,
            entry => entry.Value);

        inst = new Instatiation(
            clause.Name,
            clause.TyParams.Select(p => substitutions.Substitutions[varMap[p]]).ToList(),
            clause.Constraints.Select(_ => $"$g{++_goalSeed}".ToString()).ToList());

        instantiations[implGoalChain.Goal.ResolvesTo] = inst;
        return instantiations;
    }

    private Solver? TryReuseExistingProof(RecImplGoalChain implGoalChain, 
                                          List<RecImplGoalChain> implGoals,
                                          List<RecNormGoalChain> normGoals,
                                          List<EqGoal> eqGoals)
    {
        var goalName = this._provenImplGoals.SelectMany(x => x.Value).FirstOrDefault(x =>
            x.Target == implGoalChain.Goal.Target && x.Trait == implGoalChain.Goal.Trait).ResolvesTo;
        
        if (goalName != null)
        {
            var newInstantiations = this._instatiations.ToDictionary(entry => entry.Key, entry => entry.Value);
            newInstantiations[implGoalChain.Goal.ResolvesTo] = newInstantiations[goalName];
        
            // reuse proof
            return new Solver(this._iterations)
            {
                _implGoals = implGoals,
                _match = this._match,
                _clauses = this._clauses,
                _eqGoals = eqGoals,
                _instatiations = newInstantiations,
                _provenImplGoals = this._provenImplGoals.ToDictionary(x => x.Key, x => x.Value),
                _normGoals = normGoals
            };
        }

        return null;
    }

    private static bool TryAddProvenImplGoal(RecImplGoalChain implGoalChain, 
                                             Dictionary<ProofChain, List<ProvenImplGoal>> provenGoals,
                                             ImplClause clause,
                                             TermMatch substitutions,
                                             Dictionary<BoundVar, FreeVar> varMap)
    {
        ProofChain? chain = implGoalChain.Chain;
        var args = clause.TyParams.Select((p, i) => (I: i, S: substitutions.Substitutions[varMap[p]]))
                         .ToDictionary(x => clause.TyParams[x.I], x => x.S);
        
        while (chain != null)
        {
            if (provenGoals.TryGetValue(chain, out var list))
            {
                foreach (var pg in list)
                {
                    if (clause == pg.Impl && IsInfiniteRecursion(pg.Args, args))
                    {
                        return false;
                    }
                }
            }
            chain = chain.Parent;
        }

        if (!provenGoals.ContainsKey(implGoalChain.Chain))
        {
            provenGoals[implGoalChain.Chain] = [];
        }
        
        provenGoals[implGoalChain.Chain].Add(
            new ProvenImplGoal(
                clause,
                implGoalChain.Goal.Target,
                implGoalChain.Goal.Trait, 
                implGoalChain.Chain,
                args,
                implGoalChain.Goal.ResolvesTo));
        return true;
    }

    private static bool IsInfiniteRecursion(IReadOnlyDictionary<BoundVar, Term> onStackArgs, IReadOnlyDictionary<BoundVar, Term> newArgs)
    {
        return newArgs.All(x => x.Value.Contains(onStackArgs[x.Key]));
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
                var candidate = this.BuildCandidate(subs, newVars, implClause, implGoal);
                if (candidate != null)
                    candidates.Add(candidate);
            }
        }

        return candidates;
    }
}