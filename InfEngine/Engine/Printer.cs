namespace InfEngine.Engine;

public static class Printer
{
    private static bool _printToConsole = false;
    public static void PrintToConsole(Action print)
    {
        _printToConsole = true;
        try
        {
            print();
        }
        finally
        {
            _printToConsole = false;
        }
    }

    public static string PrintType(string type)
    {
        if (_printToConsole)
            return Magenta(type);

        return type;
    }
    
    public static string PrintKeyword(string kw)
    {
        if (_printToConsole)
            return Blue(kw);

        return kw;
    }
    
    public static string PrintVar(string var)
    {
        if (_printToConsole)
            return Green(var);

        return var;
    }
}