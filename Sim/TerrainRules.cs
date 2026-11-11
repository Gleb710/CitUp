namespace CitUp.Sim;

/// <summary>Правила использования местности при строительстве.</summary>
public static class TerrainRules
{
    /// <summary>Можно ли строить на этой местности.</summary>
    public static bool CanBuildOn(TerrainType terrain) =>
        terrain == TerrainType.Plain;

    /// <summary>Все клетки площадки, лежащие на карте, — равнина. Клетки вне карты пропускаются.</summary>
    public static bool IsPlainOnly(TerrainGrid terrain, IEnumerable<Cell> cells)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(cells);

        foreach (var cell in cells)
        {
            if (!terrain.IsInside(cell))
                continue;

            if (!CanBuildOn(terrain.GetTerrain(cell)))
                return false;
        }

        return true;
    }

    /// <summary>Все клетки площадки, лежащие на карте, имеют одну высоту. Клетки вне карты пропускаются.</summary>
    public static bool HasSingleElevation(TerrainGrid terrain, IEnumerable<Cell> cells)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(cells);

        int? elevation = null;

        foreach (var cell in cells)
        {
            if (!terrain.IsInside(cell))
                continue;

            var current = terrain.GetElevation(cell);

            if (elevation is null)
                elevation = current;
            else if (elevation != current)
                return false;
        }

        return true;
    }
}