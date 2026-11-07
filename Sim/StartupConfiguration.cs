namespace CitUp.Sim;

/// <summary>
/// Начальное состояние партии: деньги, ресурсы и бесплатные здания.
/// Позиции зданий определяются при создании партии, а не в этом файле.
/// </summary>
public sealed record StartupConfiguration(
    int StartingMoney,
    IReadOnlyList<ResourceAmount> StartingResources,
    IReadOnlyList<string> FreeBuildingIds);