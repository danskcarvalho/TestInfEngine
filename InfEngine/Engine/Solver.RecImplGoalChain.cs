using InfEngine.Engine.Goals;

namespace InfEngine.Engine;

public partial class Solver
{
    public record struct RecImplGoalChain(ImplGoal Goal, ProofChain Chain, long RecursionDepth);
}