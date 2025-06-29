using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Clauses;

public record ImplConstraint(Term Target, Term Trait, IReadOnlyDictionary<string, Term> AssocConstraints)
{
    public override string ToString() => $"{Target}: {Trait}";

    public ImplConstraint Substitute(Dictionary<BoundVar, Term> substConstraints)
    {
        return new ImplConstraint(
            Target.Substitute(substConstraints),
            Trait.Substitute(substConstraints),
            AssocConstraints.ToDictionary(x => x.Key, x => x.Value.Substitute(substConstraints)));
    }
}