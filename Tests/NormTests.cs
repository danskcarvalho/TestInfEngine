using InfEngine.Engine;

namespace Tests;

public class NormTests
{
    [Fact]
    public void Test1()
    {
        var str = Z.M("str");
        var i32 = Z.M("i32");
        var trait = Z.M("Trait");
        var aliasClause = Z.Alias0("Proj", str, trait, i32);
        var implClause = Z.Impl0("strTrait", str, trait);
        var goal = Z.NormG(str.Proj(trait, "Proj"), Z.Fv("a"));
        var solver = new Solver([goal], [aliasClause, implClause]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Contains(Z.Fv("a"), result.Value.Match.Substitutions);
        Assert.Equal(i32, result.Value.Match.Substitutions[Z.Fv("a")]);
    }

    [Fact]
    public void Test2()
    {
        var str = Z.M("str");
        var trait = Z.M("List");
        var vec = Z.A1("Vec");
        var aliasClause = Z.Alias1("Item", t => Z.Alias(vec(t), trait, t));
        var implClause = Z.Impl1("listTrait", t => Z.Impl(vec(t), trait));
        var goal = Z.NormG(vec(str).Proj(trait, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [aliasClause, implClause]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Contains(Z.Fv("a"), result.Value.Match.Substitutions);
        Assert.Equal(str, result.Value.Match.Substitutions[Z.Fv("a")]);
    }
    
    [Fact]
    public void Test3()
    {
        var str = Z.M("str");
        var trait = Z.M("List");
        var vec = Z.A1("Vec");
        var vecItem = Z.A1("VecItem");
        var aliasClause = Z.Alias1("Item", t => Z.Alias(vec(t), trait, vecItem(t)));
        var implClause = Z.Impl1("listTrait", t => Z.Impl(vec(t), trait));
        var goal = Z.NormG(vec(str).Proj(trait, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [aliasClause, implClause]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Contains(Z.Fv("a"), result.Value.Match.Substitutions);
        Assert.Equal(vecItem(str), result.Value.Match.Substitutions[Z.Fv("a")]);
    }
    
    [Fact]
    public void Test4()
    {
        var str = Z.M("str");
        var num = Z.M("num");
        var list = Z.M("List");
        var dict = Z.A2("Dict");
        var vec = Z.A1("Vec");
        var vecItem = Z.A1("VecItem");
        var aliasClause = Z.Alias1("Item", t => Z.Alias(vec(t), list, vecItem(t)));
        var implClause = Z.Impl1("listTrait", t => Z.Impl(vec(t), list));
        var aliasClause2 = Z.Alias1("Item", t => Z.Alias(vec(t), dict(t, num), t));
        var implClause2 = Z.Impl1("dictTrait", t => Z.Impl(vec(t), dict(t, num)));
        var goal = Z.NormG(vec(str).Proj(dict(str, num), "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [aliasClause, implClause, aliasClause2, implClause2]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Contains(Z.Fv("a"), result.Value.Match.Substitutions);
        Assert.Equal(str, result.Value.Match.Substitutions[Z.Fv("a")]);
    }
    
    [Fact]
    public void Test5()
    {
        var str = Z.M("str");
        var num = Z.M("num");
        var list = Z.M("List");
        var dict = Z.A2("Dict");
        var vec = Z.A1("Vec");
        var vecItem = Z.A1("VecItem");
        var aliasClause = Z.Alias1("Item", t => Z.Alias(vec(t), list, vecItem(t)));
        var implClause = Z.Impl1("listTrait", t => Z.Impl(vec(t), list));
        var aliasClause2 = Z.Alias1("Item", t => Z.Alias(vec(t), dict(t, num), t));
        var implClause2 = Z.Impl1("dictTrait", t => Z.Impl(vec(t), dict(t, num)));
        var goal = Z.NormG(vec(str).Proj(dict(num, num), "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [aliasClause, implClause, aliasClause2, implClause2]);
        var result = solver.Run();
        Assert.Null(result);
    }
    
    [Fact]
    public void Test6()
    {
        var a = Z.M("B");
        var num = Z.M("num");
        var list = Z.M("list");
        var implClause = Z.Impl0("listTrait", a, list).Assoc("Item", num);
        var goal = Z.NormG(a.Proj(list, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [implClause]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Contains(Z.Fv("a"), result.Value.Match.Substitutions);
        Assert.Equal(num, result.Value.Match.Substitutions[Z.Fv("a")]);
    }
}