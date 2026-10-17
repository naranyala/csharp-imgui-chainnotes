# ChainNotes Documentation

Documentation for the ChainNotes ImGui desktop app starter (C# / .NET 10, Silk.NET + OpenGL 3.3).

## Contents

| Document | What it covers |
| --- | --- |
| [Getting Started](getting-started.md) | Prerequisites, running the app, verifying your toolchain. |
| [Architecture](architecture.md) | Project layout, the frame loop, docking host, native-lib copy target. |
| [ImGui Backend](imgui-backend.md) | The `ImGuiController`: input translation, fonts, GL renderer. |
| [Building Your UI](building-ui.md) | Adding panels, docking, widgets, fonts, styling. |
| [Persistence](persistence.md) | `INoteStore`, the `.md` file format, atomic writes, settings, save timing. |
| [Troubleshooting](troubleshooting.md) | Launch failures, native libraries, Wayland/X11, API gotchas. |

## Reading path

**Just want it running?** → [Getting Started](getting-started.md)

**Something's broken?** → [Troubleshooting](troubleshooting.md)

**Understand how it works** → [Architecture](architecture.md), then [ImGui Backend](imgui-backend.md)

**Start writing your own screens** → [Building Your UI](building-ui.md)

**How notes are stored / change the backend** → [Persistence](persistence.md)

## The one-minute summary

ChainNotes is a single-project .NET 10 executable. `App/Application.cs` owns the window and the per-frame loop; `Core/ImGuiController.cs` is a hand-written Dear ImGui ↔ Silk.NET backend (OpenGL 3.3 renderer + keyboard/mouse input); `Core/FileNoteStore.cs` persists notes as plain Markdown files behind `INoteStore`; `UI/MainWindow.cs` holds all the application widgets. Each frame the controller starts an ImGui frame, your UI submits widgets, and the controller draws the result with OpenGL.

Swap out the panels in `MainWindow` with your own UI and you have an app — the backend stays as-is.
