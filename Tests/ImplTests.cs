using InfEngine.Engine;

namespace Tests;

public class ImplTests
{
    [Fact]
    public void Test1()
    {
        var str = Z.M("str");
        var eq = Z.M("Eq");
        var list = Z.A1("List");
        var strEq = Z.Impl0("strEq", str, eq);
        var listEq = Z.Impl1("listEq", t => Z.Impl(list(t), eq, Z.ImplC(t, eq)));
        var goal = Z.ImplG(list(list(str)), eq, "goal1");
        var solver = new Solver([goal], [strEq, listEq]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Contains("goal1", result.Value.Instantiations);
        Assert.Equal("listEq", result.Value.Instantiations["goal1"].ImplName);
        Assert.Equal("listEq", result.Value.Instantiations[result.Value.Instantiations["goal1"].Constraints[0]].ImplName);
        Assert.Equal("strEq", result.Value.Instantiations[result.Value.Instantiations[result.Value.Instantiations["goal1"].Constraints[0]].Constraints[0]].ImplName);
    }

    [Fact]
    public void Test2()
    {
        var str = Z.M("str");
        var eq = Z.M("Eq");
        var dict = Z.A2("Dict");
        var strEq = Z.Impl0("strEq", str, eq);
        var dictEq = Z.Impl2("dictEq", (t1, t2) => Z.Impl(dict(t1, t2), eq, Z.ImplC(t1, eq), Z.ImplC(t2, eq)));
        var goal1 = Z.ImplG(dict(str, str), eq, "goal1");
        var solver = new Solver([goal1], [strEq, dictEq]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Contains("goal1", result.Value.Instantiations);
        var g1 = result.Value.Instantiations[result.Value.Instantiations["goal1"].Constraints[0]];
        var g2 = result.Value.Instantiations[result.Value.Instantiations["goal1"].Constraints[1]];
        Assert.Equal(g1.ImplName, g2.ImplName);
    }
    
    [Fact]
    public void Test3()
    {
        var str = Z.M("str");
        var eq = Z.M("Eq");
        var list = Z.A1("List");
        var strEq = Z.Impl0("strEq", str, eq);
        var dictEq = Z.Impl1("listEq", (t1) => Z.Impl(list(t1), eq, Z.ImplC(list(t1), eq)));
        var goal1 = Z.ImplG(list(str), eq, "goal1");
        var solver = new Solver([goal1], [strEq, dictEq]);
        var result = solver.Run();
        // infinite recursion
        Assert.Null(result);
    }
    
    [Fact]
    public void Test4()
    {
        var str = Z.M("str");
        var eq = Z.M("Eq");
        var list = Z.A1("List");
        var strEq = Z.Impl0("strEq", str, eq);
        var dictEq = Z.Impl1("listEq", (t1) => Z.Impl(list(t1), eq, Z.ImplC(list(list(t1)), eq)));
        var goal1 = Z.ImplG(list(str), eq, "goal1");
        var solver = new Solver([goal1], [strEq, dictEq]);
        var result = solver.Run();
        // infinite recursion
        Assert.Null(result);
    }
    
    [Fact]
    public void Test5()
    {
        var a = Z.Fv("a");
        var str = Z.M("str");
        var intt = Z.M("intt");
        var eq = Z.M("Eq");
        var related = Z.A2("Related");
        var strEq = Z.Impl0("strEq", str, eq);
        var listEq = Z.Impl2("relatedEq", (t1, t2) => Z.Impl(related(t1, t2), eq, Z.ImplC(t2, eq)));
        var goal = Z.ImplG(related(intt, a), eq, "goal1");
        var solver = new Solver([goal], [strEq, listEq]);
        var result = solver.Run();
        Assert.NotNull(result);
        Assert.Contains("goal1", result.Value.Instantiations);
        Assert.Equal("relatedEq", result.Value.Instantiations["goal1"].ImplName);
        Assert.Contains(a, result.Value.Match.Substitutions);
        Assert.Equal(str, result.Value.Match.Substitutions[a]);
    }
}