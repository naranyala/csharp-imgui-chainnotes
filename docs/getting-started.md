# Getting Started

ChainNotes is a Dear ImGui desktop app starter built on Silk.NET (OpenGL 3.3 + GLFW) for .NET 10. This guide covers prerequisites, running the app, and the common setup pitfalls.

## Prerequisites

| Requirement | Notes |
| --- | --- |
| .NET SDK 10.x | `dotnet --version` should report `10.x`. Install from <https://dotnet.microsoft.com/download>. |
| OpenGL 3.3 capable driver | Any modern GPU/driver. Software renderers (llvmpipe) may work but will be slow. |
| Native GLFW + cimgui | Bundled via NuGet — no system install needed (see [Troubleshooting](troubleshooting.md#native-libraries-not-found)). |

No system-wide `libglfw` install is required: the build copies the platform's native libraries into the output folder (see [`CopyNativeLibsToOutput`](architecture.md#the-native-library-copy-target)).

## Run

Fastest path — build and run in one step:

```bash
dotnet run -c Release
```

Or build once, then run the executable directly:

```bash
dotnet build -c Release
./bin/Release/net10.0/ChainNotes
```

On Windows the executable is `bin\Release\net10.0\ChainNotes.exe`.

### Debug build

```bash
dotnet run -c Debug
```

Debug builds are useful when developing the backend, since Silk.NET prints GLFW load failures to the console in `DEBUG`.

## Test

```bash
dotnet test tests/ChainNotes.Tests -c Release
```

The xunit suite covers the persistence layer (note store round-trips, atomic writes, settings, id slugs) and the platform clipboard mechanism — all without opening a window. CI runs the same command on every push (`.github/workflows/ci.yml`).

## What you should see

A resizable window titled **ChainNotes** with:

- A **menu bar** (File / View / Help) across the top — **File** shows the shortcuts: `Ctrl+N` new note, `Ctrl+S` save, `Ctrl+Q` quit.
- A **left "Notes" panel** listing sample notes, with a **New Note** button and right-click → Delete.
- A **center editor** whose title bar shows the current note's title — a multi-line text field with working clipboard (cut/copy/paste).
- A **bottom status bar**: save state (`All changes saved` / `Unsaved changes — saving…`), note count, FPS, and ImGui version.

The **Notes** and editor panels are genuinely dockable: drag their title bars onto the dockspace (or into each other) to rearrange, split, and tab them — the arrangement persists to `imgui.ini` in the working directory. The first run places them in the classic sidebar/editor regions; drag once to re-lay them out.

Notes are real: edits save to plain `.md` files under your local data directory, and the last-opened note is restored on the next launch (see [Persistence](persistence.md)).

Open **View → Dear ImGui Demo** to browse the full ImGui widget gallery — the fastest way to discover what's available.

## Verify the toolchain

If the app doesn't start, confirm the basics first:

```bash
dotnet --version      # expect 10.x
dotnet build -c Release   # expect "Build succeeded", 0 errors
```

A successful build prints a `Copied native libs (N file(s)) ...` message — that's the [native copy target](architecture.md#the-native-library-copy-target) doing its job. If it reports `0 file(s)`, see [Troubleshooting](troubleshooting.md#native-libraries-not-found).

## Next steps

- [Architecture](architecture.md) — how the window, frame loop, and ImGui backend fit together.
- [ImGui backend](imgui-backend.md) — details of the renderer and input layer.
- [Persistence](persistence.md) — how notes are stored on disk and how to swap the backend.
- [Building your UI](building-ui.md) — adding panels, fonts, and styling.
- [Troubleshooting](troubleshooting.md) — fixes for launch and rendering problems.
