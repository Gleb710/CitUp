namespace CitUp.Sim;

/// <summary>Клиент → хост: версия протокола и имя игрока.</summary>
public sealed record HelloMessage(int ProtocolVersion, string PlayerName) : NetMessage;