using System.Runtime.InteropServices;
using System.Text;
using Winit.X11.Util;

namespace Winit.X11.Ime;

internal enum StyleKind
{
    Preedit,
    Nothing,
    None,
}

internal readonly record struct Style(StyleKind Kind, nuint Value)
{
    public static Style NoneDefault => new(StyleKind.None, ImeNative.XimNoneStyle);
}

internal sealed class InputMethod(nint im, Style preeditStyle, Style noneStyle, string name)
{
    public nint Im { get; } = im;

    public Style PreeditStyle { get; } = preeditStyle;

    public Style NoneStyle { get; } = noneStyle;

    public string Name { get; } = name;

    public static unsafe InputMethod? New(XConnection xconn, nint im, string name)
    {
        XIMStyles* styles = null;
        fixed (byte* queryInputStyleName = ImeNative.XNQueryInputStyle)
        {
            sbyte* failed = PInvoke.XGetIMValuesQueryInputStyle(
                im,
                (sbyte*)queryInputStyleName,
                &styles,
                0);

            if (failed is not null)
            {
                return null;
            }
        }

        Style? preeditStyle = null;
        Style? noneStyle = null;
        try
        {
            if (styles is null)
            {
                return null;
            }

            ReadOnlySpan<nuint> supportedStyles = new(styles->SupportedStyles, styles->CountStyles);
            foreach (nuint style in supportedStyles)
            {
                if (style == ImeNative.XimPreeditStyle)
                {
                    preeditStyle = new Style(StyleKind.Preedit, style);
                }
                else if (style == ImeNative.XimNothingStyle && preeditStyle is null)
                {
                    preeditStyle = new Style(StyleKind.Nothing, style);
                }
                else if (style == ImeNative.XimNoneStyle)
                {
                    noneStyle = new Style(StyleKind.None, style);
                }
            }
        }
        finally
        {
            if (styles is not null)
            {
                _ = PInvoke.XFree((nint)styles);
            }
        }

        if (preeditStyle is null && noneStyle is null)
        {
            return null;
        }

        Style resolvedPreeditStyle = preeditStyle ?? noneStyle!.Value;
        Style resolvedNoneStyle = noneStyle ?? resolvedPreeditStyle;
        return new InputMethod(im, resolvedPreeditStyle, resolvedNoneStyle, name);
    }
}

internal enum InputMethodResultKind
{
    XModifiers,
    Fallback,
    Failure,
}

internal readonly record struct InputMethodResult(InputMethodResultKind Kind, InputMethod? InputMethod)
{
    public bool IsFallback => Kind == InputMethodResultKind.Fallback;

    public InputMethod? Ok()
    {
        return Kind == InputMethodResultKind.Failure ? null : InputMethod;
    }
}

internal sealed class PotentialInputMethods
{
    private readonly PotentialInputMethod? _xmodifiers;
    private readonly PotentialInputMethod[] _fallbacks;

    private PotentialInputMethods(PotentialInputMethod? xmodifiers, PotentialInputMethod[] fallbacks, IReadOnlyList<string> ximServers)
    {
        _xmodifiers = xmodifiers;
        _fallbacks = fallbacks;
        XimServers = ximServers;
    }

    public IReadOnlyList<string> XimServers { get; }

    public static PotentialInputMethods New(XConnection xconn)
    {
        string? xmodifiersValue = Environment.GetEnvironmentVariable("XMODIFIERS");
        PotentialInputMethod? xmodifiers = xmodifiersValue is null
            ? null
            : PotentialInputMethod.FromString(xmodifiersValue);

        return new PotentialInputMethods(
            xmodifiers,
            [
                PotentialInputMethod.FromString("@im=local"),
                PotentialInputMethod.FromString("@im="),
            ],
            GetXimServers(xconn));
    }

    public PotentialInputMethods Clone()
    {
        return new PotentialInputMethods(
            _xmodifiers?.Clone(),
            [.. _fallbacks.Select(static fallback => fallback.Clone())],
            XimServers);
    }

    public InputMethodResult OpenIm(XConnection xconn, Action? failedXmodifiersCallback)
    {
        Reset();

        if (_xmodifiers is not null)
        {
            InputMethod? inputMethod = _xmodifiers.OpenIm(xconn);
            if (inputMethod is not null)
            {
                return new InputMethodResult(InputMethodResultKind.XModifiers, inputMethod);
            }

            failedXmodifiersCallback?.Invoke();
        }

        foreach (PotentialInputMethod fallback in _fallbacks)
        {
            InputMethod? inputMethod = fallback.OpenIm(xconn);
            if (inputMethod is not null)
            {
                return new InputMethodResult(InputMethodResultKind.Fallback, inputMethod);
            }
        }

        return new InputMethodResult(InputMethodResultKind.Failure, null);
    }

    private void Reset()
    {
        _xmodifiers?.Reset();

        foreach (PotentialInputMethod fallback in _fallbacks)
        {
            fallback.Reset();
        }
    }

    private static unsafe IReadOnlyList<string> GetXimServers(XConnection xconn)
    {
        try
        {
            nuint[] atomValues = xconn.GetProperty32(
                xconn.RootWindow,
                xconn.Atoms[AtomName.XimServers],
                new Atom(PInvoke.XaAtom));

            if (atomValues.Length == 0)
            {
                return [];
            }

            Atom[] atoms = atomValues.Select(static atom => new Atom(atom)).ToArray();
            string[] names = new string[atoms.Length];
            fixed (Atom* atomsPtr = atoms)
            {
                sbyte** atomNames = stackalloc sbyte*[atoms.Length];
                if (PInvoke.XGetAtomNames(xconn.Display, atomsPtr, atoms.Length, atomNames) == 0)
                {
                    return [];
                }

                for (int i = 0; i < atoms.Length; i++)
                {
                    sbyte* name = atomNames[i];
                    if (name is null)
                    {
                        names[i] = string.Empty;
                        continue;
                    }

                    try
                    {
                        names[i] = (Marshal.PtrToStringUTF8((nint)name) ?? string.Empty)
                            .Replace("@server=", "@im=", StringComparison.Ordinal);
                    }
                    finally
                    {
                        _ = PInvoke.XFree((nint)name);
                    }
                }
            }

            xconn.CheckErrors();
            return names.Where(static name => name.Length > 0).ToArray();
        }
        catch
        {
            return [];
        }
    }
}

internal sealed class PotentialInputMethod
{
    private readonly byte[] _nameBytes;

    private PotentialInputMethod(string name)
    {
        Name = name;
        _nameBytes = Encoding.UTF8.GetBytes(name + '\0');
    }

    public string Name { get; }

    public bool? Successful { get; private set; }

    public static PotentialInputMethod FromString(string value)
    {
        return new PotentialInputMethod(value);
    }

    public PotentialInputMethod Clone()
    {
        return new PotentialInputMethod(Name)
        {
            Successful = Successful,
        };
    }

    public void Reset()
    {
        Successful = null;
    }

    public unsafe InputMethod? OpenIm(XConnection xconn)
    {
        nint im;
        fixed (byte* namePtr = _nameBytes)
        {
            lock (ImeNative.GlobalLock)
            {
                _ = PInvoke.XSetLocaleModifiers((sbyte*)namePtr);
                im = PInvoke.XOpenIM(xconn.Display, 0, 0, 0);
            }
        }

        Successful = im != 0;
        if (im == 0)
        {
            return null;
        }

        InputMethod? inputMethod = InputMethod.New(xconn, im, Name);
        if (inputMethod is null)
        {
            _ = PInvoke.XCloseIM(im);
        }

        return inputMethod;
    }
}
