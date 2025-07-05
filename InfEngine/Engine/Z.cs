using System.Collections.Immutable;
using System.Collections.ObjectModel;
using InfEngine.Engine.Clauses;
using InfEngine.Engine.Goals;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine;

public static class Z
{
    public static FreeVar Fv(string name) => new FreeVar(name);
    public static BoundVar Bv(long name) => new BoundVar(name);
    public static App M(string name) => new App(name, []);
    public static Alias Proj(this Term target, Term trait, string assocType) => new Alias(target, trait, assocType);
    public static Func<Term, App> A1(string name) => a => new App(name, [a]);
    public static Func<Term, Term, App> A2(string name) => (a, b) => new App(name, [a, b]);
    public static Func<Term, Term, Term, App> A3(string name) => (a, b, c) => new App(name, [a, b, c]);
    public static Func<Term, Term, Term, Term, App> A4(string name) => (a, b, c, d) => new App(name, [a, b, c, d]);
    public static Func<Term, Term, Term, Term, Term, App> A5(string name) => (a, b, c, d, e) => new App(name, [a, b, c, d, e]);
    public static Func<Term, Term, Term, Term, Term, Term, App> A6(string name) => (a, b, c, d, e, f) => new App(name, [a, b, c, d, e, f]);
    
    public static EqGoal EqG(Term left, Term right) => new EqGoal(left, right);
    public static ImplGoal ImplG(Term target, Term trait, string resolvesTo) => new ImplGoal(target, trait, ReadOnlyDictionary<string, Term>.Empty, resolvesTo);
    public static NormGoal NormG(Alias alias, FreeVar var) => new NormGoal(alias, var);
    public static ImplGoal Assoc(this ImplGoal goal, string name, Term constraint) => 
        new ImplGoal(
            goal.Target, 
            goal.Trait, 
            AddAssocConstraint(goal.AssocConstraints, name, constraint), 
            goal.ResolvesTo);

    private static IReadOnlyDictionary<string, Term> AddAssocConstraint(IReadOnlyDictionary<string, Term> goalAssocConstraints, string name, Term constraint)
    {
        var dict = new Dictionary<string, Term>(goalAssocConstraints)
        {
            [name] = constraint
        };
        return dict;   
    }

    public static ImplConstraint ImplC(Term target, Term trait) => new(target, trait, ReadOnlyDictionary<string, Term>.Empty);

    public static ImplConstraint Assoc(this ImplConstraint ic, string name, Term constraint) =>
        ic with { AssocConstraints = AddAssocConstraint(ic.AssocConstraints, name, constraint) };
    public static ImplClause Assoc(this ImplClause ic, string name, Term constraint) =>
        ic with { AssocConstraints = AddAssocConstraint(ic.AssocConstraints, name, constraint) };
    
    public static ImplClause Impl0(string name, Term target, Term trait, params ReadOnlySpan<ImplConstraint> constraints) =>
        new(name, [], target, trait, ReadOnlyDictionary<string, Term>.Empty, [..constraints]);

    public static ImplClause Impl1(string name, Func<BoundVar, ZImpl> fn)
    {
        var vars = new[] { new BoundVar(0) };
        var impl = fn(vars[0]);
        return new(name, [..vars], impl.Target, impl.Trait, impl.AssocConstraints, impl.Constraints);
    }
    
    public static ImplClause Impl2(string name, Func<BoundVar, BoundVar, ZImpl> fn)
    {
        var vars = new BoundVar[] { new BoundVar(0), new BoundVar(1) };
        var impl = fn(vars[0], vars[1]);
        return new(name, [..vars], impl.Target, impl.Trait, impl.AssocConstraints, impl.Constraints);
    }
    
    public static ImplClause Impl3(string name, Func<BoundVar, BoundVar, BoundVar, ZImpl> fn)
    {
        var vars = new BoundVar[] { new BoundVar(0), new BoundVar(1), new BoundVar(2) };
        var impl = fn(vars[0], vars[1], vars[2]);
        return new(name, [..vars], impl.Target, impl.Trait, impl.AssocConstraints, impl.Constraints);
    }

    public static ZImpl Impl(Term target,
                             Term trait,
                             params ReadOnlySpan<ImplConstraint> constraints)
    {
        return new ZImpl(target, trait, [..constraints], ReadOnlyDictionary<string, Term>.Empty);
    }
    
    public static ZImpl Assoc(this ZImpl zimpl, string name, Term constraint) =>
        zimpl with { AssocConstraints = AddAssocConstraint(zimpl.AssocConstraints, name, constraint) };
    
    public static ZAlias Alias(Term target, Term trait, Term aliased) =>
        new(target, trait, aliased);
    
    public static AliasImplClause Alias0(string name, Term target, Term trait, Term aliased) =>
        new(ImmutableArray<BoundVar>.Empty, target, trait, name, aliased,
            new ImplConstraint(target, trait, ReadOnlyDictionary<string, Term>.Empty));

    public static AliasImplClause Alias1(string name, Func<BoundVar, ZAlias> fn)
    {
        var vars = new[] { new BoundVar(0) };
        var alias = fn(vars[0]);
        return new([vars[0]], alias.Target, alias.Trait, name, alias.Aliased, 
            new ImplConstraint(alias.Target, alias.Trait, ReadOnlyDictionary<string, Term>.Empty));
    }
    
    public static AliasImplClause Alias2(string name, Func<BoundVar, BoundVar, ZAlias> fn)
    {
        var vars = new BoundVar[] { new BoundVar(0), new BoundVar(1) };
        var alias = fn(vars[0], vars[1]);
        return new([vars[0], vars[1]], alias.Target, alias.Trait, name, alias.Aliased, 
            new ImplConstraint(alias.Target, alias.Trait, ReadOnlyDictionary<string, Term>.Empty));
    }
    
    public static AliasImplClause Alias3(string name, Func<BoundVar, BoundVar, BoundVar, ZAlias> fn)
    {
        var vars = new BoundVar[] { new BoundVar(0), new BoundVar(1), new BoundVar(2) };
        var alias = fn(vars[0], vars[1], vars[2]);
        return new([vars[0], vars[1], vars[2]], alias.Target, alias.Trait, name, alias.Aliased, 
            new ImplConstraint(alias.Target, alias.Trait, ReadOnlyDictionary<string, Term>.Empty));
    }
    
    public static ZAssocTy AssocTy(Term trait, Term constraint) =>
        new(trait, constraint, ReadOnlyDictionary<string, Term>.Empty);
    
    public static ZAssocTy Assoc(this ZAssocTy ic, string name, Term constraint) =>
        ic with { AssocConstraints = AddAssocConstraint(ic.AssocConstraints, name, constraint) };
    
    public static AssocTyClause AssocTy0(string aliasName, Func<BoundVar, ZAssocTy> fn)
    {
        var vars = new[] { new BoundVar(0) };
        var alias = fn(vars[0]);
        return new(vars[0], [], alias.Trait, aliasName, alias.Constraint, alias.AssocConstraints);
    }

    public static AssocTyClause AssocTy1(string aliasName, Func<BoundVar, BoundVar, ZAssocTy> fn)
    {
        var vars = new[] { new BoundVar(0), new BoundVar(0) };
        var alias = fn(vars[0], vars[1]);
        return new(vars[0], [vars[1]], alias.Trait, aliasName, alias.Constraint, alias.AssocConstraints);
    }
    
    public static AssocTyClause AssocTy2(string aliasName, Func<BoundVar, BoundVar, BoundVar, ZAssocTy> fn)
    {
        var vars = new BoundVar[] { new BoundVar(0), new BoundVar(0), new BoundVar(1) };
        var alias = fn(vars[0], vars[1], vars[2]);
        return new(vars[0], [vars[1], vars[2]], alias.Trait, aliasName, alias.Constraint, alias.AssocConstraints);
    }
    
    public static AssocTyClause AssocTy3(string aliasName, Func<BoundVar, BoundVar, BoundVar, BoundVar, ZAssocTy> fn)
    {
        var vars = new BoundVar[] { new BoundVar(0), new BoundVar(0), new BoundVar(1), new BoundVar(2) };
        var alias = fn(vars[0], vars[1], vars[2], vars[3]);
        return new(vars[0], [vars[1], vars[2], vars[3]], alias.Trait, aliasName, alias.Constraint, alias.AssocConstraints);
    }
}

public record ZImpl(Term Target,
                    Term Trait,
                    ImmutableArray<ImplConstraint> Constraints,
                    IReadOnlyDictionary<string, Term> AssocConstraints);
                    
                    
public record ZAlias(Term Target,
                    Term Trait,
                    Term Aliased);
                    
public record ZAssocTy(
                     Term Trait,
                     Term Constraint,
                     IReadOnlyDictionary<string, Term> AssocConstraints);