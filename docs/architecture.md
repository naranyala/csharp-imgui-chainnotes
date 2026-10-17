# Architecture

ChainNotes is a single-project .NET 10 app. It splits into three concerns: **bootstrapping the window** (`App`), **bridging Dear ImGui to Silk.NET** (`Core`), and **the actual application UI** (`UI`).

```
ChainNotes.csproj        Project file (net10.0, unsafe, native-lib copy target)
Program.cs               Entry point — constructs Application and calls Run()
docs/                    This documentation
App/
  Application.cs         Window creation, GL context, frame loop, dockspace host
Core/
  ImGuiController.cs     ImGui <-> Silk.NET backend (OpenGL renderer + input)
  Note.cs                Note model (Id, Title, Body)
  NoteId.cs              Pure slug/id helpers (title → file-safe id)
  INoteStore.cs          Persistence contract the UI depends on
  FileNoteStore.cs       One .md per note + settings.json, atomic writes
  NoteSettings.cs        Persisted app settings (last-selected note)
UI/
  MainWindow.cs          Application UI (notes panels, menus, stats)
tests/
  ChainNotes.Tests/      xunit contract tests for the store and ids
```

## The frame loop

`Application.Run` creates the window and subscribes to three Silk.NET events, then blocks on `window.Run()`:

| Event | When it fires | Handler |
| --- | --- | --- |
| `Load` | Once, after the window/context are ready | `OnLoad` — creates the GL context, input context, `ImGuiController`, and `MainWindow`. |
| `Update` | Every frame, before render | `OnUpdate` — ticks UI state (currently FPS smoothing). |
| `Render` | Every frame, after update | `OnRender` — clears the screen, drives ImGui, issues draw calls. |

The per-frame render sequence in `OnRender` is the heart of the app:

```csharp
_gl.ClearColor(...);  _gl.Clear(...);          // 1. clear the framebuffer
_controller.NewFrame();                          // 2. ImGui: delta-time, display size, input, NewFrame()
DrawDockspace();                                 // 3. host window that all panels dock into
_mainWindow.Draw();                              // 4. your widgets (see UI/MainWindow.cs)
ImGui.Render();                                  // 5. build draw lists
_controller.RenderImDrawData(ImGui.GetDrawData()); // 6. upload verts + issue GL draw calls
```

Steps 3 and 4 are where all application UI lives. Steps 2, 5, and 6 are the backend's job — see [ImGui backend](imgui-backend.md).

### Ownership and disposal

`Application` implements `IDisposable` and tears down in order: controller → GL → UI → window. `Program.Main` wraps it in `using`, so cleanup runs on normal exit. The `ImGuiController` also owns the input context it was given and disposes it.

## The dockspace host

`Application.DrawDockspace` creates one fullscreen, undecorated ImGui window covering the main viewport, then calls `ImGui.DockSpace(id)` inside it. Because `ImGuiConfigFlags.DockingEnable` is set in the controller's constructor, every other window becomes a dockable panel that snaps into this host.

The `##` prefix on `##DockSpaceHost` gives it an ID but no visible title, and the pushed style vars (rounding/border/padding = 0) make it an invisible backdrop.

> **Layout:** the sidebar and editor are dockable — no `NoDocking`/`NoMove`/`NoResize` pins, first placement via `ImGuiCond.FirstUseEver` (after a one-frame warm-up so `WorkPos` accounts for the menu bar), titles on both so they can be dragged, split, and tabbed. Layout persists via `imgui.ini`. The bottom status bar stays pinned (`NoDocking` + `NoMove` + `NoResize`). There is no DockBuilder API in this ImGui binding, so the default regions are placed rather than auto-docked.

## Window options

Configured in `Application.Run`:

- **Size** 1360×860, **title** `ChainNotes`, **VSync** on.
- **GraphicsAPI**: OpenGL, Core profile, Forward-compatible, **version 3.3**.

Forward-compatible core 3.3 is the most widely supported baseline across Linux/Windows/macOS. If you need newer GL features, bump `APIVersion` here — but you must also raise the `#version` in the shaders inside `ImGuiController`.

## Persistence

`Application.OnLoad` constructs one `FileNoteStore` (per-user data directory) and injects it into `MainWindow`, which loads notes at startup and saves through the `INoteStore` interface — the UI has no file-path or format knowledge. Note files are plain Markdown with a small frontmatter header; writes are atomic (temp file + rename) and keep one `.bak`. The last-selected note survives restarts via `settings.json`. Full details: [Persistence](persistence.md).

## The native library copy target

`ChainNotes.csproj` defines an MSBuild target `CopyNativeLibsToOutput` that runs after every build. It:

1. Detects the current RID (`linux-x64`, `osx-arm64`, `win-x64`, …) from `RuntimeInformation`.
2. Globs `runtimes/<rid>/native/*` in the build output.
3. Copies those files (GLFW, cimgui) into the output root.

This exists because Silk.NET's `DefaultPathResolver` looks for native libraries in `AppContext.BaseDirectory` **first**, and its fallback RID-graph resolution can fail on some distros — leaving GLFW unloadable and the window platform "not applicable." Copying the libraries to the output root sidesteps the RID graph entirely. See [Troubleshooting](troubleshooting.md#native-libraries-not-found) for the symptoms this fixes.
