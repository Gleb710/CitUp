namespace CitUp.Sim;

/// <summary>Здание в мире. Тип здания хранится строкой-id из phase1.json ("house", "well").</summary>
public sealed record Building(int Id, string Type, Cell Cell, int OwnerId);