namespace CitUp.Sim;

/// <summary>Житель в снимке. Если Path равен null, житель стоит в клетке Cell.</summary>
public sealed record ResidentSnapshot(int Id, Cell Cell, IReadOnlyList<Cell>? Path, long WalkStartTick);