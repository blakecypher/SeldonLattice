namespace SeldonLattice.Domain.Options;

/// <summary>
/// Configuration for seed data (agent colors, session IDs).
/// Read from "SeedData" section in appsettings.json.
/// </summary>
public class SeedDataOptions
{
    /// <summary>
    /// Agent display colors keyed by agent ID.
    /// Used by seed endpoints and the frontend for consistent coloring.
    /// </summary>
    public Dictionary<string, string> AgentColors { get; set; } = new()
    {
        // Classical
        ["plato"] = "#FFD700",
        ["aristotle"] = "#4ECDC4",
        ["socrates"] = "#FF6B6B",
        // Psychology (3-Tier Lattice + Container + Subnode)
        ["jung"] = "#4a5c8a",
        ["freud"] = "#8a4a4a",

        ["nietzsche"] = "#8a644a",
        ["dostoevsky"] = "#5a6a7a",
        ["frankl"] = "#4a7a5a",
        ["baudrillard"] = "#7a5a5a",
        ["weil"] = "#5a5a7a",
        ["heidegger"] = "#5a6a5a",
        // Legal & Compliance
        ["cardozo"] = "#B8924A",
        ["scalia"] = "#7B8FA1",
        ["oconnor"] = "#A0C4B8",
        ["darrow"] = "#C9A96E",
        ["rawls"] = "#D4AF37",
    };

    /// <summary>
    /// Session ID prefixes for module config seed data.
    /// </summary>
    /// <summary>
    /// Session ID prefix for the classical module seed data node.
    /// </summary>
    public string ClassicalSessionId { get; set; } = "config-classical";

    /// <summary>
    /// Session ID prefix for the psychology module seed data node.
    /// </summary>
    public string PsychologySessionId { get; set; } = "config-psychology";

    /// <summary>
    /// Session ID prefix for the quality gate config seed data node.
    /// </summary>
    public string QualityGatesSessionId { get; set; } = "config-quality-gates";

    /// <summary>
    /// Session ID prefix for the legal module seed data node.
    /// </summary>
    public string LegalSessionId { get; set; } = "config-legal";
}
