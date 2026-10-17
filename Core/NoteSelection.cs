namespace ChainNotes.Core;

/// <summary>
/// Resolves which note should be selected after a load, given the id that was
/// persisted last time. Pure so restore behavior is testable without a UI.
/// </summary>
public static class NoteSelection
{
    /// <summary>
    /// Returns the index of the note whose id equals <paramref name="savedId"/>,
    /// or 0 when the id is null/unknown (first note, or empty list).
    /// </summary>
    public static int Resolve(IReadOnlyList<Note> notes, string? savedId)
    {
        if (string.IsNullOrEmpty(savedId))
            return 0;

        for (var i = 0; i < notes.Count; i++)
        {
            if (string.Equals(notes[i].Id, savedId, StringComparison.Ordinal))
                return i;
        }

        return 0;
    }
}
