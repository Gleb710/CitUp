using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CitUp.Sim;
using Godot;

namespace CitUp.Game;

/// <summary>Корень сцены. Показывает меню, запускает режим (одиночный, хост, клиент) и превращает ввод в команды.</summary>
public partial class Main : Node2D
{
    private WorldView _view = null!;
    private SimRunner _runner = null!;
    private Hud _hud = null!;
    private Label _infoLabel = null!;
    private NetManager _net = null!;
    private MenuPanel _menu = null!;
    private ResidentView? _residentView;

    // Одиночная игра и хост: есть World, SimEngine и учёт команд. Клиент: только World по снимку.
    private World? _world;
    private SimEngine? _sim;
    private CommandRouter? _router;

    // Только хост
    private PlayerRegistry? _players;
    private readonly Dictionary<int, int> _playerByPeer = new();   // id соединения → id игрока
    private int _hostPort;
    private string _hostAddresses = "";

    // Только клиент
    private ClientClock? _clock;
    private bool _connectionLost;
    private int _nextCommandId = 1;   // нумерация команд этого клиента

    private int _localPlayerId = PlayerRegistry.HostPlayerId;
    private string _playerName = "Игрок";
    private string _role = "меню";
    private int _selectedTypeIndex;

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
        _localPlayerId = PlayerRegistry.HostPlayerId;
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

        _playerName = PlayerRegistry.CleanName(playerName);
        _players = new PlayerRegistry();
        _players.RegisterHost(_playerName);
        _playerByPeer.Clear();
        _localPlayerId = PlayerRegistry.HostPlayerId;
        _hostPort = port;
        _hostAddresses = string.Join(", ", NetManager.LocalAddresses());
        _role = "хост";

        StartWorld();
        _menu.CloseMenu();
        RefreshHostStatus();
        GetWindow().Title = "CitUp: хост";
        Log($"игра создана, порт {port}, адреса: {_hostAddresses}");
    }

    private void OnJoinPressed(string playerName, string address, int port)
    {
        var error = _net.StartClient(address, port);
        if (error != Error.Ok)
        {
            _menu.SetStatus($"Не удалось начать подключение: {error}");
            return;
        }

        _playerName = PlayerRegistry.CleanName(playerName);
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
        _router = new CommandRouter();
        _runner.Bind(_sim);

        ShowWorld(_world, () => _runner.RenderTick);
    }

    /// <summary>Показывает мир на экране: общая часть для одиночной игры, хоста и клиента.</summary>
    private void ShowWorld(World world, Func<double> renderTick)
    {
        _view.Bind(world);

        // Слой жителей создаём из кода как дочерний узел WorldView: у него та же система координат.
        // Если слой уже был (повторный снимок), старый удаляем.
        _residentView?.QueueFree();
        _residentView = new ResidentView();
        _view.AddChild(_residentView);
        _residentView.Bind(world, renderTick);

        GetNode<Camera2D>("Camera2D").Position = IsoGrid.MapCenter(world);
        _hud.ShowSelection(world.Config, _selectedTypeIndex);
    }

    // ---------- события сети ----------

    private void OnPeerJoined(int peerId)
    {
        if (_net.IsHost)
            Log($"пир {peerId} подключился, жду Hello");
    }

    private void OnPeerLeft(int peerId)
    {
        if (!_net.IsHost)
            return;

        _router?.Forget(peerId);

        if (_playerByPeer.Remove(peerId, out int playerId) && _players is not null)
        {
            string name = _players.NameOf(playerId) ?? "Игрок";
            _players.Disconnect(playerId);
            _hud.ShowMessage($"{name} отключился");
            RefreshHostStatus();
            Log($"игрок {playerId} ({name}) отключился");
        }
        else
        {
            Log($"пир {peerId} ушёл, не пройдя Hello");
        }
    }

    private void OnConnectedToHost()
    {
        _menu.SetStatus("Соединение установлено, жду мир...");
        Log("соединение установлено, отправляю Hello");

        _net.SendToHost(new HelloMessage(NetProtocol.Version, _playerName));
    }

    private void OnConnectFailed()
    {
        _net.Shutdown();
        _role = "меню";
        _menu.SetStatus("Не удалось подключиться. Проверьте адрес, порт и что хост уже запущен.");
        Log("подключиться не удалось");
    }

    private void OnHostLost()
    {
        _clock = null;   // часы встали, житель замер: мир больше не обновляется
        _connectionLost = true;
        _hud.SetNetStatus("Связь с хостом потеряна");
        _hud.ShowMessage("Связь с хостом потеряна");
        Log("связь с хостом потеряна");
    }

    private void OnMessageReceived(int peerId, NetMessage message)
    {
        // Рассылку событий не пишем в журнал: она идёт постоянно
        if (message is not EventsMessage)
            Log($"сообщение {message.GetType().Name} от пира {peerId}");

        if (_net.IsHost)
            HandleHostMessage(peerId, message);
        else if (_net.IsClient && peerId == NetManager.HostPeerId)
            HandleClientMessage(message);
        else
            Log($"сообщение от пира {peerId} проигнорировано: клиент принимает сообщения только от хоста");
    }

    // ---------- хост ----------

    private void HandleHostMessage(int peerId, NetMessage message)
    {
        if (message is HelloMessage hello)
        {
            HandleHello(peerId, hello);
            return;
        }

        // Пока пир не прошёл Hello, его сообщения не принимаются вообще
        if (!_playerByPeer.TryGetValue(peerId, out int playerId))
        {
            Log($"сообщение {message.GetType().Name} от пира {peerId} проигнорировано: Hello не пройден");
            return;
        }

        switch (message)
        {
            case CommandMessage command:
                HandleCommand(peerId, playerId, command.Command);
                break;
            default:
                Log($"неожиданное сообщение {message.GetType().Name} от игрока {playerId}");
                break;
        }
    }

    private void HandleHello(int peerId, HelloMessage hello)
    {
        if (_world is null || _players is null)
            return;

        if (_playerByPeer.ContainsKey(peerId))
        {
            Log($"повторный Hello от пира {peerId} проигнорирован");
            return;
        }

        if (hello.ProtocolVersion != NetProtocol.Version)
        {
            Reject(peerId,
                $"Версия игры не совпадает: у хоста {NetProtocol.Version}, у вас {hello.ProtocolVersion}. Нужна та же сборка игры.");
            return;
        }

        var join = _players.Join(hello.PlayerName);
        if (!join.Ok)
        {
            Reject(peerId, join.Error!);
            return;
        }

        // Сначала запоминаем пира, затем шлём снимок: все события следующих тиков он уже получит.
        // Между этими строками тик пройти не может: всё идёт в одном кадре.
        _playerByPeer[peerId] = join.PlayerId;

        var welcome = new WelcomeMessage(join.PlayerId, _world.ToSnapshot());
        _net.Send(peerId, welcome);

        string name = _players.NameOf(join.PlayerId) ?? "Игрок";
        _hud.ShowMessage($"{name} подключился");
        RefreshHostStatus();
        Log($"игрок {join.PlayerId} ({name}) принят, снимок: {welcome.Snapshot.Buildings.Count} зданий, тик {welcome.Snapshot.Tick}");
    }

    private void Reject(int peerId, string reason)
    {
        Log($"отказ пиру {peerId}: {reason}");
        _net.Send(peerId, new JoinRejectedMessage(reason));

        // Клиент уходит сам, получив отказ; если не ушёл, через секунду отключаем его принудительно
        GetTree().CreateTimer(1.0).Timeout += () => _net.Kick(peerId);
    }

    /// <summary>Команда клиента. Игрок определяется по соединению, поле PlayerId из сообщения игнорируется.</summary>
    private void HandleCommand(int peerId, int playerId, BuildCommand incoming)
    {
        if (_sim is null || _router is null)
            return;

        // Из сети может прийти что угодно, в том числе пустая команда
        if (incoming is null)
        {
            Log($"пустая команда от игрока {playerId} проигнорирована");
            return;
        }

        int hostCommandId = _router.Register(peerId, incoming.CommandId);
        _sim.Enqueue(new BuildCommand(hostCommandId, playerId, incoming.BuildingType, incoming.Cell));
        Log($"команда игрока {playerId}: {incoming.BuildingType} в {incoming.Cell}");
    }

    /// <summary>Хост: рассылает события тика всем игрокам, прошедшим Hello.</summary>
    private void BroadcastEvents(IReadOnlyList<SimEvent> events)
    {
        if (_world is null || events.Count == 0 || _playerByPeer.Count == 0)
            return;

        var message = new EventsMessage(_world.CurrentTick, events);
        foreach (int peerId in _playerByPeer.Keys)
            _net.Send(peerId, message);
    }

    /// <summary>Отправляет автору команды итог: локальному игроку показывает в HUD, клиенту шлёт отказ по сети.</summary>
    private void RouteResult(CommandResult result)
    {
        if (_router is null || !_router.TryComplete(result.CommandId, out int sourceId, out int sourceCommandId))
            return;

        if (result.Ok)
            return;

        if (sourceId == CommandRouter.LocalSource)
        {
            _hud.ShowMessage(result.Message);
            return;
        }

        if (_playerByPeer.ContainsKey(sourceId))
            _net.Send(sourceId, new CommandRejectedMessage(sourceCommandId, result.Error));
    }

    private void RefreshHostStatus()
    {
        if (_players is null)
            return;

        string players = string.Join(", ",
            _players.ConnectedPlayers.Select(p => p.Id == _localPlayerId ? $"{p.Name} (вы)" : p.Name));
        _hud.SetNetStatus($"Хост, порт {_hostPort}. Игроки: {players}. Адреса: {_hostAddresses}");
    }

    // ---------- клиент ----------

    private void HandleClientMessage(NetMessage message)
    {
        switch (message)
        {
            case WelcomeMessage welcome:
                OnWelcome(welcome);
                break;
            case JoinRejectedMessage rejected:
                OnJoinRejected(rejected);
                break;
            case EventsMessage events:
                ApplyHostEvents(events);
                break;
            case CommandRejectedMessage rejected:
                _hud.ShowMessage(new CommandResult(rejected.CommandId, rejected.Error).Message);
                break;
            default:
                Log($"неожиданное сообщение {message.GetType().Name}");
                break;
        }
    }

    private void OnWelcome(WelcomeMessage welcome)
    {
        World world;
        try
        {
            world = World.FromSnapshot(WorldConfig.LoadDefault(), welcome.Snapshot);
        }
        catch (InvalidDataException ex)
        {
            Log($"испорченный снимок: {ex.Message}");
            _net.Shutdown();
            _role = "меню";
            _menu.SetStatus($"Хост прислал испорченный мир: {ex.Message}");
            return;
        }

        _localPlayerId = welcome.PlayerId;
        _world = world;
        _sim = null;      // у клиента нет движка: мир меняется только событиями хоста
        _router = null;
        _connectionLost = false;

        var clock = new ClientClock(world.Config.TicksPerSecond, world.CurrentTick);
        _clock = clock;

        ShowWorld(world, () => clock.RenderTick);
        _menu.CloseMenu();
        _hud.SetNetStatus($"Клиент: вы игрок {_localPlayerId} ({_playerName})");
        GetWindow().Title = "CitUp: клиент";
        Log($"мир получен: {world.Buildings.Count} зданий, тик {world.CurrentTick}, вы игрок {_localPlayerId}");
    }

    private void OnJoinRejected(JoinRejectedMessage rejected)
    {
        Log($"хост отказал: {rejected.Reason}");
        _net.Shutdown();
        _role = "меню";
        _menu.SetStatus($"Хост отклонил подключение: {rejected.Reason}");
    }

    /// <summary>Клиент: применяет события хоста к своей копии мира в том же порядке, в каком они пришли.</summary>
    private void ApplyHostEvents(EventsMessage message)
    {
        if (_world is null)
            return;

        try
        {
            foreach (var simEvent in message.Events)
                _world.Apply(simEvent);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // Хост не должен присылать такие события: значит, миры разошлись. Автоматическое лечение добавим в Issue 12.
            Log($"РАССИНХРОН при применении событий тика {message.Tick}: {ex.Message}");
            _hud.ShowMessage("Рассинхрон миров. Переподключитесь к хосту.");
            _hud.SetNetStatus("Рассинхрон: переподключитесь к хосту");
            return;
        }

        if (message.Events.Count > 0)
            _view.QueueRedraw();
    }

    // ---------- ввод и отображение ----------

    private void Log(string text) => GD.Print($"[{_role}] {text}");

    public override void _Process(double delta)
    {
        _clock?.Advance(delta);

        if (_world is null)
        {
            _infoLabel.Text = "";
            return;
        }

        string cellText = _view.HoverCell is Cell cell ? $"({cell.X}, {cell.Y})" : "вне карты";
        long tick = _clock is not null ? (long)_clock.RenderTick : _world.CurrentTick;
        _infoLabel.Text = $"Клетка: {cellText}    Тик: {tick}";
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Пока мира нет (меню, клиент без снимка), играть не во что
        if (_world is null)
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
            if (_world.IsInside(cell))
                SubmitBuild(cell);
        }
    }

    /// <summary>
    /// Игрок не меняет мир сам: он только отправляет команду.
    /// Хост и одиночная игра ставят её в очередь движка, клиент отправляет хосту.
    /// </summary>
    private void SubmitBuild(Cell cell)
    {
        var typeId = _world!.Config.BuildingTypes[_selectedTypeIndex].Id;

        if (_net.IsClient)
        {
            if (_connectionLost)
            {
                _hud.ShowMessage("Нет связи с хостом");
                return;
            }

            var command = new BuildCommand(_nextCommandId++, _localPlayerId, typeId, cell);
            _net.SendToHost(new CommandMessage(command));
            return;
        }

        if (_sim is null || _router is null)
            return;

        // Собственные клики хоста идут тем же путём, что и клиентские, только без сети
        int hostCommandId = _router.Register(CommandRouter.LocalSource, 0);
        _sim.Enqueue(new BuildCommand(hostCommandId, _localPlayerId, typeId, cell));
    }

    /// <summary>Вызывается после каждого тика симуляции (у одиночной игры и у хоста).</summary>
    private void OnStepped(StepResult result)
    {
        if (result.Events.Count > 0)
            _view.QueueRedraw();

        if (_net.IsHost)
            BroadcastEvents(result.Events);

        foreach (var commandResult in result.Results)
            RouteResult(commandResult);
    }
}