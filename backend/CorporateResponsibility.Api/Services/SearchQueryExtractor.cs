using System.Text.RegularExpressions;

namespace CorporateResponsibility.Api.Services;

public interface ISearchQueryExtractor
{
    string? ExtractSearchQuery(string? phrase);
}

public sealed partial class SearchQueryExtractor : ISearchQueryExtractor
{
    private static readonly (Regex Pattern, string Query)[] ThemeMappings =
    [
        (new Regex(@"\b(e-commerce|ecommerce|commerce|retail|marketplace)\b", RegexOptions.Compiled), "ecommerce business"),
        (new Regex(@"\b(cloud|cloud-native|web|software|apps?|mobile|platforms?|networks?|portals?|systems?|interfaces?|technolog(?:y|ies)|digital|infrastructure|engineering|pipelines?|automation|automate|workflows?|reliability)\b", RegexOptions.Compiled), "technology office"),
        (new Regex(@"\b(data|metrics?|analytics?|dashboards?|roi|revenue|growth|profit|capital|markets?|financial|stewardship|quarterly|reporting|allocation|resources?|monetiz\w*)\b", RegexOptions.Compiled), "business analytics"),
        (new Regex(@"\b(teams?|leadership|collaborat\w*|human\s+capital|workforce|talents?|career|people|partners?|hybrid|workplaces?|knowledge|sharing|rituals?)\b", RegexOptions.Compiled), "business team meeting"),
        (new Regex(@"\b(customers?|clients?|users?|services?|onboarding|experiences?|support|customer-first)\b", RegexOptions.Compiled), "customer service"),
        (new Regex(@"\b(supply\s+chains?|logistics|manufactur\w*|outsourc\w*|distributions?|operations?|operational|delivery)\b", RegexOptions.Compiled), "logistics warehouse"),
        (new Regex(@"\b(global|worldwide|international|borderless|enterprise)\b", RegexOptions.Compiled), "global business"),
        (new Regex(@"\b(security|compliance|compliant|risk|governance|auditable|standards?|contracts?)\b", RegexOptions.Compiled), "corporate contract"),
        (new Regex(@"\b(strateg\w*|visions?|innovat\w*|ideas?|solutions?|roadmaps?|decision|velocity|executive|continuity|resilience|product|quality)\b", RegexOptions.Compiled), "business strategy"),
        (new Regex(@"\b(marketing|brand\w*|content|media|channels?|stakeholders?)\b", RegexOptions.Compiled), "marketing office")
    ];

    private static readonly HashSet<string> BuzzwordStopList = new(StringComparer.OrdinalIgnoreCase)
    {
        "with", "from", "into", "that", "this", "your", "their", "through", "across", "without",
        "synergize", "leverage", "foster", "drive", "scale", "optimize", "maximize", "enable",
        "empower", "deliver", "cultivate", "facilitate", "integrate", "revolutionize", "transform",
        "implement", "utilize", "harness", "architect", "orchestrate", "redefine", "streamline",
        "restore", "aggregate", "productize", "accelerate", "align", "amplify", "anchor",
        "balance", "benchmark", "bridge", "broaden", "calibrate", "champion", "clarify",
        "consolidate", "coordinate", "de-risk", "elevate", "expand", "formalize", "govern",
        "guide", "harden", "harmonize", "illuminate", "improve", "modernize", "navigate",
        "prioritize", "refine", "reinforce", "reimagine", "reshape", "safeguard", "simplify",
        "standardize", "strengthen", "unify", "validate",
        "proactive", "holistic", "seamless", "scalable", "robust", "efficient", "effective",
        "dynamic", "strategic", "innovative", "cutting-edge", "next-generation", "best-of-breed",
        "mission-critical", "value-added", "turnkey", "bespoke", "cross-platform", "high-payoff",
        "enterprise-wide", "accountable", "actionable", "adaptive", "agile", "aligned",
        "balanced", "circular", "cohesive", "connected", "consistent", "cross-functional",
        "data-driven", "dependable", "durable", "equitable", "ethical", "frictionless",
        "future-proof", "high-trust", "human-centered", "inclusive", "integrated", "interoperable",
        "long-term", "measurable", "modular", "omnichannel", "resilient", "responsible",
        "sustainable", "transparent", "unified",
        "paradigms", "paradigm", "synergies", "synergy", "deliverables", "initiatives",
        "methodologies", "bandwidth", "imperatives", "niches", "schemas", "models",
        "action-items", "practices", "core", "competencies", "value", "quality", "outcomes"
    };

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex NonTokenCharRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    public string? ExtractSearchQuery(string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return null;
        }

        var normalized = phrase.ToLowerInvariant();
        foreach (var (pattern, query) in ThemeMappings)
        {
            if (pattern.IsMatch(normalized))
            {
                return query;
            }
        }

        var cleaned = NonTokenCharRegex().Replace(normalized, " ");
        var rawTokens = WhitespaceRegex().Split(cleaned);

        string? lastConcrete = null;
        foreach (var rawToken in rawTokens)
        {
            var token = rawToken.Trim('-');
            if (token.Length < 4 || token.EndsWith("ly", StringComparison.Ordinal) || BuzzwordStopList.Contains(token))
            {
                continue;
            }

            lastConcrete = token;
        }

        return lastConcrete;
    }
}
