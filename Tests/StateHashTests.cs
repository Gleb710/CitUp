using CitUp.Sim;
using Xunit;

namespace Tests;

public class StateHashTests
{
    private static (World World, SimEngine Engine) Host()
    {
        var world = new World(WorldConfig.LoadDefault());
        var engine = new SimEngine(world, seed: 7);
        engine.Enqueue(new BuildCommand(1, 1, "house", new Cell(20, 16)));
        engine.Enqueue(new BuildCommand(2, 2, "well", new Cell(10, 12)));
        return (world, engine);
    }

    private static void StepUntilEvent<T>(SimEngine engine, int maxTicks) where T : SimEvent
    {
        for (int i = 0; i < maxTicks; i++)
        {
            if (engine.Step().Events.OfType<T>().Any())
                return;
        }

        throw new Xunit.Sdk.XunitException($"Событие {typeof(T).Name} не наступило за {maxTicks} тиков");
    }

    [Fact]
    public void TwoFreshWorlds_HaveSameHash()
    {
        var a = new World(WorldConfig.LoadDefault());
        var b = new World(WorldConfig.LoadDefault());

        Assert.Equal(a.StateHash(), b.StateHash());
    }

    [Fact]
    public void Hash_ChangesWhenBuildingAdded()
    {
        var world = new World(WorldConfig.LoadDefault());
        int before = world.StateHash();

        world.Apply(new BuildingBuilt(1, "house", new Cell(3, 3), 1));

        Assert.NotEqual(before, world.StateHash());
    }

    [Fact]
    public void Hash_DependsOnBuildingTypeAndOwner()
    {
        int HashOf(string type, int owner)
        {
            var world = new World(WorldConfig.LoadDefault());
            world.Apply(new BuildingBuilt(1, type, new Cell(3, 3), owner));
            return world.StateHash();
        }

        int baseline = HashOf("house", 1);

        Assert.NotEqual(baseline, HashOf("well", 1));
        Assert.NotEqual(baseline, HashOf("house", 2));
    }

    [Fact]
    public void Hash_DoesNotDependOnTick()
    {
        var world = new World(WorldConfig.LoadDefault());
        var engine = new SimEngine(world);
        int before = world.StateHash();

        for (int i = 0; i < 10; i++)
            engine.Step();

        Assert.Equal(before, world.StateHash());
    }

    [Fact]
    public void Hash_ChangesWhenResidentStartsWalking()
    {
        var (world, engine) = Host();
        for (int i = 0; i < 19; i++)
            engine.Step();   // здания построены, житель ещё стоит
        Assert.False(world.Residents[0].IsWalking);
        int standing = world.StateHash();

        StepUntilEvent<ResidentWalkStarted>(engine, 50);

        Assert.NotEqual(standing, world.StateHash());
    }

    [Fact]
    public void HostAndClient_HaveSameHash_AfterEveryTick()
    {
        var (host, engine) = Host();
        var client = new World(WorldConfig.LoadDefault());

        for (int tick = 1; tick <= 600; tick++)
        {
            if (tick == 200)
                engine.Enqueue(new BuildCommand(3, 1, "woodcutter", new Cell(14, 25)));

            foreach (var simEvent in engine.Step().Events)
                client.Apply(simEvent);

            Assert.Equal(host.StateHash(), client.StateHash());
        }

        Assert.NotEmpty(client.Buildings);
    }

    [Fact]
    public void LateJoiner_DuringWalk_HasSameHash()
    {
        var (host, engine) = Host();
        StepUntilEvent<ResidentWalkStarted>(engine, 100);

        var client = World.FromSnapshot(host.Config, host.ToSnapshot());

        Assert.Equal(host.StateHash(), client.StateHash());
    }

    [Fact]
    public void LateJoiner_AfterFinishedWalk_HasSameHash()
    {
        var (host, engine) = Host();
        StepUntilEvent<ResidentWalkFinished>(engine, 1000);
        Assert.False(host.Residents[0].IsWalking);

        var client = World.FromSnapshot(host.Config, host.ToSnapshot());

        Assert.Equal(host.StateHash(), client.StateHash());
    }
}