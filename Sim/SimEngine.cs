namespace CitUp.Sim;

public sealed class SimEngine
{
    private readonly Queue<BuildCommand> _commands = new();

    public World World { get; }

    public SimEngine(World world)
    {
        World = world;
    }

    /// <summary>Поставить команду в очередь. Проверка и применение произойдут на ближайшем Step().</summary>
    public void Enqueue(BuildCommand command) => _commands.Enqueue(command);

    /// <summary>Один тик: время идёт вперёд, команды из очереди проверяются по порядку.</summary>
    public StepResult Step()
    {
        World.AdvanceTick();

        var events = new List<SimEvent>();
        var results = new List<CommandResult>();

        while (_commands.Count > 0)
        {
            var command = _commands.Dequeue();
            var error = Validate(command);

            if (error == CommandError.None)
            {
                var built = new BuildingBuilt(World.NextBuildingId, command.BuildingType, command.Cell, command.PlayerId);
                World.Apply(built);
                events.Add(built);
            }

            results.Add(new CommandResult(command.CommandId, error));
        }

        // Позже здесь: поведение жителей (Issue 3).
        return new StepResult(events, results);
    }

    private CommandError Validate(BuildCommand command)
    {
        if (World.Config.FindBuildingType(command.BuildingType) is null)
            return CommandError.UnknownType;
        if (!World.IsInside(command.Cell))
            return CommandError.OutOfBounds;
        if (World.BuildingAt(command.Cell) is not null)
            return CommandError.CellOccupied;
        return CommandError.None;
    }
}