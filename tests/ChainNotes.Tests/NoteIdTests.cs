using ChainNotes.Core;

namespace ChainNotes.Tests;

public class NoteIdTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  Spaces  ", "spaces")]
    [InlineData("CAPS Lock", "caps-lock")]
    [InlineData("a--b", "a-b")]
    [InlineData("hello_world", "hello-world")]
    [InlineData("C# & .NET 10!", "c-net-10")]
    [InlineData("!!!", "note")]
    [InlineData("", "note")]
    [InlineData("   ", "note")]
    public void Slugify_NormalizesTitles(string input, string expected) =>
        Assert.Equal(expected, NoteId.Slugify(input));

    [Fact]
    public void Slugify_Null_ReturnsFallback() =>
        Assert.Equal("note", NoteId.Slugify(null));

    [Fact]
    public void Slugify_KeepsUnicodeLetters()
    {
        var slug = NoteId.Slugify("Ünïcode Café");
        Assert.Equal("ünïcode-café", slug);
    }

    [Fact]
    public void Slugify_DoesNotEndInDash() =>
        Assert.DoesNotContain('-', NoteId.Slugify("...dots..."));

    [Fact]
    public void Allocate_WithoutCollisions_ReturnsBaseSlug() =>
        Assert.Equal("my-note", NoteId.Allocate("My Note", ["other", "not-the-same"]));

    [Fact]
    public void Allocate_WithCollision_AppendsSuffix()
    {
        var id = NoteId.Allocate("My Note", ["my-note"]);
        Assert.Equal("my-note-2", id);
    }

    [Fact]
    public void Allocate_WithMultipleCollisions_TakesNextFreeSuffix()
    {
        var id = NoteId.Allocate("My Note", ["my-note", "my-note-2"]);
        Assert.Equal("my-note-3", id);
    }

    [Fact]
    public void Allocate_EmptyTitleWithTakenFallback_SuffixesFallback()
    {
        var id = NoteId.Allocate("", ["note"]);
        Assert.Equal("note-2", id);
    }

    [Fact]
    public void Allocate_NullTakenList_ReturnsBaseSlug() =>
        Assert.Equal("unique", NoteId.Allocate("Unique", null));

    [Theory]
    [InlineData("hello-world", true)]
    [InlineData("note2", true)]
    [InlineData("note", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("../escape", false)]
    [InlineData("a/b", false)]
    [InlineData(@"a\b", false)]
    [InlineData("a b", false)]
    [InlineData("a.b", false)]
    public void IsSafe_FlagsUnsafeIds(string? id, bool expected) =>
        Assert.Equal(expected, NoteId.IsSafe(id));

    [Fact]
    public void Slugify_TruncatesToMaxSlugLength()
    {
        var slug = NoteId.Slugify(new string('a', 500));
        Assert.Equal(NoteId.MaxSlugLength, slug.Length);
    }

    [Fact]
    public void Slugify_BoundaryLength_IsPreserved()
    {
        var title = new string('a', NoteId.MaxSlugLength);
        Assert.Equal(NoteId.MaxSlugLength, NoteId.Slugify(title).Length);
    }

    [Fact]
    public void Slugify_JustOverBoundary_IsTruncated()
    {
        var title = new string('a', NoteId.MaxSlugLength + 1);
        Assert.Equal(NoteId.MaxSlugLength, NoteId.Slugify(title).Length);
    }

    [Fact]
    public void Slugify_Truncation_NeverEndsInDash()
    {
        // 79 'a's + separator + 'b' -> 81-char slug; cutting at 80 would
        // leave a trailing '-' unless it is trimmed.
        var title = new string('a', 79) + " b";
        var slug = NoteId.Slugify(title);

        Assert.Equal(new string('a', 79), slug);
        Assert.DoesNotContain('-', slug);
    }

    [Fact]
    public void Slugify_HugeTitle_StaysWithinFilesystemLimits()
    {
        var slug = NoteId.Slugify("Important meeting notes " + new string('x', 10_000));
        Assert.True(slug.Length <= NoteId.MaxSlugLength, $"slug length {slug.Length}");
    }

    [Fact]
    public void Slugify_EmojiOnlyTitle_ReturnsFallback() =>
        Assert.Equal("note", NoteId.Slugify("🎉🚀✨"));

    [Fact]
    public void Slugify_EmojiInsideTitle_CollapsesToSingleDash() =>
        Assert.Equal("hello-world", NoteId.Slugify("Hello 🎉 World"));

    [Fact]
    public void Allocate_CappedSlug_StillSuffixesOnCollision()
    {
        var baseSlug = new string('a', NoteId.MaxSlugLength);
        var id = NoteId.Allocate(baseSlug, [baseSlug]);

        Assert.Equal($"{baseSlug}-2", id);
    }
}
