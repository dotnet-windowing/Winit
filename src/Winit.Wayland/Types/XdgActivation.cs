using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Winit.Core;

namespace Winit.Wayland;

internal record struct XdgActivationTokenData
{
    public readonly record struct Attention(WlSurface Surface, WeakReference<Window> Window);

    public readonly record struct Obtain(WindowId WindowId, AsyncRequestSerial Serial);

    private const byte AttentionTag = 0;
    private const byte ObtainTag = 1;

    private byte _tag;
    private Attention _attention;
    private Obtain _obtain;

    public XdgActivationTokenData(Attention value)
    {
        this = default;
        _tag = AttentionTag;
        _attention = value;
    }

    public XdgActivationTokenData(Obtain value)
    {
        this = default;
        _tag = ObtainTag;
        _obtain = value;
    }

    public bool TryGetValue(out Attention value)
    {
        value = _attention;
        return _tag == AttentionTag;
    }

    public bool TryGetValue(out Obtain value)
    {
        value = _obtain;
        return _tag == ObtainTag;
    }
}

internal sealed unsafe class XdgActivationState : IDisposable
{
    private readonly HashSet<XdgActivationTokenRequest> _pendingTokens = [];
    private XdgActivationV1 _xdgActivation;
    private bool _disposed;

    private XdgActivationState(XdgActivationV1 xdgActivation)
    {
        _xdgActivation = xdgActivation;
    }

    public static XdgActivationState Bind(WinitState state, WaylandGlobal global)
    {
        WlProxy proxy = state.BindGlobal(global, XdgActivationInterfaces.ActivationV1, maxVersion: 1);
        return new XdgActivationState(new XdgActivationV1(proxy.Value));
    }

    public AsyncRequestSerial RequestActivationToken(WinitState state, WlSurface surface, WindowId windowId)
    {
        AsyncRequestSerial serial = AsyncRequestSerial.Get();
        XdgActivationTokenRequest request = CreateToken(
            state,
            new XdgActivationTokenData(new XdgActivationTokenData.Obtain(windowId, serial)));
        request.SetSurface(surface);
        request.Commit();
        state.Connection.Flush();
        return serial;
    }

    public void RequestUserAttention(WinitState state, WlSurface surface, Window window)
    {
        XdgActivationTokenRequest request = CreateToken(
            state,
            new XdgActivationTokenData(new XdgActivationTokenData.Attention(
                surface,
                new WeakReference<Window>(window))));
        request.SetSurface(surface);
        request.Commit();
        state.Connection.Flush();
    }

    public void Activate(ActivationToken token, WlSurface surface)
    {
        if (_xdgActivation.IsNull || surface.IsNull)
        {
            return;
        }

        using Utf8Buffer tokenBuffer = Utf8Buffer.FromString(token.AsRaw());
        WlArgument* args = stackalloc WlArgument[2];
        args[0].String = tokenBuffer.Pointer;
        args[1].Object = surface.Value;
        PInvoke.WlProxyMarshalArray(_xdgActivation, XdgActivationV1Request.Activate, args);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (XdgActivationTokenRequest token in _pendingTokens.ToArray())
        {
            token.Dispose();
        }

        _pendingTokens.Clear();

        if (!_xdgActivation.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _xdgActivation,
                XdgActivationV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_xdgActivation),
                WlProxyMarshalFlags.Destroy,
                null);
            _xdgActivation = XdgActivationV1.Null;
        }
    }

    internal void RemovePending(XdgActivationTokenRequest token)
    {
        _pendingTokens.Remove(token);
    }

    private XdgActivationTokenRequest CreateToken(WinitState state, XdgActivationTokenData data)
    {
        if (_xdgActivation.IsNull)
        {
            throw new ObjectDisposedException(nameof(XdgActivationState));
        }

        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = 0;
        WlProxy proxy = PInvoke.WlProxyMarshalArrayFlags(
            _xdgActivation,
            XdgActivationV1Request.GetActivationToken,
            XdgActivationInterfaces.ActivationTokenV1,
            PInvoke.WlProxyGetVersion(_xdgActivation),
            WlProxyMarshalFlags.None,
            args);
        state.Connection.CheckError();
        if (proxy.IsNull)
        {
            throw new InvalidOperationException("xdg_activation_v1.get_activation_token failed.");
        }

        PInvoke.WlProxySetQueue(proxy, state.Connection.EventQueue);
        XdgActivationTokenRequest request = new(this, state, new XdgActivationTokenV1(proxy.Value), data);
        _pendingTokens.Add(request);
        request.InstallDispatcher();
        return request;
    }
}

internal sealed unsafe class XdgActivationTokenRequest : IDisposable
{
    private readonly XdgActivationState _owner;
    private readonly WinitState _state;
    private readonly GCHandle _selfHandle;
    private readonly XdgActivationTokenData _data;
    private XdgActivationTokenV1 _token;
    private bool _disposed;

    public XdgActivationTokenRequest(
        XdgActivationState owner,
        WinitState state,
        XdgActivationTokenV1 token,
        XdgActivationTokenData data)
    {
        _owner = owner;
        _state = state;
        _token = token;
        _data = data;
        _selfHandle = GCHandle.Alloc(this);
    }

    public void InstallDispatcher()
    {
        int result = PInvoke.WlProxyAddDispatcher(
            _token,
            &TokenDispatcher,
            (void*)GCHandle.ToIntPtr(_selfHandle),
            null);
        if (result != 0)
        {
            throw new InvalidOperationException("wl_proxy_add_dispatcher failed for xdg_activation_token_v1.");
        }
    }

    public void SetSurface(WlSurface surface)
    {
        if (_token.IsNull || surface.IsNull)
        {
            return;
        }

        WlArgument* args = stackalloc WlArgument[1];
        args[0].Object = surface.Value;
        PInvoke.WlProxyMarshalArray(_token, XdgActivationTokenV1Request.SetSurface, args);
    }

    public void Commit()
    {
        if (!_token.IsNull)
        {
            PInvoke.WlProxyMarshalArray(_token, XdgActivationTokenV1Request.Commit, null);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _owner.RemovePending(this);

        if (!_token.IsNull)
        {
            PInvoke.WlProxyMarshalArrayFlags(
                _token,
                XdgActivationTokenV1Request.Destroy,
                null,
                PInvoke.WlProxyGetVersion(_token),
                WlProxyMarshalFlags.Destroy,
                null);
            _token = XdgActivationTokenV1.Null;
        }

        if (_selfHandle.IsAllocated)
        {
            _selfHandle.Free();
        }
    }

    private void Done(string token)
    {
        ActivationToken activationToken = ActivationToken.FromRaw(token);
        if (_data.TryGetValue(out XdgActivationTokenData.Attention attention))
        {
            _state.XdgActivation?.Activate(activationToken, attention.Surface);
            if (attention.Window.TryGetTarget(out Window? window))
            {
                window.MarkAttentionRequestFinished();
            }
        }
        else if (_data.TryGetValue(out XdgActivationTokenData.Obtain obtain))
        {
            _state.PushWindowEvent(
                obtain.WindowId,
                new WindowEvent(new WindowEvent.ActivationTokenDone(obtain.Serial, activationToken)));
        }

        Dispose();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int TokenDispatcher(
        void* implementation,
        void* target,
        uint opcode,
        WlMessage* message,
        WlArgument* args)
    {
        _ = target;
        _ = message;

        if (implementation is null || args is null)
        {
            return 0;
        }

        if (GCHandle.FromIntPtr((nint)implementation).Target is not XdgActivationTokenRequest request ||
            request._disposed)
        {
            return 0;
        }

        if (opcode == XdgActivationTokenV1Event.Done)
        {
            request.Done(Marshal.PtrToStringUTF8((nint)args[0].String) ?? string.Empty);
        }

        return 0;
    }
}
