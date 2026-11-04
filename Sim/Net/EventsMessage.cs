namespace CitUp.Sim;

/// <summary>Хост → все клиенты: события одного тика. Клиент применяет их через World.Apply в том же порядке.</summary>
public sealed record EventsMessage(long Tick, IReadOnlyList<SimEvent> Events) : NetMessage;