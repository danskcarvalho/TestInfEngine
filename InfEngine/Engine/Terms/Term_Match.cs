using System.Collections.Immutable;
using InfEngine.Engine.Goals;

namespace InfEngine.Engine.Terms;

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
    
    private static TermMatch? InternalTryMatch(Term l, Term r, bool throwException) 
    {
        Dictionary<FreeVar, Term> result = new Dictionary<FreeVar, Term>();
        List<EqGoal> lateGoals = [];
        List<(Term Left, Term Right)> stack = [(l, r)];

        while (stack.Count != 0)
        {
            var current = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            var left = current.Left;
            var right = current.Right;

            if (left is FreeVar lv)
            {
                if (right is FreeVar rv2 && rv2.Name == lv.Name)
                {
                    continue;
                }

                if (right.Any<FreeVar>(v => v == lv))
                {
                    if (throwException)
                    {
                        throw new MatchException($"{left} = {right}");
                    }

                    return null;
                }

                foreach (var k in result.Keys)
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

                for (int i = 0; i < stack.Count; i++)
                {
                    var newLeft = stack[i].Left.Replace<FreeVar>(v =>
                    {
                        if (v == lv)
                        {
                            return right;
                        }

                        return null;
                    });
                    var newRight = stack[i].Right.Replace<FreeVar>(v =>
                    {
                        if (v == lv)
                        {
                            return right;
                        }

                        return null;
                    });
                    stack[i] = (newLeft, newRight);
                }

                result[lv] = right;
            }
            else if (right is FreeVar rv)
            {
                if (left is FreeVar rl2 && rl2.Name == rv.Name)
                {
                    continue;
                }

                if (left.Any<FreeVar>(v => v == rv))
                {
                    if (throwException)
                    {
                        throw new MatchException($"{left} = {right}");
                    }

                    return null;
                }

                foreach (var k in result.Keys)
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
                
                for (int i = 0; i < stack.Count; i++)
                {
                    var newLeft = stack[i].Left.Replace<FreeVar>(v =>
                    {
                        if (v == rv)
                        {
                            return right;
                        }

                        return null;
                    });
                    var newRight = stack[i].Right.Replace<FreeVar>(v =>
                    {
                        if (v == rv)
                        {
                            return right;
                        }

                        return null;
                    });
                    stack[i] = (newLeft, newRight);
                }

                result[rv] = left;
            }
            else if (left is Alias || right is Alias)
            {
                lateGoals.Add(new EqGoal(left, right));
            }
            else if (left is App al && right is App ar)
            {
                if (al.Head != ar.Head || al.Args.Length != ar.Args.Length)
                {
                    if (throwException)
                    {
                        throw new MatchException($"{left} = {right}");
                    }

                    return null;
                }

                for (int i = 0; i < al.Args.Length; i++)
                {
                    stack.Add((al.Args[i], ar.Args[i]));
                }
            }
            else if (left is IrAlias il && right is IrAlias ir)
            {
                if (il.Name != ir.Name)
                {
                    if (throwException)
                    {
                        throw new MatchException($"{left} = {right}");
                    }

                    return null;
                }
                
                stack.Add((il.Target, ir.Target));
                stack.Add((il.Trait, ir.Trait));
            }
            else if (left is BoundVar bl && right is BoundVar br)
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
        }

        for (int i = 0; i < lateGoals.Count; i++)
        {
            // do the substitution for late goals
            // tought that may be arguably not necessary, but it's safer
            lateGoals[i] = lateGoals[i].Substitute(new TermMatch(result, ImmutableArray<EqGoal>.Empty));
        }

        return new TermMatch(result, lateGoals);
    }
}