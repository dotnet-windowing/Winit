namespace Winit.Core;

public interface IEventLoopExtPumpEvents
{
    PumpStatus PumpAppEvents(TimeSpan? timeout, IApplicationHandler app);
}

public record struct PumpStatus
{
    public readonly record struct Continue;

    public readonly record struct Exit(int Status);

    private const byte ContinueTag = 0;
    private const byte ExitTag = 1;

    private byte _tag;
    private Continue _continue;
    private Exit _exit;

    public PumpStatus(Continue value)
    {
        this = default;
        _tag = ContinueTag;
        _continue = value;
    }

    public PumpStatus(Exit value)
    {
        this = default;
        _tag = ExitTag;
        _exit = value;
    }

    public bool TryGetValue(out Continue value)
    {
        value = _continue;
        return _tag == ContinueTag;
    }

    public bool TryGetValue(out Exit value)
    {
        value = _exit;
        return _tag == ExitTag;
    }
}
