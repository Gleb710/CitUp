using CitUp.Sim;
using Xunit;

namespace Tests;

public class ResidentTests
{
    private static (World World, SimEngine Engine) Create(int seed = 1)
    {
        var world = new World(WorldConfig.LoadDefault());
        return (world, new SimEngine(world, seed));
    }

    private static BuildCommand Cmd(int id, string type, int x, int y) =>
        new(id, 1, type, new Cell(x, y));

    /// <summary>Шагает, пока не появится событие нужного типа. null, если за maxTicks не дождались.</summary>
    private static T? StepUntil<T>(SimEngine engine, int maxTicks) where T : SimEvent
    {
        for (int i = 0; i < maxTicks; i++)
        {
            var found = engine.Step().Events.OfType<T>().FirstOrDefault();
            if (found is not null)
                return found;
        }
        return null;
    }

    private static Cell Center(World world) => new(world.Width / 2, world.Height / 2);

    [Fact]
    public void NewWorld_HasOneStandingResidentInCenter()
    {
        var (world, _) = Create();

        var resident = Assert.Single(world.Residents);
        Assert.Equal(Center(world), resident.Cell);
        Assert.False(resident.IsWalking);
    }

    [Fact]
    public void WithoutBuildings_ResidentStandsStill()
    {
        var (world, engine) = Create();

        for (int i = 0; i < 300; i++)
            Assert.Empty(engine.Step().Events);

        Assert.Equal(Center(world), world.Residents[0].Cell);
        Assert.False(world.Residents[0].IsWalking);
    }

    [Fact]
    public void WithBuilding_FirstWalkStartsAfterInitialPause()
    {
        var (world, engine) = Create();
        engine.Enqueue(Cmd(1, "house", 20, 16));

        var started = StepUntil<ResidentWalkStarted>(engine, 100);

        Assert.NotNull(started);
        Assert.Equal(world.Config.Resident.PauseTicks, started.StartTick);
        Assert.Equal(world.CurrentTick, started.StartTick);
        Assert.Equal(Center(world), started.Path[0]);

        var goal = started.Path[^1];
        int distanceToBuilding = Math.Abs(goal.X - 20) + Math.Abs(goal.Y - 16);
        Assert.Equal(1, distanceToBuilding);
        Assert.DoesNotContain(new Cell(20, 16), started.Path);
        Assert.True(world.Residents[0].IsWalking);
    }

    [Fact]
    public void Walk_FinishesOnExpectedTick()
    {
        var (world, engine) = Create();
        engine.Enqueue(Cmd(1, "house", 20, 16));

        var started = StepUntil<ResidentWalkStarted>(engine, 100)!;
        var finished = StepUntil<ResidentWalkFinished>(engine, 1000);

        Assert.NotNull(finished);
        long expectedTick = started.StartTick + (started.Path.Count - 1) * world.Config.Resident.TicksPerCell;
        Assert.Equal(expectedTick, world.CurrentTick);
        Assert.Equal(started.Path[^1], finished.Cell);
        Assert.Equal(finished.Cell, world.Residents[0].Cell);
        Assert.False(world.Residents[0].IsWalking);
    }

    [Fact]
    public void AfterWalk_ResidentRestsForPause_ThenWalksAgain()
    {
        var (world, engine) = Create();
        engine.Enqueue(Cmd(1, "house", 20, 16));

        StepUntil<ResidentWalkStarted>(engine, 100);
        StepUntil<ResidentWalkFinished>(engine, 1000);
        long finishTick = world.CurrentTick;

        var second = StepUntil<ResidentWalkStarted>(engine, 100);

        Assert.NotNull(second);
        Assert.Equal(finishTick + world.Config.Resident.PauseTicks, second.StartTick);
    }

    [Fact]
    public void CellAt_FollowsPathOverTime()
    {
        var (world, _) = Create();
        var path = new[] { new Cell(40, 50), new Cell(41, 50), new Cell(42, 50) };
        world.Apply(new ResidentWalkStarted(1, path, StartTick: 10));
        var resident = world.Residents[0];
        const int perCell = 5;

        Assert.Equal(new Cell(40, 50), resident.CellAt(5, perCell));    // до старта
        Assert.Equal(new Cell(40, 50), resident.CellAt(10, perCell));
        Assert.Equal(new Cell(40, 50), resident.CellAt(14, perCell));
        Assert.Equal(new Cell(41, 50), resident.CellAt(15, perCell));
        Assert.Equal(new Cell(42, 50), resident.CellAt(20, perCell));
        Assert.Equal(new Cell(42, 50), resident.CellAt(500, perCell));  // после финиша
        Assert.Equal(20, resident.WalkEndTick(perCell));
    }

    [Fact]
    public void Build_OnCellWhereResidentStands_IsRejected()
    {
        var (world, engine) = Create();

        engine.Enqueue(Cmd(1, "house", world.Width / 2, world.Height / 2));
        var result = engine.Step();

        Assert.Equal(CommandError.CellBlockedByResident, Assert.Single(result.Results).Error);
        Assert.Empty(world.Buildings);
    }

    [Fact]
    public void Build_OnResidentPath_IsRejected_ButOffThePathIsAllowed()
    {
        var (world, engine) = Create();
        var path = new[] { new Cell(40, 50), new Cell(41, 50), new Cell(42, 50) };
        world.Apply(new ResidentWalkStarted(1, path, StartTick: 10));

        engine.Enqueue(Cmd(1, "house", 41, 50));    // на пути
        engine.Enqueue(Cmd(2, "house", 41, 51));    // рядом с путём
        var result = engine.Step();

        Assert.Equal(CommandError.CellBlockedByResident, result.Results[0].Error);
        Assert.True(result.Results[1].Ok);
        Assert.Single(world.Buildings);
    }

    [Fact]
    public void Client_ApplyingHostEvents_GetsSameWorld()
    {
        var (hostWorld, engine) = Create();
        var allEvents = new List<SimEvent>();

        engine.Enqueue(Cmd(1, "house", 20, 16));
        engine.Enqueue(Cmd(2, "well", 10, 12));
        for (int i = 0; i < 400; i++)
            allEvents.AddRange(engine.Step().Events);

        Assert.Contains(allEvents, e => e is ResidentWalkStarted);
        Assert.Contains(allEvents, e => e is ResidentWalkFinished);

        var clientWorld = new World(WorldConfig.LoadDefault());
        foreach (var simEvent in allEvents)
            clientWorld.Apply(simEvent);

        var host = hostWorld.Residents[0];
        var client = clientWorld.Residents[0];
        Assert.Equal(host.Cell, client.Cell);
        Assert.Equal(host.IsWalking, client.IsWalking);
        Assert.Equal(host.Path, client.Path);
        Assert.Equal(hostWorld.Buildings, clientWorld.Buildings);
    }

    [Fact]
    public void SameSeed_GivesSameWalks()
    {
        ResidentWalkStarted RunAndGetFirstWalk()
        {
            var (_, engine) = Create(seed: 42);
            engine.Enqueue(Cmd(1, "house", 20, 16));
            engine.Enqueue(Cmd(2, "well", 10, 12));
            engine.Enqueue(Cmd(3, "woodcutter", 14, 25));
            return StepUntil<ResidentWalkStarted>(engine, 100)!;
        }

        var a = RunAndGetFirstWalk();
        var b = RunAndGetFirstWalk();

        Assert.Equal(a.StartTick, b.StartTick);
        Assert.Equal(a.Path, b.Path);
    }
}