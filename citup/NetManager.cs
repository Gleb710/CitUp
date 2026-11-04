using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CitUp.Sim;
using Godot;

namespace CitUp.Game;

/// <summary>
/// Транспорт: подключение через ENet и пересылка байтов. Игровой логики здесь нет.
/// Узел обязан лежать по пути /root/Main/NetManager, одинаковому у хоста и клиента (требование RPC).
/// </summary>
public partial class NetManager : Node
{
    public const int DefaultPort = 7777;
    private const int MaxClients = 4;
    private const int HostPeerId = 1;

    /// <summary>Пришло сообщение: id отправителя и разобранное сообщение.</summary>
    public event Action<int, NetMessage>? MessageReceived;

    /// <summary>Хост: к нему подключился пир (ещё до Hello).</summary>
    public event Action<int>? PeerJoined;

    /// <summary>Хост: пир отключился.</summary>
    public event Action<int>? PeerLeft;

    /// <summary>Клиент: соединение с хостом установлено.</summary>
    public event Action? ConnectedToHost;

    /// <summary>Клиент: подключиться не удалось.</summary>
    public event Action? ConnectFailed;

    /// <summary>Клиент: связь с хостом потеряна.</summary>
    public event Action? HostLost;

    public bool IsHost { get; private set; }
    public bool IsClient { get; private set; }

    public override void _Ready()
    {
        Multiplayer.PeerConnected += id => PeerJoined?.Invoke((int)id);
        Multiplayer.PeerDisconnected += id => PeerLeft?.Invoke((int)id);
        Multiplayer.ConnectedToServer += () => ConnectedToHost?.Invoke();
        Multiplayer.ConnectionFailed += () => ConnectFailed?.Invoke();
        Multiplayer.ServerDisconnected += () => HostLost?.Invoke();
    }

    public Error StartHost(int port)
    {
        Shutdown();

        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(port, MaxClients);
        if (error != Error.Ok)
            return error;

        Multiplayer.MultiplayerPeer = peer;
        IsHost = true;
        return Error.Ok;
    }

    /// <summary>
    /// Начинает подключение. Ok здесь значит только «попытка началась»:
    /// результат придёт позже событием ConnectedToHost или ConnectFailed (при недоступном хосте через несколько секунд).
    /// </summary>
    public Error StartClient(string address, int port)
    {
        Shutdown();

        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(address, port);
        if (error != Error.Ok)
            return error;

        Multiplayer.MultiplayerPeer = peer;
        IsClient = true;
        return Error.Ok;
    }

    /// <summary>Закрывает текущее соединение (если есть).</summary>
    public void Shutdown()
    {
        if (Multiplayer.MultiplayerPeer is ENetMultiplayerPeer enet)
            enet.Close();

        Multiplayer.MultiplayerPeer = null;
        IsHost = false;
        IsClient = false;
    }

    public void SendToHost(NetMessage message) => Send(HostPeerId, message);

    public void Send(int peerId, NetMessage message)
    {
        var error = RpcId(peerId, nameof(Receive), NetSerializer.Serialize(message));
        if (error != Error.Ok)
            GD.PushError($"NetManager: не удалось отправить {message.GetType().Name} пиру {peerId}: {error}");
    }

    /// <summary>
    /// Единственная RPC-функция проекта: сюда приходят байты любых сообщений.
    /// Имя и путь узла должны совпадать у хоста и клиента.
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Receive(byte[] data)
    {
        int sender = Multiplayer.GetRemoteSenderId();

        NetMessage message;
        try
        {
            message = NetSerializer.Deserialize(data);
        }
        catch (InvalidDataException ex)
        {
            GD.PushError($"NetManager: испорченное сообщение от пира {sender}: {ex.Message}");
            return;
        }

        MessageReceived?.Invoke(sender, message);
    }

    /// <summary>Адреса этого компьютера, по которым можно подключиться. Адрес Radmin VPN (26.x.x.x) идёт первым.</summary>
    public static IReadOnlyList<string> LocalAddresses() =>
        IP.GetLocalAddresses()
            .Where(a => a.Contains('.') && !a.Contains(':') && !a.StartsWith("127.") && !a.StartsWith("169.254."))
            .OrderBy(a => a.StartsWith("26.") ? 0 : 1)
            .ToList();
}