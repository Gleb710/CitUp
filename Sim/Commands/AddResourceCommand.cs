namespace CitUp.Sim;

/// <summary>Отладочная команда: добавляет ресурс в локальный склад здания.</summary>
public sealed record AddResourceCommand(
    int CommandId,
    int PlayerId,
    int BuildingId,
    string ResourceId,
    int Amount);