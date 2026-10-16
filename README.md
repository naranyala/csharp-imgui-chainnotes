# ChainNotes

A cross-platform **Dear ImGui** desktop app starter for **C# / .NET 10**, built on **Silk.NET** (OpenGL 3.3 + GLFW).

It opens a resizable, **docking** window with a small notes-app shell — three panels, a menu bar, and a live stats readout — so you start from a real application instead of an empty demo.

```
dotnet run -c Release
```

## Features

- OpenGL 3.3 core context via Silk.NET.Windowing / GLFW — one codebase for Linux, Windows, and macOS
- Hand-written ImGui backend: GL renderer **and** keyboard/mouse platform layer (`ImGui.NET` 1.91)
- Docking enabled, dark theme, DPI-aware font scaling
- Notes shell: list / editor / stats panels + main menu bar
- Built-in ImGui Demo & Metrics windows (**View** menu)
- Layout persisted to `imgui.ini`
- Native GLFW + cimgui bundled via NuGet and copied to the output automatically

## Documentation

| | |
| --- | --- |
| **[Getting Started](docs/getting-started.md)** | Prerequisites, run/build, verify your toolchain |
| **[Architecture](docs/architecture.md)** | Project layout, frame loop, dockspace, native-lib copy target |
| **[ImGui Backend](docs/imgui-backend.md)** | How `ImGuiController` translates input and renders with OpenGL |
| **[Building Your UI](docs/building-ui.md)** | Adding panels, docking, widgets, fonts, styling |
| **[Troubleshooting](docs/troubleshooting.md)** | Launch failures, Wayland/X11, API gotchas |

New here? Start with **[Getting Started](docs/getting-started.md)**, then read **[Architecture](docs/architecture.md)**.

## Planning

- **[`PYRAMID-OF-INTENTS.md`](PYRAMID-OF-INTENTS.md)** — vision → mission → goals → epics; higher levels constrain lower ones
- **[`TODOS.md`](TODOS.md)** — phased tracker (3 done / 25 open), every task tagged `[Ex/Gx]` back to the pyramid

## Requirements

- .NET SDK 10.x — `dotnet --version` should report `10.x`
- An OpenGL 3.3 capable GPU/driver
- No system GLFW install needed — native libraries ship with the build

## Project layout

```
ChainNotes.csproj        net10.0, unsafe, native-lib copy target
Program.cs               Entry point
App/
  Application.cs         Window, GL context, frame loop, dockspace host
Core/
  ImGuiController.cs     ImGui <-> Silk.NET backend (renderer + input)
UI/
  MainWindow.md          Application UI (notes panels, menus, stats)
docs/                    Detailed documentation (start here)
```

## How it works

`Application` owns the window and the frame loop. Each frame:

1. `ImGuiController.NewFrame()` — updates delta-time and display size, feeds input to ImGui, starts the frame.
2. Your UI code submits widgets (see `MainWindow.Draw`).
3. `ImGui.Render()` builds the draw lists; `ImGuiController.RenderImDrawData` uploads vertices and issues OpenGL draws.

Details: **[Architecture](docs/architecture.md)** and **[ImGui Backend](docs/imgui-backend.md)**.

## Quick customization

**Add a panel** — edit `UI/MainWindow.cs`, add a method that calls `ImGui.Begin(...)` / `ImGui.End()`, and invoke it from `Draw()`. See [Building Your UI](docs/building-ui.md#adding-a-panel).

**Use a custom font** — in `Core/ImGuiController.cs` → `CreateFontsTexture`, replace the default-font call:

```csharp
io.Fonts.AddFontFromFileTTF("assets/fonts/Inter-Regular.ttf", 18f * dpi);
```

**Restyle** — adjust `ImGui.StyleColorsDark()` and `ImGui.GetStyle()` in `ApplyStyle`, or theme your panels with `ImGui.PushStyleColor` / `PopStyleColor`.

## License

Use it as a starting point for your own apps — no attribution required.
