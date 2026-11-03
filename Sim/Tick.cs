namespace Sim;

public class Tick
{
    public long Number { get; private set; }
    public void Advance() => Number++;
}