using InfEngine.Engine;

namespace Tests;

public class MatchTests
{
    [Fact]
    public void Test1()
    {
        var str = Z.M("str");
        var test = Term.Match(str, str);
        Assert.NotNull(test);
    }
    
    [Fact]
    public void Test2()
    {
        var str = Z.M("str");
        var bl = Z.M("bool");
        Assert.Throws<MatchException>(() => Term.Match(str, bl));
    }
    
    [Fact]
    public void Test3()
    {
        var str = Z.M("str");
        var list = Z.A1("List");
        var test = Term.Match(list(str), list(str));
        Assert.NotNull(test);
    }
    
    [Fact]
    public void Test4()
    {
        var str = Z.M("str");
        var list = Z.A1("List");
        var a = Z.Fv("a");
        var test = Term.Match(list(a), list(str));
        Assert.NotNull(test);
        Assert.Equal(str, test.Substitutions[a]);
    }
}