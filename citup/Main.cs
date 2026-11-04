using System;
using CitUp.Sim;
using Godot;

namespace CitUp.Game;

/// <summary>Корень сцены. Создаёт мир и движок, принимает ввод игрока и превращает его в команды.</summary>
public partial class Main : Node2D
{
    private const int LocalPlayerId = 1;   // пока играем одни; в сетевой игре хост = 1

    private World _world = null!;
    private SimEngine _sim = null!;
    private WorldView _view = null!;
    private Hud _hud = null!;
    private Label _infoLabel = null!;

    private int _selectedTypeIndex;
    private int _nextCommandId = 1;

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
        runner.Stepped += OnStepped;

        GetNode<Camera2D>("Camera2D").Position = IsoGrid.MapCenter(_world);

        _hud = GetNode<Hud>("CanvasLayer");
        _infoLabel = GetNode<Label>("CanvasLayer/InfoLabel");
        _hud.ShowSelection(config, _selectedTypeIndex);
    }

    public override void _Process(double delta)
    {
        string cellText = _view.HoverCell is Cell cell ? $"({cell.X}, {cell.Y})" : "вне карты";
        _infoLabel.Text = $"Клетка: {cellText}    Тик: {_world.CurrentTick}";
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false } key)
        {
            int index = key.Keycode switch
            {
                Key.Key1 => 0,
                Key.Key2 => 1,
                Key.Key3 => 2,
                _ => -1
            };

            if (index >= 0 && index < _world.Config.BuildingTypes.Count)
            {
                _selectedTypeIndex = index;
                _hud.ShowSelection(_world.Config, _selectedTypeIndex);
            }
        }
        else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            var cell = IsoGrid.PointToCell(GetGlobalMousePosition());
            if (!_world.IsInside(cell))
                return;

            // Игрок не меняет мир сам: он только отправляет команду. Решает Sim на ближайшем тике.
            var typeId = _world.Config.BuildingTypes[_selectedTypeIndex].Id;
            _sim.Enqueue(new BuildCommand(_nextCommandId++, LocalPlayerId, typeId, cell));
        }
    }

    /// <summary>Вызывается после каждого тика симуляции.</summary>
    private void OnStepped(StepResult result)
    {
        if (result.Events.Count > 0)
            _view.QueueRedraw();

        foreach (var commandResult in result.Results)
        {
            if (!commandResult.Ok)
                _hud.ShowMessage(commandResult.Message);
        }
    }
}