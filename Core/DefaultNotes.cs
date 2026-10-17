namespace ChainNotes.Core;

/// <summary>
/// First-run demo notes, written to the store when it is empty. Kept as a
/// pure factory (side effects stay in <c>MainWindow</c>'s constructor) so the
/// seed set itself can be unit-tested.
/// </summary>
public static class DefaultNotes
{
    public static List<Note> CreateDefaults() =>
    [
        new(NoteId.Slugify("Welcome"), "Welcome",
            "Welcome to ChainNotes!\n\nThis is a Dear ImGui + Silk.NET starter app.\n\nLeft panel: note list\nCenter: editor\nRight: stats"),
        new(NoteId.Slugify("Roadmap"), "Roadmap",
            "- [x] Window + OpenGL 3.3 context\n- [x] ImGui docking + input\n- [x] Persistence\n- [ ] Themes"),
        new(NoteId.Slugify("Scratchpad"), "Scratchpad", "Type here..."),
    ];
}
