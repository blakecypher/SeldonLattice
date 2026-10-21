namespace SeldonLattice.Domain.Options;

/// <summary>
/// Configuration options for the legal module dialectical ascent.
/// Extends base DialecticsOptions with legal-specific settings.
/// </summary>
public class LegalDialecticsOptions : DialecticsOptions
{
    /// <summary>
    /// Strategy for mediator selection and invocation.
    /// "SingleAuto" = One mediator per turn, selected based on rotation (uses IMediatorSelector)
    /// "SingleOConnor" = Always use Sandra Day O'Connor
    /// "SingleDarrow" = Always use Clarence Darrow
    /// "DualParallel" = Both mediators every turn (legacy, bloats payload)
    /// "DualSequential" = Both mediators sequentially (legacy)
    /// Default: "SingleAuto"
    /// </summary>
    public string MediatorStrategy { get; set; } = "SingleAuto";

    /// <summary>
    /// Whether mediators fire every turn (inline) or only on stalemate.
    /// When false, mediators only interject when a stalemate signal is raised.
    /// Default: false (stalemate-only - mediators preserve debate energy until truly stuck).
    /// </summary>
    public bool PerTurnMediationEnabled { get; set; } = false;

    /// <summary>
    /// Keywords that indicate Cardozo-style legal reasoning (precedent, balancing, wisdom).
    /// Used for future moderator selection if extended.
    /// </summary>
    public string[] CardozoKeywords { get; set; } =
    [
        "precedent", "balance", "justice", "wisdom", "equity",
        "fairness", "proportion", "reasonableness", "duty", "negligence",
        "foreseeability", "policy", "social welfare", "doctrinal"
    ];

    /// <summary>
    /// Keywords that indicate Scalia-style legal reasoning (textualism, originalism, plain meaning).
    /// Used for future moderator selection if extended.
    /// </summary>
    public string[] ScaliaKeywords { get; set; } =
    [
        "text", "plain meaning", "originalism", "statute", "legislative intent",
        "canon", "interpretation", "constitutional", "authority", "jurisdiction",
        "stare decisis", "doctrine", "bright line", "rule of law"
    ];
}
