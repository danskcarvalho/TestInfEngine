using InfEngine.Engine.Terms;

namespace InfEngine.Engine;

public record Instatiation(string ImplName, IReadOnlyList<Term> Vars, IReadOnlyList<string> Constraints)
{
    public override string ToString()
    {
        if (Constraints.Count != 0)
            return $"{PrintType(ImplName)}<{string.Join(", ", Vars)}> {PrintKeyword("where")} {string.Join(", ", Constraints)}";
        
        return $"{PrintType(ImplName)}<{string.Join(", ", this.Vars)}>";
    }

    public Instatiation Substitute(TermMatch match)
    {
        return new Instatiation(ImplName, Vars.Select(v => v.Substitute(match)).ToList(), Constraints);
    }
}