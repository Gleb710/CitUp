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
    private const double HeartbeatSeconds = 1.0;        // как часто хост шлёт хартбит
    private const double PingSeconds = 2.0;             // как часто клиент замеряет задержку
    private const double IndicatorSeconds = 0.5;        // как часто обновляется индикатор соединения
    private const double ResyncCooldownSeconds = 5.0;   // клиент просит новый снимок не чаще, чем раз в столько секунд
    private const ulong HostResyncCooldownMs = 2000;    // хост отдаёт новый снимок одному игроку не чаще, чем раз в столько мс
    private const int SyncLogTicks = 50;                // как часто (в тиках) пишем в журнал клетку жителя для сверки окон

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
    private readonly Dictionary<int, int> _playerByPeer = new();       // id соединения → id игрока
    private readonly Dictionary<int, ulong> _lastResyncServed = new(); // когда пиру в последний раз отдавали новый снимок
    private int _hostPort;
    private string _hostAddresses = "";
    private double _heartbeatTimer;

    // Только клиент
    private ClientClock? _clock;
    private bool _connectionLost;
    private int _nextCommandId = 1;   // нумерация команд этого клиента
    private double _pingTimer;
    private long _lastLoggedBucket;
    private bool _resyncing;          // ждём новый снимок: события пока игнорируем
    private int _resyncCount;
    private double _lastResyncRequestSeconds = double.NegativeInfinity;
    private bool _lastHashMatched = true;

    private double _indicatorTimer;
    private int _localPlayerId = PlayerRegistry.HostPlayerId;
    private string _playerName = "Игрок";
    private int _selectedTypeIndex;

    public override void _Ready()
    {
        // У Godot _Ready() дочерних узлов вызывается раньше, чем у родителя:
        // к этому моменту все узлы из сцены уже существуют.
        GameLog.InstallCrashHandlers();
        Log($"запуск CitUp, версия протокола {NetProtocol.Version}");

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

    public override void _ExitTree() => GameLog.Shutdown();

    private static void Log(string text) => GameLog.Info(text);

    // ---------- запуск режимов ----------

    private void OnSoloPressed()
    {
        GameLog.Start("solo");
        _localPlayerId = PlayerRegistry.HostPlayerId;
        StartWorld();
        _menu.CloseMenu();
        _hud.SetNetStatus("Одиночная игра");
        GetWindow().Title = "CitUp: одиночная";
        Log("одиночная игра начата");
    }

    private void OnHostPressed(string playerName, int port)
    {
        var error = _net.StartHost(port);
        if (error != Error.Ok)
        {
            _menu.SetStatus($"Не удалось создать игру: {error}. Возможно, порт {port} уже занят.");
            return;
        }

        GameLog.Start("host");
        _playerName = PlayerRegistry.CleanName(playerName);
        _players = new PlayerRegistry();
        _players.RegisterHost(_playerName);
        _playerByPeer.Clear();
        _lastResyncServed.Clear();
        _localPlayerId = PlayerRegistry.HostPlayerId;
        _hostPort = port;
        _hostAddresses = string.Join(", ", NetManager.LocalAddresses());

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

        GameLog.Start("client");
        _playerName = PlayerRegistry.CleanName(playerName);
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
        _lastResyncServed.Remove(peerId);

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
        _menu.SetStatus("Не удалось подключиться. Проверьте адрес, порт и что хост уже запущен.");
        GameLog.Warn("подключиться не удалось");
    }

    private void OnHostLost()
    {
        _clock = null;   // часы встали, житель замер: мир больше не обновляется
        _connectionLost = true;
        _resyncing = false;
        _hud.SetNetStatus("Связь с хостом потеряна");
        _hud.ShowMessage("Связь с хостом потеряна");
        GameLog.Warn("связь с хостом потеряна");
    }

    private void OnMessageReceived(int peerId, NetMessage message)
    {
        // Частые служебные сообщения в журнал не пишем: события, хартбиты, пинги
        if (message is not (EventsMessage or HeartbeatMessage or PingMessage or PongMessage))
            Log($"сообщение {message.GetType().Name} от пира {peerId}");

        if (_net.IsHost)
            HandleHostMessage(peerId, message);
        else if (_net.IsClient && peerId == NetManager.HostPeerId)
            HandleClientMessage(message);
        else
            GameLog.Warn($"сообщение от пира {peerId} проигнорировано: клиент принимает сообщения только от хоста");
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
            GameLog.Warn($"сообщение {message.GetType().Name} от пира {peerId} проигнорировано: Hello не пройден");
            return;
        }

        switch (message)
        {
            case CommandMessage command:
                HandleCommand(peerId, playerId, command.Command);
                break;
            case PingMessage ping:
                _net.Send(peerId, new PongMessage(ping.ClientTimeMs));   // время клиента возвращаем без изменений
                break;
            case ResyncRequestMessage:
                ServeResync(peerId, playerId);
                break;
            default:
                GameLog.Warn($"неожиданное сообщение {message.GetType().Name} от игрока {playerId}");
                break;
        }
    }

    private void HandleHello(int peerId, HelloMessage hello)
    {
        if (_world is null || _players is null)
            return;

        if (_playerByPeer.ContainsKey(peerId))
        {
            GameLog.Warn($"повторный Hello от пира {peerId} проигнорирован");
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
        GameLog.Warn($"отказ пиру {peerId}: {reason}");
        _net.Send(peerId, new JoinRejectedMessage(reason));

        // Клиент уходит сам, получив отказ; если не ушёл, через секунду отключаем его принудительно
        GetTree().CreateTimer(1.0).Timeout += () => _net.Kick(peerId);
    }

    /// <summary>Клиент разошёлся с хостом и просит свежий снимок. Не чаще раза в 2 секунды на игрока.</summary>
    private void ServeResync(int peerId, int playerId)
    {
        if (_world is null)
            return;

        ulong now = Time.GetTicksMsec();
        if (_lastResyncServed.TryGetValue(peerId, out ulong last) && now - last < HostResyncCooldownMs)
        {
            GameLog.Warn($"игрок {playerId} просит новый снимок слишком часто, запрос проигнорирован");
            return;
        }

        _lastResyncServed[peerId] = now;
        GameLog.Warn($"игрок {playerId} запросил новый снимок мира (пересинхронизация)");
        _net.Send(peerId, new WelcomeMessage(playerId, _world.ToSnapshot()));
    }

    /// <summary>Команда клиента. Игрок определяется по соединению, поле PlayerId из сообщения игнорируется.</summary>
    private void HandleCommand(int peerId, int playerId, BuildCommand incoming)
    {
        if (_sim is null || _router is null)
            return;

        // Из сети может прийти что угодно, в том числе пустая команда
        if (incoming is null)
        {
            GameLog.Warn($"пустая команда от игрока {playerId} проигнорирована");
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

    /// <summary>Хост: хартбит с тиком (для часов клиента) и хешем состояния (для проверки рассинхрона).</summary>
    private void SendHeartbeat()
    {
        if (_world is null)
            return;

        var heartbeat = new HeartbeatMessage(_world.CurrentTick, _world.Buildings.Count, _world.StateHash());
        foreach (int peerId in _playerByPeer.Keys)
            _net.Send(peerId, heartbeat);
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
            case HeartbeatMessage heartbeat:
                _clock?.OnHeartbeat(heartbeat.HostTick);
                CheckSync(heartbeat);
                break;
            case PongMessage pong:
                // Задержка туда и обратно: сколько прошло с отправки пинга, на который хост ответил
                _clock?.OnPong(((long)Time.GetTicksMsec() - pong.ClientTimeMs) / 1000.0);
                break;
            default:
                GameLog.Warn($"неожиданное сообщение {message.GetType().Name}");
                break;
        }
    }

    /// <summary>Приходит и при первом подключении, и при пересинхронизации (повторный снимок).</summary>
    private void OnWelcome(WelcomeMessage welcome)
    {
        bool isResync = _resyncing;

        World world;
        try
        {
            world = World.FromSnapshot(WorldConfig.LoadDefault(), welcome.Snapshot);
        }
        catch (InvalidDataException ex)
        {
            GameLog.Error($"испорченный снимок: {ex.Message}");
            _net.Shutdown();
            _resyncing = false;
            _menu.SetStatus($"Хост прислал испорченный мир: {ex.Message}");
            _hud.ShowMessage("Хост прислал испорченный мир");
            return;
        }

        _localPlayerId = welcome.PlayerId;
        _world = world;
        _sim = null;      // у клиента нет движка: мир меняется только событиями хоста
        _router = null;
        _connectionLost = false;
        _resyncing = false;
        _lastHashMatched = true;

        var clock = new ClientClock(world.Config.TicksPerSecond, world.CurrentTick);
        _clock = clock;
        _lastLoggedBucket = (long)clock.RenderTick / SyncLogTicks;

        ShowWorld(world, () => clock.RenderTick);
        _menu.CloseMenu();
        _hud.SetNetStatus($"Клиент: вы игрок {_localPlayerId} ({_playerName})");
        GetWindow().Title = "CitUp: клиент";

        if (isResync)
        {
            _hud.ShowMessage("Мир синхронизирован заново");
            Log($"мир синхронизирован заново: {world.Buildings.Count} зданий, тик {world.CurrentTick}");
        }
        else
        {
            Log($"мир получен: {world.Buildings.Count} зданий, тик {world.CurrentTick}, вы игрок {_localPlayerId}");
        }

        // Первый замер пинга сразу, чтобы первый же хартбит синхронизировал часы с учётом задержки
        _pingTimer = 0;
        SendPing();
    }

    private void OnJoinRejected(JoinRejectedMessage rejected)
    {
        GameLog.Warn($"хост отказал: {rejected.Reason}");
        _net.Shutdown();
        _menu.SetStatus($"Хост отклонил подключение: {rejected.Reason}");
    }

    /// <summary>Клиент: применяет события хоста к своей копии мира в том же порядке, в каком они пришли.</summary>
    private void ApplyHostEvents(EventsMessage message)
    {
        // Пока ждём новый снимок, события относятся к испорченному миру: пропускаем
        if (_world is null || _resyncing)
            return;

        try
        {
            foreach (var simEvent in message.Events)
                _world.Apply(simEvent);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            // Хост не должен присылать такие события: значит, миры разошлись
            GameLog.Error($"РАССИНХРОН при применении событий тика {message.Tick}: {ex.Message}");
            RequestResync("событие не удалось применить");
            return;
        }

        if (message.Events.Count > 0)
            _view.QueueRedraw();
    }

    /// <summary>Сравнивает хеш состояния из хартбита со своим. Расхождение означает рассинхрон.</summary>
    private void CheckSync(HeartbeatMessage heartbeat)
    {
        if (_world is null || _resyncing || _connectionLost)
            return;

        int mine = _world.StateHash();
        if (mine == heartbeat.StateHash && _world.Buildings.Count == heartbeat.BuildingCount)
        {
            _lastHashMatched = true;
            return;
        }

        _lastHashMatched = false;
        GameLog.Error(
            $"РАССИНХРОН: у хоста зданий {heartbeat.BuildingCount}, хеш {heartbeat.StateHash:X8}; " +
            $"у клиента зданий {_world.Buildings.Count}, хеш {mine:X8}");
        RequestResync("хеш состояния не совпал с хостом");
    }

    /// <summary>Просит хоста прислать мир заново. Не чаще, чем раз в ResyncCooldownSeconds.</summary>
    private void RequestResync(string reason)
    {
        double now = Time.GetTicksMsec() / 1000.0;
        if (now - _lastResyncRequestSeconds < ResyncCooldownSeconds)
        {
            GameLog.Warn($"пересинхронизация ({reason}) отложена: прошло меньше {ResyncCooldownSeconds} с с прошлого запроса");
            return;
        }

        _lastResyncRequestSeconds = now;
        _resyncing = true;
        _resyncCount++;
        _hud.ShowMessage("Рассинхрон миров, запрашиваю мир заново...");
        GameLog.Warn($"запрос нового снимка у хоста: {reason}");
        _net.SendToHost(new ResyncRequestMessage());
    }

    private void SendPing() => _net.SendToHost(new PingMessage((long)Time.GetTicksMsec()));

    /// <summary>
    /// Клиент: когда его часы пересекают тик, кратный SyncLogTicks, пишет в журнал клетку жителя на этом тике.
    /// У хоста такие же строки для тех же тиков, поэтому окна можно сверять по журналу.
    /// </summary>
    private void LogClientSync()
    {
        if (_clock is null || _world is null || _world.Residents.Count == 0)
            return;

        long bucket = (long)_clock.RenderTick / SyncLogTicks;
        if (bucket <= _lastLoggedBucket)
            return;

        _lastLoggedBucket = bucket;
        long tick = bucket * SyncLogTicks;
        var cell = _world.Residents[0].CellAt(tick, _world.Config.Resident.TicksPerCell);
        string hash = _lastHashMatched ? "ок" : "НЕ СОВПАЛ";
        Log($"тик {tick}: житель в ({cell.X}, {cell.Y}), пинг {_clock.RttMs} мс, поправка часов {_clock.LastErrorTicks:F2} тика, хеш: {hash}");
    }

    // ---------- индикатор ----------

    private void UpdateIndicator()
    {
        if (_net.IsHost && _players is not null)
        {
            _hud.SetConnection($"● Хост · игроков: {_players.ConnectedPlayers.Count}", Hud.LinkState.Good);
        }
        else if (_net.IsClient && _world is not null)
        {
            if (_connectionLost)
            {
                _hud.SetConnection("● Нет связи с хостом", Hud.LinkState.Bad);
            }
            else if (_resyncing)
            {
                _hud.SetConnection("● Синхронизация мира...", Hud.LinkState.Medium);
            }
            else if (_clock?.RttSeconds is null)
            {
                _hud.SetConnection("● Измеряю пинг...", Hud.LinkState.Medium);
            }
            else
            {
                int ms = _clock.RttMs;
                var state = ms < 60 ? Hud.LinkState.Good : ms < 150 ? Hud.LinkState.Medium : Hud.LinkState.Bad;
                string resync = _resyncCount > 0 ? $" · пересинхр.: {_resyncCount}" : "";
                _hud.SetConnection($"● Пинг {ms} мс{resync}", state);
            }
        }
        else
        {
            _hud.HideConnection();
        }
    }

    // ---------- ввод и отображение ----------

    public override void _Process(double delta)
    {
        _clock?.Advance(delta);

        // Хост: раз в секунду хартбит всем подключённым
        if (_net.IsHost && _playerByPeer.Count > 0)
        {
            _heartbeatTimer += delta;
            if (_heartbeatTimer >= HeartbeatSeconds)
            {
                _heartbeatTimer = 0;
                SendHeartbeat();
            }
        }

        // Клиент: раз в 2 секунды замер задержки и запись в журнал для сверки
        if (_clock is not null && !_connectionLost)
        {
            _pingTimer += delta;
            if (_pingTimer >= PingSeconds)
            {
                _pingTimer = 0;
                SendPing();
            }

            LogClientSync();
        }

        _indicatorTimer += delta;
        if (_indicatorTimer >= IndicatorSeconds)
        {
            _indicatorTimer = 0;
            UpdateIndicator();
        }

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
        // Служебные клавиши работают всегда, даже в меню
        if (@event is InputEventKey { Pressed: true, Echo: false } service)
        {
            if (service.Keycode == Key.F4)
            {
                GameLog.OpenFolder();
                return;
            }

            if (service.Keycode == Key.F9)
            {
                DebugCorruptClientWorld();
                return;
            }
        }

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
    /// Отладка (F9, только у клиента): добавляет в мир клиента здание, которого нет у хоста.
    /// Так проверяется обнаружение рассинхрона. Позже это заменит консоль отладки (Фаза 2).
    /// </summary>
    private void DebugCorruptClientWorld()
    {
        if (_world is null || !_net.IsClient)
        {
            _hud.ShowMessage("F9 (порча мира для проверки) работает только у клиента");
            return;
        }

        for (int y = 0; y < _world.Height; y++)
        {
            for (int x = 0; x < _world.Width; x++)
            {
                var cell = new Cell(x, y);
                if (_world.BuildingAt(cell) is not null)
                    continue;

                _world.Apply(new BuildingBuilt(_world.NextBuildingId, "house", cell, 99));
                _view.QueueRedraw();
                _hud.ShowMessage("ОТЛАДКА: мир клиента испорчен");
                GameLog.Warn($"ОТЛАДКА: в мир клиента искусственно добавлено здание в {cell}");
                return;
            }
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
        {
            BroadcastEvents(result.Events);
            LogHostSync();
        }

        foreach (var commandResult in result.Results)
            RouteResult(commandResult);
    }

    /// <summary>Хост: на каждом тике, кратном SyncLogTicks, пишет клетку жителя. Строки сверяются с журналом клиента.</summary>
    private void LogHostSync()
    {
        if (_world is null || _world.Residents.Count == 0 || _world.CurrentTick % SyncLogTicks != 0)
            return;

        var cell = _world.Residents[0].CellAt(_world.CurrentTick, _world.Config.Resident.TicksPerCell);
        Log($"тик {_world.CurrentTick}: житель в ({cell.X}, {cell.Y}), хеш {_world.StateHash():X8}");
    }
}