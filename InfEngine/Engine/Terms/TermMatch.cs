using InfEngine.Engine.Goals;

namespace InfEngine.Engine.Terms;

public record TermMatch(
    IReadOnlyDictionary<FreeVar, Term> Substitutions,
    IReadOnlyList<EqGoal> LateGoals)
{
    public static readonly TermMatch Empty = new(new Dictionary<FreeVar, Term>(), new List<EqGoal>());

    public bool IsEmpty => Substitutions.Count == 0;

    /// <summary>
    /// Gives, for each variable, a set of all the other variables it's equal to.
    /// </summary>
    /// <returns></returns>
    public IReadOnlyDictionary<FreeVar, HashSet<FreeVar>> ToEqualitySet()
    {
        var res = new Dictionary<FreeVar, HashSet<FreeVar>>();
        
        foreach (var pair in Substitutions)
        {
            HashSet<FreeVar> left; 
            if (res.ContainsKey(pair.Key))
            {
                left = res[pair.Key];
                res[pair.Key].Add(pair.Key);
            }
            else
            {
                var hs = left = new HashSet<FreeVar>();
                res[pair.Key] = hs;
                hs.Add(pair.Key);
            }
            
            if (pair.Value is FreeVar v)
            {
                if (!res.ContainsKey(v))
                {
                    var hs = new HashSet<FreeVar>();
                    res[v] = hs;
                    hs.Add(v);
                }
                
                var union = new HashSet<FreeVar>();
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
        }
        
        return res;
    }
    
    /// <summary>
    /// Gives, for each variable, a set of all the other variables it's related to. Ex.:
    /// If ?a = List<?c, Dictionary<?d, ?e>> then ?a is related to ?d, ?e and ?c
    /// </summary>
    /// <returns></returns>
    public IReadOnlyDictionary<FreeVar, HashSet<FreeVar>> ToRelatedSet()
    {
        var res = new Dictionary<FreeVar, HashSet<FreeVar>>();
        
        foreach (var pair in Substitutions)
        {
            HashSet<FreeVar> left; 
            if (res.ContainsKey(pair.Key))
            {
                left = res[pair.Key];
                res[pair.Key].Add(pair.Key);
            }
            else
            {
                var hs = left = new HashSet<FreeVar>();
                res[pair.Key] = hs;
                hs.Add(pair.Key);
            }
            
            foreach (var v in pair.Value.DescendantsAndSelf<FreeVar>())
            {
                if (!res.ContainsKey(v))
                {
                    var hs = new HashSet<FreeVar>();
                    res[v] = hs;
                    hs.Add(v);
                }
                
                var union = new HashSet<FreeVar>();
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
        }
        
        return res;
    }

    public TermMatch Merge(TermMatch match)
    {
        Dictionary<FreeVar, Term> result = new Dictionary<FreeVar, Term>();
        foreach (var key in match.Substitutions.Keys)
        {
            result[key] = match.Substitutions[key].Substitute(match);
        }

        foreach (var key in match.Substitutions.Keys)
        {
            result[key] = match.Substitutions[key];
        }

        return new TermMatch(result, this.LateGoals.Concat(match.LateGoals).ToList());
    }
    
    public TermMatch PurgeGoals() => this with { LateGoals = [] };
}