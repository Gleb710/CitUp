using CitUp.Sim;
using Godot;

namespace CitUp.Game;

/// <summary>Рисует мир. Логики здесь нет: только чтение World и отрисовка.</summary>
public partial class WorldView : Node2D
{
    private static readonly Color GrassA = new(0.35f, 0.56f, 0.30f);
    private static readonly Color GrassB = new(0.32f, 0.52f, 0.28f);
    private static readonly Color GridLine = new(0f, 0f, 0f, 0.15f);
    private static readonly Color HoverFill = new(1f, 1f, 1f, 0.35f);

    private World _world = null!;
    private bool _bound;

    /// <summary>Клетка под курсором или null, если курсор вне карты.</summary>
    public Cell? HoverCell { get; private set; }

    public void Bind(World world)
    {
        _world = world;
        _bound = true;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!_bound)
            return;

        // GetGlobalMousePosition учитывает положение и масштаб камеры
        var cell = IsoGrid.PointToCell(GetGlobalMousePosition());
        Cell? newHover = _world.IsInside(cell) ? cell : null;

        if (newHover != HoverCell)
        {
            HoverCell = newHover;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (!_bound)
            return;

        for (int y = 0; y < _world.Height; y++)
        {
            for (int x = 0; x < _world.Width; x++)
            {
                var diamond = IsoGrid.CellDiamond(new Cell(x, y));
                DrawColoredPolygon(diamond, (x + y) % 2 == 0 ? GrassA : GrassB);
                DrawPolyline(new[] { diamond[0], diamond[1], diamond[2], diamond[3], diamond[0] }, GridLine, 1f);
            }
        }

        if (HoverCell is Cell hover)
            DrawColoredPolygon(IsoGrid.CellDiamond(hover), HoverFill);
    }
}