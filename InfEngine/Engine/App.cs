using System.Collections.Immutable;

namespace InfEngine.Engine;

public record App(string Head, ImmutableArray<Term> Args) : Term
{
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Head);
        foreach (var arg in Args) hash.Add(arg);
        return hash.ToHashCode();
    }

    public virtual bool Equals(App? other) => other != null && other.Head.Equals(Head) && other.Args.SequenceEqual(Args);

    public override string ToString() => Args.Length != 0 ? 
        $"{Head}<{string.Join(", ", Args)}>" :
        Head;

    public override bool Any<T>(Func<T, bool> pred)
    {
        if (this is T)
        {
            if (pred((this as T)!))
                return true;
        }

        foreach (var arg in Args)
        {
            if (arg.Any(pred))
            {
                return true;
            }
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
        return new App(this.Head, [..this.Args.Select(a => a.Replace(replacement))]);
    }

    public override IEnumerable<T> Descendants<T>()
    {
        foreach (var arg in Args)
        {
            if (arg is T term)
            {
                yield return term;
            }
        }
    }
}