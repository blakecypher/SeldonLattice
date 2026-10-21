using SeldonLattice.Domain.Models.Autopilot;

namespace SeldonLattice.Domain.Interfaces;

/// <summary>
/// Append-only store for structured telemetry signals consumed by the Autopilot Telemetry
/// autopilot. Implementations may be in-memory, file-backed, or backed by IMerkleStore.
/// </summary>
public interface IAutopilotSignalStore
{
    Task AppendAsync(AutopilotSignal signal, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AutopilotSignal>> GetByModuleAsync(
        string module,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);
}