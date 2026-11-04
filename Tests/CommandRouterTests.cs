using CitUp.Sim;
using Xunit;

namespace Tests;

public class CommandRouterTests
{
    [Fact]
    public void Register_GivesUniqueIds_EvenWhenSourcesNumberTheirCommandsTheSame()
    {
        var router = new CommandRouter();

        int a = router.Register(sourceId: 1001, sourceCommandId: 1);
        int b = router.Register(sourceId: 1002, sourceCommandId: 1);
        int local = router.Register(CommandRouter.LocalSource, 0);

        Assert.Equal(3, new[] { a, b, local }.Distinct().Count());
        Assert.Equal(3, router.PendingCount);
    }

    [Fact]
    public void TryComplete_ReturnsAuthor_AndForgetsTheCommand()
    {
        var router = new CommandRouter();
        int id = router.Register(1002, 7);

        Assert.True(router.TryComplete(id, out int sourceId, out int sourceCommandId));
        Assert.Equal(1002, sourceId);
        Assert.Equal(7, sourceCommandId);

        Assert.False(router.TryComplete(id, out _, out _));
        Assert.Equal(0, router.PendingCount);
    }

    [Fact]
    public void TryComplete_UnknownId_ReturnsFalse()
    {
        Assert.False(new CommandRouter().TryComplete(999, out _, out _));
    }

    [Fact]
    public void Forget_RemovesOnlyThatSource()
    {
        var router = new CommandRouter();
        int stays = router.Register(1001, 1);
        router.Register(1002, 1);
        router.Register(1002, 2);

        router.Forget(1002);

        Assert.Equal(1, router.PendingCount);
        Assert.True(router.TryComplete(stays, out int sourceId, out _));
        Assert.Equal(1001, sourceId);
    }

    [Fact]
    public void TwoClientsClickSameCell_FirstWins_SecondGetsRejectionAddressedToIt()
    {
        var world = new World(WorldConfig.LoadDefault());
        var engine = new SimEngine(world);
        var router = new CommandRouter();
        const int peerA = 1001;
        const int peerB = 1002;

        // Оба клиента нумеруют свои команды с 1; PlayerId (2 и 3) хост подставляет сам по соединению
        int idA = router.Register(peerA, 1);
        engine.Enqueue(new BuildCommand(idA, 2, "house", new Cell(5, 5)));
        int idB = router.Register(peerB, 1);
        engine.Enqueue(new BuildCommand(idB, 3, "well", new Cell(5, 5)));

        var result = engine.Step();

        Assert.Equal(2, result.Results.Count);

        Assert.True(router.TryComplete(result.Results[0].CommandId, out int sourceA, out int commandA));
        Assert.True(result.Results[0].Ok);
        Assert.Equal(peerA, sourceA);
        Assert.Equal(1, commandA);

        Assert.True(router.TryComplete(result.Results[1].CommandId, out int sourceB, out int commandB));
        Assert.Equal(CommandError.CellOccupied, result.Results[1].Error);
        Assert.Equal(peerB, sourceB);
        Assert.Equal(1, commandB);

        Assert.Equal(0, router.PendingCount);
        var building = Assert.Single(world.Buildings);
        Assert.Equal(2, building.OwnerId);
        Assert.Equal("house", building.Type);
    }
}