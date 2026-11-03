namespace CitUp.Sim;

public sealed class SimEngine
{
    private readonly Queue<BuildCommand> _commands = new();
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
        UpdateResidents(events);

        return new StepResult(events, results);
    }

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
        if (!World.IsInside(command.Cell))
            return CommandError.OutOfBounds;
        if (World.BuildingAt(command.Cell) is not null)
            return CommandError.CellOccupied;
        if (World.IsBlockedByResident(command.Cell))
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