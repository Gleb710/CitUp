using System;
using System.Collections.Generic;
using System.Linq;

namespace CitUp.Sim;

/// <summary>Генерирует природную карту по seed и настройкам.</summary>
public static class MapGenerator
{
    private const int MinBoundaryHeight = 1;
    private const int MaxBoundaryHeight = 3;

    public static TerrainGrid Generate(MapGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var grid = new TerrainGrid(options.Width, options.Height);
        var random = new SeededRandom(options.Seed);

        GenerateElevationBoundaries(grid, random, options);
        GenerateResourceVeins(grid, random, options);
        GenerateLakes(grid, random, options);
        GenerateForests(grid, random, options);

        return grid;
    }

    private static void GenerateElevationBoundaries(
        TerrainGrid grid,
        SeededRandom random,
        MapGenerationOptions options)
    {
        var boundaries = new List<ElevationBoundary>();

        for (var boundaryIndex = 0; boundaryIndex < options.CliffClusterCount; boundaryIndex++)
        {
            var boundary = TryCreateElevationBoundary(
                grid,
                random,
                boundaries);

            if (boundary is not null)
                boundaries.Add(boundary);
        }

        ApplyElevationRaises(grid, boundaries);

        var boundaryCells = CreateBoundaryMask(grid, boundaries);
        SetBoundaryElevations(grid, boundaries, boundaryCells);
        ApplyBoundaryTerrain(grid, random, boundaries, options);
    }

    private static ElevationBoundary? TryCreateElevationBoundary(
        TerrainGrid grid,
        SeededRandom random,
        IReadOnlyList<ElevationBoundary> existingBoundaries)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var rows = new int[grid.Width];
            var y = random.NextInt(1, grid.Height - 1);
            var isValid = true;

            for (var x = 0; x < grid.Width; x++)
            {
                if (!IsRowAllowed(existingBoundaries, x, y))
                {
                    isValid = false;
                    break;
                }

                rows[x] = y;

                if (x == grid.Width - 1)
                    continue;

                var deltaY = random.NextInt(-1, 2);
                y = Math.Clamp(y + deltaY, 1, grid.Height - 2);
            }

            if (!isValid)
                continue;

            var heightDelta = random.NextInt(
                MinBoundaryHeight,
                MaxBoundaryHeight + 1);

            return new ElevationBoundary(rows, heightDelta);
        }

        return null;
    }

    private static bool IsRowAllowed(
        IReadOnlyList<ElevationBoundary> existingBoundaries,
        int x,
        int y)
    {
        foreach (var boundary in existingBoundaries)
        {
            var existingRow = boundary.Rows[x];

            if (Math.Abs(existingRow - y) < 2)
                return false;
        }

        return true;
    }

    private static void ApplyElevationRaises(
        TerrainGrid grid,
        List<ElevationBoundary> boundaries)
    {
        foreach (var boundary in boundaries)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                for (var y = 0; y < boundary.Rows[x]; y++)
                {
                    var cell = new Cell(x, y);
                    grid.SetElevation(
                        cell,
                        grid.GetElevation(cell) + boundary.HeightDelta);
                }
            }
        }
    }

    private static bool[,] CreateBoundaryMask(
        TerrainGrid grid,
        List<ElevationBoundary> boundaries)
    {
        var mask = new bool[grid.Width, grid.Height];

        foreach (var boundary in boundaries)
        {
            for (var x = 0; x < grid.Width; x++)
                mask[x, boundary.Rows[x]] = true;
        }

        return mask;
    }

    private static void SetBoundaryElevations(
        TerrainGrid grid,
        List<ElevationBoundary> boundaries,
        bool[,] boundaryCells)
    {
        foreach (var boundary in boundaries)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var cell = new Cell(x, boundary.Rows[x]);
                var elevation = GetMinNeighborElevation(
                    grid,
                    cell,
                    boundaryCells);

                grid.SetElevation(cell, elevation);
            }
        }
    }

    private static void ApplyBoundaryTerrain(
        TerrainGrid grid,
        SeededRandom random,
        List<ElevationBoundary> boundaries,
        MapGenerationOptions options)
    {
        foreach (var boundary in boundaries)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                var cell = new Cell(x, boundary.Rows[x]);

                var isSlope =
                    boundary.HeightDelta == 1 &&
                    random.NextDouble() < options.SlopeChance;

                grid.SetTerrain(
                    cell,
                    isSlope ? TerrainType.Slope : TerrainType.Cliff);
            }
        }
    }

    private static int GetMinNeighborElevation(
        TerrainGrid grid,
        Cell cell,
        bool[,] boundaryCells)
    {
        int? minElevation = null;

        foreach (var neighbor in GetOrthogonalNeighbors(grid, cell))
        {
            if (boundaryCells[neighbor.X, neighbor.Y])
                continue;

            var elevation = grid.GetElevation(neighbor);
            minElevation = minElevation is null
                ? elevation
                : Math.Min(minElevation.Value, elevation);
        }

        return minElevation ?? 0;
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
        foreach (var neighbor in GetOrthogonalNeighbors(grid, cell))
        {
            if (grid.GetTerrain(neighbor) == terrainType)
                return new Cell(
                    neighbor.X - cell.X,
                    neighbor.Y - cell.Y);
        }

        return null;
    }

    private static IEnumerable<Cell> GetOrthogonalNeighbors(
        TerrainGrid grid,
        Cell cell)
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

            if (grid.IsInside(neighbor))
                yield return neighbor;
        }
    }

    private sealed record ElevationBoundary(
        int[] Rows,
        int HeightDelta);
}