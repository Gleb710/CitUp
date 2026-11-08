using CitUp.Sim;
using Godot;

namespace CitUp.Game;

/// <summary>Рисует статический рельеф карты. Перерисовывается только при смене карты.</summary>
public partial class TerrainView : Node2D
{
    private static readonly Color GrassA = new(0.35f, 0.56f, 0.30f);
    private static readonly Color GrassB = new(0.32f, 0.52f, 0.28f);
    private static readonly Color ForestColor = new(0.18f, 0.38f, 0.18f);
    private static readonly Color SlopeColor = new(0.62f, 0.52f, 0.35f);
    private static readonly Color CliffColor = new(0.55f, 0.55f, 0.58f);
    private static readonly Color ResourceCliffColor = new(0.72f, 0.58f, 0.25f);
    private static readonly Color WaterColor = new(0.20f, 0.45f, 0.80f);
    private static readonly Color GridLine = new(0f, 0f, 0f, 0.15f);
    private static readonly Color UnknownType = new(0.7f, 0.7f, 0.7f);

    private TerrainGrid? _terrain;

    public void Bind(TerrainGrid terrain)
    {
        _terrain = terrain;
        QueueRedraw();
    }

    public void Unbind()
    {
        _terrain = null;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_terrain is null)
            return;

        for (var y = 0; y < _terrain.Height; y++)
        {
            for (var x = 0; x < _terrain.Width; x++)
            {
                var cell = new Cell(x, y);
                var diamond = IsoGrid.CellDiamond(cell);

                DrawColoredPolygon(diamond, GetTerrainColor(cell));
                DrawPolyline(
                    new[] { diamond[0], diamond[1], diamond[2], diamond[3], diamond[0] },
                    GridLine,
                    1f);
            }
        }
    }

    private Color GetTerrainColor(Cell cell)
    {
        var terrain = _terrain!.GetTerrain(cell);
        var color = GetBaseTerrainColor(terrain, cell);

        var elevation = _terrain.GetElevation(cell);
        var darkening = System.Math.Min(elevation * 0.08f, 0.35f);

        return color.Darkened(darkening);
    }

    private Color GetBaseTerrainColor(TerrainType terrain, Cell cell)
    {
        return terrain switch
        {
            TerrainType.Plain => (cell.X + cell.Y) % 2 == 0 ? GrassA : GrassB,
            TerrainType.Forest => ForestColor,
            TerrainType.Slope => SlopeColor,
            TerrainType.Cliff => CliffColor,
            TerrainType.ResourceCliff => ResourceCliffColor,
            TerrainType.Water => WaterColor,
            _ => UnknownType
        };
    }
}