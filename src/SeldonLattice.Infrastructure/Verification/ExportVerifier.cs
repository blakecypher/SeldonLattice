using Microsoft.Extensions.Logging;
using SeldonLattice.Domain.Interfaces;
using SeldonLattice.Domain.Models.Storage;

namespace SeldonLattice.Infrastructure.Verification;

/// <summary>
/// Builds self-verifying machine-readable node sets from the live store, and
/// verifies them offline for the `verify <export.json>` CLI and integration tests.
/// Design: each exported node carries its original Merkle-serialized bytes
/// (`BlobBase64`) and companion signature so the verifier re-derives every check
/// from those bytes alone - no store access required offline.
/// </summary>
public sealed class ExportVerifier
{
    private readonly IMerkleStore _store;
    private readonly IPqcSigner _signer;
    private readonly ILogger<ExportVerifier> _logger;

    public ExportVerifier(IMerkleStore store, IPqcSigner signer, ILogger<ExportVerifier> logger)
    {
        _store = store;
        _signer = signer;
        _logger = logger;
    }

    /// <summary>
    /// Produces the export-time list of machine-verifiable <see cref="ExportNode"/> records for a
    /// session. Called at export time; the returned list is then embedded in the JSON export
    /// and handed to <see cref="VerifyExportAsync"/> offline.
    /// </summary>
    public async Task<IReadOnlyList<ExportNode>> BuildExportNodesAsync(
        string sessionId, CancellationToken ct = default)
    {
        var nodes = new List<ExportNode>();

        var dialectical = (await _store.GetAllNodesAsync<DialecticalNode>())
            .Where(n => n.SessionId == sessionId).ToList();
        var claims = (await _store.GetAllNodesAsync<ArgumentClaimNode>())
            .Where(n => n.SessionId == sessionId).ToList();
        var auditList = await _store.GetAuditNodesAsync(sessionId)
            .ToListAsync(ct);

        foreach (var node in dialectical)
        {
            ct.ThrowIfCancellationRequested();
            var cid = node.GetCid();
            var bytes = node.Serialize();
            var sig = await _store.GetSignatureAsync(cid);
            nodes.Add(new ExportNode(
                Kind: "dialectical",
                Cid: cid.ToString(),
                Level: node.Level,
                SpeakerId: node.SpeakerId,
                Timestamp: node.Timestamp,
                BlobBase64: Convert.ToBase64String(bytes),
                Links: CollectDialecticalLinks(node),
                Signature: sig));
        }

        foreach (var node in claims)
        {
            ct.ThrowIfCancellationRequested();
            var cid = node.GetCid();
            var bytes = node.Serialize();
            var sig = await _store.GetSignatureAsync(cid);
            nodes.Add(new ExportNode(
                Kind: "claim",
                Cid: cid.ToString(),
                Level: node.Level,
                SpeakerId: null,
                Timestamp: node.Timestamp,
                BlobBase64: Convert.ToBase64String(bytes),
                Links: CollectClaimLinks(node),
                Signature: sig));
        }

        foreach (var node in auditList)
        {
            ct.ThrowIfCancellationRequested();
            var cid = node.GetCid();
            var bytes = node.Serialize();
            var sig = await _store.GetSignatureAsync(cid);
            nodes.Add(new ExportNode(
                Kind: "audit",
                Cid: cid.ToString(),
                Level: 0,
                SpeakerId: node.SpeakerId,
                Timestamp: node.Timestamp,
                BlobBase64: Convert.ToBase64String(bytes),
                Links: string.IsNullOrEmpty(node.ParentCid) ? [] : [node.ParentCid],
                Signature: sig));
        }

        return nodes;
    }

    /// <summary>
    /// Verifies a self-contained exported node set offline. No store access is needed;
    /// all checks are re-derived from the embedded bytes, CIDs, links, and signatures.
    /// </summary>
    public async Task<SessionVerificationReport> VerifyExportAsync(
        string sessionId,
        IReadOnlyList<ExportNode> nodes,
        CancellationToken ct = default)
    {
        var issues = new List<VerificationIssue>();

        // Universe = all exported cids (export is self-contained).
        var universe = new HashSet<string>(nodes.Select(n => n.Cid), StringComparer.Ordinal);

        var nodesChecked = 0;
        var signaturesVerified = 0;
        var pointerIssues = 0;
        var signatureIssues = 0;

        foreach (var node in nodes)
        {
            ct.ThrowIfCancellationRequested();
            nodesChecked++;

            // 1. CID integrity: recompute from the embedded bytes and assert match.
            var blob = Convert.FromBase64String(node.BlobBase64);
            var recomputedCid = Cid.Compute(blob);
            if (recomputedCid.ToString() != node.Cid)
            {
                pointerIssues++;
                issues.Add(new VerificationIssue(
                    node.Kind, node.Cid, VerificationSeverity.Error,
                    $"Exported CID {node.Cid} does not match recomputed {recomputedCid} - tamper evidence."));
            }

            // 2. Link resolution: every link must resolve to an exported node.
            foreach (var link in node.Links.Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                if (!universe.Contains(link))
                {
                    pointerIssues++;
                    issues.Add(new VerificationIssue(
                        node.Kind, node.Cid, VerificationSeverity.Error,
                        $"Dangling reference: {link} does not resolve to any node in the export."));
                }
            }

            // 3. Signature integrity: audit must be signed; others should be.
            if (node.Signature is null)
            {
                var severity = node.Kind == "audit"
                    ? VerificationSeverity.Error
                    : VerificationSeverity.Warning;

                if (severity == VerificationSeverity.Error)
                {
                    signatureIssues++;
                    issues.Add(new VerificationIssue(node.Kind, node.Cid, severity,
                        "Missing ML-DSA signature (audit nodes are required to be signed)."));
                }
                else
                {
                    issues.Add(new VerificationIssue(node.Kind, node.Cid, severity,
                        "Missing ML-DSA signature (node stored through a raw/legacy path)."));
                }
                continue;
            }

            signaturesVerified++;
            var valid = await _signer.VerifyAsync(node.Signature, blob, ct);
            if (!valid)
            {
                signatureIssues++;
                issues.Add(new VerificationIssue(node.Kind, node.Cid, VerificationSeverity.Error,
                    $"ML-DSA signature by {node.Signature.SignerId} failed to verify against content - " +
                    "tamper evidence."));
            }
        }

        if (nodesChecked == 0)
        {
            issues.Add(new VerificationIssue(
                "session", sessionId, VerificationSeverity.Warning,
                "No verifiable nodes found in the export."));
        }

        _logger.LogInformation(
            "Export verification for {SessionId}: {Nodes} nodes, {Errors} error issues.",
            sessionId, nodesChecked, issues.Count(i => i.Severity == VerificationSeverity.Error));

        return new SessionVerificationReport(
            sessionId,
            Passed: issues.All(i => i.Severity != VerificationSeverity.Error),
            nodesChecked,
            signaturesVerified,
            pointerIssues,
            signatureIssues,
            issues);
    }

    private static string[] CollectDialecticalLinks(DialecticalNode node)
    {
        var links = new List<string> { node.ParentHash ?? string.Empty, node.ParentPatternHash ?? string.Empty };
        if (node.ParentHashes is { Length: > 0 })
            links.AddRange(node.ParentHashes);
        return links.ToArray();
    }

    private static string[] CollectClaimLinks(ArgumentClaimNode node)
    {
        var links = new List<string>();
        if (node.ParentHashes is { Length: > 0 })
            links.AddRange(node.ParentHashes);
        if (node.SupportedBy is { Length: > 0 })
            links.AddRange(node.SupportedBy);
        if (node.Attacks is { Length: > 0 })
            links.AddRange(node.Attacks);
        return links.ToArray();
    }
}

internal static class AsyncEnumerableExtensions
{
    internal static async Task<List<T>> ToListAsync<T>(
        this IAsyncEnumerable<T> source, CancellationToken ct = default)
    {
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(ct))
            list.Add(item);
        return list;
    }
}