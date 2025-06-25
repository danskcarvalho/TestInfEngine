namespace InfEngine.Engine;

public abstract record Goal()
{
    public abstract Goal Substitute(TermMatch match);
}

public record EqGoal(Term Left, Term Right) : Goal
{
    public override EqGoal Substitute(TermMatch match) => new EqGoal(Left.Substitute(match), Right.Substitute(match));

    public override string ToString() => $"{Left} = {Right}";
}

public record ImplGoal(Term Target, Term Trait, string ResolvesTo) : Goal
{
    public override ImplGoal Substitute(TermMatch match) => new ImplGoal(Target.Substitute(match), Trait.Substitute(match), ResolvesTo);

    public bool IsNonGeneric()
    {
        if (Target.Any<FreeVar>())
            return false;

        if (Trait.Any<FreeVar>())
            return false;
        
        return true;
    }

    public int CountFreeVars()
    {
        var vars = Target.Descendants<FreeVar>().ToHashSet();
        vars.UnionWith(Trait.Descendants<FreeVar>());
        return vars.Count;
    }

    public override string ToString() => $"{Target}: {Trait} => {ResolvesTo}";
}

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

public class ProofChain(ProofChain? parent = null)
{
    public ProofChain? Parent { get; } = parent;

    public HashSet<ProofChain> GetChainLink()
    {
        HashSet<ProofChain> result = new HashSet<ProofChain>();
        ProofChain? chain = this;
        while (chain != null)
        {
            result.Add(chain);
            chain = chain.Parent;
        }

        return result;
    }
}