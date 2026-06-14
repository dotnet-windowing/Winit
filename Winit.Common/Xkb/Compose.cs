using System.Text;

namespace Winit.Common.Xkb;

internal enum ComposeStatus
{
    None,
    Ignored,
    Accepted,
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
        byte[] locale = Encoding.ASCII.GetBytes("C\0");
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

    public ComposeStatus Feed(uint keysym)
    {
        int result = PInvoke.xkb_compose_state_feed(_state, keysym);
        return result == 0 ? ComposeStatus.Ignored : ComposeStatus.Accepted;
    }

    public int Status()
    {
        return PInvoke.xkb_compose_state_get_status(_state);
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
