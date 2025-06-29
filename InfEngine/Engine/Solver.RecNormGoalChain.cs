using InfEngine.Engine.Goals;

namespace InfEngine.Engine;

public partial class Solver
{
    public record struct RecNormGoalChain(NormalizeGoal Goal, ProofChain Chain, long RecursionDepth);
}