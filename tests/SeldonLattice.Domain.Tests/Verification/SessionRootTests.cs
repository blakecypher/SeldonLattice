using SeldonLattice.Domain.Verification;
using Xunit;

namespace SeldonLattice.Domain.Tests.Verification;

public class SessionRootTests
{
    [Fact]
    public void Root_IsOrderIndependent()
    {
        var a = Cid("alpha");
        var b = Cid("beta");
        var c = Cid("gamma");

        var one = SessionRoot.Compute([a, b, c]);
        var two = SessionRoot.Compute([c, a, b]);

        Assert.Equal(one.ToString(), two.ToString());
    }

    [Fact]
    public void Root_DeduplicatesRepeatedIdentifiers()
    {
        var a = Cid("alpha");
        var b = Cid("beta");

        Assert.Equal(
            SessionRoot.Compute([a, b]).ToString(),
            SessionRoot.Compute([a, b, a, b]).ToString());
    }

    [Fact]
    public void Root_IgnoresBlankIdentifiers()
    {
        var a = Cid("alpha");

        Assert.Equal(
            SessionRoot.Compute([a]).ToString(),
            SessionRoot.Compute([a, "", "  "]).ToString());
    }

    [Fact]
    public void Root_ChangesWhenANodeIsRemoved()
    {
        var a = Cid("alpha");
        var b = Cid("beta");

        Assert.NotEqual(
            SessionRoot.Compute([a, b]).ToString(),
            SessionRoot.Compute([a]).ToString());
    }

    [Fact]
    public void Root_ChangesWhenANodeIsReplaced()
    {
        var a = Cid("alpha");
        var b = Cid("beta");
        var injected = Cid("injected");

        Assert.NotEqual(
            SessionRoot.Compute([a, b]).ToString(),
            SessionRoot.Compute([a, injected]).ToString());
    }

    [Fact]
    public void Root_FormatIsPinned()
    {
        // Changing the canonical form changes every root and silently invalidates every
        // previously recorded anchor, so the encoding is pinned by a test.
        var a = Cid("alpha");
        var b = Cid("beta");
        var ordered = new[] { a, b }.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var expected = Models.Storage.Cid.Compute(
            SessionRoot.Domain + "\n" + ordered[0] + "\n" + ordered[1]);

        Assert.Equal(expected.ToString(), SessionRoot.Compute([b, a]).ToString());
    }

    private static string Cid(string text) => Models.Storage.Cid.Compute(text).ToString();
}
