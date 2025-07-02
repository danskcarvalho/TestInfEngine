using InfEngine.Engine.Clauses;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Goals;

public readonly record struct ProvenGoal(
    Clause Clause,
    IReadOnlyDictionary<BoundVar, Term> Args,
    bool IsNormalizing)
{
    public ProvenGoal Substitute(TermMatch match) => this with
    {
        Args = this.Args.ToDictionary(x => x.Key, x => x.Value.Substitute(match))
    };
}