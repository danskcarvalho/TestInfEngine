using InfEngine.Engine;
using InfEngine.Engine.Terms;

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

    [Fact]
    public void Test7()
    {
        var b = Z.M("B");
        var num = Z.M("Num");
        var trait = Z.M("Trait");
        var list = Z.M("List");
        var implClause1 = Z.Impl0("listTrait", b, list).Assoc("Item", num);
        var implClause2 = Z.Impl0("numTrait", num, trait);
        var goal = Z.ImplG(b.Proj(list, "Item"), trait, "goal1");
        var solver = new Solver([goal], [implClause1, implClause2]);
        var result = solver.Run();
        Assert.NotNull(result);
    }
    
    [Fact]
    public void Test8()
    {
        var b = Z.M("B");
        var str = Z.M("str");
        var num = Z.M("Num");
        var trait = Z.M("Trait");
        var list = Z.M("List");
        var implClause1 = Z.Impl0("listTrait", b, list).Assoc("Item", num);
        var implClause2 = Z.Impl0("numTrait", num, trait).Assoc("Item", str);
        var goal = Z.NormG(b.Proj(list, "Item").Proj(trait, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [implClause1, implClause2]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Contains(Z.Fv("a"), result.Value.Match.Substitutions);
        Assert.Equal(str, result.Value.Match.Substitutions[Z.Fv("a")]);
    }
    
    [Fact]
    public void Test9()
    {
        var b = Z.M("B");
        var str = Z.M("str");
        var num = Z.M("Num");
        var real = Z.M("Real");
        var trait = Z.M("Trait");
        var list = Z.M("List");
        var implClause1 = Z.Impl0("listTrait", b, list).Assoc("Item", real);
        var implClause2 = Z.Impl0("numTrait", num, trait).Assoc("Item", str);
        var goal = Z.NormG(b.Proj(list, "Item").Proj(trait, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [implClause1, implClause2]);
        var result = solver.Run();
        Assert.Null(result);
    }
    
    [Fact]
    public void Test10()
    {
        var num = Z.M("Num");
        var trait = Z.M("Trait");
        var implClause1 = Z.Impl0("numTrait", num, trait).Assoc("Item", num);
        var goal = Z.NormG(num.Proj(trait, "Item").Proj(trait, "Item").Proj(trait, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [implClause1]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Contains(Z.Fv("a"), result.Value.Match.Substitutions);
        Assert.Equal(num, result.Value.Match.Substitutions[Z.Fv("a")]);
    }

    [Fact]
    public void Test11()
    {
        var num = Z.M("Num");
        var trait = Z.M("Trait");
        var implClause1 = Z.Impl0("numTrait", num.Proj(trait, "Item"), trait).Assoc("Item", num);
        var goal = Z.NormG(num.Proj(trait, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [implClause1]);
        var result = solver.Run();
        Assert.Null(result);
    }
    
    [Fact]
    public void Test12()
    {
        var num = Z.M("Num");
        var trait = Z.M("Trait");
        var implClause1 = Z.Impl0("numTrait", num, trait).Assoc("Item", num.Proj(trait, "Item"));
        var goal = Z.NormG(num.Proj(trait, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [implClause1]);
        var result = solver.Run();
        Assert.Null(result);
    }
    
    [Fact]
    public void Test13()
    {
        var num = Z.M("Num");
        var trait = Z.M("Trait");
        var implClause1 = Z.Impl0("numTrait", num, trait);
        var goal = Z.NormG(num.Proj(trait, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [implClause1]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Contains(Z.Fv("a"), result.Value.Match.Substitutions);
        Assert.Equal(new IrAlias(num, trait, "Item"), result.Value.Match.Substitutions[Z.Fv("a")]);
    }

    [Fact]
    public void Test14()
    {
        var num = Z.M("Num");
        var trait = Z.M("Trait");
        var eq = Z.M("Eq");
        var str = Z.M("Str");
        var implClause1 = Z.Impl0("numTrait", num, trait).Assoc("Item", str);
        var goal = Z.NormG(num.Proj(trait, "Item").Proj(eq, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [implClause1]);
        var result = solver.Run();
        Assert.Null(result);
    }

    [Fact]
    public void Test15()
    {
        var num = Z.M("Num");
        var trait = Z.M("Trait");
        var eq = Z.M("Eq");
        var str = Z.M("Str");
        var implClause1 = Z.Impl0("numTrait", num, trait).Assoc("Item", str);
        var implClause2 = Z.Impl0("strTrait", str, eq);
        var clause2 = Z.AssocTy0("Item", trait, eq);
        var goal = Z.NormG(num.Proj(trait, "Item").Proj(eq, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [implClause1, implClause2, clause2]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Equal(new IrAlias(str, eq, "Item"), result.Value.Match.Substitutions[Z.Fv("a")]);
    }
    
    [Fact]
    public void Test20()
    {
        var num = Z.M("Num");
        var trait = Z.M("Trait");
        var eq = Z.M("Eq");
        var implClause1 = Z.Impl0("numTrait", num, trait);
        var clause2 = Z.AssocTy0("Item", trait, eq);
        var goal = Z.NormG(num.Proj(trait, "Item").Proj(eq, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [implClause1, clause2]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Equal(new IrAlias(new IrAlias(num, trait, "Item"), eq, "Item"), result.Value.Match.Substitutions[Z.Fv("a")]);
    }
    
    [Fact]
    public void Test16()
    {
        var num = Z.M("Num");
        var trait = Z.M("Trait");
        var eq = Z.M("Eq");
        var str = Z.M("Str");
        var implClause1 = Z.Impl0("numTrait", num, trait);
        var clause2 = Z.AssocTy0("Item", trait, eq).Assoc("Item", str);
        var goal = Z.NormG(num.Proj(trait, "Item").Proj(eq, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [implClause1, clause2]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Equal(str, result.Value.Match.Substitutions[Z.Fv("a")]);
    }

    [Fact]
    public void Test17()
    {
        var num = Z.M("Num");
        var trait = Z.M("Trait");
        var eq = Z.M("Eq");
        var str = Z.M("Str");
        var implClause1 = Z.Impl0("numTrait", num, trait).Assoc("Item", str);
        var implClause2 = Z.Impl0("strTrait", str, eq);
        var clause2 = Z.AssocTy0("Item", trait, eq);
        var goal = Z.NormG(num.Proj(trait, "Item").Proj(eq, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [implClause1, clause2, implClause2]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Equal(new IrAlias(str, eq, "Item"), result.Value.Match.Substitutions[Z.Fv("a")]);
    }

    [Fact]
    public void Test18()
    {
        var num = Z.M("Num");
        var trait = Z.M("Trait");
        var eq = Z.M("Eq");
        var str = Z.M("Str");
        var implClause1 = Z.Impl0("numTrait", num, trait);
        var clause2 = Z.AssocTy0("Item", trait, eq).Assoc("Item", str);
        var goal = Z.NormG(num.Proj(trait, "Item").Proj(eq, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [implClause1, clause2]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Equal(str, result.Value.Match.Substitutions[Z.Fv("a")]);
    }
    
    [Fact]
    public void Test19()
    {
        var num = Z.M("Num");
        var bol = Z.M("Bool");
        var trait = Z.M("Trait");
        var eq = Z.M("Eq");
        var str = Z.M("Str");
        var implClause1 = Z.Impl0("numTrait", num, trait).Assoc("Item", str);
        var implClause2 = Z.Impl0("boolTrait", bol, trait).Assoc("Item", str);
        var goal = Z.EqG(num.Proj(trait, "Item"), bol.Proj(trait, "Item"));
        var solver = new Solver([goal], [implClause1, implClause2]);
        var result = solver.Run();
        Assert.NotNull(result);
    }
}