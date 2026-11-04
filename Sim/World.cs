namespace CitUp.Sim;

/// <summary>Всё состояние игрового мира.</summary>
public sealed class World
{
    private readonly List<Building> _buildings = new();
    private readonly Dictionary<Cell, Building> _buildingByCell = new();
    private readonly List<Resident> _residents = new();

    public WorldConfig Config { get; }
    public int Width => Config.Map.Width;
    public int Height => Config.Map.Height;

    /// <summary>Номер текущего тика. Время в игре измеряется тиками, а не секундами и не кадрами.</summary>
    public long CurrentTick { get; private set; }

    public IReadOnlyList<Building> Buildings => _buildings;
    public IReadOnlyList<Resident> Residents => _residents;

    /// <summary>Id для следующего здания. Растёт по мере постройки.</summary>
    public int NextBuildingId { get; private set; } = 1;

    public World(WorldConfig config)
    {
        Config = config;

        // Начальное состояние: один житель в центре карты. Это не событие, он есть сразу при создании мира.
        _residents.Add(new Resident(1, new Cell(Width / 2, Height / 2)));
    }

    public bool IsInside(Cell cell) =>
        cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

    /// <summary>Здание в клетке или null, если клетка свободна.</summary>
    public Building? BuildingAt(Cell cell) => _buildingByCell.GetValueOrDefault(cell);

    public Resident? FindResident(int id) => _residents.FirstOrDefault(r => r.Id == id);

    /// <summary>Стоит ли в клетке житель или проходит ли через неё чей-то маршрут.</summary>
    public bool IsBlockedByResident(Cell cell) => _residents.Any(r => r.Blocks(cell));

    /// <summary>
    /// Единственный способ изменить состояние мира. Одни и те же события дают одно и то же состояние
    /// у хоста и у клиентов.
    /// </summary>
    public void Apply(SimEvent simEvent)
    {
        switch (simEvent)
        {
            case BuildingBuilt built:
                ApplyBuildingBuilt(built);
                break;
            case ResidentWalkStarted started:
                ApplyWalkStarted(started);
                break;
            case ResidentWalkFinished finished:
                ApplyWalkFinished(finished);
                break;
            default:
                throw new ArgumentException($"Неизвестное событие: {simEvent.GetType().Name}", nameof(simEvent));
        }
    }

    private void ApplyBuildingBuilt(BuildingBuilt e)
    {
        if (_buildingByCell.ContainsKey(e.Cell))
            throw new InvalidOperationException(
                $"Событие BuildingBuilt для занятой клетки {e.Cell}. Возможен рассинхрон миров.");

        var building = new Building(e.BuildingId, e.Type, e.Cell, e.OwnerId);
        _buildings.Add(building);
        _buildingByCell[e.Cell] = building;
        NextBuildingId = Math.Max(NextBuildingId, e.BuildingId + 1);
    }

    private void ApplyWalkStarted(ResidentWalkStarted e)
    {
        var resident = FindResident(e.ResidentId)
            ?? throw new InvalidOperationException($"ResidentWalkStarted: житель {e.ResidentId} не найден.");

        if (resident.IsWalking)
            throw new InvalidOperationException(
                $"ResidentWalkStarted: житель {e.ResidentId} уже идёт. Возможен рассинхрон миров.");
        if (e.Path.Count == 0 || e.Path[0] != resident.Cell)
            throw new InvalidOperationException(
                $"ResidentWalkStarted: путь жителя {e.ResidentId} не начинается с его клетки. Возможен рассинхрон миров.");

        resident.Path = e.Path;
        resident.WalkStartTick = e.StartTick;
    }

    private void ApplyWalkFinished(ResidentWalkFinished e)
    {
        var resident = FindResident(e.ResidentId)
            ?? throw new InvalidOperationException($"ResidentWalkFinished: житель {e.ResidentId} не найден.");

        if (resident.Path is null || resident.Path[resident.Path.Count - 1] != e.Cell)
            throw new InvalidOperationException(
                $"ResidentWalkFinished: житель {e.ResidentId} не идёт или финиш не совпадает с маршрутом. Возможен рассинхрон миров.");

        resident.Cell = e.Cell;
        resident.Path = null;
    }

    /// <summary>Снимок текущего состояния. Списки копируются, поэтому снимок не меняется при дальнейшей игре.</summary>
    public WorldSnapshot ToSnapshot() => new(
        CurrentTick,
        NextBuildingId,
        _buildings.ToList(),
        _residents.Select(r => new ResidentSnapshot(r.Id, r.Cell, r.Path?.ToList(), r.WalkStartTick)).ToList());

    /// <summary>Создаёт мир из снимка. Конфиг должен быть тем же, что у хоста.</summary>
    public static World FromSnapshot(WorldConfig config, WorldSnapshot snapshot)
    {
        var world = new World(config);
        world.LoadSnapshot(snapshot);
        return world;
    }

    private void LoadSnapshot(WorldSnapshot snapshot)
    {
        CurrentTick = snapshot.Tick;
        NextBuildingId = snapshot.NextBuildingId;

        _buildings.Clear();
        _buildingByCell.Clear();
        _residents.Clear();   // конструктор уже создал жителя по умолчанию, в снимке жители свои

        foreach (var building in snapshot.Buildings)
        {
            if (!IsInside(building.Cell))
                throw new InvalidDataException($"Снимок: здание {building.Id} вне карты, клетка {building.Cell}.");
            if (!_buildingByCell.TryAdd(building.Cell, building))
                throw new InvalidDataException($"Снимок: две постройки в одной клетке {building.Cell}.");
            _buildings.Add(building);
        }

        foreach (var data in snapshot.Residents)
        {
            var resident = new Resident(data.Id, data.Cell);
            if (data.Path is { Count: > 0 })
            {
                if (data.Path[0] != data.Cell)
                    throw new InvalidDataException($"Снимок: путь жителя {data.Id} не начинается с его клетки.");
                resident.Path = data.Path;
                resident.WalkStartTick = data.WalkStartTick;
            }
            _residents.Add(resident);
        }
    }
    
    // internal: двигать время может только SimEngine из этой же сборки
    internal void AdvanceTick() => CurrentTick++;
}