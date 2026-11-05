namespace CitUp.Sim;

/// <summary>
/// Помнит, когда в последний раз приходили сообщения от каждого участника (хост следит за клиентами,
/// клиент за хостом). По нему замечаем «тихий» обрыв связи, о котором транспорт сообщит не сразу.
/// Время передаётся снаружи, поэтому класс не зависит от Godot и легко тестируется.
/// </summary>
public sealed class LivenessTracker
{
    private readonly Dictionary<int, long> _lastSeenMs = new();

    public int Count => _lastSeenMs.Count;

    /// <summary>Пришла весть от участника: сбрасываем его таймер.</summary>
    public void Touch(int id, long nowMs) => _lastSeenMs[id] = nowMs;

    public void Remove(int id) => _lastSeenMs.Remove(id);

    public void Clear() => _lastSeenMs.Clear();

    public bool IsTracked(int id) => _lastSeenMs.ContainsKey(id);

    /// <summary>Сколько миллисекунд от участника не было вестей; null, если он не отслеживается.</summary>
    public long? SilenceMs(int id, long nowMs) =>
        _lastSeenMs.TryGetValue(id, out long last) ? nowMs - last : null;

    /// <summary>Те, от кого вестей нет строго дольше timeoutMs.</summary>
    public IReadOnlyList<int> FindSilent(long nowMs, long timeoutMs) =>
        _lastSeenMs.Where(p => nowMs - p.Value > timeoutMs)
            .Select(p => p.Key)
            .OrderBy(id => id)
            .ToList();
}