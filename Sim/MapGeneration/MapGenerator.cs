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
        GenerateCliffs(grid, random, options);
        GenerateResourceVeins(grid, random, options);
        GenerateSlopes(grid, random, options);
        GenerateForests(grid, random, options);

        return grid;
    }

    private static void GenerateForests(
        TerrainGrid grid,
        SeededRandom random,
        MapGenerationOptions options)
    {
        for (var clusterIndex = 0; clusterIndex < options.ForestClusterCount; clusterIndex++)
        {
            var start = FindTerrainCell(
                grid,
                random,
                TerrainType.Plain);

            if (start is null)
                return;

            var isTwoByTwoCluster = random.NextInt(0, 2) == 1;

            if (isTwoByTwoCluster &&
                CanPlaceTwoByTwoForest(grid, start.Value))
            {
                PlaceTwoByTwoForest(grid, start.Value);
            }
            else
            {
                grid.SetTerrain(start.Value, TerrainType.Forest);
            }
        }
    }

    private static bool CanPlaceTwoByTwoForest(
        TerrainGrid grid,
        Cell start)
    {
        var cells = new[]
        {
            start,
            new Cell(start.X + 1, start.Y),
            new Cell(start.X, start.Y + 1),
            new Cell(start.X + 1, start.Y + 1)
        };

        return cells.All(cell =>
            grid.IsInside(cell) &&
            grid.GetTerrain(cell) == TerrainType.Plain);
    }

    private static void PlaceTwoByTwoForest(
        TerrainGrid grid,
        Cell start)
    {
        grid.SetTerrain(start, TerrainType.Forest);
        grid.SetTerrain(new Cell(start.X + 1, start.Y), TerrainType.Forest);
        grid.SetTerrain(new Cell(start.X, start.Y + 1), TerrainType.Forest);
        grid.SetTerrain(new Cell(start.X + 1, start.Y + 1), TerrainType.Forest);
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

            var start = FindTerrainCell(
                grid,
                random,
                TerrainType.Plain);

            if (start is null)
                return;

            GrowTerrain(
                grid,
                random,
                start.Value,
                targetSize,
                TerrainType.Water,
                TerrainType.Plain);
        }
    }

    private static void GenerateCliffs(
        TerrainGrid grid,
        SeededRandom random,
        MapGenerationOptions options)
    {
        for (var cliffIndex = 0; cliffIndex < options.CliffClusterCount; cliffIndex++)
        {
            var targetSize = random.NextInt(
                options.MinCliffSize,
                options.MaxCliffSize + 1);

            var start = FindTerrainCell(
                grid,
                random,
                TerrainType.Plain);

            if (start is null)
                return;

            GrowTerrain(
                grid,
                random,
                start.Value,
                targetSize,
                TerrainType.Cliff,
                TerrainType.Plain);
        }
    }

    private static void GenerateResourceVeins(
        TerrainGrid grid,
        SeededRandom random,
        MapGenerationOptions options)
    {
        for (var veinIndex = 0; veinIndex < options.ResourceVeinCount; veinIndex++)
        {
            var veinStart = FindResourceVeinStart(grid, random);

            if (veinStart is null)
                return;

            var direction = GetOrthogonalDirection(
                grid,
                veinStart.Value,
                TerrainType.Cliff);

            if (direction is null)
                continue;

            var secondCell = new Cell(
                veinStart.Value.X + direction.Value.X,
                veinStart.Value.Y + direction.Value.Y);

            grid.SetTerrain(veinStart.Value, TerrainType.ResourceCliff);
            grid.SetTerrain(secondCell, TerrainType.ResourceCliff);
        }
    }

    private static void GenerateSlopes(
        TerrainGrid grid,
        SeededRandom random,
        MapGenerationOptions options)
    {
        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var cell = new Cell(x, y);

                if (grid.GetTerrain(cell) != TerrainType.Plain)
                    continue;

                var hasCliffNeighbor =
                    HasOrthogonalNeighbor(grid, cell, TerrainType.Cliff) ||
                    HasOrthogonalNeighbor(grid, cell, TerrainType.ResourceCliff);

                if (hasCliffNeighbor &&
                    random.NextDouble() < options.SlopeChance)
                {
                    grid.SetTerrain(cell, TerrainType.Slope);
                }
            }
        }
    }

    private static Cell? FindTerrainCell(
        TerrainGrid grid,
        SeededRandom random,
        TerrainType terrainType)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var cell = new Cell(
                random.NextInt(0, grid.Width),
                random.NextInt(0, grid.Height));

            if (grid.GetTerrain(cell) == terrainType)
                return cell;
        }

        return null;
    }

    private static void GrowTerrain(
        TerrainGrid grid,
        SeededRandom random,
        Cell start,
        int targetSize,
        TerrainType terrainType,
        TerrainType replaceableTerrain)
    {
        var frontier = new List<Cell>();
        var generatedCount = 0;

        grid.SetTerrain(start, terrainType);
        generatedCount++;

        AddNeighborsToFrontier(
            grid,
            start,
            frontier,
            replaceableTerrain);

        while (generatedCount < targetSize && frontier.Count > 0)
        {
            var frontierIndex = random.NextInt(0, frontier.Count);
            var cell = frontier[frontierIndex];
            frontier.RemoveAt(frontierIndex);

            if (grid.GetTerrain(cell) != replaceableTerrain)
                continue;

            grid.SetTerrain(cell, terrainType);
            generatedCount++;

            AddNeighborsToFrontier(
                grid,
                cell,
                frontier,
                replaceableTerrain);
        }
    }

    
    
    private static void AddNeighborsToFrontier(
        TerrainGrid grid,
        Cell cell,
        List<Cell> frontier,
        TerrainType replaceableTerrain)
    {
        AddIfTerrain(
            grid,
            new Cell(cell.X - 1, cell.Y),
            frontier,
            replaceableTerrain);

        AddIfTerrain(
            grid,
            new Cell(cell.X + 1, cell.Y),
            frontier,
            replaceableTerrain);

        AddIfTerrain(
            grid,
            new Cell(cell.X, cell.Y - 1),
            frontier,
            replaceableTerrain);

        AddIfTerrain(
            grid,
            new Cell(cell.X, cell.Y + 1),
            frontier,
            replaceableTerrain);
    }

    private static void AddIfTerrain(
        TerrainGrid grid,
        Cell cell,
        List<Cell> frontier,
        TerrainType terrainType)
    {
        if (grid.IsInside(cell) &&
            grid.GetTerrain(cell) == terrainType)
        {
            frontier.Add(cell);
        }
    }

    private static Cell? FindResourceVeinStart(
        TerrainGrid grid,
        SeededRandom random)
    {
        var candidates = new List<Cell>();

        for (var y = 0; y < grid.Height; y++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var cell = new Cell(x, y);

                if (grid.GetTerrain(cell) != TerrainType.Cliff)
                    continue;

                if (GetOrthogonalDirection(
                        grid,
                        cell,
                        TerrainType.Cliff) is not null)
                {
                    candidates.Add(cell);
                }
            }
        }

        if (candidates.Count == 0)
            return null;

        var candidateIndex = random.NextInt(0, candidates.Count);

        return candidates[candidateIndex];
    }

    private static Cell? GetOrthogonalDirection(
        TerrainGrid grid,
        Cell cell,
        TerrainType terrainType)
    {
        var directions = new[]
        {
            new Cell(-1, 0),
            new Cell(1, 0),
            new Cell(0, -1),
            new Cell(0, 1)
        };

        foreach (var direction in directions)
        {
            var neighbor = new Cell(
                cell.X + direction.X,
                cell.Y + direction.Y);

            if (grid.IsInside(neighbor) &&
                grid.GetTerrain(neighbor) == terrainType)
            {
                return direction;
            }
        }

        return null;
    }

    private static bool HasOrthogonalNeighbor(
        TerrainGrid grid,
        Cell cell,
        TerrainType terrainType) =>
        GetOrthogonalDirection(grid, cell, terrainType) is not null;
}