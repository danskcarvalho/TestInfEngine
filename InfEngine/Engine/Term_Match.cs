namespace InfEngine.Engine;

public partial record Term
{
    public static TermMatch? TryMatch(Term left, Term right)
    {
        return InternalTryMatch(left, right, false);
    }
    
    public static TermMatch Match(Term left, Term right)
    {
        return InternalTryMatch(left, right, true)!;
    }
    
    private static TermMatch? InternalTryMatch(Term left, Term right, bool throwException)
    {
        Dictionary<FreeVar, Term> result = new Dictionary<FreeVar, Term>();
        
        if (left is FreeVar lv)
        {
            if (right is FreeVar rv2 && rv2.Name == lv.Name)
            {
                return new TermMatch(new Dictionary<FreeVar, Term>(result)
                {
                    [lv] = right
                });
            }
            
            if (right.Any<FreeVar>(v => v == lv))
            {
                if (throwException)
                {
                    throw new MatchException($"{left} = {right}");
                }

                return null;
            }

            foreach (var k in result.Keys.ToList())
            {
                result[k] = result[k].Replace<FreeVar>(v =>
                {
                    if (v == lv)
                    {
                        return right;
                    }

                    return null;
                });
            }

            result[lv] = right;
        }
        else if (right is FreeVar rv)
        {
            if (left is FreeVar rl2 && rl2.Name == rv.Name)
            {
                return new TermMatch(new Dictionary<FreeVar, Term>(result)
                {
                    [rv] = left
                });
            }
            
            if (left.Any<FreeVar>(v => v == rv))
            {
                if (throwException)
                {
                    throw new MatchException($"{left} = {right}");
                }

                return null;
            }

            foreach (var k in result.Keys.ToList())
            {
                result[k] = result[k].Replace<FreeVar>(v =>
                {
                    if (v == rv)
                    {
                        return left;
                    }

                    return null;
                });
            }

            result[rv] = left;
        }
        else if(left is App al && right is App ar)
        {
            if (al.Head != ar.Head || al.Args.Length != ar.Args.Length)
            {
                if (throwException)
                {
                    throw new MatchException($"{left} = {right}");
                }

                return null;
            }

            var args = al.Args.Zip(ar.Args).ToList();
            args.Reverse();
            
            while (args.Count != 0)
            {
                var last = args[^1];
                args.RemoveAt(args.Count - 1);
                var unified = InternalTryMatch(last.First, last.Second, throwException);

                if (unified == null)
                {
                    return null;
                }

                for (int i = 0; i < args.Count; i++)
                {
                    args[i] = (args[i].First.Substitute(unified), args[i].Second.Substitute(unified));
                }
                
                Merge(result, unified);
            }
        }
        else if(left is BoundVar bl && right is BoundVar br)
        {
            if (bl.Index != br.Index)
            {
                if (throwException)
                {
                    throw new MatchException($"{left} = {right}");
                }

                return null;
            }
        }
        else
        {
            if (throwException)
            {
                throw new MatchException($"{left} = {right}");
            }

            return null;
        }

        return new TermMatch(result);
    }

    private static void Merge(Dictionary<FreeVar, Term> result, TermMatch substitutions)
    {
        foreach (var k in result.Keys.ToList())
        {
            result[k] = result[k].Substitute(substitutions);
        }
        
        foreach (var item in substitutions.Substitutions)
        {
            result[item.Key] = item.Value;
        }
    }
}