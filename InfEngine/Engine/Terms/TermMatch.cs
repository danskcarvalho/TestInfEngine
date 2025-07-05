using InfEngine.Engine.Goals;

namespace InfEngine.Engine.Terms;

public record TermMatch(
    IReadOnlyDictionary<Term, Term> Substitutions,
    IReadOnlyList<EqGoal> LateEqGoals)
{
    public static readonly TermMatch Empty = new(new Dictionary<Term, Term>(), new List<EqGoal>());

    public bool IsEmpty => Substitutions.Count == 0;

    /// <summary>
    /// Gives, for each variable, a set of all the other variables it's equal to.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyDictionary<Term, HashSet<Term>> ToEqualitySet()
    {
        var res = new Dictionary<Term, HashSet<Term>>();
        
        foreach (var pair in Substitutions)
        {
            HashSet<Term> left; 
            if (res.ContainsKey(pair.Key))
            {
                left = res[pair.Key];
                res[pair.Key].Add(pair.Key);
            }
            else
            {
                var hs = left = new HashSet<Term>();
                res[pair.Key] = hs;
                hs.Add(pair.Key);
            }
            
            if (pair.Value is FreeVar v)
            {
                if (!res.ContainsKey(v))
                {
                    var hs = new HashSet<Term>();
                    res[v] = hs;
                    hs.Add(v);
                }
                
                var union = new HashSet<Term>();
                union.UnionWith(res[v]);
                union.UnionWith(left);
                foreach (var var in union)
                {
                    res[var].UnionWith(union);
                }
            }
            
            if (pair.Value is ConstFreeVar cv)
            {
                if (!res.ContainsKey(cv))
                {
                    var hs = new HashSet<Term>();
                    res[cv] = hs;
                    hs.Add(cv);
                }
                
                var union = new HashSet<Term>();
                union.UnionWith(res[cv]);
                union.UnionWith(left);
                foreach (var var in union)
                {
                    res[var].UnionWith(union);
                }
            }
        }

        foreach (var pair in Substitutions)
        {
            foreach (var v in pair.Value.Descendants<FreeVar>())
            {
                if (res.ContainsKey(v))
                {
                    res[v].Add(v);
                }
                else
                {
                    res[v] = [v];
                }
            }
            
            foreach (var v in pair.Value.Descendants<ConstFreeVar>())
            {
                if (res.ContainsKey(v))
                {
                    res[v].Add(v);
                }
                else
                {
                    res[v] = [v];
                }
            }
        }
        
        return res;
    }
    
    /// <summary>
    /// Gives, for each variable, a set of all the other variables it's related to. Ex.:
    /// If ?a = List<?c, Dictionary<?d, ?e>> then ?a is related to ?d, ?e and ?c
    /// </summary>
    /// <returns></returns>
    public IReadOnlyDictionary<Term, HashSet<Term>> ToRelatedSet()
    {
        var res = new Dictionary<Term, HashSet<Term>>();
        
        foreach (var pair in Substitutions)
        {
            HashSet<Term> left; 
            if (res.ContainsKey(pair.Key))
            {
                left = res[pair.Key];
                res[pair.Key].Add(pair.Key);
            }
            else
            {
                var hs = left = new HashSet<Term>();
                res[pair.Key] = hs;
                hs.Add(pair.Key);
            }
            
            foreach (var v in pair.Value.DescendantsAndSelf<FreeVar>())
            {
                if (!res.ContainsKey(v))
                {
                    var hs = new HashSet<Term>();
                    res[v] = hs;
                    hs.Add(v);
                }
                
                var union = new HashSet<Term>();
                union.UnionWith(res[v]);
                union.UnionWith(left);
                foreach (var var in union)
                {
                    res[var].UnionWith(union);
                }
            }
            
            foreach (var v in pair.Value.DescendantsAndSelf<ConstFreeVar>())
            {
                if (!res.ContainsKey(v))
                {
                    var hs = new HashSet<Term>();
                    res[v] = hs;
                    hs.Add(v);
                }
                
                var union = new HashSet<Term>();
                union.UnionWith(res[v]);
                union.UnionWith(left);
                foreach (var var in union)
                {
                    res[var].UnionWith(union);
                }
            }
        }

        foreach (var pair in Substitutions)
        {
            foreach (var v in pair.Value.Descendants<FreeVar>())
            {
                if (res.ContainsKey(v))
                {
                    res[v].Add(v);
                }
                else
                {
                    res[v] = [v];
                }
            }
            
            foreach (var v in pair.Value.Descendants<ConstFreeVar>())
            {
                if (res.ContainsKey(v))
                {
                    res[v].Add(v);
                }
                else
                {
                    res[v] = [v];
                }
            }
        }
        
        return res;
    }

    public TermMatch Merge(TermMatch match)
    {
        Dictionary<Term, Term> result = new();
        
        foreach (var key in this.Substitutions.Keys)
        {
            result[key] = this.Substitutions[key].Substitute(match);
        }

        foreach (var key in match.Substitutions.Keys)
        {
            result[key] = match.Substitutions[key].Substitute(this);
        }

        return new TermMatch(result, this.LateEqGoals.Concat(match.LateEqGoals).ToList());
    }
    
    public TermMatch PurgeGoals() => this with { LateEqGoals = [] };
}