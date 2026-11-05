using System.Collections.Generic;
using System.Linq;
using CitUp.Sim;
using Godot;

namespace CitUp.Game;

/// <summary>Рисует мир. Логики здесь нет: только чтение World и отрисовка.</summary>
public partial class WorldView : Node2D
{
    private const float BlockHeight = 12f;   // насколько «столбик» здания приподнят над землёй

    private static readonly Color GrassA = new(0.35f, 0.56f, 0.30f);
    private static readonly Color GrassB = new(0.32f, 0.52f, 0.28f);
    private static readonly Color GridLine = new(0f, 0f, 0f, 0.15f);
    private static readonly Color HoverFill = new(1f, 1f, 1f, 0.35f);
    private static readonly Color UnknownType = new(0.7f, 0.7f, 0.7f);

    // Цвета рамок по номеру игрока: игрок 1, 2, 3, ...
    private static readonly Color[] OwnerColors =
    {
        new(1f, 0.9f, 0.2f), new(0.2f, 0.9f, 1f), new(1f, 0.3f, 0.9f), new(1f, 0.6f, 0.1f), new(1f, 1f, 1f)
    };

    private readonly Dictionary<string, Color> _typeColors = new();
    private World _world = null!;
    private bool _bound;

    /// <summary>Клетка под курсором или null, если курсор вне карты.</summary>
    public Cell? HoverCell { get; private set; }

    public void Bind(World world)
    {
        _world = world;
        foreach (var type in world.Config.BuildingTypes)
            _typeColors[type.Id] = Color.FromHtml(type.Color);
        _bound = true;
        QueueRedraw();
    }
    
    /// <summary>Отвязывает мир: слой перестаёт рисовать карту (выход в меню).</summary>
    public void Unbind()
    {
        _bound = false;
        HoverCell = null;
        _typeColors.Clear();
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

        // Здания рисуем от дальних к ближним (по возрастанию x+y), чтобы ближние перекрывали дальние
        foreach (var building in _world.Buildings.OrderBy(b => b.Cell.X + b.Cell.Y))
            DrawBuilding(building);
    }

    private void DrawBuilding(Building building)
    {
        var baseColor = _typeColors.GetValueOrDefault(building.Type, UnknownType);
        var d = IsoGrid.CellDiamond(building.Cell);   // 0 верх, 1 право, 2 низ, 3 лево
        var up = new Vector2(0f, -BlockHeight);

        // Две боковые грани (тёмнее) и верхняя грань
        DrawColoredPolygon(new[] { d[3], d[2], d[2] + up, d[3] + up }, baseColor.Darkened(0.35f));
        DrawColoredPolygon(new[] { d[2], d[1], d[1] + up, d[2] + up }, baseColor.Darkened(0.2f));

        var top = new[] { d[0] + up, d[1] + up, d[2] + up, d[3] + up };
        DrawColoredPolygon(top, baseColor);

        // Рамка цвета владельца вокруг верхней грани
        var owner = OwnerColors[Mathf.PosMod(building.OwnerId - 1, OwnerColors.Length)];
        DrawPolyline(new[] { top[0], top[1], top[2], top[3], top[0] }, owner, 2f);
    }
}