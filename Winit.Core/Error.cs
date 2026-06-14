namespace Winit.Core;

public abstract class WinitException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public abstract class EventLoopException(string message, Exception? innerException = null)
    : WinitException(message, innerException);

public sealed class EventLoopRecreationException()
    : EventLoopException("EventLoop can't be recreated, only a single instance of it is supported for cross-platform compatibility");

public sealed class EventLoopExitFailureException(int status)
    : EventLoopException($"Exit Failure: {status}")
{
    public int Status { get; } = status;
}

public sealed class EventLoopOsException(OsException error)
    : EventLoopException(error.Message, error)
{
    public OsException Error { get; } = error;
}

public sealed class EventLoopNotSupportedException(NotSupportedRequestException error)
    : EventLoopException(error.Message, error)
{
    public NotSupportedRequestException Error { get; } = error;
}

public abstract class RequestException(string message, Exception? innerException = null)
    : WinitException(message, innerException);

public sealed class NotSupportedRequestException(string reason)
    : RequestException($"Operation is not supported: {reason}")
{
    public string Reason { get; } = reason;
}

public sealed class IgnoredRequestException()
    : RequestException("The request was ignored");

public sealed class OsRequestException(OsException error)
    : RequestException(error.Message, error)
{
    public OsException Error { get; } = error;
}

public sealed class OsException(string file, int line, Exception error)
    : WinitException($"os error at {file}:{line}: {error.Message}", error)
{
    public string File { get; } = file;

    public int Line { get; } = line;
}

public sealed class BadIconException(string message, Exception? innerException = null)
    : WinitException(message, innerException);

public sealed class BadCursorImageException(string message)
    : WinitException(message);

public sealed class BadCursorAnimationException(string message)
    : WinitException(message);

public enum ImeSurroundingTextError
{
    TextTooLong,
    CursorBadPosition,
    AnchorBadPosition,
}

public sealed class ImeSurroundingTextException(ImeSurroundingTextError error)
    : WinitException(GetMessage(error))
{
    public ImeSurroundingTextError Error { get; } = error;

    private static string GetMessage(ImeSurroundingTextError error)
    {
        return error switch
        {
            ImeSurroundingTextError.TextTooLong => "text exceeds maximum length",
            ImeSurroundingTextError.CursorBadPosition => "cursor is not at a valid text index",
            ImeSurroundingTextError.AnchorBadPosition => "anchor is not at a valid text index",
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, null),
        };
    }
}

public enum ImeRequestError
{
    NotEnabled,
    AlreadyEnabled,
    NotSupported,
}

public sealed class ImeRequestException(ImeRequestError error)
    : WinitException(GetMessage(error))
{
    public ImeRequestError Error { get; } = error;

    private static string GetMessage(ImeRequestError error)
    {
        return error switch
        {
            ImeRequestError.NotEnabled => "ime is not enabled.",
            ImeRequestError.AlreadyEnabled => "ime is already enabled.",
            ImeRequestError.NotSupported => "ime is not supported.",
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, null),
        };
    }
}
