namespace CitUp.Sim;

/// <summary>
/// Хранилище материальных ресурсов. Деньги хранятся отдельно и в склад не входят.
/// </summary>
public sealed class Stockpile
{
    private readonly Dictionary<string, int> _amounts = new();

    public IReadOnlyDictionary<string, int> Amounts => _amounts;

    public int GetAmount(string resourceId)
    {
        ThrowIfInvalidResourceId(resourceId);
        return _amounts.GetValueOrDefault(resourceId);
    }

    public void Add(string resourceId, int amount)
    {
        ThrowIfInvalidResourceId(resourceId);

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Количество добавляемого ресурса должно быть больше 0.");

        _amounts[resourceId] = GetAmount(resourceId) + amount;
    }

    public bool TryTake(string resourceId, int amount)
    {
        ThrowIfInvalidResourceId(resourceId);

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Количество выдаваемого ресурса должно быть больше 0.");

        if (GetAmount(resourceId) < amount)
            return false;

        _amounts[resourceId] -= amount;

        if (_amounts[resourceId] == 0)
            _amounts.Remove(resourceId);

        return true;
    }

    private static void ThrowIfInvalidResourceId(string resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
            throw new ArgumentException(
                "Идентификатор ресурса не может быть пустым.",
                nameof(resourceId));
    }
}