# Building Your UI

All application UI lives in `UI/MainWindow.cs`. This guide covers how to add panels, use the docking system, load fonts, and apply styling.

## The shape of a frame

`MainWindow.Draw()` is called once per frame from `Application`. It just calls your panel methods in order:

```csharp
public void Draw()
{
    DrawMenuBar();
    DrawNotesList();
    DrawEditor();
    DrawStats();
    // optional: demo / metrics / about windows
}
```

Each panel is a plain method that brackets its widgets with `Begin`/`End`. Keep this pattern — one method per panel, no rendering logic outside ImGui calls.

## Adding a panel

The minimal panel:

```csharp
private void DrawMyPanel()
{
    ImGui.Begin("My Panel");
    ImGui.TextUnformatted("Hello");
    ImGui.End();
}
```

For a fixed-layout panel like the ones already in `MainWindow`, use the flags + positioning pattern:

```csharp
private void DrawMyPanel()
{
    var vp = ImGui.GetMainViewport();
    ImGui.SetNextWindowPos(new Vector2(vp.WorkPos.X, vp.WorkPos.Y + 400));
    ImGui.SetNextWindowSize(new Vector2(260, vp.WorkSize.Y - 400));

    const ImGuiWindowFlags flags =
        ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.NoMove |
        ImGuiWindowFlags.NoCollapse |
        ImGuiWindowFlags.NoBringToFrontOnFocus |
        ImGuiWindowFlags.NoDocking;

    ImGui.Begin("##MyPanel", flags);
    // ... widgets ...
    ImGui.End();
}
```

Two things to note:

- **`SetNextWindowPos`/`Size` apply only to the next `Begin`** — call them every frame, immediately before it.
- The `##` prefix gives the window an ID without showing a title. Omit it if you want a visible, draggable title bar.

Then add `DrawMyPanel();` to `Draw()`.

## Docking vs. fixed layout

`ImGuiConfigFlags.DockingEnable` is set in the controller, and `Application.DrawDockspace` hosts a dockspace filling the window.

- **Fixed panel** — include `ImGuiWindowFlags.NoDocking` (as the current panels do) and pin its position. It stays where you put it.
- **Dockable panel** — drop `NoDocking`. The user can drag its title bar into the dockspace, split, tab, and rearrange; the layout persists to `imgui.ini`.

You can mix both: keep a pinned sidebar and make the center area dockable.

## Common widgets

A quick reference for the widgets this starter touches. Full list in the in-app **View → Dear ImGui Demo**.

```csharp
// text
ImGui.Text("formatted {value}");
ImGui.TextUnformatted("raw, no formatting");
ImGui.TextWrapped("wraps to window width");
ImGui.TextColored(new Vector4(1, .6f, .2f, 1), "colored");

// interaction
if (ImGui.Button("Click me")) { /* ... */ }
ImGui.Checkbox("Toggle", ref myBool);
ImGui.SliderFloat("Amount", ref myFloat, 0, 1);

// selection (used by the notes list)
if (ImGui.Selectable($"{label}##{id}", isSelected)) { /* ... */ }

// menus
if (ImGui.BeginMenu("File"))
{
    if (ImGui.MenuItem("New", "Ctrl+N")) { /* ... */ }
    ImGui.EndMenu();
}

// popups / context menus
if (ImGui.BeginPopupContextItem("row_ctx"))
{
    if (ImGui.MenuItem("Delete")) { /* ... */ }
    ImGui.EndPopup();
}
```

### State and the frame lifecycle

ImGui is **immediate mode** — widgets are re-submitted every frame, and state lives in your fields, not the widgets. The pattern for editable text is to keep a `string`/`bool`/numeric field and pass it by `ref`; when the widget reports a change, write it back to your model:

```csharp
// from DrawEditor — buffer is updated live, committed on change
if (ImGui.InputTextMultiline("##note_body", ref _editorText, 16 * 1024, new Vector2(-1, -1)))
    note.Body = _editorText;
```

IDs matter: ImGui tracks widget identity by label. When looping over items, append a stable unique suffix — e.g. `$"{_notes[i].Title}##{i}"` — otherwise duplicate labels collide.

## Fonts

The default font is loaded in `ImGuiController.CreateFontsTexture` at `16f * dpi`. To use a custom TTF, replace that call:

```csharp
io.Fonts.AddFontFromFileTTF("assets/fonts/Inter-Regular.ttf", 18f * dpi);
```

The `* dpi` matters on HiDPI displays — without it, text renders at half size on 2× screens. Include the glyph ranges you need if you render non-Latin text (e.g. `AddFontFromFileTTF(path, size, config, io.Fonts.GetGlyphRangesCyrillic())`).

For multiple fonts, load the default first, then additional fonts with `io.Fonts.AddFontFromFileTTF(...)`, and switch with `ImGui.PushFont(font)` / `ImGui.PopFont()`.

## Styling

The base theme and tweaks are in `ImGuiController.ApplyStyle`:

```csharp
ImGui.StyleColorsDark();          // or StyleColorsLight / StyleColorsClassic
var style = ImGui.GetStyle();
style.WindowRounding = 4f;
style.FrameRounding  = 3f;
style.ItemSpacing    = new Vector2(8f, 6f);
```

To change colors, edit the `style.Colors[]` array after `StyleColorsDark()`. To scope a change to one region, use push/pop so it can't leak:

```csharp
ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.5f, 0.9f, 1f));
ImGui.Button("Primary");
ImGui.PopStyleColor();
```

Because `ApplyStyle` runs in the controller constructor, application-specific theming is better placed in `MainWindow`'s constructor or its own `ApplyTheme()` — the controller is shared infrastructure and ideally stays theme-agnostic.

## Where to look next

- `UI/MainWindow.cs` — every panel in this starter, with the flags and layout above applied.
- **View → Dear ImGui Demo** in the running app — the authoritative widget gallery.
- [ImGui backend](imgui-backend.md) — what happens to your widgets after `Draw()`.
