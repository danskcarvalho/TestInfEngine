using InfEngine.Engine.Goals;

namespace InfEngine.Engine;

public partial class Solver
{
    public readonly record struct RecImplGoalChain(ImplGoal Goal, ProofChain Chain, long RecursionDepth)
    {
        public override string ToString() => Goal.ToString();
    }
}