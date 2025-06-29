using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Goals;

public record ImplGoal(
    Term Target, 
    Term Trait, 
    IReadOnlyDictionary<string, Term> AssocConstraints,
    string ResolvesTo) : Goal
{
    public override ImplGoal Substitute(TermMatch match) => new ImplGoal(
        Target.Substitute(match), 
        Trait.Substitute(match), 
        AssocConstraints.ToDictionary(x => x.Key, x => x.Value.Substitute(match)),
        ResolvesTo);

    public bool IsNonGeneric()
    {
        if (Target.Any<FreeVar>())
            return false;

        if (Trait.Any<FreeVar>())
            return false;

        foreach (var term in AssocConstraints.Values)
        {
            if (term.Any<FreeVar>())
                return false;
        }
        
        return true;
    }

    public int CountFreeVars()
    {
        var vars = Target.Descendants<FreeVar>().ToHashSet();
        vars.UnionWith(Trait.Descendants<FreeVar>());
        foreach (var term in AssocConstraints.Values)
        {
            if (term.Any<FreeVar>())
                vars.UnionWith(term.Descendants<FreeVar>());
        }
        return vars.Count;
    }

    public override string ToString() => $"{Target}: {Trait} => {ResolvesTo}";
}