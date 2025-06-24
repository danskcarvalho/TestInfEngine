namespace InfEngine.Engine;

public record Instatiation(string ImplName, IReadOnlyList<Term> Vars, IReadOnlyList<string> Constraints)
{
    public override string ToString() => $"{ImplName}<{string.Join(", ", Vars)}> => {string.Join(", ", Constraints)}";

    public Instatiation Substitute(TermMatch match)
    {
        return new Instatiation(ImplName, Vars.Select(v => v.Substitute(match)).ToList(), Constraints);
    }
}