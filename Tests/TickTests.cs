using Sim;
using Xunit;

namespace Tests;

public class TickTests
{
    [Fact]
    public void Advance_IncreasesNumber()
    {
        var tick = new Tick();
        tick.Advance();
        Assert.Equal(1, tick.Number);
    }
}