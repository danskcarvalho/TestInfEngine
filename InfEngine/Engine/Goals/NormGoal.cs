using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Goals;

public record NormGoal(Alias Alias, FreeVar Var) : Goal
{
    public override NormGoal Substitute(TermMatch match)
    {
        return new NormGoal((Alias)Alias.Substitute(match), Var);
    }

    public bool IsNonGeneric()
    {
        if (Alias.Any<FreeVar>())
            return false;

        return true;
    }

    public int CountFreeVars()
    {
        var vars = Alias.Descendants<FreeVar>().ToHashSet();
        return vars.Count;
    }
}