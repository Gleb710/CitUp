using CitUp.Sim;
using Xunit;

namespace Tests;

public class ClientClockTests
{
    [Fact]
    public void Advance_MovesByTicksPerSecond()
    {
        var clock = new ClientClock(10, startTick: 100);

        clock.Advance(0.5);

        Assert.Equal(105.0, clock.RenderTick, precision: 6);
    }

    [Fact]
    public void FirstHeartbeat_WithKnownRtt_SnapsToEstimate()
    {
        var clock = new ClientClock(10, startTick: 100);
        clock.OnPong(0.2);   // половина пинга 0,1 с = 1 тик

        clock.OnHeartbeat(100);

        // 100 + 0,5 (поправка на целочисленный тик) + 1 (дорога хартбита)
        Assert.Equal(101.5, clock.RenderTick, precision: 6);
        Assert.Equal(1.5, clock.LastErrorTicks, precision: 6);
    }

    [Fact]
    public void FirstHeartbeat_WithoutRtt_IsOnlyPulledGradually()
    {
        var clock = new ClientClock(10, startTick: 100);

        clock.OnHeartbeat(100);   // оценка 100,5, ошибка 0,5, подтягиваем на 20%

        Assert.Equal(100.1, clock.RenderTick, precision: 6);
    }

    [Fact]
    public void SmallError_IsCorrectedGradually()
    {
        var clock = new ClientClock(10, startTick: 100);
        clock.OnPong(0.2);
        clock.OnHeartbeat(100);   // первая синхронизация: 101,5
        clock.Advance(1.0);       // 111,5

        clock.OnHeartbeat(111);   // оценка 112,5, ошибка 1,0, убираем 20%

        Assert.Equal(111.7, clock.RenderTick, precision: 6);
    }

    [Fact]
    public void LargeError_SnapsImmediately()
    {
        var clock = new ClientClock(10, startTick: 100);
        clock.OnPong(0.2);
        clock.OnHeartbeat(100);
        clock.Advance(1.0);

        clock.OnHeartbeat(200);   // клиент сильно отстал (например, окно зависло)

        Assert.Equal(201.5, clock.RenderTick, precision: 6);
    }

    [Fact]
    public void ClockDrift_ConvergesToHostTime_WithinAFewHeartbeats()
    {
        var clock = new ClientClock(10, startTick: 100);
        clock.OnPong(0.1);
        long host = 100;
        clock.OnHeartbeat(host);   // первая синхронизация
        clock.Advance(0.2);        // часы убежали на 2 тика вперёд

        for (int i = 0; i < 20; i++)
        {
            clock.Advance(1.0);
            host += 10;
            clock.OnHeartbeat(host);
        }

        double target = host + 0.5 + 0.05 * 10;   // 0,5 тика поправки + половина пинга 0,05 с
        Assert.True(Math.Abs(clock.RenderTick - target) < 0.05,
            $"Часы не сошлись: {clock.RenderTick} против {target}");
    }

    [Fact]
    public void Rtt_FirstSampleIsTaken_NextOnesAreSmoothed()
    {
        var clock = new ClientClock(10, 0);
        Assert.Null(clock.RttSeconds);

        clock.OnPong(0.1);
        Assert.Equal(0.1, clock.RttSeconds!.Value, precision: 6);

        clock.OnPong(0.2);
        Assert.Equal(0.13, clock.RttSeconds!.Value, precision: 6);   // 0,1 × 0,7 + 0,2 × 0,3
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(10.0)]
    public void Rtt_InvalidSamples_AreIgnored(double rtt)
    {
        var clock = new ClientClock(10, 0);

        clock.OnPong(rtt);

        Assert.Null(clock.RttSeconds);
    }

    [Fact]
    public void RttMs_IsRounded()
    {
        var clock = new ClientClock(10, 0);

        clock.OnPong(0.0234);

        Assert.Equal(23, clock.RttMs);
    }
}