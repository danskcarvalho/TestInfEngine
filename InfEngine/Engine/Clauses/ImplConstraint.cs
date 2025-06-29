using System.Runtime.CompilerServices;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Clauses;

public record ImplConstraint(Term Target, Term Trait, IReadOnlyDictionary<string, Term> AssocConstraints)
{
    public override string ToString() => $"{Target}: {Trait.ToString(AssocConstraints)}";

    public ImplConstraint Substitute(Dictionary<BoundVar, Term> substConstraints)
    {
        return new ImplConstraint(
            Target.Substitute(substConstraints),
            Trait.Substitute(substConstraints),
            AssocConstraints.ToDictionary(x => x.Key, x => x.Value.Substitute(substConstraints)));
    }
    
    
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    public virtual bool Equals(ImplConstraint? other) => ReferenceEquals(this, other);
}