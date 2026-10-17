namespace ChainNotes.Core;

/// <summary>
/// Small JSON-serialized app settings persisted alongside the notes.
/// Only the selected note is stored today; the type exists so future
/// settings (window state, theme, …) have an obvious home without
/// changing the store's shape.
/// </summary>
public sealed class NoteSettings
{
    public string? SelectedNoteId { get; set; }
}
