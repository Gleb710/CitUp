namespace CitUp.Game;

/// <summary>
/// Часы клиента: дробный номер тика для плавной анимации. Стартуют с тика из снимка и идут по собственному времени.
/// В Issue 11 добавится подстройка по хартбитам и пингу.
/// </summary>
public sealed class ClientClock
{
    private readonly double _ticksPerSecond;

    public ClientClock(int ticksPerSecond, long startTick)
    {
        _ticksPerSecond = ticksPerSecond;
        RenderTick = startTick;
    }

    public double RenderTick { get; private set; }

    public void Advance(double deltaSeconds) => RenderTick += deltaSeconds * _ticksPerSecond;
}