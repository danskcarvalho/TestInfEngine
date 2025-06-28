using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Goals;

public abstract record Goal()
{
    public abstract Goal Substitute(TermMatch match);
}