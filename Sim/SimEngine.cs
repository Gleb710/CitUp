namespace CitUp.Sim;

public sealed class SimEngine
{
    private readonly Queue<BuildCommand> _commands = new();
    private readonly Queue<AddResourceCommand> _resourceCommands = new();
    private readonly Random _random;

    // Решения жителя (когда пойти в следующий раз) живут здесь, а не в World:
    // их принимает только хост, клиентам эти данные не нужны.
    private readonly Dictionary<int, long> _nextWalkTick = new();

    public World World { get; }

    /// <param name="seed">Зерно генератора случайных чисел: с одним зерном и одинаковыми командами симуляция идёт одинаково.</param>
    public SimEngine(World world, int seed = 1)
    {
        World = world;
        _random = new Random(seed);
    }

    /// <summary>Поставить команду в очередь. Проверка и применение произойдут на ближайшем Step().</summary>
    public void Enqueue(BuildCommand command) => _commands.Enqueue(command);

    /// <summary>Один тик: время идёт вперёд, команды проверяются по порядку, затем живут жители.</summary>
    public StepResult Step()
    {
        World.AdvanceTick();

        var events = new List<SimEvent>();
        var results = new List<CommandResult>();

        ProcessCommands(events, results);
        ProcessResourceCommands(events, results);
        UpdateResidents(events);

        return new StepResult(events, results);
    }

    private void ProcessResourceCommands(
        List<SimEvent> events,
        List<CommandResult> results)
    {
        while (_resourceCommands.Count > 0)
        {
            var command = _resourceCommands.Dequeue();
            var error = ValidateResourceCommand(command);

            if (error == CommandError.None)
            {
                var added = new ResourceAdded(
                    command.BuildingId,
                    command.ResourceId,
                    command.Amount);

                World.Apply(added);
                events.Add(added);
            }

            results.Add(new CommandResult(command.CommandId, error));
        }
    }

    private CommandError ValidateResourceCommand(AddResourceCommand command)
    {
        if (World.FindBuilding(command.BuildingId) is null)
            return CommandError.UnknownBuilding;

        if (command.Amount <= 0)
            return CommandError.InvalidAmount;

        return CommandError.None;
    }
    
    public void Enqueue(AddResourceCommand command) =>
        _resourceCommands.Enqueue(command);
    
    private void ProcessCommands(List<SimEvent> events, List<CommandResult> results)
    {
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
    }

    private CommandError Validate(BuildCommand command)
    {
        if (World.Config.FindBuildingType(command.BuildingType) is null)
            return CommandError.UnknownType;

        var definition = World.BuildingCatalog.FindBuilding(command.BuildingType);
        if (definition is null)
            return CommandError.UnknownType;

        var blocked = BuildingGeometry.GetBlockedCells(definition, command.Cell);
        var passable = BuildingGeometry.GetPassableCells(definition, command.Cell);
        var footprint = blocked.Concat(passable).ToArray();
        var clearance = BuildingGeometry.GetClearanceCells(definition, command.Cell);

        if (footprint.Any(cell => !World.IsInside(cell)))
            return CommandError.OutOfBounds;

        if (footprint.Any(cell => World.FootprintAt(cell) is not null))
            return CommandError.CellOccupied;

        if (clearance.Any(cell => World.FootprintAt(cell) is not null) ||
            footprint.Any(World.IsInClearance))
            return CommandError.ClearanceBlocked;
        
        if (blocked.Any(World.IsBlockedByResident))
            return CommandError.CellBlockedByResident;

        return CommandError.None;
    }

    private void UpdateResidents(List<SimEvent> events)
    {
        long tick = World.CurrentTick;
        var settings = World.Config.Resident;

        foreach (var resident in World.Residents)
        {
            if (resident.IsWalking)
            {
                if (tick >= resident.WalkEndTick(settings.TicksPerCell))
                {
                    var path = resident.Path!;
                    var finished = new ResidentWalkFinished(resident.Id, path[path.Count - 1]);
                    World.Apply(finished);
                    events.Add(finished);
                    _nextWalkTick[resident.Id] = tick + settings.PauseTicks;
                }
            }
            else if (tick >= _nextWalkTick.GetValueOrDefault(resident.Id, settings.PauseTicks))
            {
                var started = TryCreateWalk(resident);
                if (started is not null)
                {
                    World.Apply(started);
                    events.Add(started);
                }
                else
                {
                    // Идти некуда (нет зданий или нет пути): попробуем снова после паузы.
                    _nextWalkTick[resident.Id] = tick + settings.PauseTicks;
                }
            }
        }
    }

    /// <summary>Выбирает случайное здание и строит путь к свободной клетке рядом с ним. Если не получилось, null.</summary>
    private ResidentWalkStarted? TryCreateWalk(Resident resident)
    {
        var buildings = World.Buildings;
        if (buildings.Count == 0)
            return null;

        var target = buildings[_random.Next(buildings.Count)];

        var goals = new List<Cell>();
        foreach (var d in GridPathfinder.Directions)
        {
            var neighbor = new Cell(target.Cell.X + d.X, target.Cell.Y + d.Y);
            if (neighbor != resident.Cell && GridPathfinder.IsWalkable(World, neighbor))
                goals.Add(neighbor);
        }
        if (goals.Count == 0)
            return null;

        var goal = goals[_random.Next(goals.Count)];
        var path = GridPathfinder.FindPath(World, resident.Cell, goal);
        if (path is null)
            return null;

        return new ResidentWalkStarted(resident.Id, path, World.CurrentTick);
    }
}