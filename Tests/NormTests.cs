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
}