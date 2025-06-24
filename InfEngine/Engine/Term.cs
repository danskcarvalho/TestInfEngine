namespace InfEngine.Engine;

public abstract partial record Term
{
    public bool Any<T>() where T : Term => this.Any<T>(p => true);
    public abstract bool Any<T>(Func<T, bool> pred) where T : Term;
    public abstract Term Replace<T>(Func<T, Term?> replacement) where T : Term;
    public abstract IEnumerable<T> Descendants<T>() where T : Term;

    public IEnumerable<T> DescendantsAndSelf<T>() where T : Term
    {
        if (this is T)
        {
            yield return (this as T)!;
        }

        foreach (var v in this.Descendants<T>())
        {
            yield return v;
        }
    }

    public Term Substitute(IReadOnlyDictionary<FreeVar, Term> substitutions)
    {
        return this.Replace<FreeVar>(substitutions.GetValueOrDefault);
    }
    
    public Term Substitute(IReadOnlyDictionary<BoundVar, Term> substitutions)
    {
        return this.Replace<BoundVar>(substitutions.GetValueOrDefault);
    }

    public Term Substitute(TermMatch match)
    {
        return this.Substitute(match.Substitutions);
    }
}