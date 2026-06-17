using System.Text;

namespace Winit.Common.Xkb;

internal enum ComposeFeedResult
{
    Ignored,
    Accepted,
}

internal enum XkbComposeStatus
{
    Nothing = 0,
    Composing = 1,
    Composed = 2,
    Cancelled = 3,
}

internal sealed class XkbComposeTable : IDisposable
{
    private nint _table;

    private XkbComposeTable(nint table)
    {
        _table = table;
    }

    public static XkbComposeTable? New(XkbContext context)
    {
        byte[] locale = Encoding.UTF8.GetBytes(Locale() + '\0');
        unsafe
        {
            fixed (byte* ptr = locale)
            {
                nint table = PInvoke.xkb_compose_table_new_from_locale(
                    context.Handle,
                    (sbyte*)ptr,
                    PInvoke.XkbComposeCompileNoFlags);
                return table == 0 ? null : new XkbComposeTable(table);
            }
        }
    }

    public XkbComposeState? NewState()
    {
        nint state = PInvoke.xkb_compose_state_new(_table, PInvoke.XkbComposeStateNoFlags);
        return state == 0 ? null : new XkbComposeState(state);
    }

    private static string Locale()
    {
        foreach (string variable in new[] { "LC_ALL", "LC_CTYPE", "LANG" })
        {
            string? value = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        return "C";
    }

    public void Dispose()
    {
        nint table = _table;
        if (table == 0)
        {
            return;
        }

        _table = 0;
        PInvoke.xkb_compose_table_unref(table);
    }
}

internal sealed class XkbComposeState : IDisposable
{
    private nint _state;

    public XkbComposeState(nint state)
    {
        _state = state;
    }

    public ComposeFeedResult Feed(uint keysym)
    {
        int result = PInvoke.xkb_compose_state_feed(_state, keysym);
        return result == 0 ? ComposeFeedResult.Ignored : ComposeFeedResult.Accepted;
    }

    public XkbComposeStatus Status()
    {
        return (XkbComposeStatus)PInvoke.xkb_compose_state_get_status(_state);
    }

    public unsafe string? GetString()
    {
        int size = PInvoke.xkb_compose_state_get_utf8(_state, null, 0);
        if (size == 0)
        {
            return null;
        }

        byte[] buffer = new byte[checked(size + 1)];
        fixed (byte* ptr = buffer)
        {
            int written = PInvoke.xkb_compose_state_get_utf8(_state, (sbyte*)ptr, (nuint)buffer.Length);
            return written == size ? Encoding.UTF8.GetString(buffer, 0, size) : null;
        }
    }

    public void Reset()
    {
        PInvoke.xkb_compose_state_reset(_state);
    }

    public void Dispose()
    {
        nint state = _state;
        if (state == 0)
        {
            return;
        }

        _state = 0;
        PInvoke.xkb_compose_state_unref(state);
    }
}
