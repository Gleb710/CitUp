using CitUp.Sim;
using Xunit;

namespace CitUp.Tests;

public sealed class StockpileTests
{
    [Fact]
    public void NewStockpile_IsEmpty()
    {
        var stockpile = new Stockpile();

        Assert.Empty(stockpile.Amounts);
        Assert.Equal(0, stockpile.GetAmount("wood"));
    }

    [Fact]
    public void Add_IncreasesResourceAmount()
    {
        var stockpile = new Stockpile();

        stockpile.Add("wood", 10);
        stockpile.Add("wood", 5);

        Assert.Equal(15, stockpile.GetAmount("wood"));
    }

    [Fact]
    public void TryTake_ReturnsTrue_WhenEnoughResource()
    {
        var stockpile = new Stockpile();
        stockpile.Add("wood", 10);

        var result = stockpile.TryTake("wood", 7);

        Assert.True(result);
        Assert.Equal(3, stockpile.GetAmount("wood"));
    }

    [Fact]
    public void TryTake_ReturnsFalse_WhenNotEnoughResource()
    {
        var stockpile = new Stockpile();
        stockpile.Add("wood", 3);

        var result = stockpile.TryTake("wood", 7);

        Assert.False(result);
        Assert.Equal(3, stockpile.GetAmount("wood"));
    }

    [Fact]
    public void TryTake_RemovesEmptyResource()
    {
        var stockpile = new Stockpile();
        stockpile.Add("wood", 5);

        stockpile.TryTake("wood", 5);

        Assert.Empty(stockpile.Amounts);
    }

    [Fact]
    public void Add_NegativeAmount_Throws()
    {
        var stockpile = new Stockpile();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => stockpile.Add("wood", -1));
    }

    [Fact]
    public void TryTake_NegativeAmount_Throws()
    {
        var stockpile = new Stockpile();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => stockpile.TryTake("wood", -1));
    }

    [Fact]
    public void EmptyResourceId_Throws()
    {
        var stockpile = new Stockpile();

        Assert.Throws<ArgumentException>(
            () => stockpile.Add("", 1));
    }
}