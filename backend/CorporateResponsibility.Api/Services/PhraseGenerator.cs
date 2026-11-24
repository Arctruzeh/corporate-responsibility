namespace CorporateResponsibility.Api.Services;

public interface IPhraseGenerator
{
    string GeneratePhrase();
    IReadOnlyList<string> GeneratePhrases(int count);
}

public sealed class PhraseGenerator : IPhraseGenerator
{
    private static readonly string[] Adverbs =
    [
        "Thoughtfully", "Methodically", "Decisively", "Transparently", "Reliably",
        "Persistently", "Flexibly", "Responsibly", "Intentionally", "Sustainably",
        "Predictably", "Adaptively", "Rigorously", "Conscientiously", "Harmoniously",
        "Pragmatically", "Constructively", "Collectively", "Diligently", "Measurably",
        "Confidently", "Visibly", "Openly", "Disciplinedly", "Consistently",
        "Accurately", "Swiftly", "Calmly", "Strategically", "Steadily",
        "Smartly", "Clearly", "Cleanly", "Directly", "Purposefully"
    ];

    private static readonly string[] Verbs =
    [
        "Accelerate", "Align", "Amplify", "Anchor", "Automate",
        "Balance", "Benchmark", "Bridge", "Broaden", "Calibrate",
        "Champion", "Clarify", "Consolidate", "Coordinate", "Cultivate",
        "De-risk", "Elevate", "Expand", "Formalize", "Foster",
        "Govern", "Guide", "Harden", "Harmonize", "Illuminate",
        "Improve", "Modernize", "Navigate", "Orchestrate", "Prioritize",
        "Refine", "Reinforce", "Reimagine", "Reshape", "Safeguard",
        "Simplify", "Standardize", "Strengthen", "Unify", "Validate"
    ];

    private static readonly string[] Adjectives =
    [
        "Accountable", "Actionable", "Adaptive", "Agile", "Aligned",
        "Auditable", "Balanced", "Borderless", "Circular", "Cloud-Native",
        "Cohesive", "Compliant", "Connected", "Consistent", "Cross-Functional",
        "Customer-First", "Data-Driven", "Dependable", "Digital", "Durable",
        "Equitable", "Ethical", "Frictionless", "Future-Proof", "Global",
        "High-Trust", "Human-Centered", "Inclusive", "Integrated", "Interoperable",
        "Long-Term", "Measurable", "Modular", "Omnichannel", "Resilient",
        "Responsible", "Scalable", "Sustainable", "Transparent", "Unified"
    ];

    private static readonly string[] Nouns =
    [
        "Analytics Dashboards", "Brand Trust", "Business Continuity", "Career Pathways",
        "Client Onboarding", "Cloud Infrastructure", "Collaboration Rituals", "Commerce Platforms",
        "Compliance Frameworks", "Customer Journeys", "Data Governance", "Decision Velocity",
        "Delivery Pipelines", "Digital Ecosystems", "Engineering Excellence", "Enterprise Growth",
        "Executive Alignment", "Financial Stewardship", "Global Operations", "Hybrid Workplaces",
        "Innovation Roadmaps", "Knowledge Sharing", "Leadership Alignment", "Market Expansion",
        "Operational Resilience", "Partner Networks", "People Operations", "Platform Reliability",
        "Product Quality", "Quarterly Outcomes", "Reporting Standards", "Resource Allocation",
        "Revenue Operations", "Risk Management", "Security Posture", "Service Reliability",
        "Stakeholder Value", "Supply Chains", "Talent Retention", "Workflows"
    ];

    private readonly Random _random;

    public PhraseGenerator() : this(Random.Shared)
    {
    }

    public PhraseGenerator(Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public string GeneratePhrase()
    {
        var adverb = Adverbs[_random.Next(Adverbs.Length)];
        var verb = Verbs[_random.Next(Verbs.Length)];
        var adjective = Adjectives[_random.Next(Adjectives.Length)];
        var noun = Nouns[_random.Next(Nouns.Length)];

        return $"{adverb} {verb} {adjective} {noun}";
    }

    public IReadOnlyList<string> GeneratePhrases(int count)
    {
        if (count < 1 || count > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "count must be between 1 and 20.");
        }

        var phrases = new string[count];
        for (var i = 0; i < count; i++)
        {
            phrases[i] = GeneratePhrase();
        }

        return phrases;
    }
}
