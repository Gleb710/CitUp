namespace CitUp.Sim;

/// <summary>Всё состояние игрового мира.</summary>
public sealed class World
{
    private readonly List<Building> _buildings = new();
    private readonly Dictionary<Cell, Building> _buildingByCell = new();
    private readonly Dictionary<int, ResourceStore> _stockpilesByBuildingId = new();
    private readonly Dictionary<int, ResourceStore> _stockpilesByOwnerId = new();
    private readonly List<Resident> _residents = new();

    public WorldConfig Config { get; }
    public TerrainGrid Terrain { get; }
    public int Width => Config.Map.Width;
    public int Height => Config.Map.Height;

    /// <summary>Номер текущего тика. Время в игре измеряется тиками, а не секундами и не кадрами.</summary>
    public long CurrentTick { get; private set; }

    public IReadOnlyList<Building> Buildings => _buildings;
    public IReadOnlyList<Resident> Residents => _residents;

    /// <summary>Id для следующего здания. Растёт по мере постройки.</summary>
    public int NextBuildingId { get; private set; } = 1;

    public World(WorldConfig config)
        : this(config, new TerrainGrid(config.Map.Width, config.Map.Height))
    {
    }

    public World(WorldConfig config, TerrainGrid terrain)
    {
        if (terrain.Width != config.Map.Width ||
            terrain.Height != config.Map.Height)
        {
            throw new ArgumentException(
                $"Размер карты {terrain.Width}×{terrain.Height} не совпадает " +
                $"с размером из конфигурации {config.Map.Width}×{config.Map.Height}.",
                nameof(terrain));
        }

        Config = config;
        Terrain = terrain;

        CreateInitialResident();
    }

    private void CreateInitialResident()
    {
        var center = new Cell(
            Config.Map.Width / 2,
            Config.Map.Height / 2);

        var resident = new Resident(
            1,
            center);

        _residents.Add(resident);
    }
    
    public bool IsInside(Cell cell) =>
        cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

    /// <summary>Здание в клетке или null, если клетка свободна.</summary>
    public Building? BuildingAt(Cell cell) => _buildingByCell.GetValueOrDefault(cell);

    public Building? FindBuilding(int buildingId) =>
        _buildings.FirstOrDefault(building => building.Id == buildingId);
    
    public ResourceStore GetStockpile(Building building)
    {
        ArgumentNullException.ThrowIfNull(building);

        if (FindBuilding(building.Id) is null)
            throw new InvalidOperationException(
                $"Склад можно получить только для существующего здания {building.Id}.");

        if (!_stockpilesByBuildingId.TryGetValue(building.Id, out var stockpile))
        {
            stockpile = new ResourceStore();
            _stockpilesByBuildingId[building.Id] = stockpile;
        }

        return stockpile;
    }

    public ResourceStore GetPlayerStockpile(int ownerId)
    {
        if (ownerId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(ownerId),
                ownerId,
                "Идентификатор игрока должен быть больше 0.");

        if (!_stockpilesByOwnerId.TryGetValue(ownerId, out var stockpile))
        {
            stockpile = new ResourceStore();
            _stockpilesByOwnerId[ownerId] = stockpile;
        }

        return stockpile;
    }
    
    public ResourceStore GetStockpile(int buildingId)
    {
        var building = FindBuilding(buildingId)
                       ?? throw new InvalidOperationException(
                           $"Здание {buildingId} не найдено.");

        return GetStockpile(building);
    }
    
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
    
        /// <summary>
    /// Контрольное число состояния мира. От номера тика не зависит: зависит только от того, что меняют события.
    /// Хост кладёт его в хартбит, клиент сравнивает со своим (обнаружение рассинхрона).
    /// </summary>
    public int StateHash()
    {
        uint hash = 2166136261;   // FNV-1a: простой быстрый хеш

        hash = Mix(hash, NextBuildingId);
        foreach (var building in _buildings)
        {
            hash = Mix(hash, building.Id);
            hash = MixText(hash, building.Type);
            hash = Mix(hash, building.Cell.X);
            hash = Mix(hash, building.Cell.Y);
            hash = Mix(hash, building.OwnerId);
        }

        foreach (var resident in _residents)
        {
            hash = Mix(hash, resident.Id);
            hash = Mix(hash, resident.Cell.X);
            hash = Mix(hash, resident.Cell.Y);

            // Стартовый тик и путь учитываем только пока житель идёт: после финиша в объекте остаётся
            // старое значение, а у подключившегося по снимку его нет
            if (resident.Path is { Count: > 0 } path)
            {
                hash = Mix(hash, 1);
                hash = Mix(hash, unchecked((int)resident.WalkStartTick));
                hash = Mix(hash, unchecked((int)(resident.WalkStartTick >> 32)));
                hash = Mix(hash, path.Count);
                foreach (var cell in path)
                {
                    hash = Mix(hash, cell.X);
                    hash = Mix(hash, cell.Y);
                }
            }
            else
            {
                hash = Mix(hash, 0);
            }
        }

        return unchecked((int)hash);
    }

    private static uint Mix(uint hash, int value)
    {
        unchecked
        {
            for (int i = 0; i < 4; i++)
            {
                hash ^= (uint)((value >> (8 * i)) & 0xFF);
                hash *= 16777619;
            }
        }
        return hash;
    }

    private static uint MixText(uint hash, string text)
    {
        hash = Mix(hash, text.Length);
        foreach (char c in text)
            hash = Mix(hash, c);
        return hash;
    }
    
    // internal: двигать время может только SimEngine из этой же сборки
    internal void AdvanceTick() => CurrentTick++;
}