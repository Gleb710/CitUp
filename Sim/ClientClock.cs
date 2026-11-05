namespace CitUp.Sim;

/// <summary>
/// Часы клиента: оценка текущего (дробного) тика хоста для плавной анимации.
/// Идут по собственному времени и подстраиваются по хартбитам хоста с учётом задержки сети.
/// </summary>
public sealed class ClientClock
{
    /// <summary>Расхождение больше этого числа тиков исправляется скачком, меньшее подтягивается плавно.</summary>
    public const double SnapThresholdTicks = 3.0;

    /// <summary>Какую долю ошибки убираем за один хартбит при плавной подстройке.</summary>
    public const double CorrectionPerHeartbeat = 0.2;

    /// <summary>Вес нового замера пинга в сглаженном значении.</summary>
    private const double RttSmoothing = 0.3;

    /// <summary>Целочисленный счётчик хоста в среднем на полтика отстаёт от его реального времени.</summary>
    private const double HostTickBias = 0.5;

    private readonly double _ticksPerSecond;
    private bool _hasFirstSync;

    public ClientClock(int ticksPerSecond, long startTick)
    {
        _ticksPerSecond = ticksPerSecond;
        RenderTick = startTick;
    }

    /// <summary>Текущее время клиента в тиках (дробное).</summary>
    public double RenderTick { get; private set; }

    /// <summary>Сглаженная задержка туда и обратно в секундах; null, пока не было ни одного замера.</summary>
    public double? RttSeconds { get; private set; }

    public int RttMs => (int)Math.Round((RttSeconds ?? 0.0) * 1000.0);

    /// <summary>На сколько тиков часы расходились с оценкой хоста при последнем хартбите (до исправления).</summary>
    public double LastErrorTicks { get; private set; }

    public void Advance(double deltaSeconds) => RenderTick += deltaSeconds * _ticksPerSecond;

    /// <summary>Новый замер пинга. Некорректные значения (отрицательные, NaN, больше 5 секунд) отбрасываются.</summary>
    public void OnPong(double rttSeconds)
    {
        if (double.IsNaN(rttSeconds) || rttSeconds < 0.0 || rttSeconds > 5.0)
            return;

        RttSeconds = RttSeconds is null
            ? rttSeconds
            : RttSeconds * (1.0 - RttSmoothing) + rttSeconds * RttSmoothing;
    }

    /// <summary>Пришёл хартбит: хост на тике hostTick. Подстраиваем часы.</summary>
    public void OnHeartbeat(long hostTick)
    {
        double estimate = hostTick + HostTickBias + (RttSeconds ?? 0.0) / 2.0 * _ticksPerSecond;
        double error = estimate - RenderTick;
        LastErrorTicks = error;

        if ((!_hasFirstSync && RttSeconds is not null) || Math.Abs(error) > SnapThresholdTicks)
        {
            RenderTick = estimate;
            _hasFirstSync = true;
        }
        else
        {
            RenderTick += error * CorrectionPerHeartbeat;
        }
    }
}