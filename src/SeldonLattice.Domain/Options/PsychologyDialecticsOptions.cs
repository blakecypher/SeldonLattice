namespace SeldonLattice.Domain.Options;

/// <summary>
/// Configuration options for the psychology module dialectical ascent.
/// Supports the 2-tier concentric psychology lattice:
///   Layer I: Terrestrial (Nietzsche/Dostoevsky, Frankl synthesis, Jung container)
///   Layer II: Modern (Baudrillard/Weil, Heidegger synthesis, Jung container, Freud subnode)
/// </summary>
public class PsychologyDialecticsOptions : DialecticsOptions
{
    /// <summary>
    /// Strategy for mediator selection and invocation.
    /// "SingleAuto" = One mediator per turn, selected based on tension type
    /// "DualParallel" = Both mediators every turn (legacy, bloats payload)
    /// "DualSequential" = Both mediators sequentially (legacy)
    /// Default: "SingleAuto"
    /// </summary>
    public string MediatorStrategy { get; set; } = "SingleAuto";

    /// <summary>
    /// Whether mediator responses are included in the main debate equilibrium check.
    /// Default: true (mediators contribute to equilibrium detection)
    /// </summary>
    public bool MediatorsParticipateInEquilibrium { get; set; } = true;

    /// <summary>
    /// Minimum aporia score (0.0-1.0) for the debate to be considered productively tense.
    /// When Jung's aporia evaluation falls below this threshold, the Freud subnode is
    /// activated to inject shadow material and destabilize premature consensus.
    /// Default: 0.35
    /// </summary>
    public float AporiaShadowThreshold { get; set; } = 0.35f;

    /// <summary>
    /// Keywords that indicate Layer I (Terrestrial/Existential) themes.
    /// Triggers Nietzsche/Dostoevsky dialectic with Frankl synthesis.
    /// </summary>
    public string[] LayerIKeywords { get; set; } =
    [
        "existential", "meaning", "suffering", "will", "power", "love",
        "freedom", "choice", "despair", "hope", "creation", "sacrifice",
        "overcoming", "amor", "fati", "empathy", "redemptive", "crisis",
        "dignity", "responsibility", "purpose", "logotherapy"
    ];

    /// <summary>
    /// Keywords that indicate Layer II (Modern/Depth/Hyperreal) themes.
    /// Triggers Baudrillard/Weil dialectic with Heidegger synthesis.
    /// </summary>
    public string[] LayerIIKeywords { get; set; } =
    [
        "simulation", "hyperreality", "virtual", "digital", "technology",
        "attention", "presence", "embodiment", "dwelling", "enframing",
        "gestell", "aletheia", "being", "authentic", "shadow", "psyche",
        "unconscious", "depth", "archetype", "collective", "subterranean",
        "detachment", "dissociation", "real", "consciousness"
    ];

    /// <summary>
    /// Keywords that indicate embodied/somatic tension - triggers mediator selection.
    /// Used when MediatorStrategy is "SingleAuto".
    /// </summary>
    public string[] EmbodiedTensionKeywords { get; set; } =
    [
        "body", "somatic", "embodied", "armor", "tension", "breathing", "muscle",
        "pelvic", "thoracic", "ocular", "oral", "cervical", "diaphragmatic",
        "contraction", "expansion", "charge", "discharge", "organismic", "vitality",
        "freeze", "flow", "rigidity", "posture", "sensation"
    ];

    /// <summary>
    /// Keywords that indicate relational/cultural tension - triggers secondary mediator selection.
    /// </summary>
    public string[] RelationalTensionKeywords { get; set; } =
    [
        "relational", "cultural", "wound", "trauma", "shame", "neurotic", "strategy",
        "toward", "against", "away", "compliance", "detachment", "exploitation",
        "idealized", "anxiety", "connection", "intimacy", "betrayal", "abandonment",
        "rejection", "helplessness", "hostility", "safety", "society", "interpersonal"
    ];

    /// <summary>
    /// Minimum winner score (0.0-1.0) for a moderator selection to be reported as confident.
    /// Default: 0.30
    /// </summary>
    public float ModeratorSelectionThreshold { get; set; } = 0.30f;

    /// <summary>
    /// Whether to combine keyword and embedding signals via Reciprocal Rank Fusion (RRF).
    /// Default: false (keyword presence only)
    /// </summary>
    public bool UseEmbeddingScoring { get; set; } = false;

    /// <summary>
    /// RRF damping constant. Default: 60f
    /// </summary>
    public float RrfConstant { get; set; } = 60f;

    /// <summary>
    /// Whether to use adaptive intensity based on mediation complexity.
    /// Default: true
    /// </summary>
    public bool AdaptiveIntensityEnabled { get; set; } = true;

    /// <summary>
    /// Whether mediation fires each dialectician turn (inline) or only on stalemate.
    /// Default: false (stalemate-only)
    /// </summary>
    public bool PerTurnMediationEnabled { get; set; } = true;

    /// <summary>
    /// Keywords that indicate Nietzschean themes (power, transformation, growth).
    /// Used by ModeratorSelectorService to score theme appropriateness.
    /// </summary>
    public string[] NietzscheKeywords { get; set; } =
    [
        "power", "creation", "growth", "transcendence", "will",
        "transformation", "becoming", "vitality", "overcoming",
        "flourishing", "revaluation", "ascension", "strength"
    ];

    /// <summary>
    /// Keywords that indicate Schopenhauerian themes (suffering, acceptance, wisdom).
    /// Kept for backward compatibility with existing moderator selector.
    /// </summary>
    public string[] SchopenhauerKeywords { get; set; } =
    [
        "suffering", "compassion", "acceptance", "resignation", "wisdom",
        "meaning in suffering", "ascetic", "denial", "universal will",
        "resignation to fate", "mitigation of suffering", "transcendence through acceptance"
    ];
}

    
