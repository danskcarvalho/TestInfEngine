using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Goals;

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