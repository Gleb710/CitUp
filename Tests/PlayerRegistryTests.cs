using CitUp.Sim;
using Xunit;

namespace Tests;

public class PlayerRegistryTests
{
    private static PlayerRegistry NewRegistry()
    {
        var registry = new PlayerRegistry();
        registry.RegisterHost("Хост");
        return registry;
    }

    [Fact]
    public void Host_IsPlayerOne_AndConnected()
    {
        var registry = NewRegistry();

        Assert.Equal(1, PlayerRegistry.HostPlayerId);
        Assert.Equal("Хост", registry.NameOf(1));
        Assert.True(registry.IsConnected(1));
    }

    [Fact]
    public void Joiners_GetGrowingIds()
    {
        var registry = NewRegistry();

        var first = registry.Join("Аня");
        var second = registry.Join("Боря");

        Assert.True(first.Ok);
        Assert.Equal(2, first.PlayerId);
        Assert.True(second.Ok);
        Assert.Equal(3, second.PlayerId);
    }

    [Fact]
    public void SameNameWhileConnected_IsRejected()
    {
        var registry = NewRegistry();
        registry.Join("Аня");

        var again = registry.Join("Аня");

        Assert.False(again.Ok);
        Assert.Contains("занято", again.Error);
    }

    [Fact]
    public void NameCheck_IgnoresCase()
    {
        var registry = NewRegistry();
        registry.Join("Аня");

        Assert.False(registry.Join("аня").Ok);
    }

    [Fact]
    public void HostName_CannotBeTaken()
    {
        var registry = NewRegistry();

        Assert.False(registry.Join("Хост").Ok);
    }

    [Fact]
    public void ReturningPlayer_GetsSameId()
    {
        var registry = NewRegistry();
        int id = registry.Join("Аня").PlayerId;
        registry.Disconnect(id);

        var back = registry.Join("Аня");

        Assert.True(back.Ok);
        Assert.Equal(id, back.PlayerId);
        Assert.True(registry.IsConnected(id));
    }

    [Fact]
    public void Disconnect_RemovesFromConnectedList_ButKeepsTheName()
    {
        var registry = NewRegistry();
        int id = registry.Join("Аня").PlayerId;

        registry.Disconnect(id);

        Assert.False(registry.IsConnected(id));
        Assert.Equal("Аня", registry.NameOf(id));
        Assert.Equal(new[] { 1 }, registry.ConnectedPlayers.Select(p => p.Id).ToArray());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyName_IsRejected(string? name)
    {
        Assert.False(NewRegistry().Join(name).Ok);
    }

    [Fact]
    public void Name_IsTrimmed_AndLongNameIsCut()
    {
        var registry = NewRegistry();

        int trimmedId = registry.Join("  Аня  ").PlayerId;
        int longId = registry.Join(new string('б', 40)).PlayerId;

        Assert.Equal("Аня", registry.NameOf(trimmedId));
        Assert.Equal(PlayerRegistry.MaxNameLength, registry.NameOf(longId)!.Length);
    }

    [Fact]
    public void RegisterHostTwice_Throws()
    {
        var registry = NewRegistry();

        Assert.Throws<InvalidOperationException>(() => registry.RegisterHost("Другой"));
    }
}