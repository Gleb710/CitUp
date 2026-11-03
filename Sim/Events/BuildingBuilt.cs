namespace CitUp.Sim;

/// <summary>Здание построено. Через такие события мир меняется и у хоста, и у клиентов.</summary>
public sealed record BuildingBuilt(int BuildingId, string Type, Cell Cell, int OwnerId) : SimEvent;