# Troubleshooting

Common problems and their fixes. Work through the [Getting Started](getting-started.md) checks first if the build itself fails.

## Native libraries not found

**Symptoms**

```
System.PlatformNotSupportedException: Couldn't find a suitable window platform.
(GlfwPlatform - not applicable)
```

or, from the resolver directly:

```
System.IO.FileNotFoundException: Could not load from any of the possible library names!
```

**Cause**

Silk.NET loads GLFW (`libglfw.so.3` / `glfw3.dll`) and cimgui (`libcimgui.so`) through its `DefaultPathResolver`, which checks the app base directory first, then falls back to the NuGet `runtimes/<rid>/native/` folders using the dependency-context RID graph. On some distros (observed on Fedora) that RID-graph lookup returns nothing, so the load fails and the window platform reports itself as "not applicable."

**Fix**

The `CopyNativeLibsToOutput` target in `ChainNotes.csproj` copies the current RID's native libraries into the output root after every build, where Silk.NET finds them first. Rebuild and check the log:

```bash
dotnet build -c Release
# expect: Copied native libs (2 file(s)) for linux-x64 to bin/Release/net10.0/
```

If it prints `0 file(s)`, the RID detection didn't match your platform. Confirm the libraries landed next to the executable:

```bash
ls bin/Release/net10.0/*.so*        # Linux
ls bin/Release/net10.0/*.dll        # Windows
```

You should see `libglfw` and `libcimgui` (or their platform equivalents). If they're only under `runtimes/<rid>/native/`, the target's `_NativeRid` didn't match — check `RuntimeInformation` on your machine.

## Window fails to open on Wayland

**Symptom**

The process starts but no window appears, or GLFW reports a platform error on Wayland.

**Fix**

Force the X11 backend with an environment variable:

```bash
GLFW_PLATFORM=X11 dotnet run -c Release
```

GLFW 3.4 supports both backends; this pins it to X11. On a pure-Wayland session where XWayland is unavailable, run from a TTY or an X11 session instead.

## `xkbcommon: ERROR ... Compose file for locale`

```
xkbcommon: ERROR: couldn't find a Compose file for locale "en_US.UTF-8"
(mapped to "en_US.UTF-8")
```

**This is harmless.** It's a locale warning printed by the XKB library during GLFW initialization — it does not stop the app. The window will still open and render.

If you want to silence it, generate the locale's compose data (e.g. install/enable the `en_US.UTF-8` locale), but there's no functional reason to.

## Build errors from the ImGui/Silk.NET API

**Symptom**

Errors like `CS0117: 'Key' does not contain a definition for ...` or `CS1620: Argument must be passed with the 'ref' keyword`.

**Cause**

ImGui.NET and Silk.NET have similarly-named but distinct enums and method signatures.

**Common gotchas**

| Confusion | Correct |
| --- | --- |
| `Key.LeftControl`, `Key.LeftArrow` (Silk.NET) | `Key.ControlLeft`, `Key.Left` |
| `Key.Backslash` | `Key.BackSlash` (capital S) |
| `ImGui.MenuItem(label, ref b)` | `ImGui.MenuItem(label, "", ref b)` — needs a shortcut arg |
| `AddFontDefault(16f)` | `AddFontDefault(config)` with `config->SizePixels` — no float overload |

Always check both enums when mapping input. The full mapping lives in `Core/ImGuiController.cs` → `MapKey`.

## Rendering is corrupt or shows nothing

Checklist, in order:

1. **Vertex layout drift** — the `VertexAttribPointer` calls in `CreateDeviceObjects` are hardcoded to the 20-byte `ImDrawVert` layout (pos @0, uv @8, col @16). If you changed ImGui's vertex struct, update these to match.
2. **GLSL version mismatch** — the shaders declare `#version 330 core`. If you raised the context's `APIVersion` in `Application.Run`, the shaders still work, but lowering it below 3.3 will break them.
3. **Framebuffer size zero** — minimized windows report 0×0; `RenderImDrawData` early-outs by design. Resize to test.
4. **State leaking** — if your own rendering runs alongside ImGui, ensure `RenderImDrawData`'s backup/restore covers the state you both use. It currently saves program, texture, VAO/VBO, blend, caps, viewport, scissor, and polygon mode.

## ImGui layout is stuck

Delete the persisted layout file and restart:

```bash
rm imgui.ini
```

This resets all window positions, sizes, and dock arrangements to defaults.

## Getting a stack trace

For load-time failures, use a Debug build — Silk.NET prints the underlying GLFW load exception in `DEBUG`:

```bash
dotnet build -c Debug
dotnet bin/Debug/net10.0/ChainNotes.dll
```

For GL errors, temporarily add checks after a GL call (Silk.NET's `GL.GetError()`) in `Core/ImGuiController.cs`.
