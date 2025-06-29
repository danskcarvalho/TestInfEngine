using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Clauses;

public record AliasImplClause(
    ImmutableArray<BoundVar> TyParams,
    Term Target,
    Term Trait,
    string AliasName,
    Term Aliased,
    ImplConstraint Constraint) : Clause
{
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    public virtual bool Equals(AliasImplClause? other) => ReferenceEquals(this, other);
}