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
        var solver = this.InternalRun();
        if (solver == null)
        {
            return null;
        }

        return new SolverResult(solver._instatiations, solver._match);
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
        var toBeNormalized = eqGoals.Select((e, i) => (e, i)).Where(e => e.e.Left.Any<Alias>() || e.e.Right.Any<Alias>()).Select(x => x.i).ToList();

        Dictionary<Alias, FreeVar> subs = new();
        foreach (var index in toBeNormalized)
        {
            var n = eqGoals[index];
            var left = n.Left.Replace<Alias>(a =>
            {
                if (!subs.TryGetValue(a, out var existing))
                {
                    var newVar = FreeVar.New();
                    subs[a] = newVar;
                    return newVar;
                }

                return existing;
            });
            var right = n.Right.Replace<Alias>(a =>
            {
                if (!subs.TryGetValue(a, out var existing))
                {
                    var newVar = FreeVar.New();
                    subs[a] = newVar;
                    return newVar;
                }

                return existing;
            });
            eqGoals[index] = new EqGoal(left, right);
        }

        foreach (var alias in subs.Keys)
        {
            var normGoal = new NormGoal(alias, subs[alias]);
            goalChains.Add(new RecNormGoalChain(normGoal, new ProofChain(proofChain), recursionDepth + 1));
        }
    }
}