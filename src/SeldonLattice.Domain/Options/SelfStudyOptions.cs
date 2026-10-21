namespace SeldonLattice.Domain.Options;

/// <summary>
/// Configuration for the Autopilot Phase 3 self-study workload planner
/// (AUTOMATION_DESIGN.md §3/"self-study workload", §7 Phase 3). Maps to the
/// "AutopilotTelemetry:SelfStudy" configuration section.
/// </summary>
public class SelfStudyOptions
{
    /// <summary>Master switch for the self-study workload planner. Default off until promoted.</summary>
    public bool Enabled { get; set; }

    /// <summary>Budget rationed to self-study per day (USD); 0 disables daily rationing.
    /// Local inference (LM Studio) is free and default; this meters only cloud chat.</summary>
    public float DailyBudgetUsd { get; set; }

    /// <summary>Estimated USD cost of one self-study episode under the metered provider.</summary>
    public float EstimatedUsdPerEpisode { get; set; } = 0.02f;

    /// <summary>Nominal (well-formed) calibration sessions planned per module per day.</summary>
    public int EpisodesPerModulePerDay { get; set; } = 4;

    /// <summary>Deliberately adversarial scan probes planned per module per day
    /// (off-the-root-theme/stress content that exercises the gates → reject-capture fodder).</summary>
    public int AdversarialScansPerModulePerDay { get; set; } = 2;

    /// <summary>Optional per-module root questions. Modules without a configured bank fall
    /// back to the planner's built-in probe bank.</summary>
    public Dictionary<string, string[]> ProbeQuestions { get; set; } = new();
}