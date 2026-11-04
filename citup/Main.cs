using System;
using CitUp.Sim;
using Godot;

namespace CitUp.Game;

/// <summary>Корень сцены. Показывает меню, запускает режим (одиночный, хост, клиент) и превращает ввод в команды.</summary>
public partial class Main : Node2D
{
    private const int LocalPlayerId = 1;   // у хоста и в одиночной игре игрок номер 1

    private WorldView _view = null!;
    private SimRunner _runner = null!;
    private Hud _hud = null!;
    private Label _infoLabel = null!;
    private NetManager _net = null!;
    private MenuPanel _menu = null!;

    // Мир и движок есть у одиночной игры и у хоста; у клиента мир появится после снимка (Issue 9)
    private World? _world;
    private SimEngine? _sim;

    private string _playerName = "Игрок";
    private string _role = "меню";
    private int _selectedTypeIndex;
    private int _nextCommandId = 1;

    public override void _Ready()
    {
        // У Godot _Ready() дочерних узлов вызывается раньше, чем у родителя:
        // к этому моменту все узлы из сцены уже существуют.
        _view = GetNode<WorldView>("WorldView");
        _runner = GetNode<SimRunner>("SimRunner");
        _hud = GetNode<Hud>("CanvasLayer");
        _infoLabel = GetNode<Label>("CanvasLayer/InfoLabel");
        _net = GetNode<NetManager>("NetManager");

        _runner.Stepped += OnStepped;

        _net.PeerJoined += OnPeerJoined;
        _net.PeerLeft += OnPeerLeft;
        _net.ConnectedToHost += OnConnectedToHost;
        _net.ConnectFailed += OnConnectFailed;
        _net.HostLost += OnHostLost;
        _net.MessageReceived += OnMessageReceived;

        // Меню создаётся из кода и лежит поверх всего
        _menu = new MenuPanel();
        AddChild(_menu);
        _menu.SoloPressed += OnSoloPressed;
        _menu.HostPressed += OnHostPressed;
        _menu.JoinPressed += OnJoinPressed;

        _infoLabel.Text = "";
    }

    // ---------- запуск режимов ----------

    private void OnSoloPressed()
    {
        _role = "одиночная";
        StartWorld();
        _menu.CloseMenu();
        _hud.SetNetStatus("Одиночная игра");
        GetWindow().Title = "CitUp: одиночная";
    }

    private void OnHostPressed(string playerName, int port)
    {
        var error = _net.StartHost(port);
        if (error != Error.Ok)
        {
            _menu.SetStatus($"Не удалось создать игру: {error}. Возможно, порт {port} уже занят.");
            return;
        }

        _playerName = playerName;
        _role = "хост";
        StartWorld();
        _menu.CloseMenu();

        string addresses = string.Join(", ", NetManager.LocalAddresses());
        _hud.SetNetStatus($"Хост, порт {port}. Адреса для друга: {addresses}");
        GetWindow().Title = "CitUp: хост";
        Log($"игра создана, порт {port}, адреса: {addresses}");
    }

    private void OnJoinPressed(string playerName, string address, int port)
    {
        var error = _net.StartClient(address, port);
        if (error != Error.Ok)
        {
            _menu.SetStatus($"Не удалось начать подключение: {error}");
            return;
        }

        _playerName = playerName;
        _role = "клиент";
        _menu.SetStatus($"Подключаюсь к {address}:{port}...");
        Log($"подключение к {address}:{port}");
    }

    /// <summary>Создаёт мир и движок: для одиночной игры и для хоста.</summary>
    private void StartWorld()
    {
        var config = WorldConfig.LoadDefault();
        _world = new World(config);
        _sim = new SimEngine(_world, seed: Random.Shared.Next());

        _view.Bind(_world);
        _runner.Bind(_sim);

        // Слой жителей создаём из кода как дочерний узел WorldView: у него та же система координат
        var residentView = new ResidentView();
        _view.AddChild(residentView);
        residentView.Bind(_world, _runner);

        GetNode<Camera2D>("Camera2D").Position = IsoGrid.MapCenter(_world);
        _hud.ShowSelection(config, _selectedTypeIndex);
    }

    // ---------- события сети ----------

    private void OnPeerJoined(int peerId)
    {
        if (!_net.IsHost)
            return;

        Log($"пир {peerId} подключился");
        _hud.ShowMessage($"Подключился пир {peerId}");
    }

    private void OnPeerLeft(int peerId)
    {
        if (!_net.IsHost)
            return;

        Log($"пир {peerId} отключился");
        _hud.ShowMessage($"Пир {peerId} отключился");
    }

    private void OnConnectedToHost()
    {
        _menu.CloseMenu();
        _hud.SetNetStatus("Подключён к хосту (мир придёт в Issue 9)");
        GetWindow().Title = "CitUp: клиент";
        Log("соединение установлено, отправляю Hello");

        _net.SendToHost(new HelloMessage(NetProtocol.Version, _playerName));
    }

    private void OnConnectFailed()
    {
        _net.Shutdown();
        _menu.SetStatus("Не удалось подключиться. Проверьте адрес, порт и что хост уже запущен.");
        Log("подключиться не удалось");
    }

    private void OnHostLost()
    {
        _hud.SetNetStatus("Связь с хостом потеряна");
        _hud.ShowMessage("Связь с хостом потеряна");
        Log("связь с хостом потеряна");
    }

    private void OnMessageReceived(int peerId, NetMessage message)
    {
        Log($"сообщение от пира {peerId}: {NetSerializer.ToJson(message)}");

        if (_net.IsHost && message is HelloMessage hello)
            _hud.ShowMessage($"Hello от {hello.PlayerName} (протокол {hello.ProtocolVersion})");
    }

    private static void Log(string text) => GD.Print(text);

    // ---------- ввод и отображение ----------

    public override void _Process(double delta)
    {
        if (_world is null)
        {
            _infoLabel.Text = "";
            return;
        }

        string cellText = _view.HoverCell is Cell cell ? $"({cell.X}, {cell.Y})" : "вне карты";
        _infoLabel.Text = $"Клетка: {cellText}    Тик: {_world.CurrentTick}";
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Пока мира нет (меню, клиент без снимка), играть не во что
        if (_world is null || _sim is null)
            return;

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