using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SeldonLattice.Domain.Interfaces;
using SeldonLattice.Domain.Models.Storage;
using SeldonLattice.Domain.Tests.TestSupport;
using SeldonLattice.Domain.Verification;
using SeldonLattice.Infrastructure.Signing;
using SeldonLattice.Infrastructure.Storage;
using SeldonLattice.Infrastructure.Verification;
using Xunit;

namespace SeldonLattice.Domain.Tests.Verification;

public class ExportVerifierTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static ExportVerifier Exporter(IMerkleStore store, IPqcSigner signer, VerificationPolicy? policy = null) =>
        new(store, signer, NullLogger<ExportVerifier>.Instance, policy);

    private static async Task<(FasterMerkleStore Store, IPqcSigner Signer)> SeededStoreAsync(TempStoreDirectory dir)
    {
        var signer = new MlDsaSigner();
        var store = new FasterMerkleStore(dir.Path);

        var parent = NodeFactory.Dialectical("s1", "plato");
        var parentCid = (await store.PutBlobAsync(parent)).ToString();
        await store.PutSignatureAsync(await signer.SignAsync("plato", parent.Serialize(), Ct));

        var child = NodeFactory.Dialectical("s1", "aristotle", level: 2, parentHash: parentCid);
        await store.PutBlobAsync(child);
        await store.PutSignatureAsync(await signer.SignAsync("aristotle", child.Serialize(), Ct));

        var audit = NodeFactory.Audit("s1", "plato", parentCid);
        await store.PutBlobAsync(audit);
        await store.PutSignatureAsync(await signer.SignAsync("plato", audit.Serialize(), Ct));

        return (store, signer);
    }

    [Fact]
    public async Task CleanExport_Passes()
    {
        using var dir = new TempStoreDirectory();
        var (store, signer) = await SeededStoreAsync(dir);
        await using var _ = store;

        var nodes = await Exporter(store, signer).BuildExportNodesAsync("s1", Ct);
        var report = await Exporter(store, signer).VerifyExportAsync("s1", nodes, ct: Ct);

        Assert.True(report.Passed);
        Assert.Equal(3, report.NodesChecked);
        Assert.Equal(3, report.SignaturesVerified);
    }

    [Fact]
    public async Task ExportOrdering_IsDeterministic()
    {
        using var dir = new TempStoreDirectory();
        var (store, signer) = await SeededStoreAsync(dir);
        await using var _ = store;

        var exporter = Exporter(store, signer);
        var first = await exporter.BuildExportNodesAsync("s1", Ct);
        var second = await exporter.BuildExportNodesAsync("s1", Ct);

        Assert.Equal(
            first.Select(node => node.Cid),
            second.Select(node => node.Cid));
    }

    [Fact]
    public async Task TamperedExportBytes_Fail()
    {
        using var dir = new TempStoreDirectory();
        var (store, signer) = await SeededStoreAsync(dir);
        await using var _ = store;

        var exporter = Exporter(store, signer);
        var nodes = await exporter.BuildExportNodesAsync("s1", Ct);
        var target = nodes.First(node => node.Kind == "dialectical");

        var raw = Convert.FromBase64String(target.BlobBase64);
        var from = Encoding.UTF8.GetBytes(NodeFactory.OriginalContent);
        var to = Encoding.UTF8.GetBytes(NodeFactory.TamperedContent);
        var offset = raw.AsSpan().IndexOf(from);
        Assert.True(offset >= 0);
        to.AsSpan().CopyTo(raw.AsSpan(offset));

        var tampered = nodes
            .Select(node => node.Cid == target.Cid ? node with { BlobBase64 = Convert.ToBase64String(raw) } : node)
            .ToList();

        var report = await exporter.VerifyExportAsync("s1", tampered, ct: Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("content.id-mismatch"));
        Assert.True(report.HasCode("signature.invalid"));
    }

    [Fact]
    public async Task RelabelledExportNode_Fails()
    {
        using var dir = new TempStoreDirectory();
        var (store, signer) = await SeededStoreAsync(dir);
        await using var _ = store;

        var exporter = Exporter(store, signer);
        var nodes = await exporter.BuildExportNodesAsync("s1", Ct);
        var other = nodes.First(node => node.Kind == "claim" || node.Kind == "dialectical");
        var relabelled = nodes
            .Select(node => node.Cid == other.Cid ? node with { Cid = "sha3-256-not-a-real-identifier" } : node)
            .ToList();

        var report = await exporter.VerifyExportAsync("s1", relabelled, ct: Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("content.id-mismatch"));
    }

    [Fact]
    public async Task MissingSignature_FailsUnderStrictPolicy()
    {
        using var dir = new TempStoreDirectory();
        var (store, signer) = await SeededStoreAsync(dir);
        await using var _ = store;

        var exporter = Exporter(store, signer);
        var nodes = await exporter.BuildExportNodesAsync("s1", Ct);
        var unsigned = nodes.Select(node => node with { Signature = null }).ToList();

        var report = await exporter.VerifyExportAsync("s1", unsigned, ct: Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("signature.missing"));
    }

    [Fact]
    public async Task SignatureBoundToAnotherNode_Fails()
    {
        using var dir = new TempStoreDirectory();
        var (store, signer) = await SeededStoreAsync(dir);
        await using var _ = store;

        var exporter = Exporter(store, signer);
        var nodes = await exporter.BuildExportNodesAsync("s1", Ct);
        var first = nodes[0];
        var second = nodes[1];

        var tampered = nodes
            .Select(node => node.Cid == second.Cid ? node with { Signature = first.Signature } : node)
            .ToList();

        var report = await exporter.VerifyExportAsync("s1", tampered, ct: Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("signature.bound-to-other-node"));
    }

    [Fact]
    public async Task RemovedNode_FailsAgainstAnchoredRoot()
    {
        using var dir = new TempStoreDirectory();
        var (store, signer) = await SeededStoreAsync(dir);
        await using var _ = store;

        var exporter = Exporter(store, signer);
        var nodes = await exporter.BuildExportNodesAsync("s1", Ct);

        var clean = await exporter.VerifyExportAsync("s1", nodes, ct: Ct);
        Assert.True(clean.Passed);

        // Drop a node: the export is still internally consistent, so only the anchored
        // root reveals that the session is no longer the one that was recorded.
        var truncated = nodes.Skip(1).ToList();
        var report = await exporter.VerifyExportAsync("s1", truncated, clean.SessionRoot, Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("root.mismatch"));
    }

    [Fact]
    public async Task ExportSignedByUnpinnedKey_IsRejected()
    {
        using var dir = new TempStoreDirectory();
        var trusted = new MlDsaSigner();
        var attacker = new MlDsaSigner();

        await using var store = new FasterMerkleStore(dir.Path);
        var node = NodeFactory.Dialectical("s1", "plato");
        await store.PutBlobAsync(node);
        var forged = await attacker.SignAsync("plato", node.Serialize(), Ct);
        await store.PutSignatureAsync(forged);

        var nodes = await Exporter(store, attacker).BuildExportNodesAsync("s1", Ct);

        // Sanity: the export verifies cleanly when no keys are pinned.
        var unpinned = await Exporter(store, attacker).VerifyExportAsync("s1", nodes, ct: Ct);
        Assert.True(unpinned.Passed);

        var policy = new VerificationPolicy
        {
            TrustedSignerPublicKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                (await trusted.SignAsync("plato", node.Serialize(), Ct)).PublicKey,
            },
        };

        var report = await Exporter(store, trusted, policy).VerifyExportAsync("s1", nodes, ct: Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("signature.untrusted-key"));
    }
}
