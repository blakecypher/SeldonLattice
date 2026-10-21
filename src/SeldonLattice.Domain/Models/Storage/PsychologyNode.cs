using MessagePack;
using SeldonLattice.Domain.Models.Agents;

namespace SeldonLattice.Domain.Models.Storage;

/// <summary>
/// Extended DialecticalNode for psychology module with additional metadata.
/// Includes information about mediators, moderator type, and psychological tensions.
///</summary>
[MessagePackObject]
public record PsychologyNode : DialecticalNode
{
    /// <summary>
    /// If this is a mediation node, which mediators contributed?
    /// Example: ["reich", "horney"] for dual mediation.
    ///</summary>
    [Key(100)] public string[]? MediatorContributors { get; init; }

    /// <summary>
    /// If this is a moderator synthesis node, which moderator was used?
    /// Values: "nietzsche", "schopenhauer", or null if not a synthesis node.
    ///</summary>
    [Key(101)] public string? ModeratorType { get; init; }

    /// <summary>
    /// Key psychological tensions that were being synthesized.
    /// Example: ["universal vs. personal", "symbolic vs. mechanistic"]
    ///</summary>
    [Key(103)] public string[]? PsychologicalTensions { get; init; }

    /// <summary>
    /// Context from mediation that led to this synthesis.
    /// Contains insights from both mediators (Reich and Horney).
    ///</summary>
    [Key(104)] public MediationContext? MediationContext { get; init; }

    /// <summary>
    /// Whether the ascension to the next theme was validated by the ascension gate.
    /// False = forced ascension (couldn't find valid generalization).
    ///</summary>
    [Key(105)] public bool AscensionValidated { get; init; } = true;

    // Note: ModeratorSelectionConfidence is inherited from DialecticalNode
    // (Key(15)). Defining it here would conflict with the inherited member
    // under MessagePack's source generator; the inherited field is the one
    // populated by the synthesis-storage path.
}
