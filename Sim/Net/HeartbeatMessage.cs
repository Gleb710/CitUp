namespace CitUp.Sim;

/// <summary>
/// Хост → клиенты, раз в секунду: тик хоста для часов клиента (Issue 11)
/// и контрольные числа для обнаружения рассинхрона (Issue 12).
/// </summary>
public sealed record HeartbeatMessage(long HostTick, int BuildingCount, int StateHash) : NetMessage;