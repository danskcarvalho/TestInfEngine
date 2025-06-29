using InfEngine.Engine.Clauses;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Goals;

public readonly record struct ProvenGoal(
    Clause Clause,
    IReadOnlyDictionary<BoundVar, Term> Args)
{
    public ProvenGoal Substitute(TermMatch match) => this with
    {
        Args = this.Args.ToDictionary(x => x.Key, x => x.Value.Substitute(match))
    };
}

public readonly record struct ReuseImplGoal(
    Term Target, 
    Term Trait, 
    string ResolvesTo)
{
    public ReuseImplGoal Substitute(TermMatch match) => this with
    {
        Target = this.Target.Substitute(match), 
        Trait = this.Trait.Substitute(match)
    };
}