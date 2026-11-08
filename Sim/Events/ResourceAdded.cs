namespace CitUp.Sim;

/// <summary>В локальный склад здания добавлен ресурс.</summary>
public sealed record ResourceAdded(
    int BuildingId,
    string ResourceId,
    int Amount) : SimEvent;