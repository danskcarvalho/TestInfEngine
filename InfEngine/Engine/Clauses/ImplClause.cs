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
    public override string ToString()
    {
        if (Constraints.Length != 0)
            return
                $"{PrintKeyword("impl")} {PrintType(Name)}<{string.Join(", ", TyParams)}> {Target}: {Trait.ToString(AssocConstraints)} {PrintKeyword("where")} {string.Join(", ", Constraints)}";

        return
            $"{PrintKeyword("impl")} {PrintType(Name)}<{string.Join(", ", TyParams)}> {Target}: {Trait.ToString(AssocConstraints)}";
    }

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    public virtual bool Equals(ImplClause? other) => ReferenceEquals(this, other);
}