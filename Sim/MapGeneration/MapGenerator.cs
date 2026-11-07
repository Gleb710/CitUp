namespace CitUp.Sim;

/// <summary>Генерирует природную карту по seed и настройкам.</summary>
public static class MapGenerator
{
    public static TerrainGrid Generate(MapGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var grid = new TerrainGrid(options.Width, options.Height);
        var random = new SeededRandom(options.Seed);

        GenerateLakes(grid, random, options);

        return grid;
    }

    private static void GenerateLakes(
        TerrainGrid grid,
        SeededRandom random,
        MapGenerationOptions options)
    {
        for (var lakeIndex = 0; lakeIndex < options.LakeCount; lakeIndex++)
        {
            var targetSize = random.NextInt(
                options.MinLakeSize,
                options.MaxLakeSize + 1);

            var start = FindPlainCell(grid, random);

            if (start is null)
                return;

            GenerateLake(grid, random, start.Value, targetSize);
        }
    }

    private static Cell? FindPlainCell(TerrainGrid grid, SeededRandom random)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var cell = new Cell(
                random.NextInt(0, grid.Width),
                random.NextInt(0, grid.Height));

            if (grid.GetTerrain(cell) == TerrainType.Plain)
                return cell;
        }

        return null;
    }

    private static void GenerateLake(
        TerrainGrid grid,
        SeededRandom random,
        Cell start,
        int targetSize)
    {
        var frontier = new List<Cell>();
        var waterCount = 0;

        grid.SetTerrain(start, TerrainType.Water);
        waterCount++;

        AddNeighborsToFrontier(grid, start, frontier);

        while (waterCount < targetSize && frontier.Count > 0)
        {
            var frontierIndex = random.NextInt(0, frontier.Count);
            var cell = frontier[frontierIndex];
            frontier.RemoveAt(frontierIndex);

            if (grid.GetTerrain(cell) != TerrainType.Plain)
                continue;

            grid.SetTerrain(cell, TerrainType.Water);
            waterCount++;

            AddNeighborsToFrontier(grid, cell, frontier);
        }
    }

    private static void AddNeighborsToFrontier(
        TerrainGrid grid,
        Cell cell,
        List<Cell> frontier)
    {
        AddIfPlain(grid, new Cell(cell.X - 1, cell.Y), frontier);
        AddIfPlain(grid, new Cell(cell.X + 1, cell.Y), frontier);
        AddIfPlain(grid, new Cell(cell.X, cell.Y - 1), frontier);
        AddIfPlain(grid, new Cell(cell.X, cell.Y + 1), frontier);
    }

    private static void AddIfPlain(
        TerrainGrid grid,
        Cell cell,
        List<Cell> frontier)
    {
        if (grid.IsInside(cell) &&
            grid.GetTerrain(cell) == TerrainType.Plain)
        {
            frontier.Add(cell);
        }
    }
}