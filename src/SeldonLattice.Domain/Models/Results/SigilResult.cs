namespace SeldonLattice.Domain.Models.Results;

/// <summary>
/// Result of sigil generation including the SVG representation and raw data.
/// </summary>
public sealed record SigilResult
{
    // The algorithm used to generate this sigil.
    public required SigilAlgorithm Algorithm { get; init; }
    // The CID that was used as input.
    public required string Cid { get; init; }
    // SVG markup for the sigil.
    public required string Svg { get; init; }
    // Width/height of the output in pixels.
    public required int Size { get; init; }
    // Hash bytes used to seed the deterministic pattern.
    public required byte[] HashBytes { get; init; }
}