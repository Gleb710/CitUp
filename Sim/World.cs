namespace CitUp.Sim;

/// <summary>Всё состояние игрового мира. Позже сюда добавятся жители.</summary>
public sealed class World
{
    private readonly List<Building> _buildings = new();
    private readonly Dictionary<Cell, Building> _buildingByCell = new();

    public WorldConfig Config { get; }
    public int Width => Config.Map.Width;
    public int Height => Config.Map.Height;

    /// <summary>Номер текущего тика. Время в игре измеряется тиками, а не секундами и не кадрами.</summary>
    public long CurrentTick { get; private set; }

    public IReadOnlyList<Building> Buildings => _buildings;

    /// <summary>Id для следующего здания. Растёт по мере постройки.</summary>
    public int NextBuildingId { get; private set; } = 1;

    public World(WorldConfig config)
    {
        Config = config;
    }

    public bool IsInside(Cell cell) =>
        cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

    /// <summary>Здание в клетке или null, если клетка свободна.</summary>
    public Building? BuildingAt(Cell cell) => _buildingByCell.GetValueOrDefault(cell);

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

    // internal: двигать время может только SimEngine из этой же сборки
    internal void AdvanceTick() => CurrentTick++;
}