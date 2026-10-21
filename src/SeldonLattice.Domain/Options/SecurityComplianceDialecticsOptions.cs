namespace SeldonLattice.Domain.Options;

/// <summary>
/// Configuration options for the Security & Compliance module dialectical ascent.
/// 2-debater + 1-mediator + 1-moderator architecture:
///   Debaters: Schneier (cybersecurity), Nissenbaum (regulatory)
///   Mediator: Felson (physical-cyber convergence)
///   Moderator: SecurityGovernanceSynthesizer (Rawls + Floridi)
/// </summary>
public class SecurityComplianceDialecticsOptions : DialecticsOptions
{
    /// <summary>
    /// Minimum evidence confidence threshold (0.0-1.0) for claims to be admitted.
    /// Claims below this threshold are flagged as "Unsubstantiated" in the GDR.
    /// Default: 0.7
    /// </summary>
    public float EvidenceConfidenceThreshold { get; set; } = 0.7f;

    /// <summary>
    /// Whether the Evidence Gate quality gate is enforced.
    /// When true, every agent claim must cite a CID from IngestionModule.
    /// Default: true
    /// </summary>
    public bool EnforceEvidenceGate { get; set; } = true;

    /// <summary>
    /// Whether the Dissent Gate quality gate is enforced.
    /// When true, at least one agent must dissent or abstain; false consensus is blocked.
    /// Default: true
    /// </summary>
    public bool EnforceDissentGate { get; set; } = true;

    /// <summary>
    /// Whether the Regulatory Gate quality gate is enforced.
    /// When true, Nissenbaum must sign off on jurisdictional coverage.
    /// Default: true
    /// </summary>
    public bool EnforceRegulatoryGate { get; set; } = true;

    /// <summary>
    /// Maximum number of dialectical turns per agent per level.
    /// Default: 6
    /// </summary>
    public int MaxTurnsPerAgent { get; set; } = 6;

    /// <summary>
    /// Whether to include MerkleDAG anchoring in the GDR output.
    /// Default: true
    /// </summary>
    public bool IncludeMerkleAnchoring { get; set; } = true;

    /// <summary>
    /// Whether to include ML-DSA-65 signatures in the GDR output.
    /// Default: true
    /// </summary>
    public bool IncludePostQuantumSignatures { get; set; } = true;
}
