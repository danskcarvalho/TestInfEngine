namespace InfEngine.Engine.Terms;

public record ConstBoundVar(long Index) : Term
{
    public override bool Any<T>(Func<T, bool> pred)
    {
        if (this is not T)
        {
            return false;
        }

        return pred((this as T)!);
    }

    public override Term Replace<T>(Func<T, Term?> replacement)
    {
        if (this is not T)
        {
            return this;
        }
        
        var r = replacement((this as T)!);
        if (r == null)
            return this;
        
        return r;
    }

    public override IEnumerable<T> Descendants<T>()
    {
        yield break;
    }

    public override string ToString() => PrintVar($"${Index}");
}