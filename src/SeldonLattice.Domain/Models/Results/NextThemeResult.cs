namespace SeldonLattice.Domain.Models.Results;

/// <summary>
/// Result of synthesizing the next dialectical theme from the current level's conclusion.
/// </summary>
public record NextThemeResult(string Theme, string ConnectiveSentence);