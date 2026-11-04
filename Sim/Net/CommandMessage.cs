namespace CitUp.Sim;

/// <summary>
/// Клиент → хост: команда игрока. Поле PlayerId внутри команды хост игнорирует
/// и подставляет игрока по соединению, потому что клиенту доверять нельзя.
/// </summary>
public sealed record CommandMessage(BuildCommand Command) : NetMessage;