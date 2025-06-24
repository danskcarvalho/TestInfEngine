namespace InfEngine.Engine;

public class MatchException : Exception
{
    public MatchException()
    {
    }

    public MatchException(string message) : base(message)
    {
    }

    public MatchException(string message, Exception inner) : base(message, inner)
    {
    }
}