using System.Text;

namespace InfEngine.Engine;

public record struct SolverResult(IReadOnlyDictionary<string, Instatiation> Instantiations, TermMatch Match)
{
    public override string ToString()
    {
        StringBuilder builder = new();
        foreach (var kv in Instantiations)
        {
            builder.AppendLine($"{kv.Key}: {kv.Value}");
        }

        builder.AppendLine("--- Vars ---");

        foreach (var kv in Match.Substitutions)
        {
            builder.AppendLine($"{kv.Key}: {kv.Value}");
        }

        return builder.ToString();
    }
}