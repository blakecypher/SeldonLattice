namespace SeldonLattice.Domain.Models.Cycle;

/// <summary>
/// Rotates through rhetorical moves on a per-turn basis to ensure structural variety
/// in the dialogue, not just lexical variety.
/// </summary>
public static class MoveRotation
{
    private static readonly RhetoricalMove[] Order =
    [
        RhetoricalMove.EmpiricalCase,
        RhetoricalMove.FormalDistinction,
        RhetoricalMove.Reductio,
        RhetoricalMove.ConceptualCounterexample,
        RhetoricalMove.CrossDomainAnalogy,
    ];

    /// <summary>
    /// Gets the required rhetorical move for a given turn number.
    /// </summary>
    public static RhetoricalMove ForTurn(int turn) => Order[turn % Order.Length];

    /// <summary>
    /// Gets a human-readable instruction for the specified move to inject into the agent's prompt.
    /// </summary>
    public static string GetInstruction(RhetoricalMove move) => move switch
    {
        RhetoricalMove.EmpiricalCase => "Cite one specific, checkable historical or empirical fact (name/date/measurement) that bears on the theme.",
        RhetoricalMove.FormalDistinction => "Draw a precise distinction between two senses of a key term the other philosopher is conflating.",
        RhetoricalMove.Reductio => "Take your opponent's last claim to its logical extreme and show what breaks.",
        RhetoricalMove.CrossDomainAnalogy => "Use an analogy from a domain neither of you has invoked yet this dialogue.",
        _ => "Introduce a genuine counterexample to the current working definition.",
    };
}