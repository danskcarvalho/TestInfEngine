using System.Collections.ObjectModel;
using InfEngine.Engine.Clauses;
using InfEngine.Engine.Goals;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine;

public partial class Solver
{
    private Solver? BuildNormCandidate(
        Term aliased,
        TermMatch substitutions,
        Dictionary<BoundVar, FreeVar> varMap,
        Clause clause,
        RecNormGoalChain normGoalChain)
    {
        var normGoals = this._normGoals.ToList();
        normGoals.Remove(normGoalChain);
        
        var implGoals = this._implGoals.ToList();
        var provenGoals = this._provenImplGoals.ToDictionary();
        
        // infinite recursion
        if (!TryAddProvenImplGoal(normGoalChain.Chain, provenGoals, clause, substitutions, varMap))
        {
            return null;
        }

        // possibly infinite recursion
        if (normGoalChain.RecursionDepth > MaxRecursion)
        {
            return null;
        }
        
        var existing = this.TryReuseExistingNorm(normGoalChain, normGoals);
        if (existing != null)
        {
            return existing;
        }
        
        var eqGoals = new List<EqGoal>(substitutions.LateEqGoals);
        
        // add substitutions as eq goals
        AddSubstitutionsAsEqGoals(substitutions, eqGoals);

        // Add requirements of the found instance to the solver
        AddRequirementsFromNorm(normGoalChain, substitutions, varMap, clause, implGoals);
        
        // Normalize
        eqGoals.Add(new EqGoal(normGoalChain.Goal.Var, aliased));
        
        // we create normalization goals specifically here so we can chain the proofs so we have a well-defined
        // recursion chain
        CreateNormalizationGoals(normGoals, eqGoals, normGoalChain.Chain, normGoalChain.RecursionDepth);
        
        // add the new goal to the solver so we can reuse it
        var newReuseNormGoals = this._reuseNormGoals.ToDictionary();
        newReuseNormGoals[normGoalChain.Goal.Alias] = aliased;

        var newSolver = new Solver(this._iterations)
        {
            _implGoals = implGoals,
            _match = this._match,
            _clauses = this._clauses,
            _eqGoals = eqGoals,
            _instatiations = this._instatiations.ToDictionary(),
            _provenImplGoals =  provenGoals,
            _normGoals = normGoals,
            _reuseImplGoals = this._reuseImplGoals.ToDictionary(),
            _reuseNormGoals = newReuseNormGoals
        };
        return newSolver;
    }
    
    private Solver? TryReuseExistingNorm(RecNormGoalChain normGoalChain, List<RecNormGoalChain> normGoals)
    {
        if (this._reuseNormGoals.TryGetValue(normGoalChain.Goal.Alias, out var reuse))
        {
            var eqGoals = this._eqGoals.ToList();
            eqGoals.Add(new EqGoal(normGoalChain.Goal.Var, reuse));
        
            // reuse proof
            return new Solver(this._iterations)
            {
                _implGoals = this._implGoals,
                _match = this._match,
                _clauses = this._clauses,
                _eqGoals = eqGoals,
                _instatiations = this._instatiations.ToDictionary(),
                _provenImplGoals = this._provenImplGoals.ToDictionary(),
                _normGoals = normGoals,
                _reuseImplGoals = this._reuseImplGoals.ToDictionary(),
                _reuseNormGoals = this._reuseNormGoals.ToDictionary()
            };
        }
        
        return null;
    }

    private void AddRequirementsFromNorm(RecNormGoalChain normGoalChain, 
                                         TermMatch substitutions, 
                                         Dictionary<BoundVar, FreeVar> varMap, 
                                         Clause clause,
                                         List<RecImplGoalChain> implGoals)
    {
        if (clause is AliasImplClause aic)
        {
            var substConstraints = aic.TyParams.ToDictionary(x => x, x => substitutions.Substitutions[varMap[x]]);
            var constraint = aic.Constraint.Substitute(substConstraints);
            implGoals.Add(new RecImplGoalChain(
                new ImplGoal(constraint.Target, constraint.Trait, constraint.AssocConstraints, $"$g{++_goalSeed}"),
                normGoalChain.Chain,
                normGoalChain.RecursionDepth + 1));
        }
        else if (clause is ImplClause ic)
        {
            var substConstraints = ic.TyParams.ToDictionary(x => x, x => substitutions.Substitutions[varMap[x]]);
            var constraint = new ImplGoal(
                ic.Target.Substitute(substConstraints), 
                ic.Trait.Substitute(substConstraints), 
                ReadOnlyDictionary<string, Term>.Empty, 
                $"$g{++_goalSeed}");
            implGoals.Add(new RecImplGoalChain(
                constraint,
                normGoalChain.Chain,
                normGoalChain.RecursionDepth + 1));
        }
    }
    
    private List<Solver> GetNormCandidates(RecNormGoalChain normGoalChain)
    {
        List<Solver> candidates = new();
        foreach (var aliasClause in this._clauses.OfType<AliasImplClause>())
        {
            if (aliasClause.AliasName != normGoalChain.Goal.Alias.Name)
                continue;
            var newVars = aliasClause.TyParams.Select(x => (BoundVar: x, FreeVar: FreeVar.New()))
                                    .ToDictionary(x => x.BoundVar, x => x.FreeVar);
            var trait = aliasClause.Trait.Replace<BoundVar>(b => newVars[b]);
            var target = aliasClause.Target.Replace<BoundVar>(b => newVars[b]);

            var subs = Term.TryMatch(
                new App("S", [target, trait]),
                new App("S", [normGoalChain.Goal.Alias.Target, normGoalChain.Goal.Alias.Trait]));
            if (subs == null)
            {
                continue;
            }

            if (newVars.Keys.All(k => subs.Substitutions.ContainsKey(newVars[k])))
            {
                var subsAlias = aliasClause.TyParams.ToDictionary(x => x, x => subs.Substitutions[newVars[x]]);
                var candidate = this.BuildNormCandidate(aliasClause.Aliased.Substitute(subsAlias), subs, newVars, aliasClause, normGoalChain);
                if (candidate != null)
                    candidates.Add(candidate);
            }
        }
        
        foreach (var implClause in this._clauses.OfType<ImplClause>())
        {
            if (!implClause.AssocConstraints.ContainsKey(normGoalChain.Goal.Alias.Name))
                continue;
            var newVars = implClause.TyParams.Select(x => (BoundVar: x, FreeVar: FreeVar.New()))
                                     .ToDictionary(x => x.BoundVar, x => x.FreeVar);
            var trait = implClause.Trait.Replace<BoundVar>(b => newVars[b]);
            var target = implClause.Target.Replace<BoundVar>(b => newVars[b]);

            var subs = Term.TryMatch(
                new App("S", [target, trait]),
                new App("S", [normGoalChain.Goal.Alias.Target, normGoalChain.Goal.Alias.Trait]));
            if (subs == null)
            {
                continue;
            }

            if (newVars.Keys.All(k => subs.Substitutions.ContainsKey(newVars[k])))
            {
                var aliased = implClause.AssocConstraints[normGoalChain.Goal.Alias.Name];
                var subsAlias = implClause.TyParams.ToDictionary(x => x, x => subs.Substitutions[newVars[x]]);
                var candidate = this.BuildNormCandidate(aliased.Substitute(subsAlias), subs, newVars, implClause, normGoalChain);
                if (candidate != null)
                    candidates.Add(candidate);
            }
        }

        if (normGoalChain.Goal.Alias.Target is IrAlias irAlias)
        {
            foreach (var assocTyClause in this._clauses.OfType<AssocTyClause>())
            {
                if (!assocTyClause.AssocConstraints.ContainsKey(normGoalChain.Goal.Alias.Name))
                    continue;
                if (assocTyClause.AliasName != irAlias.Name)
                    continue;
                
                var newVars = assocTyClause.TyParams.Select(x => (BoundVar: x, FreeVar: FreeVar.New()))
                                        .ToDictionary(x => x.BoundVar, x => x.FreeVar);
                var trait = assocTyClause.Trait.Replace<BoundVar>(b => newVars[b]);
                var constraint = assocTyClause.Constraint.Replace<BoundVar>(b => newVars[b]);
                
                var subs = Term.TryMatch(
                    new App("S", [trait, constraint]),
                    new App("S", [irAlias.Trait, normGoalChain.Goal.Alias.Trait]));
                
                if (subs == null)
                {
                    continue;
                }

                if (newVars.Keys.All(k => subs.Substitutions.ContainsKey(newVars[k])))
                {
                    var aliased = assocTyClause.AssocConstraints[normGoalChain.Goal.Alias.Name];
                    var subsAlias = assocTyClause.TyParams.ToDictionary(x => x, x => subs.Substitutions[newVars[x]]);
                    var candidate = this.BuildNormCandidate(aliased.Substitute(subsAlias), subs, newVars, assocTyClause, normGoalChain);
                    if (candidate != null)
                        candidates.Add(candidate);
                }
            }
        }

        {
            var eqGoals = this._eqGoals.ToList();
            eqGoals.Add(new EqGoal(normGoalChain.Goal.Var, 
                new IrAlias(
                    normGoalChain.Goal.Alias.Target, 
                    normGoalChain.Goal.Alias.Trait,
                    normGoalChain.Goal.Alias.Name)));

            var implGoals = this._implGoals.ToList();
            implGoals.Add(new RecImplGoalChain(
                    new ImplGoal(
                        normGoalChain.Goal.Alias.Target,
                        normGoalChain.Goal.Alias.Trait,
                        ReadOnlyDictionary<string, Term>.Empty,
                        $"$g{++_goalSeed}"),
                    new ProofChain(normGoalChain.Chain),
                    normGoalChain.RecursionDepth + 1
                ));
            
            candidates.Add(new Solver(this._iterations)
            {
                _implGoals = implGoals,
                _match = this._match,
                _clauses = this._clauses,
                _eqGoals = eqGoals,
                _instatiations = this._instatiations.ToDictionary(),
                _provenImplGoals = this._provenImplGoals.ToDictionary(),
                _normGoals = this._normGoals.ToList(),
                _reuseImplGoals = this._reuseImplGoals.ToDictionary(),
                _reuseNormGoals = this._reuseNormGoals.ToDictionary()
            });
        }
        
        return candidates;
    }
}