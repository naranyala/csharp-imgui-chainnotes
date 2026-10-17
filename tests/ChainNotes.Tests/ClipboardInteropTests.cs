using System.Runtime.InteropServices;
using ImGuiNET;

namespace ChainNotes.Tests;

/// <summary>
/// Proves the platform-clipboard mechanism used by <c>ImGuiController</c>:
/// ImGui calls the cdecl fn pointers installed on <c>ImGuiPlatformIO</c>, so
/// wiring them to an OS clipboard (in the app, <c>IKeyboard.ClipboardText</c>)
/// makes cut/copy/paste round-trip through the platform. Runs headless — no
/// window, GL, or input context needed.
/// </summary>
public class ClipboardInteropTests : IDisposable
{
    private static string _osClipboard = string.Empty;
    private static IntPtr _getBuffer = IntPtr.Zero;

    private readonly IntPtr _context;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetClipboardDelegate(IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetClipboardDelegate(IntPtr userData, IntPtr textUtf8);

    private readonly GetClipboardDelegate _get;
    private readonly SetClipboardDelegate _set;

    public ClipboardInteropTests()
    {
        _context = ImGui.CreateContext();

        _get = GetNative;
        _set = SetNative;

        var platformIo = ImGui.GetPlatformIO();
        platformIo.Platform_GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_get);
        platformIo.Platform_SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_set);
        platformIo.Platform_ClipboardUserData = IntPtr.Zero;
    }

    private IntPtr GetNative(IntPtr userData)
    {
        if (_getBuffer != IntPtr.Zero)
            Marshal.FreeCoTaskMem(_getBuffer);

        _getBuffer = Marshal.StringToCoTaskMemUTF8(_osClipboard);
        return _getBuffer;
    }

    private void SetNative(IntPtr userData, IntPtr textUtf8) =>
        _osClipboard = textUtf8 == IntPtr.Zero
            ? string.Empty
            : Marshal.PtrToStringUTF8(textUtf8) ?? string.Empty;

    [Fact]
    public void SetClipboardText_RoutesThroughPlatformFunction()
    {
        ImGui.SetClipboardText("hello from imgui");

        Assert.Equal("hello from imgui", _osClipboard);
    }

    [Fact]
    public void GetClipboardText_ReadsThroughPlatformFunction()
    {
        _osClipboard = "text set by the os";

        Assert.Equal("text set by the os", ImGui.GetClipboardText());
    }

    [Fact]
    public void RoundTrip_SurvivesUnicode()
    {
        const string text = "ünïcode 🎉 clipboard";

        ImGui.SetClipboardText(text);

        Assert.Equal(text, _osClipboard);
        Assert.Equal(text, ImGui.GetClipboardText());
    }

    [Fact]
    public void SetClipboardText_EmptyString_ClearsPlatformClipboard()
    {
        _osClipboard = "previous";
        ImGui.SetClipboardText(string.Empty);

        Assert.Equal(string.Empty, _osClipboard);
        Assert.Equal(string.Empty, ImGui.GetClipboardText());
    }

    public void Dispose()
    {
        if (_getBuffer != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(_getBuffer);
            _getBuffer = IntPtr.Zero;
        }

        ImGui.DestroyContext(_context);
        GC.KeepAlive(_get);
        GC.KeepAlive(_set);
    }
}
