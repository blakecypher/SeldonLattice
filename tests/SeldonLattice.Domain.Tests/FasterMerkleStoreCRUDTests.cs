using SeldonLattice.Domain.Models.Storage;
using SeldonLattice.Infrastructure.Storage;
using Xunit;

namespace SeldonLattice.Domain.Tests;

public class FasterMerkleStoreCRUDTests
{
    [Fact]
    public async Task PutAndGetBlobAsync_RoundTripsCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SeldonLatticeTest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var store = new FasterMerkleStore(tempDir);
            var node = new DialecticalNode
            {
                NodeType = "plato_reply",
                Level = 0,
                Content = "The Form of the Good is the source of all truth.",
                Embedding = [0.1f, 0.2f, 0.3f],
                SessionId = "session-1",
                SpeakerId = "plato"
            };

            var cid = await store.PutBlobAsync(node);
            Assert.False(string.IsNullOrEmpty(cid.Hash));

            var retrieved = await store.GetBlobAsync<DialecticalNode>(cid);
            Assert.NotNull(retrieved);
            Assert.Equal("plato_reply", retrieved.NodeType);
            Assert.Equal("The Form of the Good is the source of all truth.", retrieved.Content);
            Assert.Equal("plato", retrieved.SpeakerId);
            Assert.Equal("session-1", retrieved.SessionId);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task PutAsync_StoresAndGetAsync_RetrievesObject()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SeldonLatticeTest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var store = new FasterMerkleStore(tempDir);
            var session = new Session
            {
                SessionId = "test-session",
                RootQuestion = "What is justice?",
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                NodeCount = 0,
                MaxLevel = 0,
            };

            var cid = await store.PutAsync(session);
            Assert.False(string.IsNullOrEmpty(cid.Hash));

            var retrieved = await store.GetAsync<Session>(cid);
            Assert.NotNull(retrieved);
            Assert.Equal("test-session", retrieved.SessionId);
            Assert.Equal("What is justice?", retrieved.RootQuestion);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetAllNodesAsync_ReturnsAllNodesOfType()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SeldonLatticeTest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var store = new FasterMerkleStore(tempDir);

            for (int i = 0; i < 5; i++)
            {
                var node = new DialecticalNode
                {
                    NodeType = "plato_reply",
                    Level = 0,
                    Content = $"Plato's argument {i}",
                    Embedding = [0.1f, 0.2f, 0.3f],
                    SessionId = "session-1",
                    SpeakerId = "plato"
                };
                await store.PutBlobAsync(node);
            }

            var nodes = await store.GetAllNodesAsync<DialecticalNode>();
            Assert.Equal(5, nodes.Count());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetAllNodesAsync_FiltersByType()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SeldonLatticeTest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var store = new FasterMerkleStore(tempDir);

            var platoNode = new DialecticalNode
            {
                NodeType = "plato_reply",
                Level = 0,
                Content = "Plato's argument",
                Embedding = [0.1f, 0.2f, 0.3f],
                SessionId = "session-1",
                SpeakerId = "plato"
            };
            await store.PutBlobAsync(platoNode);

            var aristotleNode = new DialecticalNode
            {
                NodeType = "aristotle_reply",
                Level = 0,
                Content = "Aristotle's argument",
                Embedding = [0.2f, 0.3f, 0.4f],
                SessionId = "session-1",
                SpeakerId = "aristotle"
            };
            await store.PutBlobAsync(aristotleNode);

            var platoNodes = await store.GetAllNodesAsync<DialecticalNode>();
            Assert.Equal(2, platoNodes.Count());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task PutBlobAsync_GeneratesUniqueCids()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "SeldonLatticeTest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var store = new FasterMerkleStore(tempDir);

            var node1 = new DialecticalNode
            {
                NodeType = "plato_reply",
                Level = 0,
                Content = "First argument",
                Embedding = [0.1f, 0.2f, 0.3f],
                SessionId = "session-1",
                SpeakerId = "plato"
            };
            var cid1 = await store.PutBlobAsync(node1);

            var node2 = new DialecticalNode
            {
                NodeType = "plato_reply",
                Level = 0,
                Content = "Second argument",
                Embedding = [0.1f, 0.2f, 0.3f],
                SessionId = "session-1",
                SpeakerId = "plato"
            };
            var cid2 = await store.PutBlobAsync(node2);

            Assert.NotEqual(cid1.Hash, cid2.Hash);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}