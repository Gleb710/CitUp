namespace CitUp.Sim;

/// <summary>Житель дошёл и остановился в клетке Cell.</summary>
public sealed record ResidentWalkFinished(int ResidentId, Cell Cell) : SimEvent;