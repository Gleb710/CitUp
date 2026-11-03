namespace CitUp.Sim;

/// <summary>
/// Житель вышел в путь. Path включает стартовую клетку и конечную.
/// Позиция в любой момент вычисляется из пути и тика (см. Resident.CellAt).
/// </summary>
public sealed record ResidentWalkStarted(int ResidentId, IReadOnlyList<Cell> Path, long StartTick) : SimEvent;