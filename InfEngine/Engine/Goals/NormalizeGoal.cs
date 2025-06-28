using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Goals;

public record NormalizeGoal(Alias Alias, FreeVar Var) : Goal
{
    public override Goal Substitute(TermMatch match)
    {
        return new NormalizeGoal((Alias)Alias.Substitute(match), Var);
    }
}