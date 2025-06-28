using InfEngine.Engine.Clauses;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Goals;

public readonly record struct ProvenImplGoal(
    ImplClause Impl,
    Term Target, 
    Term Trait, 
    ProofChain Chain,
    IReadOnlyDictionary<BoundVar, Term> Args,
    string ResolvesTo)
{
    public ProvenImplGoal Substitute(TermMatch match) => new ProvenImplGoal(
        Impl,
        Target.Substitute(match), 
        Trait.Substitute(match), 
        this.Chain,
        this.Args.ToDictionary(x => x.Key, x => x.Value.Substitute(match)),
        ResolvesTo);

    public override string ToString() => $"{Target}: {Trait}";
}