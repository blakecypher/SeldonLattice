using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using SeldonLattice.Domain.Models.Storage;
using SeldonLattice.Domain.Verification;
using SeldonLattice.Infrastructure.Signing;
using SeldonLattice.Infrastructure.Storage;
using SeldonLattice.Infrastructure.Verification;

// End-to-end demonstration of what the verifier can and cannot establish.
// Run: dotnet run --project samples/VerificationDemo

var baseDir = Path.Combine(Path.GetTempPath(), "seldon-lattice-demo-" + Guid.NewGuid().ToString("n"));
Directory.CreateDirectory(baseDir);

var signer = new MlDsaSigner(new MlDsaSignerOptions { KeyDirectory = Path.Combine(baseDir, "keys") });
var storeDir = Path.Combine(baseDir, "store");
var dataFile = Path.Combine(storeDir, "store.dat");
var failures = new List<string>();

try
{
    Section("1. A clean session");
    var clean = await SeedAsync(storeDir, signer);
    Console.WriteLine($"    root {clean.Root}");
    await ReportAsync(storeDir, signer, clean.Root, expectPass: true, expectCode: null);

    Section("2. A signed leaf is rewritten on disk");
    Console.WriteLine("    the object key is untouched, only the bytes change");
    TamperText(dataFile, "original-claim-text-0123456789", "tampered-claim-text-0123456789");
    await ReportAsync(storeDir, signer, clean.Root, expectPass: false, expectCode: "content.id-mismatch");

    Section("3. A node is deleted");
    var fresh = await SeedAsync(storeDir, signer, reset: true);
    DeleteKey(dataFile, "obj:" + fresh.ParentCid);
    await ReportAsync(storeDir, signer, fresh.Root, expectPass: false, expectCode: "link.dangling");

    Section("4. The store is replaced with different content, re-signed with another key");
    var attacker = new MlDsaSigner(new MlDsaSignerOptions { KeyDirectory = Path.Combine(baseDir, "attacker-keys") });
    await SeedAsync(storeDir, attacker, reset: true, label: "forged");

    var rewritten = await VerifyAsync(storeDir, attacker);
    Console.WriteLine($"    internal consistency: passed={rewritten.Passed} " +
                      "(every signature is valid for the bytes it covers)");

    var anchored = await VerifyAsync(storeDir, signer, fresh.Root);
    Console.WriteLine($"    anchored root check:  passed={anchored.Passed} code={FirstCode(anchored)}");
    Check("replaced content is caught by the externally recorded root",
        !anchored.Passed && anchored.HasCode("root.mismatch"));

    Section("5. The same content is re-signed with another key (the root still matches)");
    var original = await SeedAsync(storeDir, signer, reset: true);
    await SeedAsync(storeDir, attacker, reset: true);

    var resigned = await VerifyAsync(storeDir, signer, original.Root);
    Console.WriteLine($"    root matches: {resigned.RootMatches}, passed={resigned.Passed} " +
                      "(nothing in the store says who was allowed to sign)");

    var pinned = await VerifyAsync(storeDir, signer, original.Root, Pin(original.PublicKeys));
    Console.WriteLine($"    with pinned signer keys: passed={pinned.Passed} code={FirstCode(pinned)}");
    Check("root alone does not catch a re-sign", resigned.RootMatches == true && resigned.Passed);
    Check("pinned signer keys catch the re-sign",
        !pinned.Passed && pinned.HasCode("signature.untrusted-key"));

    Section("Summary");
    if (failures.Count == 0)
    {
        Console.WriteLine("  All expectations held.");
        return 0;
    }

    foreach (var failure in failures)
    {
        Console.WriteLine("  FAILED: " + failure);
    }

    return 1;
}
finally
{
    try
    {
        Directory.Delete(baseDir, recursive: true);
    }
    catch (IOException)
    {
        // Best effort.
    }
}

async Task<Seeded> SeedAsync(
    string directory, MlDsaSigner signingKey, bool reset = false, string label = "original")
{
    if (reset)
    {
        await using var clearing = new FasterMerkleStore(directory);
        await clearing.ClearAsync();
    }

    await using var store = new FasterMerkleStore(directory);

    var parent = Node("plato", $"{label}-claim-text-0123456789", level: 1, parentHash: null);
    var parentCid = await store.PutBlobAsync(parent);
    var parentSignature = await signingKey.SignAsync("plato", parent.Serialize());
    await store.PutSignatureAsync(parentSignature);

    var child = Node("aristotle", $"{label}-reply-text-0123456789", level: 2,
        parentHash: parentCid.ToString());
    var childCid = await store.PutBlobAsync(child);
    var childSignature = await signingKey.SignAsync("aristotle", child.Serialize());
    await store.PutSignatureAsync(childSignature);

    var audit = new LlmAuditNode
    {
        SessionId = "s1",
        SpeakerId = "plato",
        ModuleKey = "classical",
        Level = 1,
        Turn = 1,
        PromptMessages = ["system: summarised prompt"],
        CompletionText = $"{label} completion text",
        ModelId = "demo-model",
        LatencyMs = 42,
        Timestamp = new DateTime(2026, 10, 20, 12, 5, 0, DateTimeKind.Utc),
        ParentCid = parentCid.ToString(),
    };
    var auditCid = await store.PutBlobAsync(audit);
    var auditSignature = await signingKey.SignAsync("plato", audit.Serialize());
    await store.PutSignatureAsync(auditSignature);

    // The root is what a caller records outside the store to anchor the session.
    var root = SessionRoot.Compute([parentCid.ToString(), childCid.ToString(), auditCid.ToString()]);
    return new Seeded(
        parentCid.ToString(),
        auditCid.ToString(),
        root,
        [parentSignature.PublicKey, childSignature.PublicKey, auditSignature.PublicKey]);
}

async Task ReportAsync(string directory, MlDsaSigner signingKey, Cid? expectedRoot,
    bool expectPass, string? expectCode)
{
    var report = await VerifyAsync(directory, signingKey, expectedRoot);
    Console.WriteLine($"    nodes={report.NodesChecked} signatures={report.SignaturesVerified} " +
                      $"passed={report.Passed}");

    foreach (var issue in report.Issues)
    {
        Console.WriteLine($"      [{issue.Severity}] {issue.Code}: {issue.Message}");
    }

    Check("expected outcome", report.Passed == expectPass);
    if (expectCode is not null)
    {
        Check($"expected code {expectCode}", report.HasCode(expectCode));
    }
}

async Task<SessionVerificationReport> VerifyAsync(
    string directory, MlDsaSigner signingKey, Cid? expectedRoot = null, VerificationPolicy? policy = null)
{
    await using var store = new FasterMerkleStore(directory);
    var verifier = new SessionVerifier(store, signingKey, NullLogger<SessionVerifier>.Instance, policy);
    return await verifier.VerifySessionAsync("s1", expectedRoot);
}

VerificationPolicy Pin(IEnumerable<string> publicKeys) => new()
{
    TrustedSignerPublicKeys = new HashSet<string>(publicKeys, StringComparer.Ordinal),
};

DialecticalNode Node(string speaker, string content, int level, string? parentHash) => new()
{
    NodeType = speaker + "_reply",
    Level = level,
    ParentHash = parentHash,
    Content = content,
    Embedding = [0.1f, 0.2f, 0.3f],
    Timestamp = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc).AddMinutes(level),
    CyclePhase = "ascent",
    SessionId = "s1",
    SpeakerId = speaker,
    AscentScore = 0.24f,
    Turn = level,
    ReasoningTrace = "trace",
    QualityScore = 0.8,
    WordCount = 120,
};

void TamperText(string file, string from, string to)
{
    var fromBytes = Encoding.UTF8.GetBytes(from);
    var toBytes = Encoding.UTF8.GetBytes(to);
    var lines = File.ReadAllLines(file);

    for (var i = 0; i < lines.Length; i++)
    {
        var separator = lines[i].IndexOf('|');
        if (separator < 0)
        {
            continue;
        }

        var raw = Convert.FromBase64String(lines[i][(separator + 1)..]);
        var offset = raw.AsSpan().IndexOf(fromBytes);
        if (offset < 0)
        {
            continue;
        }

        toBytes.AsSpan().CopyTo(raw.AsSpan(offset));
        lines[i] = lines[i][..(separator + 1)] + Convert.ToBase64String(raw);
    }

    File.WriteAllLines(file, lines);
}

void DeleteKey(string file, string key)
{
    var lines = File.ReadAllLines(file)
        .Where(line => !line.StartsWith(key + "|", StringComparison.Ordinal));
    File.WriteAllLines(file, lines);
}

void Section(string title)
{
    Console.WriteLine();
    Console.WriteLine("== " + title);
}

void Check(string label, bool ok)
{
    if (!ok)
    {
        failures.Add(label);
    }

    Console.WriteLine($"    [{(ok ? "ok" : "FAIL")}] {label}");
}

string FirstCode(SessionVerificationReport report) =>
    report.Errors.Select(issue => issue.Code).FirstOrDefault() ?? "none";

internal sealed record Seeded(
    string ParentCid,
    string AuditCid,
    Cid Root,
    IReadOnlyList<string> PublicKeys);
