using InfEngine.Engine.Terms;

namespace InfEngine.Engine.Clauses;

public record LitClause : Clause
{
    public LitClause(FreeVar var, 
                     ConstType defaultConstType,
                     IReadOnlySet<ConstType> allowedTypes)
    {
        this.DefaultConstType = defaultConstType;
        this.AllowedConstTypes = allowedTypes;
        this.Var = var;
    }

    public FreeVar Var { get; }

    public IReadOnlySet<ConstType> AllowedConstTypes { get; set; }

    /// <summary>
    /// Default const type for the variable when it's not constrained enough
    /// </summary>
    public ConstType DefaultConstType { get; }
}