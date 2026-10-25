using MessagePack;
using SeldonLattice.Domain.Models.Storage;
using SeldonLattice.Domain.Tests.TestSupport;
using SeldonLattice.Infrastructure.Storage;
using Xunit;

namespace SeldonLattice.Domain.Tests.Storage;

public class ContentAddressingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void Serialization_IsStableAcrossRoundTrip()
    {
        // Everything downstream depends on this: if a deserialised node re-serialises to
        // different bytes, its CID changes and every verification fails or, worse, silently
        // stops matching the identifier it was filed under.
        var node = NodeFactory.Dialectical("s1", "plato");

        var first = node.Serialize();
        var restored = MessagePackSerializer.Deserialize<DialecticalNode>(
            first, cancellationToken: TestContext.Current.CancellationToken);
        var second = restored.Serialize();

        Assert.Equal(first, second);
        Assert.Equal(node.GetCid().ToString(), restored.GetCid().ToString());
    }

    [Fact]
    public async Task Scan_ReturnsTheKeyEachNodeIsFiledUnder()
    {
        using var dir = new TempStoreDirectory();
        await using var store = new FasterMerkleStore(dir.Path);

        var node = NodeFactory.Dialectical("s1", "plato");
        var cid = await store.PutBlobAsync(node);

        var scan = await store.ScanBlobsAsync<DialecticalNode>(Ct);

        var stored = Assert.Single(scan.Nodes);
        Assert.Equal(cid.ToString(), stored.KeyCid.ToString());
        Assert.Equal(cid.ToString(), stored.Node.GetCid().ToString());
        Assert.Empty(scan.UnreadableKeys);
    }

    [Fact]
    public async Task Scan_SeparatesTypesWithoutReportingThemAsUnreadable()
    {
        using var dir = new TempStoreDirectory();
        await using var store = new FasterMerkleStore(dir.Path);

        await store.PutBlobAsync(NodeFactory.Dialectical("s1", "plato"));
        await store.PutBlobAsync(NodeFactory.Audit("s1", "plato"));

        var dialectical = await store.ScanBlobsAsync<DialecticalNode>(Ct);
        var audits = await store.ScanBlobsAsync<LlmAuditNode>(Ct);

        Assert.Single(dialectical.Nodes);
        Assert.Empty(dialectical.UnreadableKeys);
        Assert.Single(audits.Nodes);
        Assert.Empty(audits.UnreadableKeys);
    }

    [Fact]
    public async Task GetBlob_ReturnsTheOriginalNode()
    {
        using var dir = new TempStoreDirectory();
        await using var store = new FasterMerkleStore(dir.Path);

        var node = NodeFactory.Dialectical("s1", "plato");
        var cid = await store.PutBlobAsync(node);
        var loaded = await store.GetBlobAsync<DialecticalNode>(cid);

        Assert.Equal(node.GetCid().ToString(), loaded.GetCid().ToString());
        Assert.Equal(node.Content, loaded.Content);
    }
}
