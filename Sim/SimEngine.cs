namespace CitUp.Sim;

public sealed class SimEngine
{
    public World World { get; }

    public SimEngine(World world)
    {
        World = world;
    }

    /// <summary>Один шаг симуляции. Возвращает события, которые произошли на этом тике.</summary>
    public List<SimEvent> Step()
    {
        World.AdvanceTick();
        // Позже здесь: обработка команд (Issue 2) и поведение жителей (Issue 3).
        return new List<SimEvent>();
    }
}