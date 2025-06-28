using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Goals;

public record EqGoal(Term Left, Term Right) : Goal
{
    public override EqGoal Substitute(TermMatch match) => new EqGoal(Left.Substitute(match), Right.Substitute(match));

    public override string ToString() => $"{Left} = {Right}";
}