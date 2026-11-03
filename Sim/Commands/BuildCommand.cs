namespace CitUp.Sim;

/// <summary>Просьба игрока построить здание. CommandId нужен, чтобы по сети сообщить автору, что стало с его командой.</summary>
public sealed record BuildCommand(int CommandId, int PlayerId, string BuildingType, Cell Cell);