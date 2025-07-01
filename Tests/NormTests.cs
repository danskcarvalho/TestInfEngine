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
        var implClause = Z.Impl1("lisTrait", t => Z.Impl(vec(t), trait));
        var goal = Z.NormG(vec(str).Proj(trait, "Item"), Z.Fv("a"));
        var solver = new Solver([goal], [aliasClause, implClause]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Contains(Z.Fv("a"), result.Value.Match.Substitutions);
        Assert.Equal(str, result.Value.Match.Substitutions[Z.Fv("a")]);
    }
}