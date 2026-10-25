using MessagePack;
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

public class SessionVerifierTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static SessionVerifier Verifier(IMerkleStore store, IPqcSigner signer, VerificationPolicy? policy = null) =>
        new(store, signer, NullLogger<SessionVerifier>.Instance, policy);

    [Fact]
    public async Task CleanSession_Passes()
    {
        using var dir = new TempStoreDirectory();
        var signer = new MlDsaSigner();
        string parentCid;

        await using (var store = new FasterMerkleStore(dir.Path))
        {
            var parent = NodeFactory.Dialectical("s1", "plato");
            parentCid = (await store.PutBlobAsync(parent)).ToString();
            await store.PutSignatureAsync(await signer.SignAsync("plato", parent.Serialize(), Ct));

            var child = NodeFactory.Dialectical("s1", "aristotle", level: 2, parentHash: parentCid);
            await store.PutBlobAsync(child);
            await store.PutSignatureAsync(await signer.SignAsync("aristotle", child.Serialize(), Ct));
        }

        await using var reopened = new FasterMerkleStore(dir.Path);
        var report = await Verifier(reopened, signer).VerifySessionAsync("s1", Ct);

        Assert.True(report.Passed);
        Assert.Equal(2, report.NodesChecked);
        Assert.Equal(2, report.SignaturesVerified);
        Assert.Equal(0, report.ContentIssues);
        Assert.Empty(report.Errors);
    }

    [Fact]
    public async Task ModifiedLeafNode_IsReportedAsError()
    {
        // Regression: a signed leaf could be rewritten on disk and still report Passed=True,
        // because the store scan discarded the key and a missing signature was only a warning.
        using var dir = new TempStoreDirectory();
        var signer = new MlDsaSigner();

        await using (var store = new FasterMerkleStore(dir.Path))
        {
            var leaf = NodeFactory.Dialectical("s1", "plato");
            await store.PutBlobAsync(leaf);
            await store.PutSignatureAsync(await signer.SignAsync("plato", leaf.Serialize(), Ct));
        }

        var rewritten = DiskTamper.ReplaceText(dir.DataFile, NodeFactory.OriginalContent, NodeFactory.TamperedContent);
        Assert.Equal(1, rewritten);

        await using var reopened = new FasterMerkleStore(dir.Path);
        var report = await Verifier(reopened, signer).VerifySessionAsync("s1", Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("content.id-mismatch"));
        Assert.Equal(1, report.ContentIssues);
        Assert.Equal(1, report.NodesChecked);
    }

    [Fact]
    public async Task UnsignedNode_FailsUnderStrictPolicy()
    {
        using var dir = new TempStoreDirectory();

        await using var store = new FasterMerkleStore(dir.Path);
        await store.PutBlobAsync(NodeFactory.Dialectical("s1", "plato"));

        var report = await Verifier(store, new MlDsaSigner()).VerifySessionAsync("s1", Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("signature.missing"));
        Assert.Equal(1, report.SignatureIssues);
    }

    [Fact]
    public async Task UnsignedNode_WarnsUnderPermissivePolicy()
    {
        using var dir = new TempStoreDirectory();

        await using var store = new FasterMerkleStore(dir.Path);
        await store.PutBlobAsync(NodeFactory.Dialectical("s1", "plato"));

        var report = await Verifier(store, new MlDsaSigner(), VerificationPolicy.Permissive)
            .VerifySessionAsync("s1", Ct);

        Assert.True(report.Passed);
        Assert.True(report.HasCode("signature.missing"));
        Assert.All(report.Issues, issue => Assert.Equal(VerificationSeverity.Warning, issue.Severity));
    }

    [Fact]
    public async Task CorruptBlob_IsReportedAsError()
    {
        // Regression: an undecodable payload was silently skipped, so a destroyed node
        // looked like a session with one fewer node and verified clean.
        using var dir = new TempStoreDirectory();
        var signer = new MlDsaSigner();

        await using (var store = new FasterMerkleStore(dir.Path))
        {
            var node = NodeFactory.Dialectical("s1", "plato");
            var cid = await store.PutBlobAsync(node);
            await store.PutSignatureAsync(await signer.SignAsync("plato", node.Serialize(), Ct));
            Assert.Equal(1, DiskTamper.CorruptPayloads(dir.DataFile, "obj:" + cid));
        }

        await using var reopened = new FasterMerkleStore(dir.Path);
        var report = await Verifier(reopened, signer).VerifySessionAsync("s1", Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("content.unreadable"));
        Assert.Equal(1, report.UnreadableBlobs);
    }

    [Fact]
    public async Task CorruptPayloadInsideIntactWrapper_IsReportedAsError()
    {
        // A scan can tell "not this type" from "this type but broken" only if the wrapper is
        // inspected. A payload corrupted behind a readable wrapper must not vanish silently.
        using var dir = new TempStoreDirectory();
        var signer = new MlDsaSigner();

        await using (var store = new FasterMerkleStore(dir.Path))
        {
            var node = NodeFactory.Dialectical("s1", "plato");
            var cid = await store.PutBlobAsync(node);
            await store.PutSignatureAsync(await signer.SignAsync("plato", node.Serialize(), Ct));
            Assert.True(DiskTamper.CorruptPayloadKeepingWrapper(dir.DataFile, "obj:" + cid));
        }

        await using var reopened = new FasterMerkleStore(dir.Path);
        var report = await Verifier(reopened, signer).VerifySessionAsync("s1", Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("content.unreadable"));
        Assert.Equal(1, report.UnreadableBlobs);
    }

    [Fact]
    public async Task LegacyRawBlobOfAnotherType_IsNotReportedUnreadable()
    {
        // The legacy raw format has no type wrapper. A legacy audit blob must be decoded by the
        // audit scan, not flagged as unreadable by the dialectical scan.
        using var dir = new TempStoreDirectory();
        var signer = new MlDsaSigner();

        var audit = NodeFactory.Audit("s1", "plato");
        var raw = audit.Serialize();
        var cid = Cid.Compute(raw);
        var signature = await signer.SignAsync("plato", raw, Ct);

        File.WriteAllLines(dir.DataFile,
        [
            $"obj:{cid}|{Convert.ToBase64String(raw)}",
            $"sig:{cid}|{Convert.ToBase64String(MessagePackSerializer.Serialize(signature, cancellationToken: Ct))}",
        ]);

        await using var store = new FasterMerkleStore(dir.Path);
        var report = await Verifier(store, signer).VerifySessionAsync("s1", Ct);

        Assert.True(report.Passed, string.Join("; ", report.Issues.Select(issue => $"{issue.Code}: {issue.Message}")));
        Assert.False(report.HasCode("content.unreadable"));
        Assert.Equal(1, report.NodesChecked);
    }

    [Fact]
    public async Task SignatureFiledUnderDifferentNode_IsReportedAsError()
    {
        using var dir = new TempStoreDirectory();
        var signer = new MlDsaSigner();
        string firstCid;
        string secondCid;

        await using (var store = new FasterMerkleStore(dir.Path))
        {
            var first = NodeFactory.Dialectical("s1", "plato", "first-content-0123456789");
            var second = NodeFactory.Dialectical("s1", "plato", "second-content-0123456789");
            firstCid = (await store.PutBlobAsync(first)).ToString();
            secondCid = (await store.PutBlobAsync(second)).ToString();
            await store.PutSignatureAsync(await signer.SignAsync("plato", first.Serialize(), Ct));
            await store.PutSignatureAsync(await signer.SignAsync("plato", second.Serialize(), Ct));
        }

        // Copy the first node's signature blob over the second node's slot. The blob is a
        // valid signature, but it is bound to a different node.
        Assert.True(DiskTamper.CopyLine(dir.DataFile, "sig:" + firstCid, "sig:" + secondCid));

        await using var reopened = new FasterMerkleStore(dir.Path);
        var report = await Verifier(reopened, signer).VerifySessionAsync("s1", Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("signature.bound-to-other-node"));
    }

    [Fact]
    public async Task DeletedNode_ProducesDanglingReference()
    {
        using var dir = new TempStoreDirectory();
        var signer = new MlDsaSigner();
        string parentCid;

        await using (var store = new FasterMerkleStore(dir.Path))
        {
            var parent = NodeFactory.Dialectical("s1", "plato");
            parentCid = (await store.PutBlobAsync(parent)).ToString();
            await store.PutSignatureAsync(await signer.SignAsync("plato", parent.Serialize(), Ct));

            var child = NodeFactory.Dialectical("s1", "aristotle", level: 2, parentHash: parentCid);
            await store.PutBlobAsync(child);
            await store.PutSignatureAsync(await signer.SignAsync("aristotle", child.Serialize(), Ct));
        }

        DiskTamper.RemoveKey(dir.DataFile, "obj:" + parentCid);

        await using var reopened = new FasterMerkleStore(dir.Path);
        var report = await Verifier(reopened, signer).VerifySessionAsync("s1", Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("link.dangling"));
        Assert.Equal(1, report.PointerIssues);
    }

    [Fact]
    public async Task AnchoredRoot_MismatchIsReportedAsError()
    {
        using var dir = new TempStoreDirectory();
        var signer = new MlDsaSigner();

        await using var store = new FasterMerkleStore(dir.Path);
        var node = NodeFactory.Dialectical("s1", "plato");
        await store.PutBlobAsync(node);
        await store.PutSignatureAsync(await signer.SignAsync("plato", node.Serialize(), Ct));

        var report = await Verifier(store, signer)
            .VerifySessionAsync("s1", Cid.Compute("a root this session never had"), Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("root.mismatch"));
        Assert.False(report.RootMatches);
    }

    [Fact]
    public async Task AnchoredRoot_MatchPasses()
    {
        using var dir = new TempStoreDirectory();
        var signer = new MlDsaSigner();

        await using var store = new FasterMerkleStore(dir.Path);
        var node = NodeFactory.Dialectical("s1", "plato");
        var cid = await store.PutBlobAsync(node);
        await store.PutSignatureAsync(await signer.SignAsync("plato", node.Serialize(), Ct));

        // The root is taken out of band at write time, then compared later.
        var anchored = SessionRoot.Compute([cid.ToString()]);
        var report = await Verifier(store, signer).VerifySessionAsync("s1", anchored, Ct);

        Assert.True(report.Passed);
        Assert.True(report.RootMatches);
        Assert.Equal(anchored.ToString(), report.SessionRoot!.ToString());
    }

    [Fact]
    public async Task EmptySession_WarnsWithoutFailing()
    {
        using var dir = new TempStoreDirectory();
        await using var store = new FasterMerkleStore(dir.Path);

        var report = await Verifier(store, new MlDsaSigner()).VerifySessionAsync("missing", Ct);

        Assert.True(report.Passed);
        Assert.True(report.HasCode("session.empty"));
        Assert.Equal(0, report.NodesChecked);
    }

    [Fact]
    public async Task AuditNodesRequireSignatures()
    {
        using var dir = new TempStoreDirectory();

        await using var store = new FasterMerkleStore(dir.Path);
        await store.PutBlobAsync(NodeFactory.Audit("s1", "plato"));

        var report = await Verifier(store, new MlDsaSigner()).VerifySessionAsync("s1", Ct);

        Assert.False(report.Passed);
        Assert.True(report.HasCode("signature.missing"));
        Assert.Equal(1, report.NodesChecked);
    }

    [Fact]
    public async Task CrossSessionReferencesStillResolve()
    {
        using var dir = new TempStoreDirectory();
        var signer = new MlDsaSigner();

        await using var store = new FasterMerkleStore(dir.Path);

        // Node in another session that this session cites.
        var shared = NodeFactory.Dialectical("other", "plato", "shared-content-0123456789");
        var sharedCid = (await store.PutBlobAsync(shared)).ToString();
        await store.PutSignatureAsync(await signer.SignAsync("plato", shared.Serialize(), Ct));

        var here = NodeFactory.Dialectical("s1", "aristotle", level: 2, parentHash: sharedCid);
        await store.PutBlobAsync(here);
        await store.PutSignatureAsync(await signer.SignAsync("aristotle", here.Serialize(), Ct));

        var report = await Verifier(store, signer).VerifySessionAsync("s1", Ct);

        Assert.True(report.Passed);
        Assert.Empty(report.Errors);
    }

    [Fact]
    public async Task ValidSignatureFromUnpinnedKey_IsRejected()
    {
        // Regression: a full rewrite re-signed with the attacker's own key was internally
        // consistent, because any self-carried key verified. Pinning the accepted signer turns
        // that into a failure.
        using var dir = new TempStoreDirectory();
        var trusted = new MlDsaSigner();
        var attacker = new MlDsaSigner();

        await using var store = new FasterMerkleStore(dir.Path);
        var node = NodeFactory.Dialectical("s1", "plato");
        await store.PutBlobAsync(node);

        var forged = await attacker.SignAsync("plato", node.Serialize(), Ct);
        await store.PutSignatureAsync(forged);

        // The signature really is valid for these bytes - just not from a pinned key.
        Assert.True(await attacker.VerifyAsync(forged, node.Serialize(), Ct));

        var pinned = new VerificationPolicy
        {
            TrustedSignerPublicKeys = new HashSet<string>(StringComparer.Ordinal) { forged.PublicKey },
        };
        var replacedKey = new VerificationPolicy
        {
            TrustedSignerPublicKeys = new HashSet<string>(StringComparer.Ordinal)
            {
                (await trusted.SignAsync("plato", node.Serialize(), Ct)).PublicKey,
            },
        };

        var accepted = await Verifier(store, attacker, pinned).VerifySessionAsync("s1", Ct);
        Assert.True(accepted.Passed);

        var rejected = await Verifier(store, trusted, replacedKey).VerifySessionAsync("s1", Ct);
        Assert.False(rejected.Passed);
        Assert.True(rejected.HasCode("signature.untrusted-key"));
    }

    [Fact]
    public async Task PinnedKey_Passes()
    {
        using var dir = new TempStoreDirectory();
        var signer = new MlDsaSigner();

        await using var store = new FasterMerkleStore(dir.Path);
        var node = NodeFactory.Dialectical("s1", "plato");
        await store.PutBlobAsync(node);
        var signature = await signer.SignAsync("plato", node.Serialize(), Ct);
        await store.PutSignatureAsync(signature);

        var policy = new VerificationPolicy
        {
            TrustedSignerPublicKeys = new HashSet<string>(StringComparer.Ordinal) { signature.PublicKey },
        };

        var report = await Verifier(store, signer, policy).VerifySessionAsync("s1", Ct);

        Assert.True(report.Passed);
        Assert.Equal(1, report.SignaturesVerified);
    }
}
