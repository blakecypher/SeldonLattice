using System.Buffers;
using MessagePack;
using SeldonLattice.Domain.Models.Storage;

namespace SeldonLattice.Domain.Tests.TestSupport;

/// <summary>
/// Temporary on-disk store directory, deleted on dispose.
/// </summary>
public sealed class TempStoreDirectory : IDisposable
{
    public string Path { get; }

    public TempStoreDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sl-tests-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path);
    }

    public string DataFile => System.IO.Path.Combine(Path, "store.dat");

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best effort; the OS cleans temp directories eventually.
        }
    }
}

/// <summary>
/// Rewrites the persisted store the way an attacker with filesystem access would.
/// These helpers edit <c>store.dat</c> directly, leaving the object key untouched, which is
/// precisely the case a store scan has to be able to detect.
/// </summary>
public static class DiskTamper
{
    /// <summary>
    /// Replaces a substring inside every stored payload. Equal-length replacements keep the
    /// MessagePack framing intact, so only the node's content changes.
    /// </summary>
    public static int ReplaceText(string dataFile, string from, string to)
    {
        if (from.Length != to.Length)
        {
            throw new ArgumentException("Replacement must preserve length.", nameof(to));
        }

        var fromBytes = System.Text.Encoding.UTF8.GetBytes(from);
        var toBytes = System.Text.Encoding.UTF8.GetBytes(to);
        var rewritten = 0;
        var lines = File.ReadAllLines(dataFile);

        for (var i = 0; i < lines.Length; i++)
        {
            var parts = lines[i].Split('|', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var raw = Convert.FromBase64String(parts[1]);
            var offset = IndexOf(raw, fromBytes);
            if (offset < 0)
            {
                continue;
            }

            Array.Copy(toBytes, 0, raw, offset, toBytes.Length);
            lines[i] = parts[0] + "|" + Convert.ToBase64String(raw);
            rewritten++;
        }

        File.WriteAllLines(dataFile, lines);
        return rewritten;
    }

    /// <summary>
    /// Replaces a payload with undecodable bytes, keeping the key that points at it.
    /// </summary>
    public static int CorruptPayloads(string dataFile, params string[] keys)
    {
        var lines = File.ReadAllLines(dataFile);
        var corrupted = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var separator = lines[i].IndexOf('|');
            if (separator < 0)
            {
                continue;
            }

            var key = lines[i][..separator];
            if (keys.Length > 0 && !keys.Contains(key, StringComparer.Ordinal))
            {
                continue;
            }

            lines[i] = key + "|" + Convert.ToBase64String(RandomBytes(64));
            corrupted++;
        }

        File.WriteAllLines(dataFile, lines);
        return corrupted;
    }

    /// <summary>
    /// Removes the line holding a given object key, simulating deletion of a node.
    /// </summary>
    public static bool RemoveKey(string dataFile, string key)
    {
        var lines = File.ReadAllLines(dataFile).Where(line => !line.StartsWith(key + "|", StringComparison.Ordinal)).ToArray();
        File.WriteAllLines(dataFile, lines);
        return true;
    }

    /// <summary>
    /// Duplicates a stored line under a different key, simulating a signature blob that was
    /// moved or copied between nodes on disk.
    /// </summary>
    public static bool CopyLine(string dataFile, string fromKey, string toKey)
    {
        var lines = File.ReadAllLines(dataFile);
        var source = lines.FirstOrDefault(line => line.StartsWith(fromKey + "|", StringComparison.Ordinal));
        if (source is null)
        {
            return false;
        }

        var payload = source[(source.IndexOf('|') + 1)..];
        File.AppendAllLines(dataFile, [toKey + "|" + payload]);
        return true;
    }

    /// <summary>
    /// Corrupts the payload inside a stored blob while leaving the type wrapper readable.
    /// This is the case a scan could previously mistake for an unrelated blob and skip.
    /// </summary>
    public static bool CorruptPayloadKeepingWrapper(string dataFile, string key)
    {
        var lines = File.ReadAllLines(dataFile);

        for (var i = 0; i < lines.Length; i++)
        {
            var separator = lines[i].IndexOf('|');
            if (separator < 0 || !string.Equals(lines[i][..separator], key, StringComparison.Ordinal))
            {
                continue;
            }

            var raw = Convert.FromBase64String(lines[i][(separator + 1)..]);
            var reader = new MessagePackReader(raw);
            if (reader.ReadArrayHeader() != 2)
            {
                return false;
            }

            var typeName = reader.ReadString();
            var payload = reader.ReadBytes()?.ToArray();
            if (typeName is null || payload is null || payload.Length == 0)
            {
                return false;
            }

            payload[0] ^= 0xFF;

            var buffer = new ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(buffer);
            writer.WriteArrayHeader(2);
            writer.Write(typeName);
            writer.Write(payload);
            writer.Flush();

            lines[i] = key + "|" + Convert.ToBase64String(buffer.WrittenSpan);
            File.WriteAllLines(dataFile, lines);
            return true;
        }

        return false;
    }

    private static byte[] RandomBytes(int length)
    {
        var bytes = new byte[length];
        // Deterministic enough for tests, and avoids depending on crypto RNG availability.
        new Random(20261025).NextBytes(bytes);
        return bytes;
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i + needle.Length <= haystack.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }
}

/// <summary>
/// Builds nodes with every field populated, so serialization differences would show up.
/// </summary>
public static class NodeFactory
{
    public const string OriginalContent = "original-claim-text-0123456789";
    public const string TamperedContent = "tampered-claim-text-0123456789";

    public static DialecticalNode Dialectical(
        string sessionId,
        string speakerId,
        string content = OriginalContent,
        int level = 1,
        string? parentHash = null) => new()
    {
        NodeType = "plato_reply",
        Level = level,
        ParentHash = parentHash,
        Content = content,
        Embedding = [0.1f, 0.2f, 0.3f, 0.4f],
        Timestamp = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc).AddMinutes(level),
        CyclePhase = "ascent",
        VariabilityScore = 0.5f,
        ResonanceScore = 0.75f,
        CenterShiftScore = 0.125f,
        SessionId = sessionId,
        SpeakerId = speakerId,
        ModeratorSelectionConfidence = 0.9f,
        AuthorityReferences = [new Citation { Id = "c1", Title = "Republic", Url = "https://example.org/r", SourceId = "s1", Confidence = 0.88 }],
        JSpaceConcepts = ["justice", "city"],
        JSpaceWeights = [0.4f, 0.6f],
        SilentThoughts = ["held back"],
        JSpaceHash = "jhash-1",
        JDepthScore = 0.33f,
        IsSustained = true,
        AscentScore = 0.24f,
        JoinMediation = 0.31f,
        JoinNovelty = 0.42f,
        IrreducibleTension = 0.11f,
        Turn = level,
        ReasoningTrace = "trace",
        RemainingDissensus = "whether the city is just",
        RepeatsPremise = 0.0f,
        UnresolvedTensions = ["t1"],
        TensionIntensity = 0.2f,
        QualityScore = 0.81,
        CeremonyScore = 0.12,
        SimilarityToPrevious = 0.05,
        NewArgumentCount = 3,
        WordCount = 210,
        HasFarewell = false,
        HasAgreement = true,
        HasDisagreement = false,
        SelfSimilarity = 0.02,
    };

    public static LlmAuditNode Audit(string sessionId, string speakerId, string? parentCid = null) => new()
    {
        SessionId = sessionId,
        SpeakerId = speakerId,
        ModuleKey = "classical",
        Level = 1,
        Turn = 1,
        PromptMessages = ["system: summarised prompt"],
        CompletionText = "completion text",
        InputTokenCount = 10,
        OutputTokenCount = 20,
        ModelId = "test-model",
        LatencyMs = 42,
        Timestamp = new DateTime(2026, 10, 20, 12, 5, 0, DateTimeKind.Utc),
        ParentCid = parentCid,
    };
}
