using System.Net.Mime;

namespace SeldonLattice.Domain.Options;

/// <summary>
/// Configuration for <see cref="MediaTypeNames.Application.Audit.RejectCapture"/> - export
/// of the engine's own gate episodes to the LoRA training datasets (Reasoning Integrity
/// Layer §S4 / row 4). Mapped from the "RejectCapture" configuration section.
/// </summary>
public class RejectCaptureOptions
{
    /// <summary>
    /// Default output root for captured datasets. `rejects.jsonl` is written to
    /// <c>&lt;OutputDirectory&gt;</c> ("seldon_lattice_rejects") and claims to
    /// sibling "seldon_lattice_claims".
    /// </summary>
    public string OutputDirectory { get; set; } = "lora-training/data-generation/datasets/seldon_lattice_rejects";

    /// <summary>Maximum chars of node content kept in an episode (keeps JSONL lean).</summary>
    public int MaxContentChars { get; set; } = 4000;

    /// <summary>
    /// Re-derive off-the-root-theme episodes by embedding each node's content against the
    /// session root question (mirrors the runtime relevance gate). Requires an embedding
    /// service. Disable to only capture attribute-derived flags (security/sustained/low-ascent).
    /// </summary>
    public bool EnableDriftScan { get; set; } = true;

    /// <summary>
    /// Cosine floor below which a node's content is treated as off-the-root-theme. Matches
    /// the default of <c>QualityGateOptions.OnThemeRelevanceThreshold</c>.
    /// </summary>
    public double OnThemeSimilarityFloor { get; set; } = 0.15;

    /// <summary>
    /// H3 ascent-quality floor: synthesis nodes with <c>AscentScore</c> below this are
    /// captured with the <c>low_ascent</c> flag. 0 disables low-ascent capture.
    /// </summary>
    public float LowAscentThreshold { get; set; } = 0.10f;

    /// <summary>Whether to also export claims-with-support as the "structure-abiding" positives.</summary>
    public bool ExportClaims { get; set; } = false;
}