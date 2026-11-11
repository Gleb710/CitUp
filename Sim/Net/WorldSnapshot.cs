namespace CitUp.Sim;

/// <summary>
/// Полный снимок состояния мира для подключающегося игрока.
/// Конфиг (phase1.json) в снимок не входит: он встроен в сборку игры и у всех одинаковый.
/// </summary>
public sealed record WorldSnapshot(
    long Tick,
    int NextBuildingId,
    IReadOnlyList<Building> Buildings,
    IReadOnlyList<ResidentSnapshot> Residents,
    TerrainSnapshot? Terrain = null);