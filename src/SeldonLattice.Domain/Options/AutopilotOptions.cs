using SeldonLattice.Domain.Models.Autopilot;

namespace SeldonLattice.Domain.Options;

/// <summary>
/// Configuration for the Autopilot Telemetry background daemon - the self-regulating autopilot.
/// Maps to the "AutopilotTelemetry" configuration section. See docs/AUTOMATION_DESIGN.md.
/// </summary>
public class AutopilotOptions
{
    /// <summary>Master switch for the hosted daemon. Default off until Phase 5 promote.</summary>
    public bool Enabled { get; set; }

    /// <summary>While true the autopilot only records what it would change; nothing applies.</summary>
    public bool DryRun { get; set; } = true;

    /// <summary>Seconds between control-loop ticks.</summary>
    public int CadenceSeconds { get; set; } = 3600;

    /// <summary>Sliding telemetry window (minutes) the controllers aggregate signals over.</summary>
    public int WindowMinutes { get; set; } = 60 * 24; // 24h

    /// <summary>Target share of relevance checks flagged off-the-root-theme. When the
    /// observed rate leaves [Min, Max], the T1 controller nudges OnThemeRelevanceThreshold.</summary>
    public float TargetDriftBandMin { get; set; } = 0.02f;

    public float TargetDriftBandMax { get; set; } = 0.15f;

    /// <summary>Maximum threshold delta per tick per module.</summary>
    public float ThresholdStep { get; set; } = 0.01f;

    /// <summary>Absolute bounds the T1 controller may never move a threshold past.</summary>
    public float ThresholdFloor { get; set; } = 0.10f;

    public float ThresholdCeiling { get; set; } = 0.60f;

    /// <summary>Per-module drift bands override the global ones (key = module name).</summary>
    public Dictionary<string, DriftBandOverride> ModuleBands { get; set; } = new();

    /// <summary>Minimum number of window signals before a module is considered reviewable.</summary>
    public int MinSignalsForControl { get; set; } = 30;

    /// <summary>Constitutional hard spend caps (USD); 0 = unlimited. Enforced by the usage
    /// collector; surfaced in the daily System Health Report §6 spend-to-date-vs-cap.</summary>
    public float DailySpendCap { get; set; }

    public float MonthlySpendCap { get; set; }

    /// <summary>Maximum acceptable share of level syntheses flagged as sustained theme (T2 trigger).</summary>
    public float TargetSustainBandMax { get; set; } = 0.10f;

    /// <summary>Minimum average ascent quality score in sliding window before T2 intervenes.</summary>
    public float MinAscentQualityFloor { get; set; } = 0.30f;

    /// <summary>Maximum tolerable exit/denial rate before T4 escalation triggers.</summary>
    public float MaxAcceptableDenialRate { get; set; } = 0.25f;
}

/// <summary>Optional per-module target drift band.</summary>
public sealed record DriftBandOverride(float Min, float Max)
{
    public float Min { get; init; } = Min;
    public float Max { get; init; } = Max;
}

/// <summary>
/// A proposed threshold change produced by a T1 controller.
/// </summary>
public sealed record AutopilotProposal(
    string Component,
    string Key,
    string Module,
    float OldValue,
    float NewValue,
    string ReasonSignal)
{
    public AutopilotProposal(string component, string key, string module, float oldValue, float newValue, float reasonValue)
        : this(component, key, module, oldValue, newValue, $"{AutopilotSignalKinds.RelevanceCheck}@{reasonValue:F3}") { }
}