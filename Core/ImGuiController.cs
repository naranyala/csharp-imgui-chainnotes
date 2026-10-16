using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace ChainNotes.Core;

/// <summary>
/// Dear ImGui backend for Silk.NET: OpenGL 3.3 core renderer + keyboard/mouse platform layer.
/// </summary>
public sealed unsafe class ImGuiController : IDisposable
{
    private readonly GL _gl;
    private readonly IWindow _window;
    private readonly IInputContext _input;

    private uint _fontTexture;
    private uint _shader;
    private uint _vao;
    private uint _vbo;
    private uint _ebo;

    private int _attribLocationTex;
    private int _attribLocationProjMtx;
    private int _attribLocationPosition;
    private int _attribLocationUV;
    private int _attribLocationColor;

    private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    private double _lastTime;

    public ImGuiController(GL gl, IWindow window, IInputContext input)
    {
        _gl = gl;
        _window = window;
        _input = input;

        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigWindowsResizeFromEdges = true;

        CreateDeviceObjects();
        ApplyStyle();
        WireInput();
    }

    private static void ApplyStyle()
    {
        ImGui.StyleColorsDark();
        var style = ImGui.GetStyle();
        style.WindowRounding = 4f;
        style.FrameRounding = 3f;
        style.GrabRounding = 3f;
        style.TabRounding = 3f;
        style.ScrollbarRounding = 6f;
        style.WindowBorderSize = 1f;
        style.FrameBorderSize = 0f;
        style.ItemSpacing = new Vector2(8f, 6f);
        style.CellPadding = new Vector2(6f, 4f);
    }

    // ------------------------------------------------------------------ input

    private void WireInput()
    {
        foreach (var keyboard in _input.Keyboards)
        {
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
            keyboard.KeyChar += OnKeyChar;
        }

        foreach (var mouse in _input.Mice)
        {
            mouse.MouseMove += OnMouseMove;
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp += OnMouseUp;
            mouse.Scroll += OnScroll;
        }

        _window.FocusChanged += focused => ImGui.GetIO().AddFocusEvent(focused);
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        var io = ImGui.GetIO();
        var imguiKey = MapKey(key);
        io.AddKeyEvent(imguiKey, true);
        io.SetKeyEventNativeData(imguiKey, keyCode, keyCode);
        UpdateModifiers(io, keyboard);
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int keyCode)
    {
        var io = ImGui.GetIO();
        io.AddKeyEvent(MapKey(key), false);
        UpdateModifiers(io, keyboard);
    }

    private static void UpdateModifiers(ImGuiIOPtr io, IKeyboard keyboard)
    {
        io.AddKeyEvent(ImGuiKey.ModCtrl, keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight));
        io.AddKeyEvent(ImGuiKey.ModShift, keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight));
        io.AddKeyEvent(ImGuiKey.ModAlt, keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight));
        io.AddKeyEvent(ImGuiKey.ModSuper, keyboard.IsKeyPressed(Key.SuperLeft) || keyboard.IsKeyPressed(Key.SuperRight));
    }

    private void OnKeyChar(IKeyboard keyboard, char c) => ImGui.GetIO().AddInputCharacter(c);

    private void OnMouseMove(IMouse mouse, Vector2 position) =>
        ImGui.GetIO().AddMousePosEvent(position.X, position.Y);

    private void OnMouseDown(IMouse mouse, MouseButton button) =>
        ImGui.GetIO().AddMouseButtonEvent(MapMouseButton(button), true);

    private void OnMouseUp(IMouse mouse, MouseButton button) =>
        ImGui.GetIO().AddMouseButtonEvent(MapMouseButton(button), false);

    private void OnScroll(IMouse mouse, ScrollWheel wheel) =>
        ImGui.GetIO().AddMouseWheelEvent(wheel.X, wheel.Y);

    private static int MapMouseButton(MouseButton button) => button switch
    {
        MouseButton.Left => 0,
        MouseButton.Right => 1,
        MouseButton.Middle => 2,
        MouseButton.Button4 => 3,
        MouseButton.Button5 => 4,
        _ => 0,
    };

    private static ImGuiKey MapKey(Key key) => key switch
    {
        Key.Tab => ImGuiKey.Tab,
        Key.Left => ImGuiKey.LeftArrow,
        Key.Right => ImGuiKey.RightArrow,
        Key.Up => ImGuiKey.UpArrow,
        Key.Down => ImGuiKey.DownArrow,
        Key.PageUp => ImGuiKey.PageUp,
        Key.PageDown => ImGuiKey.PageDown,
        Key.Home => ImGuiKey.Home,
        Key.End => ImGuiKey.End,
        Key.Insert => ImGuiKey.Insert,
        Key.Delete => ImGuiKey.Delete,
        Key.Backspace => ImGuiKey.Backspace,
        Key.Space => ImGuiKey.Space,
        Key.Enter => ImGuiKey.Enter,
        Key.Escape => ImGuiKey.Escape,
        Key.ControlLeft => ImGuiKey.LeftCtrl,
        Key.ShiftLeft => ImGuiKey.LeftShift,
        Key.AltLeft => ImGuiKey.LeftAlt,
        Key.SuperLeft => ImGuiKey.LeftSuper,
        Key.ControlRight => ImGuiKey.RightCtrl,
        Key.ShiftRight => ImGuiKey.RightShift,
        Key.AltRight => ImGuiKey.RightAlt,
        Key.SuperRight => ImGuiKey.RightSuper,
        Key.Menu => ImGuiKey.Menu,
        Key.Apostrophe => ImGuiKey.Apostrophe,
        Key.Comma => ImGuiKey.Comma,
        Key.Minus => ImGuiKey.Minus,
        Key.Period => ImGuiKey.Period,
        Key.Slash => ImGuiKey.Slash,
        Key.Semicolon => ImGuiKey.Semicolon,
        Key.Equal => ImGuiKey.Equal,
        Key.LeftBracket => ImGuiKey.LeftBracket,
        Key.BackSlash => ImGuiKey.Backslash,
        Key.RightBracket => ImGuiKey.RightBracket,
        Key.GraveAccent => ImGuiKey.GraveAccent,
        Key.CapsLock => ImGuiKey.CapsLock,
        Key.ScrollLock => ImGuiKey.ScrollLock,
        Key.NumLock => ImGuiKey.NumLock,
        Key.PrintScreen => ImGuiKey.PrintScreen,
        Key.Pause => ImGuiKey.Pause,
        Key.Keypad0 => ImGuiKey.Keypad0,
        Key.Keypad1 => ImGuiKey.Keypad1,
        Key.Keypad2 => ImGuiKey.Keypad2,
        Key.Keypad3 => ImGuiKey.Keypad3,
        Key.Keypad4 => ImGuiKey.Keypad4,
        Key.Keypad5 => ImGuiKey.Keypad5,
        Key.Keypad6 => ImGuiKey.Keypad6,
        Key.Keypad7 => ImGuiKey.Keypad7,
        Key.Keypad8 => ImGuiKey.Keypad8,
        Key.Keypad9 => ImGuiKey.Keypad9,
        Key.KeypadDecimal => ImGuiKey.KeypadDecimal,
        Key.KeypadDivide => ImGuiKey.KeypadDivide,
        Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
        Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
        Key.KeypadAdd => ImGuiKey.KeypadAdd,
        Key.KeypadEnter => ImGuiKey.KeypadEnter,
        Key.KeypadEqual => ImGuiKey.KeypadEqual,
        >= Key.F1 and <= Key.F24 => ImGuiKey.F1 + (key - Key.F1),
        >= Key.Number0 and <= Key.Number9 => ImGuiKey._0 + (key - Key.Number0),
        >= Key.A and <= Key.Z => ImGuiKey.A + (key - Key.A),
        _ => ImGuiKey.None,
    };

    // -------------------------------------------------------- device objects

    private const string VertexShaderSource = """
        #version 330 core
        layout (location = 0) in vec2 Position;
        layout (location = 1) in vec2 UV;
        layout (location = 2) in vec4 Color;
        uniform mat4 ProjMtx;
        out vec4 Frag_Color;
        out vec2 Frag_UV;
        void main()
        {
            Frag_UV = UV;
            Frag_Color = Color;
            gl_Position = ProjMtx * vec4(Position.xy, 0.0, 1.0);
        }
        """;

    private const string FragmentShaderSource = """
        #version 330 core
        in vec4 Frag_Color;
        in vec2 Frag_UV;
        out vec4 Out_Color;
        uniform sampler2D Texture;
        void main()
        {
            Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
        }
        """;

    private void CreateDeviceObjects()
    {
        _shader = CreateProgram(VertexShaderSource, FragmentShaderSource);
        _attribLocationTex = _gl.GetUniformLocation(_shader, "Texture");
        _attribLocationProjMtx = _gl.GetUniformLocation(_shader, "ProjMtx");
        _attribLocationPosition = _gl.GetAttribLocation(_shader, "Position");
        _attribLocationUV = _gl.GetAttribLocation(_shader, "UV");
        _attribLocationColor = _gl.GetAttribLocation(_shader, "Color");

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

        uint stride = (uint)sizeof(ImDrawVert);
        _gl.EnableVertexAttribArray((uint)_attribLocationPosition);
        _gl.VertexAttribPointer((uint)_attribLocationPosition, 2, VertexAttribPointerType.Float, false, stride, (void*)0);
        _gl.EnableVertexAttribArray((uint)_attribLocationUV);
        _gl.VertexAttribPointer((uint)_attribLocationUV, 2, VertexAttribPointerType.Float, false, stride, (void*)8);
        _gl.EnableVertexAttribArray((uint)_attribLocationColor);
        _gl.VertexAttribPointer((uint)_attribLocationColor, 4, VertexAttribPointerType.UnsignedByte, true, stride, (void*)16);

        CreateFontsTexture();
    }

    private uint CreateProgram(string vertexSource, string fragmentSource)
    {
        uint vs = CompileShader(ShaderType.VertexShader, vertexSource);
        uint fs = CompileShader(ShaderType.FragmentShader, fragmentSource);

        uint program = _gl.CreateProgram();
        _gl.AttachShader(program, vs);
        _gl.AttachShader(program, fs);
        _gl.LinkProgram(program);

        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int status);
        if (status == 0)
            throw new InvalidOperationException($"ImGui shader program failed to link: {_gl.GetProgramInfoLog(program)}");

        _gl.DetachShader(program, vs);
        _gl.DetachShader(program, fs);
        _gl.DeleteShader(vs);
        _gl.DeleteShader(fs);
        return program;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
        if (status == 0)
            throw new InvalidOperationException($"ImGui {type} failed to compile: {_gl.GetShaderInfoLog(shader)}");

        return shader;
    }

    private void CreateFontsTexture()
    {
        var io = ImGui.GetIO();

        float dpi = MathF.Max(1f, _window.FramebufferSize.X / (float)Math.Max(1, _window.Size.X));

        var config = ImGuiNative.ImFontConfig_ImFontConfig();
        config->SizePixels = 16f * dpi;
        io.Fonts.AddFontDefault(config);
        ImGuiNative.ImFontConfig_destroy(config);

        io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int _);

        _fontTexture = _gl.GenTexture();
        _gl.BindTexture(GLEnum.Texture2D, _fontTexture);
        _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)GLEnum.Linear);
        _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)GLEnum.ClampToEdge);
        _gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)GLEnum.ClampToEdge);
        _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        _gl.TexImage2D(
            GLEnum.Texture2D,
            0,
            InternalFormat.Rgba,
            (uint)width,
            (uint)height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            pixels);

        io.Fonts.SetTexID(new IntPtr((int)_fontTexture));
        io.Fonts.ClearTexData();
    }

    // ------------------------------------------------------------------ frame

    public void NewFrame()
    {
        var io = ImGui.GetIO();

        double now = _clock.Elapsed.TotalSeconds;
        float delta = (float)(now - _lastTime);
        _lastTime = now;
        io.DeltaTime = delta > 0f ? delta : 1f / 60f;

        var windowSize = _window.Size;
        var framebufferSize = _window.FramebufferSize;
        io.DisplaySize = new Vector2(windowSize.X, windowSize.Y);
        if (windowSize.X > 0 && windowSize.Y > 0)
        {
            io.DisplayFramebufferScale = new Vector2(
                framebufferSize.X / (float)windowSize.X,
                framebufferSize.Y / (float)windowSize.Y);
        }

        ImGui.NewFrame();
    }

    // ----------------------------------------------------------------- render

    public void RenderImDrawData(ImDrawDataPtr drawData)
    {
        int fbWidth = (int)(drawData.DisplaySize.X * drawData.FramebufferScale.X);
        int fbHeight = (int)(drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
        if (fbWidth <= 0 || fbHeight <= 0 || drawData.CmdListsCount == 0)
            return;

        var backup = BackupRenderState();

        Vector2 clipOff = drawData.DisplayPos;
        Vector2 clipScale = drawData.FramebufferScale;

        _gl.Viewport(0, 0, (uint)fbWidth, (uint)fbHeight);

        float left = drawData.DisplayPos.X;
        float right = drawData.DisplayPos.X + drawData.DisplaySize.X;
        float top = drawData.DisplayPos.Y;
        float bottom = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

        float[] ortho =
        [
            2.0f / (right - left), 0.0f, 0.0f, 0.0f,
            0.0f, 2.0f / (top - bottom), 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, 0.0f,
            (right + left) / (left - right), (top + bottom) / (bottom - top), 0.0f, 1.0f,
        ];

        _gl.UseProgram(_shader);
        _gl.Uniform1(_attribLocationTex, 0);
        _gl.UniformMatrix4(_attribLocationProjMtx, false, ortho);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindVertexArray(_vao);

        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.Disable(EnableCap.CullFace);
        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.PrimitiveRestart);
        _gl.PolygonMode(GLEnum.FrontAndBack, PolygonMode.Fill);
        _gl.Enable(EnableCap.ScissorTest);

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            var vtxBuffer = cmdList.VtxBuffer;
            var idxBuffer = cmdList.IdxBuffer;

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
            _gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)(vtxBuffer.Size * sizeof(ImDrawVert)),
                (void*)vtxBuffer.Data,
                BufferUsageARB.StreamDraw);

            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
            _gl.BufferData(
                BufferTargetARB.ElementArrayBuffer,
                (nuint)(idxBuffer.Size * sizeof(ushort)),
                (void*)idxBuffer.Data,
                BufferUsageARB.StreamDraw);

            for (int cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                var cmd = cmdList.CmdBuffer[cmdI];

                if (cmd.UserCallback != IntPtr.Zero)
                {
                    var callback = (delegate* unmanaged[Cdecl]<ImDrawList*, ImDrawCmd*, void>)cmd.UserCallback;
                    callback(cmdList.NativePtr, cmd.NativePtr);
                    continue;
                }

                Vector4 clipRect = cmd.ClipRect;
                float x1 = clipRect.X - clipOff.X;
                float y1 = clipRect.Y - clipOff.Y;
                float x2 = clipRect.Z - clipOff.X;
                float y2 = clipRect.W - clipOff.Y;

                if (x2 <= x1 || y2 <= y1)
                    continue;

                int scissorX = (int)(x1 * clipScale.X);
                int scissorY = (int)(y2 * clipScale.Y);
                int scissorW = (int)((x2 - x1) * clipScale.X);
                int scissorH = (int)((y2 - y1) * clipScale.Y);

                _gl.Scissor(scissorX, fbHeight - scissorY - scissorH, (uint)scissorW, (uint)scissorH);

                _gl.BindTexture(GLEnum.Texture2D, (uint)cmd.GetTexID().ToInt32());

                _gl.DrawElements(
                    PrimitiveType.Triangles,
                    cmd.ElemCount,
                    DrawElementsType.UnsignedShort,
                    (void*)(cmd.IdxOffset * sizeof(ushort)));
            }
        }

        RestoreRenderState(backup);
    }

    private struct GlBackup
    {
        public int LastActiveTexture;
        public int CurrentProgram;
        public int TextureBinding2D;
        public int SamplerBinding;
        public int ArrayBuffer;
        public int VertexArray;
        public int BlendSrcRgb;
        public int BlendDstRgb;
        public int BlendSrcAlpha;
        public int BlendDstAlpha;
        public int BlendEquationRgb;
        public int BlendEquationAlpha;
        public bool Blend;
        public bool CullFace;
        public bool DepthTest;
        public bool StencilTest;
        public bool ScissorTest;
        public bool PrimitiveRestart;
        public fixed int Viewport[4];
        public fixed int ScissorBox[4];
        public fixed int PolygonMode[2];
    }

    private GlBackup BackupRenderState()
    {
        var backup = new GlBackup();

        _gl.GetInteger(GetPName.ActiveTexture, out backup.LastActiveTexture);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.GetInteger(GetPName.CurrentProgram, out backup.CurrentProgram);
        _gl.GetInteger(GetPName.TextureBinding2D, out backup.TextureBinding2D);
        _gl.GetInteger(GetPName.SamplerBinding, out backup.SamplerBinding);
        _gl.GetInteger(GetPName.ArrayBufferBinding, out backup.ArrayBuffer);
        _gl.GetInteger(GetPName.VertexArrayBinding, out backup.VertexArray);
        _gl.GetInteger(GetPName.BlendSrcRgb, out backup.BlendSrcRgb);
        _gl.GetInteger(GetPName.BlendDstRgb, out backup.BlendDstRgb);
        _gl.GetInteger(GetPName.BlendSrcAlpha, out backup.BlendSrcAlpha);
        _gl.GetInteger(GetPName.BlendDstAlpha, out backup.BlendDstAlpha);
        _gl.GetInteger(GetPName.BlendEquationRgb, out backup.BlendEquationRgb);
        _gl.GetInteger(GetPName.BlendEquationAlpha, out backup.BlendEquationAlpha);

        backup.Blend = _gl.IsEnabled(EnableCap.Blend);
        backup.CullFace = _gl.IsEnabled(EnableCap.CullFace);
        backup.DepthTest = _gl.IsEnabled(EnableCap.DepthTest);
        backup.StencilTest = _gl.IsEnabled(EnableCap.StencilTest);
        backup.ScissorTest = _gl.IsEnabled(EnableCap.ScissorTest);
        backup.PrimitiveRestart = _gl.IsEnabled(EnableCap.PrimitiveRestart);

        _gl.GetInteger(GetPName.Viewport, 4, backup.Viewport);
        _gl.GetInteger(GetPName.ScissorBox, 4, backup.ScissorBox);
        _gl.GetInteger(GetPName.PolygonMode, 2, backup.PolygonMode);

        return backup;
    }

    private void RestoreRenderState(GlBackup backup)
    {
        _gl.UseProgram((uint)backup.CurrentProgram);
        _gl.BindTexture(GLEnum.Texture2D, (uint)backup.TextureBinding2D);
        _gl.ActiveTexture((TextureUnit)backup.LastActiveTexture);
        _gl.BindVertexArray((uint)backup.VertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, (uint)backup.ArrayBuffer);

        _gl.BlendEquationSeparate((GLEnum)backup.BlendEquationRgb, (GLEnum)backup.BlendEquationAlpha);
        _gl.BlendFuncSeparate(
            (BlendingFactor)backup.BlendSrcRgb,
            (BlendingFactor)backup.BlendDstRgb,
            (BlendingFactor)backup.BlendSrcAlpha,
            (BlendingFactor)backup.BlendDstAlpha);

        SetEnabled(EnableCap.Blend, backup.Blend);
        SetEnabled(EnableCap.CullFace, backup.CullFace);
        SetEnabled(EnableCap.DepthTest, backup.DepthTest);
        SetEnabled(EnableCap.StencilTest, backup.StencilTest);
        SetEnabled(EnableCap.ScissorTest, backup.ScissorTest);
        SetEnabled(EnableCap.PrimitiveRestart, backup.PrimitiveRestart);

        _gl.PolygonMode(GLEnum.FrontAndBack, (PolygonMode)backup.PolygonMode[0]);
        _gl.Viewport(backup.Viewport[0], backup.Viewport[1], (uint)backup.Viewport[2], (uint)backup.Viewport[3]);
        _gl.Scissor(backup.ScissorBox[0], backup.ScissorBox[1], (uint)backup.ScissorBox[2], (uint)backup.ScissorBox[3]);
    }

    private void SetEnabled(EnableCap cap, bool enabled)
    {
        if (enabled)
            _gl.Enable(cap);
        else
            _gl.Disable(cap);
    }

    public void Dispose()
    {
        ImGui.DestroyContext();

        if (_vao != 0) _gl.DeleteVertexArray(_vao);
        if (_vbo != 0) _gl.DeleteBuffer(_vbo);
        if (_ebo != 0) _gl.DeleteBuffer(_ebo);
        if (_shader != 0) _gl.DeleteProgram(_shader);
        if (_fontTexture != 0) _gl.DeleteTexture(_fontTexture);

        _input.Dispose();
    }
}
