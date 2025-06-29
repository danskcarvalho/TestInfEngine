using InfEngine.Engine.Goals;

namespace InfEngine.Engine;

public partial class Solver
{
    public record struct RecNormGoalChain(NormGoal Goal, ProofChain Chain, long RecursionDepth);
}