using SeldonLattice.Domain.Options;

namespace SeldonLattice.Domain.Interfaces;

/// <summary>
/// Resolves runtime engine configuration (QualityGates and Dialectics) for a given framework.
/// Values are read from the SQLite settings store (DB-first) with memory caching.
/// This replaces the static IOptions&lt;T&gt; pattern so that settings changes take effect
/// without an application restart.
/// </summary>
public interface IEngineConfigService
{
    /// <summary>
    /// Returns the effective QualityGateOptions for the given framework.
    /// DB row wins if present; otherwise returns the seeded defaults.
    /// </summary>
    Task<QualityGateOptions> GetQualityGateOptionsAsync(string framework, CancellationToken ct = default);

    /// <summary>
    /// Returns the effective DialecticsOptions for the given framework.
    /// DB row wins if present; otherwise returns the seeded defaults.
    /// For psychology/legal the returned instance will be the appropriate subclass.
    /// </summary>
    Task<DialecticsOptions> GetDialecticsOptionsAsync(string framework, CancellationToken ct = default);

    /// <summary>
    /// Invalidates the cache for the given framework so the next call re-reads from DB.
    /// Called automatically after a settings save.
    /// </summary>
    void InvalidateCache(string framework);
}
