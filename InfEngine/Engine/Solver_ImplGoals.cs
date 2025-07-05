using InfEngine.Engine.Clauses;
using InfEngine.Engine.Goals;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine;

public partial class Solver
{
    private Solver? BuildImplCandidate(TermMatch substitutions,
                                   Dictionary<BoundVar, FreeVar> varMap,
                                   Clause clause,
                                   RecImplGoalChain implGoalChain)
    {
        var implGoals = this._implGoals.ToList();
        implGoals.Remove(implGoalChain);
        
        var normGoals = this._normGoals.ToList();
        var provenGoals = this._provenImplGoals.ToDictionary();
        
        // infinite recursion
        if (!TryAddProvenGoal(implGoalChain.Chain, provenGoals, clause, substitutions, varMap, isNormalizing: false))
        {
            return new Solver(this._iterations) { _infRec = true };
        }

        // possibly infinite recursion
        if (implGoalChain.RecursionDepth > MaxRecursion)
        {
            LogMsg("Max Recursion", "{0} > {1}", implGoalChain.RecursionDepth, MaxRecursion);
            return new Solver(this._iterations) { _infRec = true };
        }
        
        // test if the goal has already been solved
        if (clause is ImplClause)
        {
            var existing = this.TryReuseExistingProof(implGoalChain, implGoals);
            if (existing != null)
            {
                LogMsg("Reused Impl Goal", "{0} : {1}", implGoalChain.Goal.Target, implGoalChain.Goal.Trait);
                return existing;
            }
        }
        
        var eqGoals = new List<EqGoal>(substitutions.LateEqGoals);
        if (substitutions.LateEqGoals.Count > 0)
        {
            foreach (var eq in substitutions.LateEqGoals)
            {
                LogMsg("Late Goal added", "{0}", eq);
            }
        }

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
            _reuseImplGoals = newReuseImplGoals,
            _reuseNormGoals = this._reuseNormGoals.ToDictionary()
        };
        return newSolver;
    }

    private void AddAssocTraitGoals(List<EqGoal> eqGoals, ImplGoal goal)
    {
        foreach (var goalAssocConstraint in goal.AssocConstraints)
        {
            var left = new Alias(goal.Target, goal.Trait, goalAssocConstraint.Key);
            var right = goalAssocConstraint.Value;
            LogMsg("Add assoc trait", "{0}", new EqGoal(left, right));
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
                var goal = new ImplGoal(
                    c.Target.Substitute(substConstraints),
                    c.Trait.Substitute(substConstraints),
                    c.AssocConstraints.ToDictionary(x => x.Key, x => x.Value.Substitute(substConstraints)),
                    n);
                LogMsg("Requirement added", "{0}", goal);
                implGoals.Add(new RecImplGoalChain(
                    goal,
                    new ProofChain(implGoalChain.Chain),
                    implGoalChain.RecursionDepth + 1));
            }
            
            return;
        }

        if (clause is not AssocTyClause)
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
                ic.Constraints.Select(_ => $"$g{++_goalSeed}").ToList());

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
                _reuseImplGoals = this._reuseImplGoals.ToDictionary(),
                _reuseNormGoals = this._reuseNormGoals.ToDictionary()
            };
        }
        
        return null;
    }

    private IEnumerable<Solver> GetImplCandidates(RecImplGoalChain implGoal)
    {
        if (implGoal.Goal.Target is IrAlias irAlias)
        {
            foreach (var clause in this._clauses.OfType<AssocTyClause>())
            {
                LogMsg("Trying impl candidate", "{0}", clause);
                if (clause.AliasName != irAlias.Name)
                {
                    continue;
                }
                
                var newVars = clause.TyParams
                                    .Select(x => (BoundVar: x, FreeVar: FreeVar.New()))
                                    .ToDictionary(x => x.BoundVar, x => x.FreeVar);
                newVars[clause.SelfParam] = FreeVar.New();
                var trait = clause.Trait.Replace<BoundVar>(b => newVars[b]);
                var constraint = clause.Constraint.Replace<BoundVar>(b => newVars[b]);
                
                LogMsg("Matching Self", "{0} = {1}", newVars[clause.SelfParam], irAlias.Target);
                LogMsg("Matching", "{0} = {1}", trait, irAlias.Trait);
                LogMsg("Matching", "{0} = {1}", constraint, implGoal.Goal.Trait);
                
                var subs = Term.TryMatch(
                    new App("S", [newVars[clause.SelfParam], trait, constraint]),
                    new App("S", [irAlias.Target, irAlias.Trait, implGoal.Goal.Trait]));
                
                if (subs == null)
                {
                    Log("matching failed");
                    continue;
                }

                if (newVars.Keys.All(k => subs.Substitutions.ContainsKey(newVars[k])))
                {
                    Log("matched clause {0}", clause);
                    var candidate = this.BuildImplCandidate(subs, newVars, clause, implGoal);
                    if (candidate != null)
                        yield return candidate;
                }
            }
        }
        else
        {
            foreach (var implClause in this._clauses.OfType<ImplClause>())
            {
                LogMsg("Trying impl candidate", "{0}", implClause);
                var newVars = implClause.TyParams.Select(x => (BoundVar: x, FreeVar: FreeVar.New()))
                                        .ToDictionary(x => x.BoundVar, x => x.FreeVar);
                var trait = implClause.Trait.Replace<BoundVar>(b => newVars[b]);
                var target = implClause.Target.Replace<BoundVar>(b => newVars[b]);
                
                LogMsg("Matching", "{0} = {1}", target, implGoal.Goal.Target);
                LogMsg("Matching", "{0} = {1}", trait, implGoal.Goal.Trait);

                var subs = Term.TryMatch(
                    new App("S", [target, trait]),
                    new App("S", [implGoal.Goal.Target, implGoal.Goal.Trait]));
                if (subs == null)
                {
                    Log("matching failed");
                    continue;
                }

                if (newVars.Keys.All(k => subs.Substitutions.ContainsKey(newVars[k])))
                {
                    Log("matched clause {0}", implClause);
                    foreach (var k in subs.Substitutions.Keys)
                    {
                        Log("{0} => {1}", k, subs.Substitutions[k]);
                    }
                    var candidate = this.BuildImplCandidate(subs, newVars, implClause, implGoal);
                    if (candidate != null)
                        yield return candidate;
                }
            }
        }
    }
}