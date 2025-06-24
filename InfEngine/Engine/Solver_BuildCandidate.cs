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
        var implGoalStack = this._implGoalStack.ToHashSet();

        // infinite recursion
        if (!TryAddGoalStack(implGoalChain, implGoalStack))
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
            implGoals.Add(implGoalChain with
            {
                Goal = new ImplGoal(c.Target.Substitute(substConstraints), c.Trait.Substitute(substConstraints), n),
                RecursionDepth = implGoalChain.RecursionDepth + 1
            });
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

    private Solver? TryReuseExistingProof(RecImplGoalChain implGoalChain, List<RecImplGoalChain> implGoals,
                                          List<EqGoal> eqGoals)
    {
        if (this._implGoalStack.Any(x =>
            x.Goal.Trait == implGoalChain.Goal.Trait && x.Goal.Target == implGoalChain.Goal.Target))
        {
            var goal = this._implGoalStack.First(x =>
                x.Goal.Trait == implGoalChain.Goal.Trait && x.Goal.Target == implGoalChain.Goal.Target);
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
                _implGoalStack = this._implGoalStack
            };
        }

        return null;
    }

    private static bool TryAddGoalStack(RecImplGoalChain implGoalChain, HashSet<ImplGoalChain> implGoalStack)
    {
        return implGoalStack.Add(new ImplGoalChain(implGoalChain.Goal, implGoalChain.ChainId));
    }
}