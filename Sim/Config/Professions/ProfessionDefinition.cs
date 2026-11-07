namespace CitUp.Sim;

/// <summary>
/// Профессия жителя: строитель, носильщик или будущая производственная профессия.
/// Время работы и отдыха задаётся в тиках.
/// </summary>
public sealed record ProfessionDefinition(
    string Id,
    string Name,
    int WorkTicks,
    int RestTicks);