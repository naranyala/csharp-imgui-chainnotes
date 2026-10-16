using System.Numerics;
using ChainNotes.Core;
using ChainNotes.UI;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace ChainNotes.App;

public sealed class Application : IDisposable
{
    private const int DefaultWidth = 1360;
    private const int DefaultHeight = 860;

    private IWindow? _window;
    private GL? _gl;
    private ImGuiController? _controller;
    private MainWindow? _mainWindow;

    public void Run(string[] args)
    {
        var options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(DefaultWidth, DefaultHeight),
            Title = "ChainNotes",
            VSync = true,
            API = new GraphicsAPI(
                ContextAPI.OpenGL,
                ContextProfile.Core,
                ContextFlags.ForwardCompatible,
                new APIVersion(3, 3)),
        };

        using var window = Window.Create(options);
        _window = window;

        window.Load += OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;

        // Optional: allow `dotnet run -- --demo` style flags later.
        _ = args;

        window.Run();
    }

    private void OnLoad()
    {
        if (_window is null)
            return;

        _gl = _window.CreateOpenGL();
        var input = _window.CreateInput();

        _controller = new ImGuiController(_gl, _window, input);
        _mainWindow = new MainWindow();
    }

    private void OnUpdate(double delta)
    {
        if (_window is { IsClosing: true })
            return;

        _mainWindow?.Update(delta);
    }

    private void OnRender(double delta)
    {
        if (_controller is null || _mainWindow is null || _gl is null)
            return;

        _gl.ClearColor(0.09f, 0.10f, 0.12f, 1.0f);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        _controller.NewFrame();

        DrawDockspace();
        _mainWindow.Draw();

        ImGui.Render();
        _controller.RenderImDrawData(ImGui.GetDrawData());
    }

    private static void DrawDockspace()
    {
        var viewport = ImGui.GetMainViewport();

        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.SetNextWindowViewport(viewport.ID);

        const ImGuiWindowFlags hostFlags =
            ImGuiWindowFlags.NoDocking |
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoNavFocus |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);

        ImGui.Begin("##DockSpaceHost", hostFlags);

        uint dockspaceId = ImGui.GetID("MainDockSpace");
        ImGui.DockSpace(dockspaceId);

        ImGui.End();
        ImGui.PopStyleVar(3);
    }

    public void Dispose()
    {
        _controller?.Dispose();
        _gl?.Dispose();
        _mainWindow?.Dispose();
        _window?.Dispose();
    }
}
