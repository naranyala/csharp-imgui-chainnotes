using System.Numerics;
using System.Text;
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

    private const uint MinEditorBufferBytes = 64 * 1024;
    private const uint EditorBufferSlackBytes = 8 * 1024;

    /// <summary>Editor edits are coalesced so typing doesn't write once per keystroke.</summary>
    private const double SaveDebounceSeconds = 0.4;

    private Note? _pendingSave;
    private double _timeSinceEdit;

    public MainWindow(INoteStore store)
    {
        _store = store;
        _notes = store.Load().ToList();

        if (_notes.Count == 0)
        {
            _notes = CreateSeedNotes();
            _store.SaveAll(_notes);
        }

        _selectedNote = IndexOfNote(store.LoadSettings().SelectedNoteId);
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
        DrawMenuBar();

        DrawNotesList();
        DrawEditor();
        DrawStats();

        if (_showDemoWindow)
            ImGui.ShowDemoWindow(ref _showDemoWindow);

        if (_showMetricsWindow)
            ImGui.ShowMetricsWindow(ref _showMetricsWindow);

        if (_showAboutWindow)
            DrawAboutWindow();
    }

    private void DrawMenuBar()
    {
        if (!ImGui.BeginMainMenuBar())
            return;

        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("New Note"))
                AddNote();

            ImGui.Separator();

            if (ImGui.MenuItem("Quit", "Alt+F4"))
            {
                FlushPendingSave();
                PersistSelection();
                Environment.Exit(0);
            }

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
        ImGui.SetNextWindowPos(new Vector2(ImGui.GetMainViewport().WorkPos.X, ImGui.GetMainViewport().WorkPos.Y));
        ImGui.SetNextWindowSize(new Vector2(260, ImGui.GetMainViewport().WorkSize.Y));

        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoDocking;

        ImGui.Begin("##NotesSidebar", flags);

        ImGui.TextUnformatted("Notes");
        ImGui.Separator();

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
        ImGui.SetNextWindowPos(new Vector2(viewport.WorkPos.X + 260, viewport.WorkPos.Y));
        ImGui.SetNextWindowSize(new Vector2(viewport.WorkSize.X - 260 - 280, viewport.WorkSize.Y));

        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoDocking;

        ImGui.Begin("##Editor", flags);

        var note = _notes[_selectedNote];
        ImGui.TextUnformatted(note.Title);
        ImGui.Separator();

        // Size the buffer to the note (with slack) instead of a fixed cap, so
        // long notes are never truncated. See docs/persistence.md.
        var bufferBytes = (uint)Math.Max(
            MinEditorBufferBytes,
            (long)Encoding.UTF8.GetByteCount(_editorText) + EditorBufferSlackBytes);

        if (ImGui.InputTextMultiline("##note_body", ref _editorText, bufferBytes, new Vector2(-1, -1)))
        {
            note.Body = _editorText;
            _pendingSave = note;
            _timeSinceEdit = 0;
        }

        ImGui.End();
    }

    private void DrawStats()
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new Vector2(viewport.WorkPos.X + viewport.WorkSize.X - 280, viewport.WorkPos.Y));
        ImGui.SetNextWindowSize(new Vector2(280, viewport.WorkSize.Y));

        const ImGuiWindowFlags flags =
            ImGuiWindowFlags.NoTitleBar |
            ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoMove |
            ImGuiWindowFlags.NoCollapse |
            ImGuiWindowFlags.NoBringToFrontOnFocus |
            ImGuiWindowFlags.NoDocking;

        ImGui.Begin("##Stats", flags);

        ImGui.TextUnformatted("Stats");
        ImGui.Separator();
        ImGui.Text($"FPS: {_fpsSmoothed:0}");
        ImGui.Text($"Frame: {_delta * 1000.0:0.00} ms");
        ImGui.Text($"Notes: {_notes.Count}");

        var io = ImGui.GetIO();
        ImGui.Separator();
        ImGui.TextUnformatted("ImGui");
        ImGui.Text($"Version: {ImGui.GetVersion()}");
        ImGui.Text($"Display: {io.DisplaySize.X:0}x{io.DisplaySize.Y:0}");

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

    private int IndexOfNote(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return 0;

        var index = _notes.FindIndex(n => n.Id == id);
        return index >= 0 ? index : 0;
    }

    private static List<Note> CreateSeedNotes() =>
    [
        new(NoteId.Slugify("Welcome"), "Welcome",
            "Welcome to ChainNotes!\n\nThis is a Dear ImGui + Silk.NET starter app.\n\nLeft panel: note list\nCenter: editor\nRight: stats"),
        new(NoteId.Slugify("Roadmap"), "Roadmap",
            "- [x] Window + OpenGL 3.3 context\n- [x] ImGui docking + input\n- [x] Persistence\n- [ ] Themes"),
        new(NoteId.Slugify("Scratchpad"), "Scratchpad", "Type here..."),
    ];

    public void Dispose()
    {
        // Flush on exit so nothing typed in the last debounce window is lost.
        FlushPendingSave();
        PersistSelection();
    }
}
