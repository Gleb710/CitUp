using System.Text;
using CitUp.Sim;
using Xunit;

namespace Tests;

public class SerializationTests
{
    private static NetMessage RoundTrip(NetMessage message) =>
        NetSerializer.Deserialize(NetSerializer.Serialize(message));

    /// <summary>
    /// Сообщение должно пережить круг «в байты и обратно»: тот же тип, а повторная сериализация даёт тот же JSON.
    /// (Сравнивать сами объекты нельзя: у записей со списками сравнение идёт по ссылкам.)
    /// </summary>
    private static T AssertRoundTrips<T>(T message) where T : NetMessage
    {
        var back = Assert.IsType<T>(RoundTrip(message));
        Assert.Equal(NetSerializer.ToJson(message), NetSerializer.ToJson(back));
        return back;
    }

    private static (World World, SimEngine Engine) HostWithWalkingResident()
    {
        var world = new World(WorldConfig.LoadDefault());
        var engine = new SimEngine(world, seed: 7);
        engine.Enqueue(new BuildCommand(1, 1, "house", new Cell(20, 16)));
        engine.Enqueue(new BuildCommand(2, 2, "well", new Cell(10, 12)));

        for (int i = 0; i < 100 && !world.Residents[0].IsWalking; i++)
            engine.Step();

        Assert.True(world.Residents[0].IsWalking);
        return (world, engine);
    }

    // ---------- сообщения ----------

    [Fact]
    public void Hello_RoundTrips()
    {
        var back = AssertRoundTrips(new HelloMessage(NetProtocol.Version, "Глеб"));

        Assert.Equal(NetProtocol.Version, back.ProtocolVersion);
        Assert.Equal("Глеб", back.PlayerName);
    }

    [Fact]
    public void Command_RoundTrips_IncludingCell()
    {
        var command = new BuildCommand(5, 2, "well", new Cell(5, 7));

        var back = AssertRoundTrips(new CommandMessage(command));

        Assert.Equal(command, back.Command);
    }
    
    [Fact]
    public void Command_RoundTrips_IncludingRotation()
    {
        var command = new BuildCommand(5, 2, "fisherman", new Cell(5, 7), 90);

        var back = AssertRoundTrips(new CommandMessage(command));

        Assert.Equal(90, back.Command.Rotation);
        Assert.Equal(command, back.Command);
    }

    [Fact]
    public void CommandRejected_RoundTrips_AndErrorIsReadableInJson()
    {
        var message = new CommandRejectedMessage(9, CommandError.CellOccupied);

        var back = AssertRoundTrips(message);

        Assert.Equal(CommandError.CellOccupied, back.Error);
        Assert.Contains("CellOccupied", NetSerializer.ToJson(message));
    }

    [Fact]
    public void Events_RoundTrip_WithAllEventTypes()
    {
        var events = new SimEvent[]
        {
            new BuildingBuilt(1, "house", new Cell(3, 4), 2),
            new ResidentWalkStarted(1, new[] { new Cell(16, 16), new Cell(17, 16) }, 120),
            new ResidentWalkFinished(1, new Cell(17, 16)),
            new BuildingBuilt(2, "fisherman", new Cell(8, 9), 2, 90)
        };

        var back = AssertRoundTrips(new EventsMessage(121, events));

        Assert.Equal(121, back.Tick);
        Assert.Equal(4, back.Events.Count);
        Assert.Equal(events[0], back.Events[0]);

        var started = Assert.IsType<ResidentWalkStarted>(back.Events[1]);
        Assert.Equal(new[] { new Cell(16, 16), new Cell(17, 16) }, started.Path);
        Assert.Equal(120, started.StartTick);

        Assert.Equal(events[2], back.Events[2]);
        Assert.Equal(events[3], back.Events[3]);
        Assert.Equal(90, Assert.IsType<BuildingBuilt>(back.Events[3]).Rotation);
    }

    [Fact]
    public void Heartbeat_RoundTrips()
    {
        var back = AssertRoundTrips(new HeartbeatMessage(1234, 7, -98765));

        Assert.Equal(1234, back.HostTick);
        Assert.Equal(7, back.BuildingCount);
        Assert.Equal(-98765, back.StateHash);
    }

    [Fact]
    public void PingAndPong_RoundTrip()
    {
        Assert.Equal(555, AssertRoundTrips(new PingMessage(555)).ClientTimeMs);
        Assert.Equal(555, AssertRoundTrips(new PongMessage(555)).ClientTimeMs);
    }
    
    [Fact]
    public void ResyncRequest_RoundTrips()
    {
        AssertRoundTrips(new ResyncRequestMessage());
    }
    
    [Fact]
    public void JoinRejected_RoundTrips()
    {
        var back = AssertRoundTrips(new JoinRejectedMessage("Версия игры не совпадает"));

        Assert.Equal("Версия игры не совпадает", back.Reason);
    }

    // ---------- плохие данные ----------

    [Fact]
    public void Deserialize_NotJson_ThrowsInvalidData()
    {
        Assert.Throws<InvalidDataException>(() =>
            NetSerializer.Deserialize(Encoding.UTF8.GetBytes("not json")));
    }

    [Fact]
    public void Deserialize_UnknownType_ThrowsInvalidData()
    {
        Assert.Throws<InvalidDataException>(() =>
            NetSerializer.Deserialize(Encoding.UTF8.GetBytes("""{"$type":"nope"}""")));
    }

    // ---------- снимок мира ----------

    [Fact]
    public void Snapshot_OfNewWorld_HasOneStandingResident_AndNoBuildings()
    {
        var snapshot = new World(WorldConfig.LoadDefault()).ToSnapshot();

        Assert.Equal(0, snapshot.Tick);
        Assert.Empty(snapshot.Buildings);
        var resident = Assert.Single(snapshot.Residents);
        Assert.Null(resident.Path);
    }

    [Fact]
    public void Snapshot_ThroughJson_ReproducesWorld_WithWalkingResident()
    {
        var (host, _) = HostWithWalkingResident();

        var welcome = new WelcomeMessage(2, host.ToSnapshot());
        var received = Assert.IsType<WelcomeMessage>(RoundTrip(welcome));
        var client = World.FromSnapshot(host.Config, received.Snapshot);

        Assert.Equal(2, received.PlayerId);
        Assert.Equal(host.CurrentTick, client.CurrentTick);
        Assert.Equal(host.NextBuildingId, client.NextBuildingId);
        Assert.Equal(host.Buildings, client.Buildings);

        var hostResident = host.Residents[0];
        var clientResident = client.Residents[0];
        Assert.True(clientResident.IsWalking);
        Assert.Equal(hostResident.Cell, clientResident.Cell);
        Assert.Equal(hostResident.WalkStartTick, clientResident.WalkStartTick);
        Assert.Equal(hostResident.Path, clientResident.Path);

        // Снимок восстановленного мира совпадает с исходным снимком
        Assert.Equal(
            NetSerializer.ToJson(welcome),
            NetSerializer.ToJson(new WelcomeMessage(2, client.ToSnapshot())));
    }

    [Fact]
    public void LateJoiner_AfterSnapshot_StaysInSync_WithHostEvents()
    {
        var (host, engine) = HostWithWalkingResident();

        // Клиент подключается посреди прогулки жителя
        var welcome = Assert.IsType<WelcomeMessage>(RoundTrip(new WelcomeMessage(2, host.ToSnapshot())));
        var client = World.FromSnapshot(host.Config, welcome.Snapshot);

        // Дальше хост играет, а клиент только применяет присланные события (через сеть)
        engine.Enqueue(new BuildCommand(3, 1, "woodcutter", new Cell(14, 25)));
        var received = new List<SimEvent>();
        for (int i = 0; i < 300; i++)
        {
            var events = engine.Step().Events;
            if (events.Count == 0)
                continue;

            var message = Assert.IsType<EventsMessage>(RoundTrip(new EventsMessage(host.CurrentTick, events)));
            received.AddRange(message.Events);
        }

        foreach (var simEvent in received)
            client.Apply(simEvent);

        Assert.Contains(received, e => e is ResidentWalkFinished);
        Assert.Contains(received, e => e is ResidentWalkStarted);
        Assert.Equal(host.Buildings, client.Buildings);
        Assert.Equal(host.Residents[0].Cell, client.Residents[0].Cell);
        Assert.Equal(host.Residents[0].IsWalking, client.Residents[0].IsWalking);
        Assert.Equal(host.Residents[0].Path, client.Residents[0].Path);
    }

    [Fact]
    public void FromSnapshot_TwoBuildingsInOneCell_ThrowsInvalidData()
    {
        var config = WorldConfig.LoadDefault();
        var bad = new WorldSnapshot(
            0, 3,
            new[] { new Building(1, "house", new Cell(2, 2), 1), new Building(2, "well", new Cell(2, 2), 1) },
            new[] { new ResidentSnapshot(1, new Cell(16, 16), null, 0) });

        Assert.Throws<InvalidDataException>(() => World.FromSnapshot(config, bad));
    }

    [Fact]
    public void FromSnapshot_BuildingOutsideMap_ThrowsInvalidData()
    {
        var config = WorldConfig.LoadDefault();
        var bad = new WorldSnapshot(
            0, 2,
            new[] { new Building(1, "house", new Cell(99, 99), 1) },
            new[] { new ResidentSnapshot(1, new Cell(16, 16), null, 0) });

        Assert.Throws<InvalidDataException>(() => World.FromSnapshot(config, bad));
    }
    
    [Fact]
    public void Welcome_ThroughJson_KeepsGeneratedTerrain()
    {
        var host = PartyFactory.CreateNewParty(GameDataCatalog.LoadDefault(), 12345);

        var welcome = Assert.IsType<WelcomeMessage>(RoundTrip(new WelcomeMessage(2, host.ToSnapshot())));
        var client = World.FromSnapshot(host.Config, welcome.Snapshot);

        TerrainSnapshotTests.AssertSameTerrain(host.Terrain, client.Terrain);
    }
}