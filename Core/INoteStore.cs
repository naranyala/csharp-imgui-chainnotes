namespace ChainNotes.Core;

/// <summary>
/// Persistence contract for notes. The UI depends only on this interface so
/// the storage format can change (or be faked in tests) without touching
/// widgets — principle L1#2 ("UI never touches disk directly").
/// </summary>
public interface INoteStore
{
    /// <summary>Loads every note in the store. Empty (never throws) on a fresh directory.</summary>
    IReadOnlyList<Note> Load();

    /// <summary>Atomically writes a single note, creating or overwriting its file.</summary>
    void Save(Note note);

    /// <summary>Writes every note (used when seeding / bulk-writing).</summary>
    void SaveAll(IEnumerable<Note> notes);

    /// <summary>Deletes the note's file if present. No-op otherwise.</summary>
    void Delete(string noteId);

    /// <summary>Loads persisted settings, or defaults when absent/corrupt.</summary>
    NoteSettings LoadSettings();

    /// <summary>Atomically writes the settings file.</summary>
    void SaveSettings(NoteSettings settings);
}
