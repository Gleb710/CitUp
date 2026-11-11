using System.Collections.Generic;
using System.Linq;
using CitUp.Sim;
using Godot;

namespace CitUp.Game;

/// <summary>Рисует мир. Логики здесь нет: только чтение World и отрисовка.</summary>
public partial class WorldView : Node2D
{
    private const float BlockHeight = 12f;   // насколько «столбик» здания приподнят над землёй

    private static readonly Color HoverFill = new(1f, 1f, 1f, 0.35f);
    private static readonly Color UnknownType = new(0.7f, 0.7f, 0.7f);
    
    private TerrainView? _terrainView;
    
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
        foreach (var building in world.BuildingCatalog.Buildings)
            _typeColors[building.Id] = Color.FromHtml(building.Color);
        _terrainView ??= new TerrainView();

        if (_terrainView.GetParent() is null)
        {
            _terrainView.ShowBehindParent = true;
            AddChild(_terrainView);
        }

        _terrainView.Bind(world.Terrain);

        _bound = true;
        QueueRedraw();
    }
    
    /// <summary>Отвязывает мир: слой перестаёт рисовать карту (выход в меню).</summary>
    public void Unbind()
    {
        _terrainView?.Unbind();

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

        if (HoverCell is Cell hover)
            DrawColoredPolygon(IsoGrid.CellDiamond(hover), HoverFill);

        var blocks = new List<(Cell Cell, Building Building)>();

        foreach (var building in _world.Buildings)
        {
            var definition = _world.BuildingCatalog.FindBuilding(building.Type);
            if (definition is null)
                continue;

            var baseColor = _typeColors.GetValueOrDefault(building.Type, UnknownType);

            // Проходимые клетки (площадка ратуши) рисуем плоско, под блоками
            foreach (var cell in BuildingGeometry.GetPassableCells(definition, building.Cell, building.Rotation))
                DrawColoredPolygon(IsoGrid.CellDiamond(cell), baseColor.Lightened(0.3f));

            foreach (var cell in BuildingGeometry.GetBlockedCells(definition, building.Cell, building.Rotation))
                blocks.Add((cell, building));
        }

        // От дальних клеток к ближним (по возрастанию x+y), чтобы ближние перекрывали дальние
        foreach (var (cell, building) in blocks.OrderBy(b => b.Cell.X + b.Cell.Y))
            DrawBlock(cell, building);
    }

    private void DrawBlock(Cell cell, Building building)
    {
        var baseColor = _typeColors.GetValueOrDefault(building.Type, UnknownType);
        var d = IsoGrid.CellDiamond(cell);   // 0 верх, 1 право, 2 низ, 3 лево
        var up = new Vector2(0f, -BlockHeight);

        DrawColoredPolygon(new[] { d[3], d[2], d[2] + up, d[3] + up }, baseColor.Darkened(0.35f));
        DrawColoredPolygon(new[] { d[2], d[1], d[1] + up, d[2] + up }, baseColor.Darkened(0.2f));

        var top = new[] { d[0] + up, d[1] + up, d[2] + up, d[3] + up };
        DrawColoredPolygon(top, baseColor);

        var owner = OwnerColors[Mathf.PosMod(building.OwnerId - 1, OwnerColors.Length)];
        DrawPolyline(new[] { top[0], top[1], top[2], top[3], top[0] }, owner, 2f);
    }
}