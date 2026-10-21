using SeldonLattice.Domain.Interfaces;

namespace SeldonLattice.Domain.Configuration;

/// <summary>
/// Configuration that parameterizes the generic DialecticalAscentService for a specific module.
/// Each module (classical, psychology, literary, etc.) provides one of these to define
/// which agents debate, which mediators bridge, and which moderator synthesizes.
/// </summary>
public record DialecticalConfiguration
{
    public required string ModuleKey { get; init; }

    public required IDialectician Dialectician1 { get; init; }
    public required IDialectician Dialectician2 { get; init; }

    /// <summary>
    /// Zero or more mediators that analyze tension between dialecticians.
    /// Classical module: [SocratesAgent] (single mediator).
    /// Psychology module Terrestrial/Modern layers: no mediators (empty).
    /// </summary>
    public required IMediator[] Mediators { get; init; }

    /// <summary>
    /// Dynamic mediator selector, used when a single mediator should be chosen per turn
    /// based on the type of tension. Psychology module uses DefaultMediatorSelector
    /// for "SingleAuto" strategy.
    /// </summary>
    public IMediatorSelector? MediatorSelector { get; init; }

    /// <summary>
    /// Fixed moderator, used when IModeratorSelector is null.
    /// Classical module: SocratesAgent (always Socrates).
    /// Psychology module: layer-specific moderator (Frankl/Heidegger).
    /// </summary>
    public IModerator? Moderator { get; init; }

    /// <summary>
    /// Dynamic moderator selector, used when Moderator is null.
    /// Psychology module: PsychLayerModeratorSelector (routes to layer-specific moderator).
    /// </summary>
    public IModeratorSelector? ModeratorSelector { get; init; }

    public string Dialectician1NodeType { get; init; } = "reply_1";
    public string Dialectician2NodeType { get; init; } = "reply_2";
    public string SynthesisNodeType { get; init; } = "synthesis";

    /// <summary>
    /// Speaker identifier for Dialectician1, used to populate DebateTurnEvent.SpeakerId.
    /// Examples: "jung", "plato"
    /// </summary>
    public string Dialectician1SpeakerId { get; init; } = "speaker_1";

    /// <summary>
    /// Speaker identifier for Dialectician2.
    /// Examples: "freud", "aristotle"
    /// </summary>
    public string Dialectician2SpeakerId { get; init; } = "speaker_2";

    /// <summary>
    /// Speaker identifiers for each mediator, in the same order as Mediators.
    /// Used to populate DebateTurnEvent.SpeakerId for mediator events.
    /// Examples: ["reich", "horney"]
    /// </summary>
    public string[] MediatorSpeakerIds { get; init; } = [];

    /// <summary>
    /// How the mediator context should influence synthesis:
    /// "Default" = mediators run after equilibrium; their insights are fed to the moderator
    /// "Inline" = mediators participate in the debate loop (psychology module)
    /// </summary>
    public string MediatorMode { get; init; } = "Default";
}