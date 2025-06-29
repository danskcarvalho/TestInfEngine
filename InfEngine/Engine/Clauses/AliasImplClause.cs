using System.Collections.Immutable;
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
    
}