using System;
using CitUp.Sim;
using Godot;

namespace CitUp.Game;

/// <summary>Корень сцены. Создаёт мир и движок и раздаёт их дочерним узлам.</summary>
public partial class Main : Node2D
{
    private World _world = null!;
    private SimEngine _sim = null!;
    private WorldView _view = null!;
    private Label _infoLabel = null!;

    private double _debugTimer; // DEBUG

    public override void _Ready()
    {
        // У Godot _Ready() дочерних узлов вызывается раньше, чем у родителя:
        // к этому моменту все узлы из сцены уже существуют.
        var config = WorldConfig.LoadDefault();
        _world = new World(config);
        _sim = new SimEngine(_world, seed: Random.Shared.Next());

        _view = GetNode<WorldView>("WorldView");
        _view.Bind(_world);

        var runner = GetNode<SimRunner>("SimRunner");
        runner.Bind(_sim);

        GetNode<Camera2D>("Camera2D").Position = IsoGrid.MapCenter(_world);
        _infoLabel = GetNode<Label>("CanvasLayer/InfoLabel");

        GD.Print($"Main: _Ready завершён, тик мира = {_world.CurrentTick}"); // DEBUG
    }

    public override void _Process(double delta)
    {
        string cellText = _view.HoverCell is Cell cell ? $"({cell.X}, {cell.Y})" : "вне карты";
        _infoLabel.Text = $"Клетка: {cellText}    Тик: {_world.CurrentTick}";
    }
}