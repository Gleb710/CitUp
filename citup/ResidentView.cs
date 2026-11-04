using System;
using CitUp.Sim;
using Godot;

namespace CitUp.Game;

/// <summary>Рисует жителей и (по F3) их маршруты. Логики здесь нет: только чтение World.</summary>
public partial class ResidentView : Node2D
{
    private static readonly Color BodyColor = new(0.97f, 0.97f, 0.97f);
    private static readonly Color OutlineColor = new(0.1f, 0.1f, 0.1f);
    private static readonly Color PathColor = new(1f, 1f, 1f, 0.55f);
    private static readonly Vector2 Lift = new(0f, -4f);   // чуть приподнимаем, чтобы «стоял» на клетке

    private World _world = null!;
    private Func<double> _renderTick = null!;
    private bool _bound;
    private bool _showPaths;

    public void Bind(World world, Func<double> renderTick)
    {
        _world = world;
        _renderTick = renderTick;
        _bound = true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F3 })
            _showPaths = !_showPaths;
    }

    // Жители движутся плавно, поэтому перерисовываем этот слой каждый кадр.
    // Он маленький, карта (WorldView) при этом не перерисовывается.
    public override void _Process(double delta)
    {
        if (_bound)
            QueueRedraw();
    }

    public override void _Draw()
    {
        if (!_bound)
            return;

        double renderTick = _renderTick();
        int ticksPerCell = _world.Config.Resident.TicksPerCell;

        foreach (var resident in _world.Residents)
        {
            if (_showPaths && resident.Path is { Count: > 1 } path)
            {
                var points = new Vector2[path.Count];
                for (int i = 0; i < path.Count; i++)
                    points[i] = IsoGrid.CellCenter(path[i]);
                DrawPolyline(points, PathColor, 2f);
            }

            var position = PositionOf(resident, renderTick, ticksPerCell) + Lift;
            DrawCircle(position, 9f, OutlineColor);
            DrawCircle(position, 7f, BodyColor);
        }
    }

    /// <summary>Где нарисовать жителя: стоит в центре клетки или плавно идёт между клетками маршрута.</summary>
    private static Vector2 PositionOf(Resident resident, double renderTick, int ticksPerCell)
    {
        if (resident.Path is not { Count: > 0 } path)
            return IsoGrid.CellCenter(resident.Cell);

        // Сколько клеток пройдено (дробное число), не меньше 0 и не больше длины маршрута
        double progress = (renderTick - resident.WalkStartTick) / ticksPerCell;
        progress = Math.Clamp(progress, 0.0, path.Count - 1);

        int index = (int)Math.Floor(progress);
        if (index >= path.Count - 1)
            return IsoGrid.CellCenter(path[path.Count - 1]);

        float t = (float)(progress - index);
        return IsoGrid.CellCenter(path[index]).Lerp(IsoGrid.CellCenter(path[index + 1]), t);
    }
}