# The ImGui Backend

`Core/ImGuiController.cs` is the bridge between Dear ImGui (via ImGui.NET) and Silk.NET. It owns both halves of a typical ImGui integration:

- The **platform layer** — translating keyboard/mouse input into ImGui's event queue.
- The **renderer** — compiling GL shaders, building the font atlas, and drawing ImGui's command lists with OpenGL 3.3.

You generally don't call into it directly beyond two methods: `NewFrame()` and `RenderImDrawData(drawData)`, both driven by `Application`.

## Lifecycle

```csharp
var controller = new ImGuiController(gl, window, input);
```

The constructor does, in order:

1. `ImGui.CreateContext()` and enables `DockingEnable` + `RendererHasVtxOffset`.
2. `CreateDeviceObjects()` — compiles the shader program, creates the VAO/VBO/EBO, and builds the font texture.
3. `ApplyStyle()` — dark theme + corner rounding tweaks.
4. `WireInput()` — subscribes to keyboard/mouse/focus events.

`Dispose()` destroys the ImGui context and deletes every GL object it created, plus the input context.

## The platform (input) layer

Input is wired once in `WireInput`. Each Silk.NET event is converted into an ImGui `io.Add*Event` call:

| Silk.NET event | ImGui call |
| --- | --- |
| `keyboard.KeyDown` / `KeyUp` | `io.AddKeyEvent(MapKey(key), down)` |
| `keyboard.KeyChar` | `io.AddInputCharacter(c)` |
| `mouse.MouseMove` | `io.AddMousePosEvent(x, y)` |
| `mouse.MouseDown` / `MouseUp` | `io.AddMouseButtonEvent(button, down)` |
| `mouse.Scroll` | `io.AddMouseWheelEvent(x, y)` |
| `window.FocusChanged` | `io.AddFocusEvent(focused)` |

On every key event, `UpdateModifiers` also reports the current Ctrl/Shift/Alt/Super state via `ImGuiKey.Mod*`. This is what makes shortcuts like `Ctrl+A` work inside widgets.

### Key mapping

`MapKey` is a switch expression from Silk.NET's `Key` enum to ImGui's `ImGuiKey`. It handles named keys explicitly, then maps ranges arithmetically:

```csharp
>= Key.F1 and <= Key.F24    => ImGuiKey.F1 + (key - Key.F1),
>= Key.Number0 and <= Key.Number9 => ImGuiKey._0 + (key - Key.Number0),
>= Key.A and <= Key.Z       => ImGuiKey.A + (key - Key.A),
_                           => ImGuiKey.None,
```

The range trick works because both enums declare those keys contiguously. If you add support for a key the switch doesn't cover, it falls through to `ImGuiKey.None` (ignored by ImGui) rather than crashing.

> **Naming gotcha:** Silk.NET uses `Key.Left`, `Key.ControlLeft`, `Key.BackSlash`, while ImGui uses `ImGuiKey.LeftArrow`, `ImGuiKey.LeftCtrl`, `ImGuiKey.Backslash`. The names are deliberately different — check both enums when extending `MapKey`.

## Fonts

`CreateFontsTexture` builds the default font at a DPI-aware size:

```csharp
float dpi = MathF.Max(1f, _window.FramebufferSize.X / (float)_window.Size.X);
config->SizePixels = 16f * dpi;
```

It then rasterizes the atlas to RGBA32, uploads it to a GL texture, and hands the texture ID to ImGui with `io.Fonts.SetTexID(...)`. The atlas CPU copy is freed with `ClearTexData()`.

To load a custom font instead, see [Building your UI → Fonts](building-ui.md#fonts).

## The renderer

### `NewFrame()`

Called first each frame. It:

- Computes `io.DeltaTime` from a `Stopwatch` (clamped to a 60 FPS fallback on the first frame).
- Sets `io.DisplaySize` from the window size, and `io.DisplayFramebufferScale` from framebuffer ÷ window size (these differ on HiDPI).
- Calls `ImGui.NewFrame()` to begin the frame.

### `RenderImDrawData(drawData)`

Called after `ImGui.Render()`. It early-outs if the framebuffer is zero-sized or there's nothing to draw, then:

1. **Backs up GL state** into a `GlBackup` struct — active texture, program, bindings, blend factors/equation, enabled caps, viewport, scissor box, and polygon mode.
2. **Sets up the render state**: full framebuffer viewport, orthographic projection matrix matching ImGui's display coordinates, shader program bound, blending on (`SrcAlpha` / `OneMinusSrcAlpha`), depth/cull off, scissor on.
3. **Iterates draw lists**: for each, streams its vertex and index buffers into the shared VBO/EBO with `BufferData(..., StreamDraw)`.
4. **Iterates draw commands**: computes the scissor rect (Y-flipped to GL's bottom-left origin), binds the command's texture, and issues `DrawElements`. Commands with a `UserCallback` are invoked instead of drawn.
5. **Restores** the backed-up state exactly as it was.

Backing up and restoring state is what lets ImGui coexist with your own rendering — you can draw your 3D scene before or after ImGui without state leaking between them.

### Shaders

Two GLSL 330 core shaders are embedded as string literals:

- **Vertex**: transforms `Position` by the `ProjMtx` uniform, passes `UV` and `Color` through.
- **Fragment**: `Out_Color = Frag_Color * texture(Texture, Frag_UV)` — textured, vertex-colored triangles.

The vertex layout matches `ImDrawVert` exactly — 20 bytes: `vec2 Position` (offset 0), `vec2 UV` (offset 8), `vec4 Color` (offset 16, normalized `UnsignedByte`). **If you change `ImDrawVert` or the layout, the `VertexAttribPointer` calls in `CreateDeviceObjects` must be updated to match**, or rendering will be corrupt.

`RendererHasVtxOffset` is set, so `cmd.VtxOffset`/`IdxOffset` are honored — this lets ImGui split large meshes instead of being limited to 16-bit index ranges within a draw.
