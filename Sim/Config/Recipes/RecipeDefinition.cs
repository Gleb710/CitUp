namespace CitUp.Sim;

/// <summary>
/// Рецепт производства: что здание потребляет и производит за один цикл.
/// В Фазе 2 рецепты только описываются и валидируются, но не исполняются.
/// </summary>
public sealed record RecipeDefinition(
    string Id,
    string BuildingId,
    IReadOnlyList<ResourceAmount> Inputs,
    IReadOnlyList<ResourceAmount> Outputs,
    int CycleTicks);