using System.Numerics;
using ImGuiNET;

namespace ChainNotes.UI;

public sealed class MainWindow : IDisposable
{
    private readonly List<Note> _notes =
    [
        new("Welcome", "Welcome to ChainNotes!\n\nThis is a Dear ImGui + Silk.NET starter app.\n\nLeft panel: note list\nCenter: editor\nRight: stats"),
        new("Roadmap", "- [x] Window + OpenGL 3.3 context\n- [x] ImGui docking + input\n- [ ] Persistence\n- [ ] Themes"),
        new("Scratchpad", "Type here..."),
    ];

    private int _selectedNote;
    private bool _showDemoWindow;
    private bool _showMetricsWindow;
    private bool _showAboutWindow;
    private double _delta;
    private double _fpsSmoothed;
    private string _editorText = string.Empty;

    public void Update(double delta)
    {
        _delta = delta;
        _fpsSmoothed = _fpsSmoothed <= 0 ? 1.0 / Math.Max(delta, 1e-6) : _fpsSmoothed * 0.95 + (1.0 / Math.Max(delta, 1e-6)) * 0.05;
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
                Environment.Exit(0);

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
            }

            if (ImGui.BeginPopupContextItem($"note_ctx_{i}"))
            {
                if (ImGui.MenuItem("Delete") && _notes.Count > 1)
                {
                    _notes.RemoveAt(i);
                    _selectedNote = Math.Clamp(_selectedNote, 0, _notes.Count - 1);
                    LoadSelectedIntoBuffer();
                    ImGui.EndPopup();
                    break;
                }

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

        if (ImGui.InputTextMultiline("##note_body", ref _editorText, 16 * 1024, new Vector2(-1, -1)))
            note.Body = _editorText;

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

    private void AddNote()
    {
        _notes.Add(new Note($"Note {_notes.Count + 1}", string.Empty));
        _selectedNote = _notes.Count - 1;
        LoadSelectedIntoBuffer();
    }

    private void LoadSelectedIntoBuffer()
    {
        _editorText = _notes[_selectedNote].Body;
    }

    public void Dispose()
    {
        // nothing to release yet
    }

    private sealed class Note(string title, string body)
    {
        public string Title { get; } = title;
        public string Body { get; set; } = body;
    }
}
