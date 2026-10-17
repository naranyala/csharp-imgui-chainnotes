using System.Numerics;
using ChainNotes.Core;
using ImGuiNET;

namespace ChainNotes.UI;

public sealed class MainWindow : IDisposable
{
    private readonly INoteStore _store;
    private readonly List<Note> _notes;

    private int _selectedNote;
    private bool _showDemoWindow;
    private bool _showMetricsWindow;
    private bool _showAboutWindow;
    private double _delta;
    private double _fpsSmoothed;
    private string _editorText = string.Empty;

    /// <summary>Editor edits are coalesced so typing doesn't write once per keystroke.</summary>
    private const double SaveDebounceSeconds = 0.4;

    private const float SidebarWidth = 260f;

    /// <summary>At least ImGui's default WindowMinSize.Y (32) or the bar gets clamped.</summary>
    private const float StatusHeight = 32f;

    private Note? _pendingSave;
    private double _timeSinceEdit;
    private int _drawnFrames;

    public MainWindow(INoteStore store)
    {
        _store = store;
        _notes = store.Load().ToList();

        if (_notes.Count == 0)
        {
            _notes = DefaultNotes.CreateDefaults();
            _store.SaveAll(_notes);
        }

        _selectedNote = NoteSelection.Resolve(_notes, store.LoadSettings().SelectedNoteId);
        LoadSelectedIntoBuffer();
    }

    public void Update(double delta)
    {
        _delta = delta;
        _fpsSmoothed = _fpsSmoothed <= 0 ? 1.0 / Math.Max(delta, 1e-6) : _fpsSmoothed * 0.95 + (1.0 / Math.Max(delta, 1e-6)) * 0.05;

        if (_pendingSave is not null)
        {
            _timeSinceEdit += delta;
            if (_timeSinceEdit >= SaveDebounceSeconds)
                FlushPendingSave();
        }
    }

    public void Draw()
    {
        _drawnFrames++;

        DrawShortcuts();
        DrawMenuBar();

        // FirstUseEver placement must not run on frame 1: the viewport's
        // WorkPos/WorkSize don't yet account for the main menu bar (the
        // adjustment lands at end of frame), so panels would freeze under it.
        // Skipping one frame captures the corrected rect.
        if (_drawnFrames > 1)
        {
            DrawNotesList();
            DrawEditor();
        }

        DrawStatusBar();

        if (_showDemoWindow)
            ImGui.ShowDemoWindow(ref _showDemoWindow);

        if (_showMetricsWindow)
            ImGui.ShowMetricsWindow(ref _showMetricsWindow);

        if (_showAboutWindow)
            DrawAboutWindow();
    }

    /// <summary>
    /// Global shortcuts. Runs before widgets each frame; chords are checked
    /// via ImGui so they respect modifiers and the OS keyboard layout.
    /// </summary>
    private void DrawShortcuts()
    {
        if (ImGui.IsKeyChordPressed(ImGuiKey.ModCtrl | ImGuiKey.N))
            AddNote();

        if (ImGui.IsKeyChordPressed(ImGuiKey.ModCtrl | ImGuiKey.S))
            FlushPendingSave();

        if (ImGui.IsKeyChordPressed(ImGuiKey.ModCtrl | ImGuiKey.Q))
            Quit();
    }

    private void DrawMenuBar()
    {
        if (!ImGui.BeginMainMenuBar())
            return;

        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("New Note", "Ctrl+N"))
                AddNote();

            if (ImGui.MenuItem("Save", "Ctrl+S"))
                FlushPendingSave();

            ImGui.Separator();

            if (ImGui.MenuItem("Quit", "Ctrl+Q"))
                Quit();

            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("View"))
        {
            ImGui.MenuItem("Dear ImGui Demo", "", ref _showDemoWindow);
            ImGui.MenuItem("Metrics", "", ref _showMetricsWindow);
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Help"))
        {
            ImGui.MenuItem("About ChainNotes", "", ref _showAboutWindow);
            ImGui.EndMenu();
        }

        ImGui.EndMainMenuBar();
    }

    private void DrawNotesList()
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new Vector2(viewport.WorkPos.X, viewport.WorkPos.Y), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(SidebarWidth, viewport.WorkSize.Y - StatusHeight), ImGuiCond.FirstUseEver);

        // Genuinely dockable: no NoDocking/NoMove/NoResize pins; first placement
        // only (ImGuiCond.FirstUseEver), afterwards imgui.ini owns the layout.
        const ImGuiWindowFlags flags = ImGuiWindowFlags.None;

        ImGui.Begin("Notes##sidebar", flags);

        if (ImGui.Button("New Note"))
            AddNote();

        ImGui.Separator();

        for (int i = 0; i < _notes.Count; i++)
        {
            bool selected = i == _selectedNote;
            if (ImGui.Selectable($"{_notes[i].Title}##{i}", selected))
            {
                _selectedNote = i;
                LoadSelectedIntoBuffer();
                PersistSelection();
            }

            if (ImGui.BeginPopupContextItem($"note_ctx_{i}"))
            {
                if (ImGui.MenuItem("Delete") && _notes.Count > 1)
                    DeleteNoteAt(i);

                ImGui.EndPopup();
            }
        }

        ImGui.End();
    }

    private void DrawEditor()
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new Vector2(viewport.WorkPos.X + SidebarWidth, viewport.WorkPos.Y), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(viewport.WorkSize.X - SidebarWidth, viewport.WorkSize.Y - StatusHeight), ImGuiCond.FirstUseEver);

        const ImGuiWindowFlags flags = ImGuiWindowFlags.None;

        var note = _notes[_selectedNote];

        // Title shows the current note; the "##editor" suffix keeps a stable
        // window id (and dock state) across note switches.
        ImGui.Begin($"{EditorWindowTitle(note.Title)}##editor", flags);

        // Size the buffer to the note (with slack) instead of a fixed cap, so
        // long notes are never truncated. See docs/persistence.md.
        var bufferBytes = EditorBufferSize.RequiredBytes(_editorText);

        if (ImGui.InputTextMultiline("##note_body", ref _editorText, bufferBytes, new Vector2(-1, -1)))
        {
            note.Body = _editorText;
            _pendingSave = note;
            _timeSinceEdit = 0;
        }

        ImGui.End();
    }

    private static string EditorWindowTitle(string title)
    {
        // "##" starts the id segment in an ImGui window name; strip it so a
        // title can never change the window's identity.
        var clean = title.Replace("##", " ").Trim();
        return clean.Length == 0 ? "Untitled" : clean;
    }

    private void DrawStatusBar()
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new Vector2(viewport.WorkPos.X, viewport.WorkPos.Y + viewport.WorkSize.Y - StatusHeight));
        ImGui.SetNextWindowSize(new Vector2(viewport.WorkSize.X, StatusHeight));

        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoDocking |
            ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse;

        ImGui.Begin("##StatusBar", flags);

        ImGui.TextUnformatted(_pendingSave is not null ? "Unsaved changes — saving…" : "All changes saved");
        ImGui.SameLine();
        ImGui.TextDisabled("|");
        ImGui.SameLine();
        ImGui.Text($"Notes: {_notes.Count}");
        ImGui.SameLine();
        ImGui.TextDisabled("|");
        ImGui.SameLine();
        ImGui.Text($"FPS: {_fpsSmoothed:0}  ({_delta * 1000.0:0.0} ms)");

        var io = ImGui.GetIO();
        ImGui.SameLine();
        ImGui.TextDisabled("|");
        ImGui.SameLine();
        ImGui.TextUnformatted($"ImGui {ImGui.GetVersion()}  {io.DisplaySize.X:0}x{io.DisplaySize.Y:0}");

        ImGui.End();
    }

    private void DrawAboutWindow()
    {
        ImGui.SetNextWindowSize(new Vector2(360, 0), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("About ChainNotes", ref _showAboutWindow, ImGuiWindowFlags.NoCollapse))
        {
            ImGui.TextUnformatted("ChainNotes");
            ImGui.TextWrapped("Dear ImGui desktop app starter built with Silk.NET (OpenGL 3.3) on .NET 10.");
            ImGui.Separator();
            ImGui.TextWrapped("Replace these panels with your own application UI.");
        }
        ImGui.End();
    }

    // ------------------------------------------------------------- note actions

    private void Quit()
    {
        FlushPendingSave();
        PersistSelection();
        Environment.Exit(0);
    }

    private void AddNote()
    {
        var title = $"Note {_notes.Count + 1}";
        var id = NoteId.Allocate(title, _notes.Select(n => n.Id));
        var note = new Note(id, title, string.Empty);

        _notes.Add(note);
        _store.Save(note);

        _selectedNote = _notes.Count - 1;
        LoadSelectedIntoBuffer();
        PersistSelection();
    }

    private void DeleteNoteAt(int index)
    {
        var note = _notes[index];
        _notes.RemoveAt(index);

        if (_pendingSave == note)
            _pendingSave = null;
        else
            FlushPendingSave();

        _store.Delete(note.Id);

        _selectedNote = Math.Clamp(_selectedNote, 0, _notes.Count - 1);
        LoadSelectedIntoBuffer();
        PersistSelection();
    }

    private void LoadSelectedIntoBuffer()
    {
        // Commit any debounced edit before the selection moves away from it.
        FlushPendingSave();

        if (_notes.Count == 0)
        {
            _editorText = string.Empty;
            return;
        }

        _selectedNote = Math.Clamp(_selectedNote, 0, _notes.Count - 1);
        _editorText = _notes[_selectedNote].Body;
    }

    private void FlushPendingSave()
    {
        if (_pendingSave is null)
            return;

        _store.Save(_pendingSave);
        _pendingSave = null;
        _timeSinceEdit = 0;
    }

    private void PersistSelection()
    {
        var settings = _store.LoadSettings();
        settings.SelectedNoteId = _notes.Count == 0 ? null : _notes[_selectedNote].Id;
        _store.SaveSettings(settings);
    }

    public void Dispose()
    {
        // Flush on exit so nothing typed in the last debounce window is lost.
        FlushPendingSave();
        PersistSelection();
    }
}
