using System.Collections.Immutable;

namespace InfEngine.Engine;

public abstract record Clause()
{
}

// impl Name<TyParams> Target : Trait
public record ImplClause(
    string Name,
    ImmutableArray<BoundVar> TyParams,
    Term Target,
    Term Trait,
    ImmutableArray<ImplConstraint> Constraints) : Clause
{
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
    
        // Add hash codes for each element in TyParams
        foreach (var tyParam in TyParams)
        {
            hash.Add(tyParam);
        }
    
        hash.Add(Target);
        hash.Add(Trait);
    
        // Add hash codes for each element in Constraints
        foreach (var constraint in Constraints)
        {
            hash.Add(constraint);
        }
    
        return hash.ToHashCode();
    }

    public virtual bool Equals(ImplClause? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
    
        // Check each property for equality
        if (Name != other.Name) return false;
        if (!Target.Equals(other.Target)) return false;
        if (!Trait.Equals(other.Trait)) return false;
    
        // Check TyParams array equality
        if (TyParams.Length != other.TyParams.Length) return false;
        for (int i = 0; i < TyParams.Length; i++)
        {
            if (!TyParams[i].Equals(other.TyParams[i])) return false;
        }
    
        // Check Constraints array equality
        if (Constraints.Length != other.Constraints.Length) return false;
        for (int i = 0; i < Constraints.Length; i++)
        {
            if (!Constraints[i].Equals(other.Constraints[i])) return false;
        }
    
        return true;
    }

    public override string ToString() => $"impl {Name}<{string.Join(", ", TyParams)}> {Target}: {Trait} where {string.Join(", ", Constraints)}";
}

public record ImplConstraint(Term Target, Term Trait)
{
    public override string ToString() => $"{Target}: {Trait}";
}