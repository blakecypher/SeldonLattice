namespace SeldonLattice.Domain.Options;

/// <summary>
/// Scoring mode for the H3 ascent-quality objective. See
/// <see cref="DialecticsOptions.AscentScorerMode"/> and <c>docs/E3_RESULTS.md</c>.
/// </summary>
public enum AscentScorerMode
{
    /// <summary>Legacy 0.5·betweenness + 0.5·advance blend (E3: non-discriminating, inverted on pole restatement).</summary>
    Legacy = 0,

    /// <summary>Spec-conformant: advance as objective, betweenness ≥ floor as hard constraint (E3: AUC 1.000 vs pole restatement).</summary>
    Spec = 1
}

/// <summary>
/// Configuration options for the dialectical ascent process.
/// Controls the dialogue dynamics between philosophical agents and quantum measurement integration.
/// </summary>
public class DialecticsOptions
{
    /// <summary>
    /// The user's name for agents to address them by.
    /// </summary>
    public string UserName { get; set; } = "";

    /// <summary>
    /// The threshold for statistical dialogue convergence.
    /// When the standard deviation of cosine distances over the last StabilityWindow turns
    /// falls below this value, the dialogue is considered stable enough for synthesis.
    /// Raise if equilibrium is never reached; lower it for stricter convergence.
    /// </summary>
    public float StabilityThreshold { get; set; } = 0.08f;

    /// <summary>
    /// The number of recent turns to consider when computing stability metrics.
    /// A rolling window of this size is maintained for distance and variability calculations.
    /// Smaller windows react faster but are noisier; larger windows are smoother but slower.
    /// Default is 3 turns.
    /// </summary>
    public int StabilityWindow { get; set; } = 3;

    /// <summary>
    /// Maximum number of turns before forcing synthesis, even if equilibrium isn't reached.
    /// Prevents infinite dialogue loops when convergence doesn't occur naturally.
    /// Each turn consists of one dialectician response and one counter-response.
    /// Default is 10 turns.
    /// </summary>
    public int MaxTurns { get; set; } = 10;

    /// <summary>
    /// Maximum depth of the dialectical ascent pyramid.
    /// Each level ascends to a more fundamental question derived from the previous synthesis.
    /// The process can be terminated early if a satisfactory conclusion is reached.
    /// Default is 5 levels.
    /// </summary>
    public int MaxAscentLevels { get; set; } = 5;

    /// <summary>
    /// Whether to enable quantum gate measurement for equilibrium detection.
    /// When true, uses the Quantum Elenchus Gate to measure quantum variance alongside
    /// classical stability, requiring both to be low for equilibrium.
    /// This provides an additional signal based on embedding-to-angle mappings.
    /// Default is false (classical-only detection).
    /// </summary>
    public bool UseQuantumGate { get; set; } = false;

    /// <summary>
    /// The default intensity level used by dialectician agents when none is supplied.
    /// </summary>
    public int DefaultIntensity { get; set; } = 5;

    /// <summary>
    /// Maximum number of times Socrates may interject per ascent level to break a stalemate.
    /// Once this budget is exhausted the existing hard-stop quality gates take over.
    /// Default is 3 interjections per level.
    /// </summary>
    public int MaxInterjections { get; set; } = 3;

    /// <summary>
    /// When true, a tiered dialectical pressure directive is appended to each user message
    /// delivered to Plato and Aristotle, compelling them to advance the argument rather than
    /// restate or agree. The directive escalates from mild → medium → hard as turns increase
    /// and quality scores drop. The directive is embedded in the LLM history only - it does
    /// NOT appear in DebateTurnEvent.Content, so the UI transcript stays clean.
    /// Default is true.
    /// </summary>
    public bool AscentPressureEnabled { get; set; } = true;

    /// <summary>
    /// Scoring mode for the H3 ascent-quality objective (<c>AscentQualityScorer</c>).
    /// <para><b>Legacy</b> (default): <c>clamp01(0.5·betweenness + 0.5·advance)</c>. Measured
    /// (E3, 2026-09-21, <c>docs/E3_RESULTS.md</c>) to be non-discriminating between good and
    /// degraded syntheses and INVERTED on pole restatement (AUC 0.234): the betweenness term
    /// saturates for a synthesis that simply restates a pole, so pole restatements score
    /// HIGHER than real syntheses. Kept only for continuity with existing telemetry.</para>
    /// <para><b>Spec</b>: the mode <c>REASONING_INTEGRITY_LAYER.md</c> §S3 prescribed —
    /// advance is the continuous objective, betweenness is a hard constraint (score is zeroed
    /// when <c>betweenness &lt; AscentBetweennessFloor</c>). Separates real syntheses from
    /// pole restatements perfectly in E3 (AUC 1.000, d +3.77).</para>
    /// Default is Legacy until cross-module validation completes (E3 limitation: n=8,
    /// one psychology session); flip to Spec after re-deriving <c>LowAscentThreshold</c>.
    /// </summary>
    public AscentScorerMode AscentScorerMode { get; set; } = AscentScorerMode.Legacy;

    /// <summary>
    /// Betweenness floor used as the hard constraint in <see cref="AscentScorerMode.Spec"/>.
    /// A synthesis whose betweenness (cosine to the pole midpoint) falls below this floor is
    /// not grounded in the poles at all and scores 0 regardless of advance. Default 0.60,
    /// the value validated in E3 (<c>docs/E3_RESULTS.md</c>).
    /// </summary>
    public float AscentBetweennessFloor { get; set; } = 0.60f;
    
    /// <summary>
    /// Minimum average NewArgumentCount (summed across both agents per turn, averaged over
    /// StabilityWindow turns) required to treat a stable-embedding-distance state as productive
    /// "renewal" rather than stagnant "saturation"/"crisis". Prevents genuinely novel, high-content
    /// debates from being trapped purely because both agents share vocabulary within one theme.
    /// Default is 1.0.
    /// </summary>
    public float NoveltyThreshold { get; set; } = 1.0f;

    /// <summary>
    /// Maximum number of times the Socratic ascension gate (SocratesAgent.EvaluateAscensionAsync)
    /// may reject a candidate NextTheme and ask for a redo before the ascent gives up. When the
    /// gate exhausts its retries the level produces a "Sustained" outcome - the current theme is
    /// carried forward with no forced next theme (see DialogueLevelResult.IsSustained) - unless
    /// a moderator selector reported confidence at or above SustainedConfidenceThreshold, in which
    /// case the last candidate is accepted anyway and flagged via AscensionValidated = false so
    /// downstream consumers can surface it for review rather than silently drifting.
    /// Total candidates evaluated per level is at most this value + 1.
    /// Default is 2 (i.e. up to 3 attempts total per level).
    /// </summary>
    public int MaxAscensionGateRetries { get; set; } = 2;

    /// <summary>
    /// Maximum number of times the synthesis metaphor generation may be retried when
    /// the result is too similar to a prior synthesis (resonance above threshold) or
    /// reuses a forbidden image domain. Each retry feeds the forbidden-domains list
    /// back in so the LLM is forced to pick a genuinely different image domain.
    /// Default is 2 (i.e. up to 3 attempts total per level).
    /// </summary>
    public int MaxSynthesisRetries { get; set; } = 2;

    /// <summary>
    /// Cosine-similarity threshold for rejecting a synthesis metaphor as too similar
    /// to a prior one. When ComputeResonance() returns a value at or above this
    /// threshold, the metaphor is considered a near-repeat and retried with a broader
    /// forbidden-domains list. Tune against real ResonanceScore values from your
    /// socratic_synthesis nodes - set near whatever value separates "different metaphor,
    /// same idea" from "genuinely new" in your actual run history.
    /// Default is 0.55f.
    /// </summary>
    public float SynthesisResonanceThreshold { get; set; } = 0.55f;

    /// <summary>
    /// Minimum variability (absolute change in cosine similarity between turns)
    /// required to consider the dialogue as genuinely evolving rather than stagnant.
    /// Below this threshold the stability check treats the dialogue as converged.
    /// Default is 0.01f.
    /// </summary>
    public float VariabilityMinimum { get; set; } = 0.01f;

    /// <summary>
    /// Threshold for quantum variance (standard deviation of p_+ measurements)
    /// below which the quantum gate considers the state coherent enough for equilibrium.
    /// Default is 0.01.
    /// </summary>
    public double QuantumVarianceThreshold { get; set; } = 0.01;

    /// <summary>
    /// Number of quantum measurement shots per equilibrium check when the quantum
    /// gate is enabled. Higher values reduce noise at the cost of compute.
    /// Default is 50.
    /// </summary>
    public int QuantumGateShots { get; set; } = 50;

    /// <summary>
    /// Maximum number of recent synthesis embeddings retained for resonance comparison.
    /// Controls how far back the system looks when checking if a new synthesis metaphor
    /// is too similar to a prior one.
    /// Default is 10.
    /// </summary>
    public int SynthesisMemoryWindow { get; set; } = 10;

    /// <summary>
    /// Maximum number of recent turn pairs to retain in each dialectician's chat history.
    /// Older turns are trimmed to reduce LLM context size and improve inference speed.
    /// The system prompt (index 0) and ascent context (index 1, if present) are always preserved.
    /// Each turn pair consists of 2 messages: a user message (opponent's text) and an assistant message (own response).
    /// Mediation messages within the retained window are also preserved.
    /// Default is 3 (retaining the last 3 turn pairs = 6 messages plus potential mediation).
    /// </summary>
    public int MaxContextTurns { get; set; } = 3;

    /// <summary>
    /// Maximum number of previous ascent levels to include in the ascent context.
    /// When > 0, only the most recent N levels' syntheses are carried forward.
    /// When 0 or negative, all previous levels are included (legacy behavior).
    /// Default is 1 (fixed-context: only the immediate previous round's synthesis).
    /// </summary>
    public int MaxAscentContextLevels { get; set; } = 1;

    /// <summary>
    /// Intensity level used when enhancing prompts during saturation/crisis phase boost.
    /// Default is 10.
    /// </summary>
    public int SaturationBoostIntensity { get; set; } = 10;

    /// <summary>
    /// Number of top citations to fetch from the citation provider for each synthesis node.
    /// Default is 5.
    /// </summary>
    public int CitationTopN { get; set; } = 5;

    /// <summary>
    /// Threshold for variability below which a stable dialogue is classified as "crisis" phase
    /// rather than "renewal" in CyclePhaseDetector.
    /// Default is 0.1.
    /// </summary>
    public float VariabilityCrisisThreshold { get; set; } = 0.1f;

    /// <summary>
    /// Minimum turn count before a stable dialogue can be classified as "crisis" phase.
    /// Prevents premature crisis classification in early turns.
    /// Default is 6.
    /// </summary>
    public int CrisisMinTurns { get; set; } = 6;

    /// <summary>
    /// Maximum turn count for "growth" phase classification. After this many turns,
    /// a non-stable dialogue transitions from growth to saturation.
    /// Default is 4.
    /// </summary>
    public int GrowthMaxTurns { get; set; } = 4;

    /// <summary>
    /// Threshold for velocity (rate of change between consecutive turns' cosine distance) below which
    /// the dialogue tension is considered to have plateaued (velocity vector calculus, Primitive 2.1).
    /// Default is 0.05.
    /// </summary>
    public float VelocityPlateauThreshold { get; set; } = 0.05f;

    /// <summary>
    /// Lower absolute distance threshold when velocity has plateaued to classify the state
    /// as "saturation" (lexical looping / circular reasoning).
    /// Default is 0.20.
    /// </summary>
    public float VelocitySaturationThreshold { get; set; } = 0.20f;

    /// <summary>
    /// Upper absolute distance threshold when velocity has plateaued to classify the state
    /// as "crisis" (structural deadlock / talking past each other).
    /// Default is 0.60.
    /// </summary>
    public float VelocityCrisisThreshold { get; set; } = 0.60f;

    /// <summary>
    /// Threshold above which a response's similarity to its own previous response
    /// triggers a pressure directive ("your last response was very similar").
    /// Default is 0.85.
    /// </summary>
    public float SimilarityToPreviousHigh { get; set; } = 0.85f;

    /// <summary>
    /// Threshold above which a response's ceremony score triggers a pressure directive
    /// ("focus on substance rather than rhetorical flourishes").
    /// Default is 2.0.
    /// </summary>
    public float CeremonyScoreHigh { get; set; } = 2.0f;

    /// <summary>
    /// Intensity level at or above which the "MAXIMUM intensity" enhanced prompt is used
    /// in dialectician CreateHistory methods.
    /// Default is 10.
    /// </summary>
    public int EnhancedPromptIntensityThreshold { get; set; } = 10;

    /// <summary>
    /// Maximum character length for dialectician response snippets used in
    /// SocratesAgent interjection prompts.
    /// Default is 800.
    /// </summary>
    public int InterjectionSnippetMaxLength { get; set; } = 800;

    /// <summary>
    /// Default TensionIntensity value returned by mediators when the LLM response
    /// cannot be parsed.
    /// Default is 0.5.
    /// </summary>
    public float DefaultTensionIntensity { get; set; } = 0.5f;

    /// <summary>
    /// Threshold below which a dynamic moderator-selector confidence counts as "low
    /// confidence" when deciding whether a level that exhausted its Socratic ascension
    /// gate retries should produce a "Sustained" outcome (declining to manufacture a
    /// next theme) instead of forcing the last rejected candidate through.
    /// Modules without a moderator selector (e.g. classical) always trigger Sustained
    /// on gate exhaustion, since there is no confidence signal to consider.
    /// Default is 0.5.
    /// </summary>
    public float SustainedConfidenceThreshold { get; set; } = 0.5f;

    /// <summary>
    /// Maximum character length for mediator messages before truncation in
    /// ChatHistoryExtensions.FormatForMediator.
    /// Default is 3000.
    /// </summary>
    public int MediatorMessageTruncateLength { get; set; } = 3000;

    /// <summary>
    /// Target word count for dialectician responses.
    /// Default: 80
    /// </summary>
    public int DialecticianTargetWordCount { get; set; } = 80;

    /// <summary>
    /// Maximum word count for dialectician responses.
    /// Default: 180
    /// </summary>
    public int DialecticianMaxWordCount { get; set; } = 180;

    /// <summary>
    /// Target word count for moderator synthesis responses.
    /// Default: 80
    /// </summary>
    public int ModeratorTargetWordCount { get; set; } = 80;

    /// <summary>
    /// Maximum word count for moderator synthesis responses.
    /// Default: 180
    /// </summary>
    public int ModeratorMaxWordCount { get; set; } = 180;

    /// <summary>
    /// Target word count for mediator responses.
    /// Default: 80
    /// </summary>
    public int MediatorTargetWordCount { get; set; } = 80;

    /// <summary>
    /// Maximum word count for mediator responses.
    /// Default: 180
    /// </summary>
    public int MediatorMaxWordCount { get; set; } = 180;

    /// <summary>
    /// Maximum word count for debate rules directive (the "under N words" cap).
    /// Default: 80
    /// </summary>
    public int DialecticianDebateMaxWords { get; set; } = 80;

    /// <summary>
    /// Maximum character length for moderator theme sanitization.
    /// Default: 180
    /// </summary>
    public int ModeratorThemeMaxLength { get; set; } = 180;

    /// <summary>
    /// Maximum character length for moderator connective sentence sanitization.
    /// Default: 300
    /// </summary>
    public int ModeratorConnectiveMaxLength { get; set; } = 300;

    /// <summary>
    /// When true, prior sessions' <see cref="SeldonLattice.Domain.Models.Storage.ArgumentClaimNode">claims</see>
    /// overlapping the session's root question are recalled (ClaimMemoryRetriever) and folded
    /// into each level's opening user turn as "prior adjudicated positions" grounding - the
    /// cross-session memory (Reasoning Integrity Phase 1 row 3). Retrieved claims are treated
    /// as provisional and must be engaged, not echoed. Default is false (off until observed
    /// to improve H2-validity on repeat root questions).
    /// </summary>
    public bool EnableCrossSessionMemory { get; set; } = false;

    /// <summary>
    /// Maximum number of prior-session claims recalled per session when cross-session memory
    /// is enabled. Default is 5.
    /// </summary>
    public int CrossSessionMemoryMaxClaims { get; set; } = 5;

    /// <summary>
    /// Minimum token-overlap similarity for a prior claim to be recalled when cross-session
    /// memory is enabled. Default is 0.20.
    /// </summary>
    public float CrossSessionMemoryMinSimilarity { get; set; } = 0.20f;

    /// <summary>
    /// Scoring mode for cross-session claim retrieval. TokenOverlap (default) uses
    /// deterministic Jaccard similarity; Embedding uses the provider's embedding service
    /// for cosine similarity. Embedding mode requires a registered embedding service
    /// and falls back to TokenOverlap if unavailable.
    /// </summary>
    public ClaimMemoryMode CrossSessionMemoryMode { get; set; } = ClaimMemoryMode.TokenOverlap;
}
