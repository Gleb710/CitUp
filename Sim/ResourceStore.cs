namespace CitUp.Sim;

/// <summary>
/// Хранилище денег и материальных ресурсов.
/// Используется как основа для общего склада ратуши и локальных складов зданий.
/// </summary>
public sealed class ResourceStore
{
    private readonly Stockpile _resources = new();

    public long Money { get; private set; }

    public IReadOnlyDictionary<string, int> Resources => _resources.Amounts;

    public void AddMoney(long amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Количество добавляемых денег должно быть больше 0.");

        Money += amount;
    }

    public bool TryTakeMoney(long amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Количество выдаваемых денег должно быть больше 0.");

        if (Money < amount)
            return false;

        Money -= amount;
        return true;
    }

    public void AddResource(string resourceId, int amount) =>
        _resources.Add(resourceId, amount);

    public bool TryTakeResource(string resourceId, int amount) =>
        _resources.TryTake(resourceId, amount);

    public int GetResourceAmount(string resourceId) =>
        _resources.GetAmount(resourceId);
}