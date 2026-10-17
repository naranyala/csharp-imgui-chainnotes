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
