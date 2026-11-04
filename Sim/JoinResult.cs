namespace CitUp.Sim;

/// <summary>Итог попытки подключиться к партии: либо PlayerId, либо текст отказа для показа игроку.</summary>
public sealed record JoinResult(int PlayerId, string? Error)
{
    public bool Ok => Error is null;
}