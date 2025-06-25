namespace InfEngine.Engine;

public partial class Solver
{
    private Solver? BuildCandidate(TermMatch substitutions,
                                   Dictionary<BoundVar, FreeVar> varMap,
                                   ImplClause clause,
                                   RecImplGoalChain implGoalChain)
    {
        var eqGoals = new List<EqGoal>();
        var implGoals = this._implGoals.ToList();
        implGoals.Remove(implGoalChain);
        var provenGoals = this._provenImplGoals.ToDictionary(x => x.Key, x => x.Value);

        // infinite recursion
        if (!TryAddProvenImplGoal(implGoalChain, provenGoals))
        {
            return null;
        }

        // test if the goal has already been solved
        var existing = this.TryReuseExistingProof(implGoalChain, implGoals, eqGoals);
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
            _provenImplGoals =  provenGoals
        };
        return newSolver;
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
                new ImplGoal(c.Target.Substitute(substConstraints), c.Trait.Substitute(substConstraints), n), 
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
                                          List<EqGoal> eqGoals)
    {
        var goalNames = this._provenImplGoals.FirstOrDefault(x =>
            x.Key.Target == implGoalChain.Goal.Target && x.Key.Trait == implGoalChain.Goal.Trait).Value;
        if (goalNames != null && goalNames.Count != 0)
        {
            var newInstantiations = this._instatiations.ToDictionary(entry => entry.Key, entry => entry.Value);
            newInstantiations[implGoalChain.Goal.ResolvesTo] = newInstantiations[goalNames[0]];

            // reuse proof
            return new Solver(this._iterations)
            {
                _implGoals = implGoals,
                _match = this._match,
                _clauses = this._clauses,
                _eqGoals = eqGoals,
                _instatiations = newInstantiations,
                _provenImplGoals = this._provenImplGoals.ToDictionary(x => x.Key, x => x.Value)
            };
        }

        return null;
    }

    private static bool TryAddProvenImplGoal(RecImplGoalChain implGoalChain, Dictionary<ProvenImplGoal, List<string>> provenGoals)
    {
        ProofChain? chain = implGoalChain.Chain;
        while (chain != null)
        {
            if (provenGoals.ContainsKey(implGoalChain.Goal.ToProvenGoal(chain)))
            {
                return false;
            }
            chain = chain.Parent;
        }
        
        provenGoals[implGoalChain.Goal.ToProvenGoal(implGoalChain.Chain)] = [implGoalChain.Goal.ResolvesTo];
        return true;
    }
}