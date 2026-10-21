using SeldonLattice.Domain.Models.Autopilot;

namespace SeldonLattice.Domain.Interfaces;

/// <summary>
/// Append-only, tamper-evident record of autopilot parameter mutations. Entries must be
/// hash-chained (each record links the previous entry's digest) so that any retroactive
/// edit is detectable; verification is responsibility of the implementation.
/// </summary>
public interface IAutopilotLedger
{
    Task AppendAsync(AutopilotMutation mutation, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AutopilotMutation>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Re-hashes the chain and reports whether every entry links correctly.</summary>
    Task<bool> VerifyIntegrityAsync(CancellationToken cancellationToken = default);
}