using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Clauses;

public record ImplClause(
    string Name,
    ImmutableArray<BoundVar> TyParams,
    Term Target,
    Term Trait,
    IReadOnlyDictionary<string, Term> AssocConstraints,
    ImmutableArray<ImplConstraint> Constraints) : Clause
{
    public override string ToString() => $"impl {Name}<{string.Join(", ", TyParams)}> {Target}: {Trait.ToString(AssocConstraints)} where {string.Join(", ", Constraints)}";

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    public virtual bool Equals(ImplClause? other) => ReferenceEquals(this, other);
}