namespace Winit.X11.Util;

internal static class Randr
{
    public static bool SelectXrandrInput(this XConnection xconn, XlibWindow window)
    {
        if (xconn.RandrFirstEvent is null)
        {
            return false;
        }

        try
        {
            PInvoke.XRRSelectInput(
                xconn.Display,
                window,
                PInvoke.RRScreenChangeNotifyMask |
                PInvoke.RRCrtcChangeNotifyMask |
                PInvoke.RROutputPropertyNotifyMask);
            xconn.Flush();
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    public static double CalcDpiFactor(
        (uint Width, uint Height) sizePx,
        (nuint Width, nuint Height) sizeMm)
    {
        if (sizeMm.Width == 0 || sizeMm.Height == 0)
        {
            return 1.0;
        }

        double ppmm = Math.Sqrt(
            sizePx.Width * (double)sizePx.Height /
            ((ulong)sizeMm.Width * (double)(ulong)sizeMm.Height));
        double dpiFactor = Math.Max(Math.Round(ppmm * (12.0 * 25.4 / 96.0)) / 12.0, 1.0);
        return dpiFactor <= 20.0 ? dpiFactor : 1.0;
    }

    public static unsafe uint? GetCrtcMode(this XConnection xconn, uint crtcId)
    {
        return WithScreenResources(xconn, resources =>
        {
            nint crtcInfoPtr = PInvoke.XRRGetCrtcInfo(xconn.Display, resources, crtcId);
            if (crtcInfoPtr == 0)
            {
                return (uint?)null;
            }

            try
            {
                XRRCrtcInfo* crtc = (XRRCrtcInfo*)crtcInfoPtr;
                return crtc->Mode;
            }
            finally
            {
                PInvoke.XRRFreeCrtcInfo(crtcInfoPtr);
            }
        });
    }

    public static unsafe bool SetCrtcConfig(this XConnection xconn, uint crtcId, uint modeId)
    {
        return WithScreenResources(xconn, resources =>
        {
            nint crtcInfoPtr = PInvoke.XRRGetCrtcInfo(xconn.Display, resources, crtcId);
            if (crtcInfoPtr == 0)
            {
                return false;
            }

            try
            {
                XRRCrtcInfo* crtc = (XRRCrtcInfo*)crtcInfoPtr;
                int status = PInvoke.XRRSetCrtcConfig(
                    xconn.Display,
                    resources,
                    crtcId,
                    crtc->Timestamp,
                    crtc->X,
                    crtc->Y,
                    modeId,
                    crtc->Rotation,
                    crtc->Outputs,
                    crtc->NOutput);
                xconn.Flush();
                return status == 0;
            }
            finally
            {
                PInvoke.XRRFreeCrtcInfo(crtcInfoPtr);
            }
        });
    }

    private static unsafe T WithScreenResources<T>(XConnection xconn, Func<nint, T> callback)
    {
        if (xconn.RandrVersion is null)
        {
            return default!;
        }

        nint resources = 0;
        try
        {
            resources = ShouldUseCurrentScreenResources(xconn.RandrVersion.Value)
                ? PInvoke.XRRGetScreenResourcesCurrent(xconn.Display, xconn.RootWindow)
                : PInvoke.XRRGetScreenResources(xconn.Display, xconn.RootWindow);
            return resources == 0 ? default! : callback(resources);
        }
        finally
        {
            if (resources != 0)
            {
                PInvoke.XRRFreeScreenResources(resources);
            }
        }
    }

    private static bool ShouldUseCurrentScreenResources((int Major, int Minor) version)
    {
        return version.Major > 1 || (version.Major == 1 && version.Minor >= 3);
    }
}
