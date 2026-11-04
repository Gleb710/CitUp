namespace CitUp.Sim;

/// <summary>
/// Хост: учёт того, кто прислал команду. У каждого клиента своя нумерация команд, поэтому хост выдаёт
/// свои уникальные номера и помнит, кому потом отвечать об отказе.
/// </summary>
public sealed class CommandRouter
{
    /// <summary>Источник «сам хост» (его собственные клики и одиночная игра).</summary>
    public const int LocalSource = 0;

    private readonly Dictionary<int, (int SourceId, int SourceCommandId)> _pending = new();
    private int _nextId = 1;

    /// <summary>Сколько команд ещё ждут ответа.</summary>
    public int PendingCount => _pending.Count;

    /// <summary>Регистрирует команду и возвращает номер, под которым она пойдёт в SimEngine.</summary>
    public int Register(int sourceId, int sourceCommandId)
    {
        int id = _nextId++;
        _pending[id] = (sourceId, sourceCommandId);
        return id;
    }

    /// <summary>Находит автора команды по её хостовому номеру и убирает запись. false, если команда неизвестна.</summary>
    public bool TryComplete(int hostCommandId, out int sourceId, out int sourceCommandId)
    {
        if (_pending.Remove(hostCommandId, out var entry))
        {
            sourceId = entry.SourceId;
            sourceCommandId = entry.SourceCommandId;
            return true;
        }

        sourceId = 0;
        sourceCommandId = 0;
        return false;
    }

    /// <summary>Забывает все команды отключившегося источника: отвечать им уже некому.</summary>
    public void Forget(int sourceId)
    {
        foreach (var id in _pending.Where(p => p.Value.SourceId == sourceId).Select(p => p.Key).ToList())
            _pending.Remove(id);
    }
}