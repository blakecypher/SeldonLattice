namespace SeldonLattice.Domain.Models.Autopilot;

/// <summary>
/// A proposed or applied parameter mutation of the autopilot (Autopilot Telemetry). Every
/// correction the system makes about itself is recorded here so its self-governance is
/// as auditable as the debates it supervises. Entries are hash-chained by the ledger
/// implementation and (Phase 5 of the Autopilot design) ML-DSA signed.
/// </summary>
public sealed record AutopilotMutation
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>Tunable surface being corrected: "QualityGates", "ModeratorSelector", ….</summary>
    public string Component { get; init; } = string.Empty;

    /// <summary>Setting key, e.g. "OnThemeRelevanceThreshold".</summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>Module the mutation targets ("all" for the shared global default).</summary>
    public string Module { get; init; } = "all";

    public string? OldValue { get; init; }

    public string NewValue { get; init; } = string.Empty;

    /// <summary>The signal (kind + value) that triggered this correction.</summary>
    public string ReasonSignal { get; init; } = string.Empty;

    /// <summary>True while the autopilot only records what it WOULD do. False once
    /// auto-approval is enabled (Phase 5) and the change is live.</summary>
    public bool DryRun { get; init; } = true;
}