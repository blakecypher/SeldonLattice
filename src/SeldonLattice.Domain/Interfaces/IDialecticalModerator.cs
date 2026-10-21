using SeldonLattice.Domain.Models.Cycle;
using SeldonLattice.Domain.Models.Results;

namespace SeldonLattice.Domain.Interfaces;

/// <summary>
/// Extended moderator contract for agents that drive the full dialectical ascent
/// cycle: synthesizing a metaphor from the debate, proposing the next inquiry
/// theme, evaluating whether that theme genuinely generalises the root question,
/// and generating targeted interjections to break stalemates.
///
/// <see cref="IModerator"/> covers the basic synthesis path used by most moderators.
/// <see cref="IDialecticalModerator"/> adds the four methods required by the
/// full ascent path so that <see cref="DialecticalAscentServiceBase"/> never
/// needs to cast to a concrete agent type.
/// </summary>
public interface IDialecticalModerator : IModerator
{
    /// <summary>
    /// Generates a synthesis metaphor from the two dialecticians' final texts,
    /// selecting an image domain not already used this session.
    /// </summary>
    Task<MetaphorResult> SynthesizeMetaphorAsync(
        string theme,
        string dialectician1Reply,
        string dialectician2Reply,
        string phase,
        IReadOnlyList<string> forbiddenDomains,
        CancellationToken ct = default,
        int maxRetries = 2);

    /// <summary>
    /// Proposes the next dialectical inquiry theme, ascending from the current
    /// synthesis toward a more general formulation of the root question.
    /// </summary>
    Task<NextThemeResult> SynthesizeNextThemeAsync(
        string metaphor,
        string currentTheme,
        string rootTheme,
        string? priorRejectionFeedback = null,
        CancellationToken ct = default);

    /// <summary>
    /// Evaluates whether a candidate next-theme genuinely generalises from the
    /// current level's inquiry rather than drifting to an unrelated topic.
    /// </summary>
    Task<AscensionVerdict> EvaluateAscensionAsync(
        string rootTheme,
        string candidateTheme,
        string connectiveSentence,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a targeted interjection in the moderator's own persona to break
    /// a detected stalemate. The interjection is injected into both dialecticians'
    /// histories so the next turn is forced off the stuck track.
    /// </summary>
    /// <param name="signal">The stalemate type that was detected.</param>
    /// <param name="theme">The current debate theme.</param>
    /// <param name="dialectician1Text">Most recent text from dialectician 1.</param>
    /// <param name="dialectician2Text">Most recent text from dialectician 2.</param>
    /// <param name="d1Name">Display name of dialectician 1 (used in the prompt).</param>
    /// <param name="d2Name">Display name of dialectician 2 (used in the prompt).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string> GenerateInterjectionAsync(
        StalemateSignal signal,
        string theme,
        string dialectician1Text,
        string dialectician2Text,
        string d1Name,
        string d2Name,
        CancellationToken ct = default);
}
