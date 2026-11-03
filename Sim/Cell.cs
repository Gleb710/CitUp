namespace CitUp.Sim;

/// <summary>Клетка карты. Свой тип вместо Vector2I, потому что Sim не знает про Godot.</summary>
public readonly record struct Cell(int X, int Y);