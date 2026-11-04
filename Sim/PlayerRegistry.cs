namespace CitUp.Sim;

/// <summary>
/// Реестр игроков партии (ведёт хост). Хост всегда игрок 1, остальные получают номера по имени.
/// Вернувшийся игрок с тем же именем получает прежний номер.
/// </summary>
public sealed class PlayerRegistry
{
    public const int HostPlayerId = 1;
    public const int MaxNameLength = 24;

    private readonly Dictionary<string, int> _idByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, string> _nameById = new();
    private readonly HashSet<int> _connected = new();
    private int _nextId = HostPlayerId + 1;

    /// <summary>Убирает пробелы по краям и обрезает слишком длинное имя.</summary>
    public static string CleanName(string? name)
    {
        string clean = (name ?? "").Trim();
        return clean.Length > MaxNameLength ? clean[..MaxNameLength].TrimEnd() : clean;
    }

    public void RegisterHost(string name)
    {
        if (_nameById.Count > 0)
            throw new InvalidOperationException("Хост уже зарегистрирован.");

        string clean = CleanName(name);
        Add(HostPlayerId, clean.Length == 0 ? "Хост" : clean);
    }

    public JoinResult Join(string? name)
    {
        string clean = CleanName(name);
        if (clean.Length == 0)
            return new JoinResult(0, "Имя игрока не может быть пустым.");

        if (_idByName.TryGetValue(clean, out int existingId))
        {
            if (_connected.Contains(existingId))
                return new JoinResult(0, $"Имя «{clean}» уже занято в этой партии. Выберите другое.");

            _connected.Add(existingId);   // вернувшийся игрок
            return new JoinResult(existingId, null);
        }

        int id = _nextId++;
        Add(id, clean);
        return new JoinResult(id, null);
    }

    public void Disconnect(int playerId) => _connected.Remove(playerId);

    public bool IsConnected(int playerId) => _connected.Contains(playerId);

    public string? NameOf(int playerId) => _nameById.GetValueOrDefault(playerId);

    public IReadOnlyList<(int Id, string Name)> ConnectedPlayers =>
        _connected.OrderBy(id => id).Select(id => (id, _nameById[id])).ToList();

    private void Add(int id, string name)
    {
        _idByName[name] = id;
        _nameById[id] = name;
        _connected.Add(id);
    }
}