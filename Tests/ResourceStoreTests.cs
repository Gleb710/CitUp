using CitUp.Sim;
using Xunit;

namespace CitUp.Tests;

public sealed class ResourceStoreTests
{
    [Fact]
    public void NewStore_HasNoMoneyAndNoResources()
    {
        var store = new ResourceStore();

        Assert.Equal(0, store.Money);
        Assert.Empty(store.Resources);
    }

    [Fact]
    public void AddMoney_IncreasesMoney()
    {
        var store = new ResourceStore();

        store.AddMoney(100);
        store.AddMoney(50);

        Assert.Equal(150, store.Money);
    }

    [Fact]
    public void TryTakeMoney_ReturnsTrue_WhenEnoughMoney()
    {
        var store = new ResourceStore();
        store.AddMoney(100);

        var result = store.TryTakeMoney(40);

        Assert.True(result);
        Assert.Equal(60, store.Money);
    }

    [Fact]
    public void TryTakeMoney_ReturnsFalse_WhenNotEnoughMoney()
    {
        var store = new ResourceStore();
        store.AddMoney(30);

        var result = store.TryTakeMoney(40);

        Assert.False(result);
        Assert.Equal(30, store.Money);
    }

    [Fact]
    public void AddResource_IncreasesResourceAmount()
    {
        var store = new ResourceStore();

        store.AddResource("wood", 10);
        store.AddResource("wood", 5);

        Assert.Equal(15, store.GetResourceAmount("wood"));
    }

    [Fact]
    public void TryTakeResource_ReturnsTrue_WhenEnoughResource()
    {
        var store = new ResourceStore();
        store.AddResource("wood", 10);

        var result = store.TryTakeResource("wood", 4);

        Assert.True(result);
        Assert.Equal(6, store.GetResourceAmount("wood"));
    }

    [Fact]
    public void TryTakeResource_ReturnsFalse_WhenNotEnoughResource()
    {
        var store = new ResourceStore();
        store.AddResource("wood", 3);

        var result = store.TryTakeResource("wood", 4);

        Assert.False(result);
        Assert.Equal(3, store.GetResourceAmount("wood"));
    }

    [Fact]
    public void Money_DoesNotAppearInResources()
    {
        var store = new ResourceStore();

        store.AddMoney(100);

        Assert.Empty(store.Resources);
    }

    [Fact]
    public void NegativeMoney_Throws()
    {
        var store = new ResourceStore();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => store.AddMoney(-1));
    }

    [Fact]
    public void NegativeResourceAmount_Throws()
    {
        var store = new ResourceStore();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => store.AddResource("wood", -1));
    }
}