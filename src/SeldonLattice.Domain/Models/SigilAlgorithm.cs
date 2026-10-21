namespace SeldonLattice.Domain.Models;

/// <summary>
/// Available sigil generation algorithms. Each algorithm interprets a CID hash
/// to produce a distinct geometric pattern for cryptographic hash visualization.
/// </summary>
public enum SigilAlgorithm
{
    /// <summary>Directional vector random walk from hash-derived directions.</summary>
    VectorWalk,
    /// <summary>Polar coordinate spiral with hash-derived parameters.</summary>
    PolarSpiral,
    /// <summary>Smooth Bezier curves between hash-derived control points.</summary>
    BezierCurves,
    /// <summary>Voronoi diagram from hash-derived seed points.</summary>
    Voronoi,
    /// <summary>Radially symmetric pattern with hash-derived sectors.</summary>
    RadialSymmetry,
    /// <summary>Lissajous curve with hash-derived frequency ratios.</summary>
    Lissajous,
    /// <summary>Mandala pattern with hash-derived rotational symmetry.</summary>
    Mandala,
    /// <summary>Recursive fractal tree with hash-derived angles.</summary>
    FractalTree,
}