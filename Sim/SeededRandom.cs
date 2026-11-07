namespace CitUp.Sim;

/// <summary>
/// Детерминированный генератор псевдослучайных чисел на основе SplitMix64.
/// Одинаковый seed всегда даёт одинаковую последовательность чисел.
/// </summary>
public sealed class SeededRandom
{
    private ulong _state;

    public SeededRandom(ulong seed)
    {
        _state = seed;
    }

    public ulong NextUInt64()
    {
        _state += 0x9E3779B97F4A7C15UL;

        var value = _state;
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;

        return value ^ (value >> 31);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (minInclusive >= maxExclusive)
            throw new ArgumentOutOfRangeException(
                nameof(maxExclusive),
                maxExclusive,
                "maxExclusive должен быть больше minInclusive.");

        var range = (ulong)(maxExclusive - minInclusive);
        var value = NextUInt64() % range;

        return minInclusive + (int)value;
    }

    public double NextDouble()
    {
        const double divisor = 1.0 / (1UL << 53);

        return (NextUInt64() >> 11) * divisor;
    }
}