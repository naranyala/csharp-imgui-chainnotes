using ChainNotes.Core;

namespace ChainNotes.Tests;

public class DefaultNotesTests
{
    [Fact]
    public void CreatesThreeNotes()
    {
        Assert.Equal(3, DefaultNotes.CreateDefaults().Count);
    }

    [Fact]
    public void IdsAreUnique()
    {
        var ids = DefaultNotes.CreateDefaults().Select(n => n.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void IdsAreFilesystemSafe()
    {
        Assert.All(DefaultNotes.CreateDefaults(), n => Assert.True(NoteId.IsSafe(n.Id), n.Id));
    }

    [Fact]
    public void IdsMatchSlugifiedTitles()
    {
        foreach (var note in DefaultNotes.CreateDefaults())
            Assert.Equal(NoteId.Slugify(note.Title), note.Id);
    }

    [Fact]
    public void TitlesAreNonEmptyAndUnique()
    {
        var titles = DefaultNotes.CreateDefaults().Select(n => n.Title).ToList();

        Assert.All(titles, t => Assert.False(string.IsNullOrWhiteSpace(t)));
        Assert.Equal(titles.Count, titles.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void BodiesArePresent()
    {
        Assert.All(DefaultNotes.CreateDefaults(), n => Assert.False(string.IsNullOrEmpty(n.Body)));
    }

    [Fact]
    public void FreshFactoryCall_ReturnsIndependentInstances()
    {
        var first = DefaultNotes.CreateDefaults();
        var second = DefaultNotes.CreateDefaults();

        first[0].Body = "mutated";

        Assert.NotEqual(first[0].Body, second[0].Body);
    }
}
