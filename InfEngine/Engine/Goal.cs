namespace InfEngine.Engine;

public abstract record Goal()
{
    public abstract Goal Substitute(TermMatch match);
}

public record EqGoal(Term Left, Term Right) : Goal
{
    public override EqGoal Substitute(TermMatch match) => new EqGoal(Left.Substitute(match), Right.Substitute(match));

    public override string ToString() => $"{Left} = {Right}";
}

public record ImplGoal(Term Target, Term Trait, string ResolvesTo) : Goal
{
    public override ImplGoal Substitute(TermMatch match) => new ImplGoal(Target.Substitute(match), Trait.Substitute(match), ResolvesTo);

    public bool IsNonGeneric()
    {
        if (Target.Any<FreeVar>())
            return false;

        if (Trait.Any<FreeVar>())
            return false;
        
        return true;
    }

    public int CountFreeVars()
    {
        var vars = Target.Descendants<FreeVar>().ToHashSet();
        vars.UnionWith(Trait.Descendants<FreeVar>());
        return vars.Count;
    }

    public override string ToString() => $"{Target}: {Trait} => {ResolvesTo}";
}