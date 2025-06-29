using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Clauses;

public record ImplConstraint(Term Target, Term Trait, IReadOnlyDictionary<string, Term> AssocConstraints)
{
    public override string ToString() => $"{Target}: {Trait}";
}