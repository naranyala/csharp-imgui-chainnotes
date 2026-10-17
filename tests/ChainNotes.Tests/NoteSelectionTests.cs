using ChainNotes.Core;

namespace ChainNotes.Tests;

public class NoteSelectionTests
{
    private static List<Note> Notes(params string[] ids) =>
        ids.Select(id => new Note(id, id.ToUpperInvariant(), "body")).ToList();

    [Fact]
    public void EmptyList_ReturnsZero() =>
        Assert.Equal(0, NoteSelection.Resolve([], "anything"));

    [Fact]
    public void NullSavedId_SelectsFirstNote() =>
        Assert.Equal(0, NoteSelection.Resolve(Notes("a", "b", "c"), null));

    [Fact]
    public void EmptySavedId_SelectsFirstNote() =>
        Assert.Equal(0, NoteSelection.Resolve(Notes("a", "b"), string.Empty));

    [Fact]
    public void KnownId_SelectsThatNote() =>
        Assert.Equal(2, NoteSelection.Resolve(Notes("a", "b", "roadmap"), "roadmap"));

    [Fact]
    public void FirstNote_CanBeRestored() =>
        Assert.Equal(0, NoteSelection.Resolve(Notes("a", "b"), "a"));

    [Fact]
    public void UnknownId_FallsBackToFirstNote() =>
        Assert.Equal(0, NoteSelection.Resolve(Notes("a", "b"), "deleted-note"));

    [Fact]
    public void IdComparison_IsCaseSensitive() =>
        Assert.Equal(0, NoteSelection.Resolve(Notes("roadmap"), "Roadmap"));
}
