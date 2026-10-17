# Persistence

How ChainNotes stores notes on disk: plain Markdown files, atomic writes, and a small JSON settings file. The UI never touches the filesystem directly — everything goes through `INoteStore`.

## The contract

```csharp
public interface INoteStore
{
    IReadOnlyList<Note> Load();
    void Save(Note note);
    void SaveAll(IEnumerable<Note> notes);
    void Delete(string noteId);
    NoteSettings LoadSettings();
    void SaveSettings(NoteSettings settings);
}
```

`UI/MainWindow.cs` receives an `INoteStore` through its constructor and knows nothing about paths, formats, or encodings. `App/Application.cs` chooses the implementation (`FileNoteStore`) at startup. Swapping the backend — or handing tests an in-memory fake — is a one-line change at the composition root.

## Format choice: one `.md` file per note

Alternatives considered when this was built:

| Option | Verdict |
| --- | --- |
| Single JSON manifest | Rejected — opaque to users and other tools; every save rewrites the whole store; a corrupt byte loses all notes. |
| **One `.md` file per note** | **Chosen** — files the user owns, readable/editable in any editor, one bad file can't take down the rest, natural fit for a future import/export story. |

Each note is stored as `notes/<id>.md`, where `<id>` is a stable slug derived from the title once at creation (`NoteId.Allocate` — `My Note!` → `my-note`, collisions get `-2`, `-3`, …). The id becomes the filename and the identity used for selection persistence; renaming a title later does not move the file.

### File format

```
---
title: Roadmap
---
- [x] Window + OpenGL 3.3 context
- [x] Persistence
- [ ] Themes
```

Rules the parser (`FileNoteStore.ParseFile`) guarantees:

- **Body is verbatim.** Everything after the closing `---` line is the body, byte for byte — a body may itself start with `---`.
- **Title is one line.** Stray newlines in a title are collapsed to spaces so the header can never break.
- **Unknown frontmatter keys are ignored**, so extra metadata can be added later without breaking old files.
- **A `.md` file without frontmatter still loads**: the title is derived from the id (`scratch-note` → `scratch note`) and the entire file becomes the body. Hand-written files just work.
- **Only exact `.md` files load.** `note.md.bak`, `note.md.tmp`, and ids with unsafe characters (spaces, path separators) are skipped, and ids are validated (`NoteId.IsSafe`) before being joined with the directory path — a tampered filename can't escape the store directory.

## Write safety

Every write goes through `FileNoteStore.WriteAtomic`:

1. Write the new contents to `<file>.md.tmp`.
2. Copy the existing `<file>.md` to `<file>.md.bak` (kept as the previous state; a failed backup never blocks the save).
3. `File.Move(tmp, path, overwrite: true)` — an atomic rename on all supported OSes.

A crash at any point leaves either the old file or the new file, never a half-written one. One `.bak` is kept per note; `Delete` removes the note and its leftovers.

## Settings

`settings.json` sits next to the notes and currently holds the last-selected note:

```json
{
  "selectedNoteId": "roadmap"
}
```

Missing or corrupt settings never block startup — they fall back to defaults. The `NoteSettings` type is the home for future persisted preferences (theme, window state).

## Save timing

- **Body edits** are debounced: `MainWindow` marks the note dirty on each `InputTextMultiline` change and flushes after 0.4 s of quiet (`SaveDebounceSeconds`). Typing never writes once per keystroke.
- **The debounce is flushed** when you switch notes, delete, quit from the menu, or the app disposes — nothing typed in the last window is lost.
- **Selection** is saved immediately on every change plus once more on exit.
- **First run** seeds three demo notes via `SaveAll` when the store directory is empty.

## Storage location

`FileNoteStore.DefaultDirectory()` resolves to the per-user local data folder:

| OS | Path |
| --- | --- |
| Linux | `~/.local/share/ChainNotes/notes` |
| Windows | `%LOCALAPPDATA%\ChainNotes\notes` |
| macOS | `~/Library/Application Support/ChainNotes/notes` |

The constructor takes a directory, so tests (and alternative deployments) inject their own.

## Editor buffer sizing

The editor no longer uses a fixed 16 KB `InputTextMultiline` buffer. `DrawEditor` sizes the buffer to the UTF-8 byte length of the text plus 8 KB of slack, with a 64 KB floor — long notes are not truncated, and the buffer grows with the note instead of capping it.

## Changing or testing the store

Implement `INoteStore`, then swap it in `App/Application.cs → OnLoad`:

```csharp
_store = new FileNoteStore(FileNoteStore.DefaultDirectory());
_mainWindow = new MainWindow(_store);
```

Contract tests live in `tests/ChainNotes.Tests` (`dotnet test tests/ChainNotes.Tests`) and cover: slug generation/collision, save→load round-trip (including empty bodies and `---`-leading bodies), `.bak` creation, frontmatter-less files, unsafe/backup/temp file skipping, settings round-trip and corruption fallback, and delete.
