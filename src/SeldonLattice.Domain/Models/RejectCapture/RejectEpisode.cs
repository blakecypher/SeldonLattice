using System.Net.Mime;
using System.Text.Json.Serialization;

namespace SeldonLattice.Domain.Models.RejectCapture;

/// <summary>
/// Canonical flags that mark a stored node as a "gate episode" - engine output that a
/// quality gate, the security guard, the ascension gate, or the H3 ascent scorer judged
/// as failing the R3+A3 mandate. These are the supervised negative labels the
/// Autopilot Telemetry autopilot (and the LoRA training stack) learn from.
/// </summary>
public static class RejectFlags
{
    /// <summary>Response diverged from the root theme at gate-check time (off-the-root-theme).</summary>
    public const string OffTopicDrift = "off_topic_drift";

    /// <summary>Flagged by the prompt-injection security guard.</summary>
    public const string Security = "security";

    /// <summary>Synthesis sustained the current theme instead of ascending (dissent list full / low-confidence join).</summary>
    public const string SustainedTheme = "sustained_theme";

    /// <summary>Accepted synthesis scored below the H3 ascent-quality floor.</summary>
    public const string LowAscent = "low_ascent";
}

/// <summary>
/// One captured gate episode (Reasoning Integrity Layer §S4, row 4): a stored dialectical
/// node with the flags that mark it as a reject, plus provenance. Written by
/// <see cref="MediaTypeNames.Application.Audit.RejectCapture"/> into the
/// <c>seldon_lattice_rejects</c> dataset in this documented JSONL schema.
/// </summary>
public sealed record RejectEpisode
{
    /// <summary>Session the failing node belongs to.</summary>
    [JsonPropertyName("session_id")]
    public string SessionId { get; init; } = string.Empty;

    /// <summary>Module (framework) the session ran under, e.g. "psychology".</summary>
    [JsonPropertyName("module")]
    public string Module { get; init; } = string.Empty;

    /// <summary>The session's root question the node failed to stay anchored to.</summary>
    [JsonPropertyName("root_question")]
    public string RootQuestion { get; init; } = string.Empty;

    /// <summary>Ascent level of the failing node.</summary>
    [JsonPropertyName("level")]
    public int Level { get; init; }

    /// <summary>Agent (speaker) that produced the failing content; empty for question nodes.</summary>
    [JsonPropertyName("agent")]
    public string AgentId { get; init; } = string.Empty;

    /// <summary>One or more <see cref="RejectFlags"/> reasons, sorted, deduplicated.</summary>
    [JsonPropertyName("flags")]
    public string[] Flags { get; init; } = [];

    /// <summary>The offending content (trimmed, length-bounded by options).</summary>
    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    /// <summary>CID of the stored node the episode was derived from.</summary>
    [JsonPropertyName("node_cid")]
    public string NodeCid { get; init; } = string.Empty;

    /// <summary>When the underlying node was created.</summary>
    [JsonPropertyName("occurred_at")]
    public DateTime OccurredAt { get; init; }

    /// <summary>Security-threat reason when the security flag is present.</summary>
    [JsonPropertyName("security_reason")]
    public string? SecurityThreatReason { get; init; }

    /// <summary>Tensions that survived mediation, carried on the synthesis node.</summary>
    [JsonPropertyName("unresolved_tensions")]
    public string[]? UnresolvedTensions { get; init; }

    /// <summary>Intensity of unresolved tensions (0.0-1.0), if measured.</summary>
    [JsonPropertyName("tension_intensity")]
    public float? TensionIntensity { get; init; }
}