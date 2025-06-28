namespace InfEngine.Engine.Terms;

public record FreeVar(string Name) : Term
{
    private static long _seed = 0;
    
    public static FreeVar New() => new($"?{++_seed}");
    
    public override string ToString() => $"?{Name}";

    public override bool Any<T>(Func<T, bool> pred)
    {
        if (this is T)
        {
            return pred((this as T)!);
        }
        
        return false;
    }

    public override Term Replace<T>(Func<T, Term?> replacement)
    {
        if (this is T)
        {
            return replacement((this as T)!) ?? this;
        }

        return this;
    }

    public override IEnumerable<T> Descendants<T>()
    {
        yield break;
    }
}