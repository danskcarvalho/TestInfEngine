namespace InfEngine.Engine.Goals;

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