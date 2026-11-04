namespace CitUp.Sim;

/// <summary>Клиент → хост: замер задержки. ClientTimeMs хост вернёт без изменений.</summary>
public sealed record PingMessage(long ClientTimeMs) : NetMessage;