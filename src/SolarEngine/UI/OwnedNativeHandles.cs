using Microsoft.Win32.SafeHandles;

namespace SolarEngine.UI;

internal sealed class SafeGdiObjectHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private SafeGdiObjectHandle()
        : base(ownsHandle: true)
    {
    }

    internal static SafeGdiObjectHandle FromHandle(nint handle)
    {
        SafeGdiObjectHandle safeHandle = new();
        safeHandle.SetHandle(handle);
        return safeHandle;
    }

    protected override bool ReleaseHandle()
    {
        return NativeInterop.DeleteObject(handle);
    }
}

internal sealed class SafeIconHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private SafeIconHandle()
        : base(ownsHandle: true)
    {
    }

    internal static SafeIconHandle FromHandle(nint handle)
    {
        SafeIconHandle safeHandle = new();
        safeHandle.SetHandle(handle);
        return safeHandle;
    }

    protected override bool ReleaseHandle()
    {
        return NativeInterop.DestroyIcon(handle);
    }
}

internal sealed class SafeMenuHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private SafeMenuHandle()
        : base(ownsHandle: true)
    {
    }

    internal static SafeMenuHandle FromHandle(nint handle)
    {
        SafeMenuHandle safeHandle = new();
        safeHandle.SetHandle(handle);
        return safeHandle;
    }

    protected override bool ReleaseHandle()
    {
        return NativeInterop.DestroyMenu(handle);
    }
}
