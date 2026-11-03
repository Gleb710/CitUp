namespace CitUp.Sim;

/// <summary>Итог одного тика: что изменилось в мире и что стало с командами.</summary>
public sealed record StepResult(IReadOnlyList<SimEvent> Events, IReadOnlyList<CommandResult> Results);