using System;
using CitUp.Sim;
using Godot;

namespace CitUp.Game;

/// <summary>
/// Вызывает SimEngine.Step() с частотой TicksPerSecond (10 в секунду), независимо от частоты кадров.
/// </summary>
public partial class SimRunner : Node
{
    // Не больше стольких тиков за один кадр: защита от «лавины» после долгого зависания
    private const int MaxStepsPerFrame = 5;

    private SimEngine _sim = null!;
    private bool _bound;
    private double _accumulator;

    /// <summary>Срабатывает после каждого тика. Понадобится в Issue 5 для перерисовки.</summary>
    public event Action<StepResult>? Stepped;

    public void Bind(SimEngine sim)
    {
        _sim = sim;
        _bound = true;
        _accumulator = 0;
    }

    public override void _Process(double delta)
    {
        if (!_bound)
            return;

        // Сколько секунд длится один тик: при 10 тиках в секунду это 0.1
        double tickLength = 1.0 / _sim.World.Config.TicksPerSecond;

        // Копим время, прошедшее с прошлого кадра
        _accumulator += delta;

        // Пока накопилось на целый тик, делаем шаг симуляции и «тратим» накопленное время
        int steps = 0;
        while (_accumulator >= tickLength && steps < MaxStepsPerFrame)
        {
            // Step() вызываем отдельной строкой, чтобы он выполнялся всегда,
            // даже если на событие Stepped никто не подписан
            var result = _sim.Step();
            Stepped?.Invoke(result);

            _accumulator -= tickLength;
            steps++;
        }

        // Если компьютер сильно отстал, лишнее время отбрасываем, а не пытаемся догонять
        if (_accumulator >= tickLength)
            _accumulator = 0;
    }
}