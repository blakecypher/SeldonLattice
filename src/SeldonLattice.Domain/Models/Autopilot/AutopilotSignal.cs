namespace SeldonLattice.Domain.Models.Autopilot;

/// <summary>
/// Canonical signal kinds emitted by the engine and consumed by the Autopilot Telemetry
/// autopilot. Kind strings are stable identifiers used in telemetry windows and
/// mutation-reason records; do not rename an existing value once sessions depend on it.
/// </summary>
public static class AutopilotSignalKinds
{
    /// <summary>One relevance check performed by the on-theme gate. Value = cosine
    /// similarity of the response against the root-theme embedding.</summary>
    public const string RelevanceCheck = "relevance_check";

    /// <summary>A response was flagged off-the-root-theme (soft warn or exit). Value = similarity.</summary>
    public const string OffTopicDrift = "off_topic_drift";

    /// <summary>A level's synthesis was sustained instead of ascended. Value = the
    /// sustain confidence (0 = forced by retries, else selected-confidence).</summary>
    public const string Sustain = "sustain";

    /// <summary>H3 ascent-quality score for an accepted synthesis. Value = combined score
    /// in [0,1]; details carries the split (betweenness/advance).</summary>
    public const string AscentScore = "ascent_score";

    /// <summary>Negative-dialectic measure for an accepted synthesis: how far the synthesis
    /// holds the contradiction open rather than resolving it. Value in [0,1]; details carries
    /// the full quality split. Independent of <see cref="AscentScore"/> - a module can advance
    /// without holding tension, or hold tension without advancing.</summary>
    public const string IrreducibleTension = "irreducible_tension";

    /// <summary>Any forced dialogue exit. Value = severity (1.0 for exits, 0.5 for warnings).</summary>
    public const string Exit = "exit";

    /// <summary>A user overrode a hard-stop guard (same root theme + framework). Value = 1.0; Details = user reason.</summary>
    public const string HardStopOverride = "hard_stop_override";
}

/// <summary>
/// A timestamped, structured telemetry event produced somewhere in the engine
/// (gates, ascent services, audit middleware). The autopilot aggregates these into
/// per-module windows and derives corrective proposals from the rates.
/// </summary>
public sealed record AutopilotSignal
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>Module origin ("classical", "psychology", "governance", …).</summary>
    public string Module { get; init; } = string.Empty;

    /// <summary>One of <see cref="AutopilotSignalKinds"/>.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Numeric value of the signal (similarity, score, rate contribution, …).</summary>
    public float Value { get; init; }

    /// <summary>Optional free-form context (text snippet, node id, exit reason).</summary>
    public string? Details { get; init; }
}