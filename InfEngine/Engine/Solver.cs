using InfEngine.Engine.Clauses;
using InfEngine.Engine.Goals;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine;

public partial class Solver
{
    public const long MaxRecursion = 200;
    public const long MaxIterations = 50000;
    
    private List<EqGoal> _eqGoals = new();
    private List<RecNormGoalChain> _normGoals = new();
    private List<RecImplGoalChain> _implGoals = new();
    private Dictionary<ProofChain, List<ProvenGoal>> _provenImplGoals = new();
    private Dictionary<(Term Target, Term Trait), ReuseImplGoal> _reuseImplGoals = new();
    private Dictionary<Term, Term> _reuseNormGoals = new();
    private List<Clause> _clauses = new();
    private TermMatch _match = TermMatch.Empty;
    private Dictionary<string, Instatiation> _instatiations = new();
    private static long _goalSeed;
    private readonly IterationCount _iterations;

    public SolverResult? Run()
    {
        this.LogGoalsAndClauses();


        var solver = this.InternalRun();
        if (solver == null)
        {
            return null;
        }
        
        this.LogResultsAndMatches(solver);

        return new SolverResult(solver._instatiations, solver._match);
    }

    private void LogResultsAndMatches(Solver solver)
    {
        LogTitle("Results:");
        LogTitle("Instantiations:");
        foreach (var instatiation in solver._instatiations)
        {
            Log("{0} = {1}", instatiation.Key, instatiation.Value);
        }
        LogTitle("Matches:");
        foreach (var sub in solver._match.Substitutions)
        {
            Log("{0} => {1}", sub.Key, sub.Value);
        }
    }

    private void LogGoalsAndClauses()
    {
        LogTitle("Starting Solver:");
        LogTitle("Constraints:");
        foreach (var eqGoal in this._eqGoals)
        {
            Log(eqGoal);
        }
        foreach (var normGoal in this._normGoals)
        {
            Log(normGoal);
        }
        foreach (var implGoal in this._implGoals)
        {
            Log(implGoal);
        }

        LogTitle("Clauses:");
        foreach (var clause in this._clauses)
        {
            Log(clause);
        }
    }

    private static void Log(object? obj)
    {
        PrintToConsole(() =>
            Console.WriteLine(obj?.ToString() ?? "<null>"));
    }
    
    private static void Log(string message, params object[] format)
    {
        PrintToConsole(() =>
            Console.WriteLine(message, format));
    }
    
    private static void LogTitle(string message, params object[] format)
    {
        PrintToConsole(() =>
            Console.WriteLine(Bold().Text(string.Format(message, format))));
    }
    
    private static void LogMsg(string title, string message, params object[] format)
    {
        PrintToConsole(() =>
            Console.WriteLine(Bold().Text(title + ": ") + string.Format(message, format)));
    }

    // The correctness of this depends on clauses not overlapping.
    public Solver(List<Goal> goals, List<Clause> clauses)
    {
        this._eqGoals.AddRange(goals.OfType<EqGoal>());
        this._implGoals.AddRange(goals.OfType<ImplGoal>().Select(x => new RecImplGoalChain(x, new ProofChain(), 0)));
        this._normGoals.AddRange(goals.OfType<NormGoal>().Select(x => new RecNormGoalChain(x, new ProofChain(), 0)));
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
            LogTitle("Overflown: {0}", this._iterations.Count);
            return null;
        }
        
        if (!this.HandleEqGoals())
        {
            return null;
        }

        return this.HandleImplAndNormalGoals();
    }

    private Solver? HandleImplAndNormalGoals()
    {
        var bestGoal = this.ElectBestGoal();
        if (bestGoal.Impl != null)
            LogMsg("Best Impl Goal", "{0}", bestGoal.Impl);
        if (bestGoal.Norm != null)
            LogMsg("Best Norm Goal", "{0}", bestGoal.Norm);
        
        if (bestGoal.Impl == null && bestGoal.Norm == null)
        {
            return this;
        }

        if (bestGoal.Impl != null)
        {
            var candidates = this.GetImplCandidates(bestGoal.Impl!.Value);

            foreach (var candidate in candidates)
            {
                var solver = candidate.InternalRun();
                if (solver != null)
                    return solver;
            }
        }
        else
        {
            var candidates = this.GetNormCandidates(bestGoal.Norm!.Value);

            foreach (var candidate in candidates)
            {
                var solver = candidate.InternalRun();
                if (solver != null)
                    return solver;
            }
        }

        return null;
    }

    private (RecImplGoalChain? Impl, RecNormGoalChain? Norm) ElectBestGoal()
    {
        if (this._implGoals.Count == 0 && this._normGoals.Count == 0)
            return (null, null);
        
        var nonGenNormGoal = this._normGoals.FirstOrDefault(g => g.Goal.IsNonGeneric());
        if (nonGenNormGoal.Goal != null)
        {
            return (null, nonGenNormGoal);
        }
        
        var nonGenImplGoals = this._implGoals.FirstOrDefault(g => g.Goal.IsNonGeneric());
        if (nonGenImplGoals.Goal != null)
        {
            return (nonGenImplGoals, null);
        }

        var nonGenericTarget = this._implGoals.Where(x => !x.Goal.Target.Any<FreeVar>())
                                   .Select(x => (ImplGoal: (RecImplGoalChain?)x, CountFreeVars: x.Goal.CountFreeVars()))
                                   .OrderBy(x => x.CountFreeVars)
                                   .FirstOrDefault();

        if (nonGenericTarget.ImplGoal != null)
        {
            return (nonGenericTarget.ImplGoal, null);
        }
        
        var normGoalWithLeastVars = this._normGoals
                                        .Select(x => (NormGoal: (RecNormGoalChain?)x, CountFreeVars: x.Goal.CountFreeVars(), x.RecursionDepth))
                                        .OrderBy(x => x.CountFreeVars)
                                        .FirstOrDefault();
        
        if (normGoalWithLeastVars.NormGoal != null)
        {
            return (null, normGoalWithLeastVars.NormGoal);
        }

        var implGoalWithLeastVars = this._implGoals
                                    .Select(x => (ImplGoal: (RecImplGoalChain?)x, CountFreeVars: x.Goal.CountFreeVars(), x.RecursionDepth))
                                    .OrderBy(x => x.CountFreeVars)
                                    .FirstOrDefault();

        return (implGoalWithLeastVars.ImplGoal, null);
    }

    private bool HandleEqGoals()
    {
        if(this._eqGoals.Count != 0)
        {
            LogTitle("Handling Eq Goals:");
            foreach (var eqGoal in this._eqGoals)
            {
                Log(eqGoal);
            }
        }
        
        if(this._eqGoals.Count == 0)
            return true;

        if (!this.ProcessEquationGoals())
        {
            return false;
        }

        this.ApplySubstitutionsToGoals();

        return true;
    }

    private static void CreateNormalizationGoals(
        List<RecNormGoalChain> goalChains,
        List<EqGoal> eqGoals, ProofChain? proofChain, long recursionDepth)
    {
        HashSet<Alias> aliases = new HashSet<Alias>();
        foreach (var eqGoal in eqGoals)
        {
            aliases.UnionWith(eqGoal.Left.DescendantsAndSelf<Alias>());
            aliases.UnionWith(eqGoal.Right.DescendantsAndSelf<Alias>());
        }
        var toBeNormalized = aliases.ToDictionary(x => x, _ => FreeVar.New());
        var subs = new Dictionary<Alias, FreeVar>();
        foreach (var alias in toBeNormalized)
        {
            var newAlias = (Alias)alias.Key.Replace<Alias>(x =>
            {
                if (x == alias.Key)
                {
                    return null;
                }
                return toBeNormalized.GetValueOrDefault(x);
            });
            subs[newAlias] = alias.Value;
        }

        for (int i = 0; i < eqGoals.Count; i++)
        {
            var n = eqGoals[i];
            var left = n.Left.Replace<Alias>(a => toBeNormalized.GetValueOrDefault(a));
            var right = n.Right.Replace<Alias>(a => toBeNormalized.GetValueOrDefault(a));
            eqGoals[i] = new EqGoal(left, right);
        }

        foreach (var alias in subs.Keys)
        {
            var normGoal = new NormGoal(alias, subs[alias]);
            LogMsg("Add norm goal", "{0}", normGoal);
            goalChains.Add(new RecNormGoalChain(normGoal, new ProofChain(proofChain), recursionDepth + 1));
        }
    }
    
    private static bool IsInfiniteRecursion(
        Clause onStackClause, Clause newClause,
        IReadOnlyDictionary<BoundVar, Term> onStackArgs, IReadOnlyDictionary<BoundVar, Term> newArgs)
    {
        if (onStackClause != newClause)
        {
            return false;
        }
        
        if (onStackArgs.Count == newArgs.Count && onStackArgs.Count == 0)
        {
            return true;
        }
        
        return newArgs.All(x => x.Value.Contains(onStackArgs[x.Key]));
    }

    private static bool TryAddProvenGoal(
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
        else if (clause is AliasImplClause aic)
            args = aic.TyParams.Select((p, i) => (I: i, S: substitutions.Substitutions[varMap[p]]))
                      .ToDictionary(x => aic.TyParams[x.I], x => x.S);
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
                    if (IsInfiniteRecursion(pg.Clause, clause, pg.Args, args))
                    {
                        LogMsg("Infinite recursion", "clause {0}", clause);
                        LogMsg("Already Proven Args", string.Join(", ", pg.Args.Select((_, i) => $"{{{i}}}")), 
                            pg.Args.Select(x => $"{x.Key} = {x.Value}").ToArray<object>());
                        LogMsg("To Be Proven Args", string.Join(", ", args.Select((_, i) => $"{{{i}}}")), 
                            args.Select(x => $"{x.Key} = {x.Value}").ToArray<object>());
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
}