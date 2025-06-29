using System.Collections.Immutable;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Clauses;

public record ImplClause(
    string Name,
    ImmutableArray<BoundVar> TyParams,
    Term Target,
    Term Trait,
    ImmutableArray<ImplConstraint> Constraints) : Clause
{
    public override string ToString() => $"impl {Name}<{string.Join(", ", TyParams)}> {Target}: {Trait} where {string.Join(", ", Constraints)}";
}