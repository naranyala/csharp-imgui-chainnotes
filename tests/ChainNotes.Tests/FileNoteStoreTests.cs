using ChainNotes.Core;

namespace ChainNotes.Tests;

public sealed class FileNoteStoreTests : IDisposable
{
    private readonly TempDir _dir = new();
    private readonly FileNoteStore _store;

    public FileNoteStoreTests()
    {
        _store = new FileNoteStore(_dir.Path);
    }

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Load_MissingDirectory_ReturnsEmpty()
    {
        var store = new FileNoteStore(Path.Combine(_dir.Path, "does-not-exist"));
        Assert.Empty(store.Load());
    }

    [Fact]
    public void Load_ExistingEmptyDirectory_ReturnsEmpty()
    {
        Directory.CreateDirectory(_dir.Path);
        Assert.Empty(_store.Load());
    }

    [Fact]
    public void SaveThenLoad_RoundTripsNote()
    {
        _store.Save(new Note("my-note", "My Note", "body text\nsecond line"));

        var loaded = Assert.Single(_store.Load());
        Assert.Equal("my-note", loaded.Id);
        Assert.Equal("My Note", loaded.Title);
        Assert.Equal("body text\nsecond line", loaded.Body);
    }

    [Fact]
    public void SaveThenLoad_PreservesMultipleNotes()
    {
        _store.SaveAll(
        [
            new Note("alpha", "Alpha", "first"),
            new Note("beta", "Beta", "second\n\nwith paragraphs"),
            new Note("gamma", "Gamma", string.Empty),
        ]);

        var notes = _store.Load();
        Assert.Equal(3, notes.Count);
        Assert.Equal("first", notes.Single(n => n.Id == "alpha").Body);
        Assert.Equal("second\n\nwith paragraphs", notes.Single(n => n.Id == "beta").Body);
        Assert.Equal(string.Empty, notes.Single(n => n.Id == "gamma").Body);
    }

    [Fact]
    public void Save_OverwritesExistingFile()
    {
        _store.Save(new Note("note", "Old", "old body"));
        _store.Save(new Note("note", "New", "new body"));

        var loaded = Assert.Single(_store.Load());
        Assert.Equal("New", loaded.Title);
        Assert.Equal("new body", loaded.Body);
    }

    [Fact]
    public void Save_SecondWriteKeepsBackupOfPreviousContents()
    {
        _store.Save(new Note("note", "Note", "version one"));
        var firstSave = File.ReadAllText(Path.Combine(_dir.Path, "note.md"));

        _store.Save(new Note("note", "Note", "version two"));

        var backup = Path.Combine(_dir.Path, "note.md.bak");
        Assert.True(File.Exists(backup));
        Assert.Equal(firstSave, File.ReadAllText(backup));
        Assert.Contains("version two", File.ReadAllText(Path.Combine(_dir.Path, "note.md")));
    }

    [Fact]
    public void Save_LeavesNoTempFileBehind()
    {
        _store.Save(new Note("note", "Note", "body"));

        Assert.False(File.Exists(Path.Combine(_dir.Path, "note.md.tmp")));
        Assert.True(File.Exists(Path.Combine(_dir.Path, "note.md")));
    }

    [Fact]
    public void Save_UnsafeId_Throws()
    {
        Assert.Throws<ArgumentException>(() => _store.Save(new Note("../evil", "Evil", "body")));
    }

    [Fact]
    public void Load_SkipsBackupAndTempAndUnsafeFileNames()
    {
        Directory.CreateDirectory(_dir.Path);
        File.WriteAllText(Path.Combine(_dir.Path, "note.md.bak"), "---\ntitle: Bak\n---\nbody");
        File.WriteAllText(Path.Combine(_dir.Path, "note2.md.tmp"), "---\ntitle: Tmp\n---\nbody");
        File.WriteAllText(Path.Combine(_dir.Path, "bad id.md"), "---\ntitle: Bad\n---\nbody");
        File.WriteAllText(Path.Combine(_dir.Path, "good.md"), "---\ntitle: Good\n---\nbody");

        var loaded = Assert.Single(_store.Load());
        Assert.Equal("good", loaded.Id);
    }

    [Fact]
    public void Load_HandWrittenFileWithoutFrontMatter_KeepsBodyVerbatim()
    {
        Directory.CreateDirectory(_dir.Path);
        const string contents = "# Just a heading\n\nplain markdown, no header.\n";
        File.WriteAllText(Path.Combine(_dir.Path, "scratch.md"), contents);

        var loaded = Assert.Single(_store.Load());
        Assert.Equal("scratch", loaded.Id);
        Assert.Equal("scratch", loaded.Title);
        Assert.Equal(contents, loaded.Body);
    }

    [Fact]
    public void Load_IgnoresUnknownFrontMatterKeys()
    {
        Directory.CreateDirectory(_dir.Path);
        File.WriteAllText(
            Path.Combine(_dir.Path, "future.md"),
            "---\ntitle: Future\nid: something-else\ncreated: 2026-10-17\n---\nBODY");

        var loaded = Assert.Single(_store.Load());
        Assert.Equal("future", loaded.Id);
        Assert.Equal("Future", loaded.Title);
        Assert.Equal("BODY", loaded.Body);
    }

    [Fact]
    public void Load_BodyBeginningWithDelimiter_IsNotConfusedWithHeader()
    {
        _store.Save(new Note("dashy", "Dashy", "---\nnot a header\n"));

        var loaded = Assert.Single(_store.Load());
        Assert.Equal("---\nnot a header\n", loaded.Body);
    }

    [Fact]
    public void Save_CollapsesNewlinesInTitle()
    {
        _store.Save(new Note("multi", "Line one\nLine two", "body"));

        var loaded = Assert.Single(_store.Load());
        Assert.Equal("Line one Line two", loaded.Title);
        Assert.Equal("body", loaded.Body);
    }

    [Fact]
    public void Save_EmptyTitle_FallsBackToSlugWord()
    {
        _store.Save(new Note("empty", "   ", "body"));

        var loaded = Assert.Single(_store.Load());
        Assert.Equal("empty", loaded.Title);
    }

    [Fact]
    public void Save_CreatesDirectoryOnFirstWrite()
    {
        var nested = Path.Combine(_dir.Path, "a", "b", "c");
        var store = new FileNoteStore(nested);

        store.Save(new Note("note", "Note", "body"));

        Assert.True(File.Exists(Path.Combine(nested, "note.md")));
    }

    [Fact]
    public void Delete_RemovesFileAndLeftovers()
    {
        _store.Save(new Note("gone", "Gone", "body"));
        _store.Save(new Note("gone", "Gone", "body2"));
        Assert.True(File.Exists(Path.Combine(_dir.Path, "gone.md.bak")));

        _store.Delete("gone");

        Assert.Empty(_store.Load());
        Assert.False(File.Exists(Path.Combine(_dir.Path, "gone.md")));
        Assert.False(File.Exists(Path.Combine(_dir.Path, "gone.md.bak")));
        Assert.False(File.Exists(Path.Combine(_dir.Path, "gone.md.tmp")));
    }

    [Fact]
    public void Delete_UnknownOrUnsafeId_IsNoOp()
    {
        _store.Save(new Note("keep", "Keep", "body"));

        _store.Delete("missing");
        _store.Delete("../escape");
        _store.Delete(string.Empty);

        Assert.Single(_store.Load());
    }

    [Fact]
    public void Settings_RoundTripsSelection()
    {
        _store.SaveSettings(new NoteSettings { SelectedNoteId = "my-note" });

        Assert.Equal("my-note", _store.LoadSettings().SelectedNoteId);
    }

    [Fact]
    public void Settings_MissingFile_ReturnsDefaults()
    {
        var settings = _store.LoadSettings();

        Assert.Null(settings.SelectedNoteId);
    }

    [Fact]
    public void Settings_CorruptFile_ReturnsDefaults()
    {
        Directory.CreateDirectory(_dir.Path);
        File.WriteAllText(Path.Combine(_dir.Path, "settings.json"), "{ not valid json ");

        var settings = _store.LoadSettings();

        Assert.Null(settings.SelectedNoteId);
    }

    [Fact]
    public void Settings_UnknownJsonFields_AreIgnoredForForwardCompatibility()
    {
        Directory.CreateDirectory(_dir.Path);
        File.WriteAllText(
            Path.Combine(_dir.Path, "settings.json"),
            "{\"selectedNoteId\":\"x\",\"futureOption\":42,\"theme\":\"dark\"}");

        Assert.Equal("x", _store.LoadSettings().SelectedNoteId);
    }

    [Fact]
    public void Settings_NullSelection_RoundTrips()
    {
        _store.SaveSettings(new NoteSettings());

        Assert.Null(_store.LoadSettings().SelectedNoteId);
    }

    [Fact]
    public void Save_PreservesCrLfAndTrailingNewlines()
    {
        const string body = "line one\r\nline two\r\n\r\n";

        _store.Save(new Note("crlf", "CRLF", body));

        Assert.Equal(body, Assert.Single(_store.Load()).Body);
    }

    [Fact]
    public void Save_PreservesMultibyteBody()
    {
        const string body = "emoji 🎉🚀 and ünïcode café — exact round trip";

        _store.Save(new Note("unicode", "Unicode", body));

        Assert.Equal(body, Assert.Single(_store.Load()).Body);
    }

    [Fact]
    public void RoundTripsMegabyteBody()
    {
        var body = new System.Text.StringBuilder(1_048_576 + 2048);
        for (var i = 0; i < 1000; i++)
            body.Append('x', 1000).Append('\n');

        _store.Save(new Note("huge", "Huge", body.ToString()));

        Assert.Equal(body.ToString(), Assert.Single(_store.Load()).Body);
    }

    [Fact]
    public void SaveAll_EmptyEnumerable_IsNoOp()
    {
        _store.SaveAll([]);

        Assert.Empty(_store.Load());
    }

    [Fact]
    public void Save_NullNote_Throws() =>
        Assert.Throws<ArgumentNullException>(() => _store.Save(null!));

    [Fact]
    public void Delete_ThenRecreateSameId_StartsFresh()
    {
        _store.Save(new Note("reborn", "Old", "old"));
        _store.Delete("reborn");
        _store.Save(new Note("reborn", "New", "new"));

        var loaded = Assert.Single(_store.Load());
        Assert.Equal("New", loaded.Title);
        Assert.Equal("new", loaded.Body);
        Assert.False(File.Exists(Path.Combine(_dir.Path, "reborn.md.bak")));
    }

    [Fact]
    public void Save_TitleChange_KeepsIdAndSingleFile()
    {
        _store.Save(new Note("roadmap", "Roadmap", "body"));
        _store.Save(new Note("roadmap", "My Roadmap", "body"));

        var files = Directory.GetFiles(_dir.Path, "*.md");
        var loaded = Assert.Single(_store.Load());

        Assert.Single(files);
        Assert.Equal("roadmap", loaded.Id);
        Assert.Equal("My Roadmap", loaded.Title);
    }

    [Fact]
    public void Load_SortsByTitleCaseInsensitive()
    {
        _store.SaveAll(
        [
            new Note("c", "banana", ""),
            new Note("a", "Apple", ""),
            new Note("b", "cherry", ""),
        ]);

        var titles = _store.Load().Select(n => n.Title).ToList();

        Assert.Equal(new[] { "Apple", "banana", "cherry" }, titles);
    }

    [Fact]
    public void Load_DuplicateTitles_BothSurviveUnderDistinctIds()
    {
        _store.SaveAll(
        [
            new Note("meeting-1", "Meeting", "first"),
            new Note("meeting-2", "Meeting", "second"),
        ]);

        var notes = _store.Load();

        Assert.Equal(2, notes.Count);
        Assert.Contains(notes, n => n.Id == "meeting-1" && n.Body == "first");
        Assert.Contains(notes, n => n.Id == "meeting-2" && n.Body == "second");
    }

    [Fact]
    public void Load_WindowsLineEndingsInFrontMatter_Parses()
    {
        Directory.CreateDirectory(_dir.Path);
        File.WriteAllText(
            Path.Combine(_dir.Path, "win.md"),
            "---\r\ntitle: Win\r\n---\r\nbody line");

        var loaded = Assert.Single(_store.Load());

        Assert.Equal("Win", loaded.Title);
        Assert.Equal("body line", loaded.Body);
    }

    [Fact]
    public void Load_FileWithUtf8Bom_ParsesFrontMatter()
    {
        Directory.CreateDirectory(_dir.Path);
        File.WriteAllText(
            Path.Combine(_dir.Path, "bom.md"),
            "---\ntitle: Bom\n---\nbody",
            new System.Text.UTF8Encoding(true));

        var loaded = Assert.Single(_store.Load());

        Assert.Equal("Bom", loaded.Title);
        Assert.Equal("body", loaded.Body);
    }

    [Fact]
    public void Load_TitleLookingLikeFrontMatterKey_DoesNotConfuseParser()
    {
        Directory.CreateDirectory(_dir.Path);
        File.WriteAllText(
            Path.Combine(_dir.Path, "tricky.md"),
            "---\ntitle: title: not a second key\n---\nbody");

        var loaded = Assert.Single(_store.Load());

        Assert.Equal("title: not a second key", loaded.Title);
        Assert.Equal("body", loaded.Body);
    }

    [Fact]
    public void Load_FrontMatterWithoutTitleKey_FallsBackToIdHumanization()
    {
        Directory.CreateDirectory(_dir.Path);
        File.WriteAllText(
            Path.Combine(_dir.Path, "no-title.md"),
            "---\nother: value\n---\nbody");

        var loaded = Assert.Single(_store.Load());

        Assert.Equal("no title", loaded.Title);
        Assert.Equal("body", loaded.Body);
    }

    [Fact]
    public void Save_ThenLoad_Twice_IsStable()
    {
        _store.Save(new Note("stable", "Stable", "content"));
        var first = _store.Load();
        var second = _store.Load();

        Assert.Equal(first.Count, second.Count);
        Assert.Equal(first[0].Id, second[0].Id);
        Assert.Equal(first[0].Title, second[0].Title);
        Assert.Equal(first[0].Body, second[0].Body);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } =
            System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "chainnotes-tests-" + Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
