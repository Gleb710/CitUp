namespace CitUp.Sim;

/// <summary>Стоимость строительства здания: деньги и материальные ресурсы.</summary>
public sealed record BuildingCost(
    int Money,
    IReadOnlyList<ResourceAmount> Resources)
{
    public static readonly BuildingCost Free = new(0, Array.Empty<ResourceAmount>());
}