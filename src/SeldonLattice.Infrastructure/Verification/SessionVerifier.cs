using Microsoft.Extensions.Logging;
using SeldonLattice.Domain.Interfaces;
using SeldonLattice.Domain.Models.Storage;

namespace SeldonLattice.Infrastructure.Verification;

/// <summary>Severity of a single verification finding.</summary>
public enum VerificationSeverity
{
    /// <summary>Fatal integrity failure (dangling reference, failed signature, tamper evidence).</summary>
    Error,

    /// <summary>Non-fatal observation (e.g. a node stored without a signature).</summary>
    Warning
}

/// <summary>A single finding from a session integrity verification.</summary>
public sealed record VerificationIssue(
    string NodeType,
    string NodeCid,
    VerificationSeverity Severity,
    string Message);

/// <summary>
/// Result of verifying a session's reasoning chain. <see cref="Passed"/> is true only
/// when there are no Error-severity issues.
/// </summary>
public sealed record SessionVerificationReport(
    string SessionId,
    bool Passed,
    int NodesChecked,
    int SignaturesVerified,
    int PointerIssues,
    int SignatureIssues,
    IReadOnlyList<VerificationIssue> Issues);

/// <summary>
/// Verifies the cryptographic and structural integrity of a session's reasoning chain
/// (Reasoning Integrity Layer, row 5 core): every session node's content is
/// content-addressed into a recomputed CID, every cross-reference (parent, support,
/// attack, audit-parent) must resolve to an existing node, and every companion
/// ML-DSA signature must verify against the node's serialized content with a matching
/// signed CID. Verification is read-only and relies solely on re-derivation - nothing
/// on disk is trusted, so all evidence is reproducible.
/// </summary>
public interface ISessionVerifier
{
    Task<SessionVerificationReport> VerifySessionAsync(string sessionId, CancellationToken ct = default);
}

public sealed class SessionVerifier : ISessionVerifier
{
    private readonly IMerkleStore _store;
    private readonly IPqcSigner _signer;
    private readonly ILogger<SessionVerifier> _logger;

    public SessionVerifier(IMerkleStore store, IPqcSigner signer, ILogger<SessionVerifier> logger)
    {
        _store = store;
        _signer = signer;
        _logger = logger;
    }

    public async Task<SessionVerificationReport> VerifySessionAsync(string sessionId, CancellationToken ct = default)
    {
        var issues = new List<VerificationIssue>();

        // Snapshot the store-wide node universe so cross-references resolve both within
        // and across sessions (citations, reused syntheses, cross-session claims).
        var dialectical = (await _store.GetAllNodesAsync<DialecticalNode>()).ToList();
        var claims = (await _store.GetAllNodesAsync<ArgumentClaimNode>()).ToList();
        var audits = (await _store.GetAllNodesAsync<LlmAuditNode>()).ToList();

        var universe = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in dialectical) universe.Add(node.GetCid().ToString());
        foreach (var node in claims) universe.Add(node.GetCid().ToString());
        foreach (var node in audits) universe.Add(node.GetCid().ToString());

        var sessionNodes = new List<(string Kind, string Cid, string[] Links, byte[] Bytes)>();
        foreach (var node in dialectical.Where(n => n.SessionId == sessionId))
            sessionNodes.Add(("dialectical", node.GetCid().ToString(), CollectDialecticalLinks(node), node.Serialize()));
        foreach (var node in claims.Where(n => n.SessionId == sessionId))
            sessionNodes.Add(("claim", node.GetCid().ToString(), CollectClaimLinks(node), node.Serialize()));
        foreach (var node in audits.Where(n => n.SessionId == sessionId))
            sessionNodes.Add(("audit", node.GetCid().ToString(), CollectAuditLinks(node), node.Serialize()));

        var nodesChecked = 0;
        var signaturesVerified = 0;
        var pointerIssues = 0;
        var signatureIssues = 0;

        foreach (var (kind, cid, links, bytes) in sessionNodes)
        {
            ct.ThrowIfCancellationRequested();
            nodesChecked++;

            // 1. Reference integrity: every cross-reference must resolve to a node.
            foreach (var link in links)
            {
                if (string.IsNullOrWhiteSpace(link))
                    continue;
                if (!universe.Contains(link))
                {
                    pointerIssues++;
                    issues.Add(new VerificationIssue(
                        kind, cid, VerificationSeverity.Error,
                        $"Dangling reference: {link} does not resolve to any stored node."));
                }
            }

            // 2. Signature integrity: companions must exist, verify, and reference this CID.
            var signature = await _store.GetSignatureAsync(Cid.FromString(cid));
            if (signature is null)
            {
                // Audit nodes are mandated signed (see SignedMerkleStore); other session
                // nodes signed through the same spine, but raw/legacy stores may hold
                // unsigned copies - report those as warnings.
                var severity = kind == "audit"
                    ? VerificationSeverity.Error
                    : VerificationSeverity.Warning;
                if (severity == VerificationSeverity.Error)
                {
                    signatureIssues++;
                    issues.Add(new VerificationIssue(kind, cid, severity,
                        "Missing ML-DSA signature (audit nodes are required to be signed)."));
                }
                else
                {
                    issues.Add(new VerificationIssue(kind, cid, severity,
                        "Missing ML-DSA signature (node stored through a raw/legacy path)."));
                }
                continue;
            }

            signaturesVerified++;
            var valid = await _signer.VerifyAsync(signature, bytes, ct);
            if (!valid)
            {
                signatureIssues++;
                issues.Add(new VerificationIssue(kind, cid, VerificationSeverity.Error,
                    $"ML-DSA signature by {signature.SignerId} failed to verify against content - " +
                    "tamper evidence."));
            }
        }

        if (nodesChecked > 0)
        {
            _logger.LogInformation(
                "Verified {Nodes} nodes for session {SessionId}: {Errors} error issues.", nodesChecked, sessionId,
                issues.Count(i => i.Severity == VerificationSeverity.Error));
        }
        else
        {
            issues.Add(new VerificationIssue(
                "session", sessionId, VerificationSeverity.Warning,
                "No verifiable nodes found for this session."));
        }

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
        var links = new List<string>(node.ParentHashes?.Length ?? 0 + 2)
        {
            node.ParentHash ?? string.Empty,
            node.ParentPatternHash ?? string.Empty
        };
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

    private static string[] CollectAuditLinks(LlmAuditNode node) =>
        string.IsNullOrEmpty(node.ParentCid) ? [] : [node.ParentCid];
}