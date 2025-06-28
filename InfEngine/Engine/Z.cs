using System.Collections.Immutable;
using InfEngine.Engine.Clauses;
using InfEngine.Engine.Goals;
using InfEngine.Engine.Terms;

namespace InfEngine.Engine;

public static class Z
{
    public static FreeVar Fv(string name) => new FreeVar(name);
    public static BoundVar Bv(long name) => new BoundVar(name);
    public static App M(string name) => new App(name, []);
    public static Func<Term, App> A1(string name) => a => new App(name, [a]);
    public static Func<Term, Term, App> A2(string name) => (a, b) => new App(name, [a, b]);
    public static Func<Term, Term, Term, App> A3(string name) => (a, b, c) => new App(name, [a, b, c]);
    public static Func<Term, Term, Term, Term, App> A4(string name) => (a, b, c, d) => new App(name, [a, b, c, d]);
    public static Func<Term, Term, Term, Term, Term, App> A5(string name) => (a, b, c, d, e) => new App(name, [a, b, c, d, e]);
    public static Func<Term, Term, Term, Term, Term, Term, App> A6(string name) => (a, b, c, d, e, f) => new App(name, [a, b, c, d, e, f]);
    
    public static EqGoal EqG(Term left, Term right) => new EqGoal(left, right);
    public static ImplGoal ImplG(Term target, Term trait, string resolvesTo) => new ImplGoal(target, trait, resolvesTo);
    public static ImplConstraint ImplC(Term target, Term trait) => new ImplConstraint(target, trait);
    public static ImplClause Impl0(string name, Term target, Term trait, params ReadOnlySpan<ImplConstraint> constraints) =>
        new(name, [], target, trait, [..constraints]);

    public static ImplClause Impl1(string name, Func<BoundVar, ZImpl> fn)
    {
        var vars = new BoundVar[] { new BoundVar(0) };
        var impl = fn(vars[0]);
        return new(name, [..vars], impl.Target, impl.Trait, impl.Constraints);
    }
    
    public static ImplClause Impl2(string name, Func<BoundVar, BoundVar, ZImpl> fn)
    {
        var vars = new BoundVar[] { new BoundVar(0), new BoundVar(1) };
        var impl = fn(vars[0], vars[1]);
        return new(name, [..vars], impl.Target, impl.Trait, impl.Constraints);
    }
    
    public static ImplClause Impl3(string name, Func<BoundVar, BoundVar, BoundVar, ZImpl> fn)
    {
        var vars = new BoundVar[] { new BoundVar(0), new BoundVar(1), new BoundVar(2) };
        var impl = fn(vars[0], vars[1], vars[2]);
        return new(name, [..vars], impl.Target, impl.Trait, impl.Constraints);
    }

    public static ZImpl Impl(Term target,
                             Term trait,
                             params ReadOnlySpan<ImplConstraint> constraints)
    {
        return new ZImpl(target, trait, [..constraints]);
    }
}

public record ZImpl(Term Target,
                    Term Trait,
                    ImmutableArray<ImplConstraint> Constraints);