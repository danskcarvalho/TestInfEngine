using InfEngine.Engine.Clauses;
using InfEngine.Engine.Goals;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine;

public partial class Solver
{
    private Solver? BuildCandidate(TermMatch substitutions,
                                   Dictionary<BoundVar, FreeVar> varMap,
                                   Clause clause,
                                   RecImplGoalChain implGoalChain)
    {
        var implGoals = this._implGoals.ToList();
        implGoals.Remove(implGoalChain);
        
        var normGoals = this._normGoals.ToList();
        var provenGoals = this._provenImplGoals.ToDictionary();
        
        // infinite recursion
        if (!TryAddProvenImplGoal(implGoalChain.Chain, provenGoals, clause, substitutions, varMap))
        {
            return null;
        }

        // possibly infinite recursion
        if (implGoalChain.RecursionDepth > MaxRecursion)
        {
            return null;
        }
        
        // test if the goal has already been solved
        if (clause is ImplClause)
        {
            var existing = this.TryReuseExistingProof(implGoalChain, implGoals);
            if (existing != null)
            {
                return existing;
            }
        }
        
        var eqGoals = new List<EqGoal>(substitutions.LateEqGoals);
        AddAssocTraitGoals(eqGoals, implGoalChain.Goal);

        // add new instantiation
        var instantiations = this.AddNewInstantiation(substitutions, varMap, clause, implGoalChain, out var inst);

        // add substitutions as eq goals
        AddSubstitutionsAsEqGoals(substitutions, eqGoals);

        // Add requirements of the found instance to the solver
        AddRequirementsFromInstantiation(substitutions, varMap, clause, implGoalChain, inst, implGoals);
        
        // we create normalization goals specifically here so we can chain the proofs so we have a well-defined
        // recursion chain
        CreateNormalizationGoals(normGoals, eqGoals, implGoalChain.Chain, implGoalChain.RecursionDepth);
        
        // add the new goal to the solver so we can reuse it
        var newReuseImplGoals = this._reuseImplGoals.ToDictionary();
        newReuseImplGoals[(implGoalChain.Goal.Target, implGoalChain.Goal.Trait)] = new ReuseImplGoal(
            implGoalChain.Goal.Target,
            implGoalChain.Goal.Trait,
            implGoalChain.Goal.ResolvesTo);

        var newSolver = new Solver(this._iterations)
        {
            _implGoals = implGoals,
            _match = this._match,
            _clauses = this._clauses,
            _eqGoals = eqGoals,
            _instatiations = instantiations,
            _provenImplGoals =  provenGoals,
            _normGoals = normGoals,
            _reuseImplGoals = newReuseImplGoals
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

    private static void AddRequirementsFromInstantiation(TermMatch substitutions, 
                                                         Dictionary<BoundVar, FreeVar> varMap,
                                                         Clause clause,
                                                         RecImplGoalChain implGoalChain, 
                                                         Instatiation inst,
                                                         List<RecImplGoalChain> implGoals)
    {
        if (clause is ImplClause ic)
        {
            var substConstraints = ic.TyParams.ToDictionary(x => x, x => substitutions.Substitutions[varMap[x]]);
            for (int i = 0; i < ic.Constraints.Length; i++)
            {
                var c = ic.Constraints[i];
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
        else if (clause is AssocTyClause atc)
        {
            // TODO: REMOVE THIS ONCE WE DON'T NEED TO PROVE ANYTHING WHEN DEALING WITH IRREDUCIBLE ASSOCIATED TYPES
            // var substConstraints = atc.TyParams.ToDictionary(x => x, x => substitutions.Substitutions[varMap[x]]);
            // var n = inst.Constraints[0];
            // implGoals.Add(new RecImplGoalChain(
            //     new ImplGoal(
            //         implGoalChain.Goal.Target,
            //         atc.Constraint.Substitute(substConstraints),
            //         atc.AssocConstraints.ToDictionary(x => x.Key, x => x.Value.Substitute(substConstraints)),
            //         n),
            //     new ProofChain(implGoalChain.Chain),
            //     implGoalChain.RecursionDepth + 1));
        }
        else
        {
            throw new InvalidOperationException("invalid clause");       
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
                                                                 Clause clause,
                                                                 RecImplGoalChain implGoalChain, 
                                                                 out Instatiation inst)
    {
        var instantiations = this._instatiations.ToDictionary(
            entry => entry.Key,
            entry => entry.Value);

        if (clause is ImplClause ic)
        {
            inst = new Instatiation(
                ic.Name,
                ic.TyParams.Select(p => substitutions.Substitutions[varMap[p]]).ToList(),
                ic.Constraints.Select(_ => $"$g{++_goalSeed}".ToString()).ToList());

            instantiations[implGoalChain.Goal.ResolvesTo] = inst;
        }
        else if (clause is AssocTyClause atc)
        {
            inst = new Instatiation(
                // won't be used
                atc.AliasName,
                atc.TyParams.Select(p => substitutions.Substitutions[varMap[p]]).ToList(),
                [$"$g{++_goalSeed}"]);
        }
        else
        {
            throw new InvalidOperationException("invalid clause");
        }

        return instantiations;
    }

    private Solver? TryReuseExistingProof(RecImplGoalChain implGoalChain, List<RecImplGoalChain> implGoals)
    {
        var key = (implGoalChain.Goal.Target, implGoalChain.Goal.Trait);
        if (this._reuseImplGoals.TryGetValue(key, out var reuse))
        {
            var goalName = reuse.ResolvesTo;
            var newInstantiations = this._instatiations.ToDictionary(entry => entry.Key, entry => entry.Value);
            newInstantiations[implGoalChain.Goal.ResolvesTo] = newInstantiations[goalName];
        
            // reuse proof
            return new Solver(this._iterations)
            {
                _implGoals = implGoals,
                _match = this._match,
                _clauses = this._clauses,
                _eqGoals = this._eqGoals.ToList(),
                _instatiations = newInstantiations,
                _provenImplGoals = this._provenImplGoals.ToDictionary(),
                _normGoals = this._normGoals.ToList(),
                _reuseImplGoals = this._reuseImplGoals.ToDictionary()
            };
        }
        
        return null;
    }

    private static bool TryAddProvenImplGoal(
                                             ProofChain proofChain,
                                             Dictionary<ProofChain, List<ProvenGoal>> provenGoals,
                                             Clause clause,
                                             TermMatch substitutions,
                                             Dictionary<BoundVar, FreeVar> varMap)
    {
        ProofChain? chain = proofChain;
        Dictionary<BoundVar, Term> args;
        if (clause is ImplClause ic)
            args = ic.TyParams.Select((p, i) => (I: i, S: substitutions.Substitutions[varMap[p]]))
                     .ToDictionary(x => ic.TyParams[x.I], x => x.S);
        else if (clause is AssocTyClause atc)
            args = atc.TyParams.Select((p, i) => (I: i, S: substitutions.Substitutions[varMap[p]]))
                     .ToDictionary(x => atc.TyParams[x.I], x => x.S);
        else
        {
            throw new InvalidOperationException("invalid clause");
        }
        
        while (chain != null)
        {
            if (provenGoals.TryGetValue(chain, out var list))
            {
                foreach (var pg in list)
                {
                    if (clause == pg.Clause && IsInfiniteRecursion(pg.Args, args))
                    {
                        return false;
                    }
                }
            }
            chain = chain.Parent;
        }

        if (!provenGoals.ContainsKey(proofChain))
        {
            provenGoals[proofChain] = [];
        }
        
        provenGoals[proofChain].Add(
            new ProvenGoal(
                clause,
                args));
        return true;
    }

    private static bool IsInfiniteRecursion(IReadOnlyDictionary<BoundVar, Term> onStackArgs, IReadOnlyDictionary<BoundVar, Term> newArgs)
    {
        return newArgs.All(x => x.Value.Contains(onStackArgs[x.Key]));
    }

    private List<Solver> GetCandidates(RecImplGoalChain implGoal)
    {
        if (implGoal.Goal.Target is IrAlias irAlias)
        {
            List<Solver> candidates = new();
            foreach (var clause in this._clauses.OfType<AssocTyClause>())
            {
                if (clause.AliasName != irAlias.Name)
                {
                    continue;
                }
                
                var newVars = clause.TyParams.Select(x => (BoundVar: x, FreeVar: FreeVar.New()))
                                        .ToDictionary(x => x.BoundVar, x => x.FreeVar);
                var trait = clause.Trait.Replace<BoundVar>(b => newVars[b]);
                var constraint = clause.Constraint.Replace<BoundVar>(b => newVars[b]);
                
                var subs = Term.TryMatch(
                    new App("S", [trait, constraint]),
                    new App("S", [irAlias.Trait, implGoal.Goal.Trait]));
                
                if (subs == null)
                {
                    continue;
                }

                if (newVars.Keys.All(k => subs.Substitutions.ContainsKey(newVars[k])))
                {
                    var candidate = this.BuildCandidate(subs, newVars, clause, implGoal);
                    if (candidate != null)
                        candidates.Add(candidate);
                }
            }

            return candidates;
        }
        else
        {
            List<Solver> candidates = new();
            foreach (var implClause in this._clauses.OfType<ImplClause>())
            {
                var newVars = implClause.TyParams.Select(x => (BoundVar: x, FreeVar: FreeVar.New()))
                                        .ToDictionary(x => x.BoundVar, x => x.FreeVar);
                var trait = implClause.Trait.Replace<BoundVar>(b => newVars[b]);
                var target = implClause.Target.Replace<BoundVar>(b => newVars[b]);

                var subs = Term.TryMatch(
                    new App("S", [target, trait]),
                    new App("S", [implGoal.Goal.Target, implGoal.Goal.Trait]));
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
}