using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using PokedexTracker.Core;

namespace PokedexTracker.CatalogGenerator;

[JsonConverter(typeof(JsonStringEnumConverter<EvolutionCoverageStatus>))]
public enum EvolutionCoverageStatus { Included, Unavailable, Unresolved }

// A reviewed supplement or an explanation of why a family relationship cannot
// evolve locally. The fingerprint makes source changes require another review.
public sealed record EvolutionDecision(string GameGroup, string FromId, string ToId,
    string? Requirement, string Reason, string Source, string RuleFingerprint);

public sealed record EvolutionRuleTrace(string RuleId, string Outcome);

public sealed record EvolutionCoveragePath(string GameId, string FromId, string ToId,
    EvolutionCoverageStatus Status, List<string> Requirements, string Reason,
    List<string> Sources, List<EvolutionRuleTrace> Rules, string RuleFingerprint);

public sealed record EvolutionCoverageReport(string RuleSource,
    List<EvolutionCoveragePath> Paths, List<string> Issues)
{
    public bool Complete => Issues.Count == 0 && Paths.All(p => p.Status != EvolutionCoverageStatus.Unresolved);
}

public sealed record EvolutionBuildResult(Dictionary<string, List<Evolution>> Evolutions,
    EvolutionCoverageReport Coverage);

public static class EvolutionRuleFingerprint
{
    public static string Of(IEnumerable<Dictionary<string, string>> rules)
    {
        string content = string.Join("\n", rules.OrderBy(r => int.Parse(r["id"]))
            .Select(r => string.Join("|", r.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"))));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
}
