namespace CitUp.Sim;

/// <summary>Поворачивает маску здания на 0, 90, 180 или 270 градусов.</summary>
public static class BuildingMaskRotator
{
    public static BuildingMask Rotate(BuildingMask mask, int degrees)
    {
        ArgumentNullException.ThrowIfNull(mask);

        var normalizedDegrees = ((degrees % 360) + 360) % 360;

        return normalizedDegrees switch
        {
            0 => mask,
            90 => RotateClockwise(mask),
            180 => Rotate180(mask),
            270 => RotateCounterClockwise(mask),
            _ => throw new ArgumentOutOfRangeException(
                nameof(degrees),
                degrees,
                "Маску можно поворачивать только на 0, 90, 180 или 270 градусов.")
        };
    }

    private static BuildingMask RotateClockwise(BuildingMask mask)
    {
        var rows = mask.Rows;
        var newWidth = mask.Height;
        var newHeight = mask.Width;
        var result = new List<string>(newHeight);

        for (var y = 0; y < newHeight; y++)
        {
            var row = new char[newWidth];

            for (var x = 0; x < newWidth; x++)
                row[x] = rows[rows.Count - 1 - x][y];

            result.Add(new string(row));
        }

        return new BuildingMask(result);
    }

    private static BuildingMask Rotate180(BuildingMask mask)
    {
        var result = mask.Rows
            .Reverse()
            .Select(row => new string(row.Reverse().ToArray()))
            .ToList();

        return new BuildingMask(result);
    }

    private static BuildingMask RotateCounterClockwise(BuildingMask mask)
    {
        var rows = mask.Rows;
        var newWidth = mask.Height;
        var newHeight = mask.Width;
        var result = new List<string>(newHeight);

        for (var y = 0; y < newHeight; y++)
        {
            var row = new char[newWidth];

            for (var x = 0; x < newWidth; x++)
                row[x] = rows[x][mask.Width - 1 - y];

            result.Add(new string(row));
        }

        return new BuildingMask(result);
    }
}