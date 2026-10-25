using System.Text;
using SeldonLattice.Domain.Models.Storage;
using SeldonLattice.Domain.Tests.TestSupport;
using SeldonLattice.Infrastructure.Signing;
using Xunit;

namespace SeldonLattice.Domain.Tests.Signing;

public class MlDsaSignerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static readonly byte[] Content = Encoding.UTF8.GetBytes("reasoning node bytes");

    [Fact]
    public async Task SignThenVerify_Succeeds()
    {
        var signer = new MlDsaSigner();
        var signature = await signer.SignAsync("plato", Content, Ct);

        Assert.Equal(MlDsaSigner.AlgorithmName, signature.Algorithm);
        Assert.Equal("plato", signature.SignerId);
        Assert.True(signature.IsComplete);
        Assert.Equal(Cid.Compute(Content).ToString(), signature.SignedCid);
        Assert.True(await signer.VerifyAsync(signature, Content, Ct));
    }

    [Fact]
    public async Task Verify_RejectsModifiedContent()
    {
        var signer = new MlDsaSigner();
        var signature = await signer.SignAsync("plato", Content, Ct);
        var modified = (byte[])Content.Clone();
        modified[0] ^= 0xFF;

        Assert.False(await signer.VerifyAsync(signature, modified, Ct));
    }

    [Fact]
    public async Task Verify_RejectsTamperedSignatureBytes()
    {
        var signer = new MlDsaSigner();
        var signature = await signer.SignAsync("plato", Content, Ct);
        var raw = Convert.FromBase64String(signature.Signature);
        raw[^1] ^= 0xFF;

        var tampered = signature with { Signature = Convert.ToBase64String(raw) };

        Assert.False(await signer.VerifyAsync(tampered, Content, Ct));
    }

    [Fact]
    public async Task Verify_RejectsUnknownAlgorithm()
    {
        var signer = new MlDsaSigner();
        var signature = await signer.SignAsync("plato", Content, Ct);

        Assert.False(await signer.VerifyAsync(signature with { Algorithm = "sha256-hmac" }, Content, Ct));
    }

    [Fact]
    public async Task Verify_RejectsMalformedMaterialWithoutThrowing()
    {
        var signer = new MlDsaSigner();
        var malformed = new AgentSignature
        {
            Algorithm = MlDsaSigner.AlgorithmName,
            SignerId = "plato",
            PublicKey = "not-base64",
            Signature = "not-base64",
            SignedCid = Cid.Compute(Content).ToString(),
            Timestamp = DateTime.UnixEpoch,
        };

        Assert.False(await signer.VerifyAsync(malformed, Content, Ct));
    }

    [Fact]
    public async Task Signing_IsDeterministicForAKeyAndContent()
    {
        var signer = new MlDsaSigner();
        var first = await signer.SignAsync("plato", Content, Ct);
        var second = await signer.SignAsync("plato", Content, Ct);

        Assert.Equal(first.PublicKey, second.PublicKey);
        Assert.Equal(first.Signature, second.Signature);
    }

    [Fact]
    public async Task Key_IsStableAcrossInstances_WhenKeyDirectoryIsConfigured()
    {
        using var dir = new TempStoreDirectory();
        var options = new MlDsaSignerOptions { KeyDirectory = dir.Path };

        var first = await new MlDsaSigner(options).SignAsync("plato", Content, Ct);
        var second = await new MlDsaSigner(options).SignAsync("plato", Content, Ct);

        Assert.Equal(first.PublicKey, second.PublicKey);
        Assert.Equal(first.Signature, second.Signature);
    }

    [Fact]
    public async Task Keys_ArePerIdentity()
    {
        var signer = new MlDsaSigner();
        var plato = await signer.SignAsync("plato", Content, Ct);
        var aristotle = await signer.SignAsync("aristotle", Content, Ct);

        Assert.NotEqual(plato.PublicKey, aristotle.PublicKey);
    }

    [Fact]
    public async Task PublicKeyIsSelfVerifying()
    {
        // A verifier that never saw the signer can verify from the signature's own key.
        var producer = new MlDsaSigner();
        var consumer = new MlDsaSigner();
        var signature = await producer.SignAsync("plato", Content, Ct);

        Assert.True(await consumer.VerifyAsync(signature, Content, Ct));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("")]
    [InlineData(".hidden")]
    public async Task SignerId_MustBeUsableAsAKeyName(string signerId)
    {
        var signer = new MlDsaSigner();
        await Assert.ThrowsAsync<ArgumentException>(() => signer.SignAsync(signerId, Content, Ct));
    }
}
