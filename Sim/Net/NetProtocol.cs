namespace CitUp.Sim;

public static class NetProtocol
{
    /// <summary>
    /// Увеличивать при любом изменении формата сообщений или данных в phase1.json.
    /// Хост сверяет версию в Hello (Issue 9).
    /// </summary>
    public const int Version = 1;
}