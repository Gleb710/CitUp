namespace CitUp.Sim;

/// <summary>
/// Агрегированный отчёт о ресурсах игрока для HUD.
/// </summary>
/// <param name="Money">Деньги игрока.</param>
/// <param name="Stockpile">Ресурсы в общем складе игрока.</param>
/// <param name="LocalStockpiles">Сумма ресурсов во всех локальных складах игрока.</param>
public sealed record ResourceReport(
    long Money,
    IReadOnlyDictionary<string, int> Stockpile,
    IReadOnlyDictionary<string, int> LocalStockpiles);