using System.Runtime.InteropServices;

namespace WindowPlacementManager.Models;

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left, Top, Right, Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;

    public override string ToString() => $"L:{Left}, T:{Top}, R:{Right}, B:{Bottom} (W:{Width}, H:{Height})";
}