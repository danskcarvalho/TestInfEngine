using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Goals;

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