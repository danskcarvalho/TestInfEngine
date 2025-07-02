namespace InfEngine.Engine.Terms;

/// <summary>
/// Irreducible Alias
/// </summary>
public record IrAlias(Term Target, Term Trait, string Name) : Term
{
    public override bool Any<T>(Func<T, bool> pred)
    {
        if (this is T)
        {
            if (pred((this as T)!))
                return true;
        }

        if (Target.Any(pred))
        {
            return true;
        }

        if (Trait.Any(pred))
        {
            return true;
        }

        return false;
    }

    public override Term Replace<T>(Func<T, Term?> replacement)
    {
        if (this is T)
        {
            var r =  replacement((this as T)!);
            if (r != null)
                return r;
        }
        
        return new IrAlias(Target.Replace(replacement), Trait.Replace(replacement), Name);
    }

    public override IEnumerable<T> Descendants<T>()
    {
        if (Target is T term1)
        {
            yield return term1;
        }

        foreach (var descendant in Target.Descendants<T>())
        {
            yield return descendant;
        }
        
        if (Trait is T term2)
        {
            yield return term2;
        }

        foreach (var descendant in Trait.Descendants<T>())
        {
            yield return descendant;
        }
    }

    public override string ToString() => $"[{Target}::<{Trait}>::{Name}]";
}