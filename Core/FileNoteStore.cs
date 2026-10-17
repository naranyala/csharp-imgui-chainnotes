using System.Text;
using System.Text.Json;

namespace ChainNotes.Core;

/// <summary>
/// Stores one Markdown file per note plus a <c>settings.json</c>, all in a
/// single directory. Each note is a plain <c>.md</c> file with a small
/// frontmatter header, so the files stay readable and editable in any
/// editor (principle L1#1 — "files the user owns").
///
/// File layout:
/// <code>
/// ---
/// title: My Note
/// ---
/// body text…
/// </code>
///
/// Writes are atomic (temp file + rename) and keep a single <c>.bak</c> of
/// the previous contents so a crash mid-save cannot corrupt the store.
/// </summary>
public sealed class FileNoteStore : INoteStore
{
    private const string SettingsFileName = "settings.json";
    private const string Extension = ".md";
    private const string FrontMatterDelimiter = "---";

    private static readonly JsonSerializerOptions SettingsJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _directory;

    public FileNoteStore(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
    }

    /// <summary>
    /// Default store location: the per-user local data folder
    /// (<c>~/.local/share/ChainNotes/notes</c> on Linux,
    /// <c>%LOCALAPPDATA%</c> on Windows, <c>~/Library/Application Support</c>
    /// on macOS).
    /// </summary>
    public static string DefaultDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChainNotes",
            "notes");

    public IReadOnlyList<Note> Load()
    {
        if (!Directory.Exists(_directory))
            return Array.Empty<Note>();

        var notes = new List<Note>();

        foreach (var path in Directory.EnumerateFiles(_directory, "*" + Extension))
        {
            // Guard against the Windows 3-char-extension glob quirk where
            // "*.md" can also match e.g. "note.md.tmp".
            if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
                continue;

            var id = Path.GetFileNameWithoutExtension(path);
            if (!NoteId.IsSafe(id))
                continue;

            try
            {
                notes.Add(ParseFile(path, id));
            }
            catch (IOException)
            {
                // A single unreadable file should not take down the whole store.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        // Stable, deterministic order so the sidebar doesn't reshuffle.
        notes.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase));
        return notes;
    }

    public void Save(Note note)
    {
        ArgumentNullException.ThrowIfNull(note);
        if (!NoteId.IsSafe(note.Id))
            throw new ArgumentException($"Note id '{note.Id}' is not a safe file name.", nameof(note));

        var path = PathFor(note.Id);
        WriteAtomic(path, Serialize(note));
    }

    public void SaveAll(IEnumerable<Note> notes)
    {
        ArgumentNullException.ThrowIfNull(notes);
        foreach (var note in notes)
            Save(note);
    }

    public void Delete(string noteId)
    {
        if (!NoteId.IsSafe(noteId))
            return;

        var path = PathFor(noteId);
        if (!File.Exists(path))
            return;

        File.Delete(path);

        // Best-effort cleanup of this note's leftovers.
        TryDelete(path + ".tmp");
        TryDelete(path + ".bak");
    }

    public NoteSettings LoadSettings()
    {
        var path = Path.Combine(_directory, SettingsFileName);
        try
        {
            if (!File.Exists(path))
                return new NoteSettings();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<NoteSettings>(json, SettingsJsonOptions) ?? new NoteSettings();
        }
        catch (IOException)
        {
            return new NoteSettings();
        }
        catch (JsonException)
        {
            // Corrupt settings must never block startup.
            return new NoteSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new NoteSettings();
        }
    }

    public void SaveSettings(NoteSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var path = Path.Combine(_directory, SettingsFileName);
        var json = JsonSerializer.Serialize(settings, SettingsJsonOptions);
        WriteAtomic(path, json);
    }

    // ------------------------------------------------------------ serialization

    private static string Serialize(Note note)
    {
        // Titles are single-line by construction; collapse any stray newline so
        // the frontmatter header can never be broken by a title.
        var title = note.Title.Replace("\r", " ").Replace("\n", " ").Trim();
        if (title.Length == 0)
            title = Humanize(note.Id);

        var sb = new StringBuilder(note.Body.Length + title.Length + 24);
        sb.Append(FrontMatterDelimiter).Append('\n');
        sb.Append("title: ").Append(title).Append('\n');
        sb.Append(FrontMatterDelimiter).Append('\n');
        sb.Append(note.Body);
        return sb.ToString();
    }

    private static Note ParseFile(string path, string id)
    {
        var text = File.ReadAllText(path);

        if (TryParseFrontMatter(text, out var title, out var body))
        {
            // Header present but title missing/blank: derive a readable title.
            if (string.IsNullOrWhiteSpace(title))
                title = Humanize(id);

            return new Note(id, title, body);
        }

        // A hand-written .md with no frontmatter: derive a readable title
        // from the id and keep every byte as the body.
        return new Note(id, Humanize(id), text);
    }

    /// <summary>
    /// Parses a leading <c>---</c> header block. Returns false only when the
    /// opening delimiter is absent; a header without a title key still counts
    /// (the body is whatever follows the closing delimiter).
    /// </summary>
    private static bool TryParseFrontMatter(string text, out string title, out string body)
    {
        title = string.Empty;
        body = string.Empty;

        using var reader = new StringReader(text);

        if (!string.Equals(reader.ReadLine()?.TrimEnd('\r'), FrontMatterDelimiter, StringComparison.Ordinal))
            return false;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.Equals(line.TrimEnd('\r'), FrontMatterDelimiter, StringComparison.Ordinal))
                break;

            const string TitleKey = "title:";
            if (line.StartsWith(TitleKey, StringComparison.OrdinalIgnoreCase))
            {
                var value = line[TitleKey.Length..];
                if (value.StartsWith(' '))
                    value = value[1..];
                title = value;
            }
        }

        // Whatever remains after the closing delimiter is the body verbatim.
        body = reader.ReadToEnd();
        return true;
    }

    private static string Humanize(string id) =>
        string.Join(' ', id.Split('-', StringSplitOptions.RemoveEmptyEntries));

    // ------------------------------------------------------------- file helpers

    private string PathFor(string id)
    {
        if (!NoteId.IsSafe(id))
            throw new ArgumentException($"Note id '{id}' is not a safe file name.", nameof(id));

        return Path.Combine(_directory, id + Extension);
    }

    /// <summary>
    /// Writes via temp file then renames over the target so a reader (or a
    /// crash) never observes a half-written file. Keeps one <c>.bak</c> of the
    /// previous contents.
    /// </summary>
    private void WriteAtomic(string path, string content)
    {
        Directory.CreateDirectory(_directory);

        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content);

        if (File.Exists(path))
        {
            try
            {
                File.Copy(path, path + ".bak", overwrite: true);
            }
            catch (IOException)
            {
                // A failed backup must not block the save itself.
            }
        }

        File.Move(tmp, path, overwrite: true);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
