using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Clauses;

// this clause asserts that an associated type implements a certain trait
// it's useful when trying to prove that a non-reducible alias implements a trait
// irreducible aliases happens when you have T::AssocType and T is a bound var (a type parameter)
// this clause is generated for every trait declaration
// ex.:
// trait Iterable:
//      type It: Iterator<Item=int>
// Then Trait = Iterable, AliasName = It, Constraint = Iterator, AssocConstraints = {Item: int}
public record AssocTyClause(
    ImmutableArray<BoundVar> TyParams,
    Term Trait,
    string AliasName,
    Term Constraint,
    // Ex.: Iterator<Item = str>, Item = is an assoc type constraint
    IReadOnlyDictionary<string, Term> AssocConstraints) : Clause
{
    public override string ToString() => $"?::<{Trait}>::{AliasName}: {Constraint.ToString(AssocConstraints)}";

    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

    public virtual bool Equals(AssocTyClause? other) => ReferenceEquals(this, other);
}