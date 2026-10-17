# TODOS — ChainNotes

Parent intent for every item: `PYRAMID-OF-INTENTS.md`. Tags `[Ex/Gx]` point at
the epic and goal each task serves. Phase order is intentional — finish P0
before starting P1.

Legend: `[ ]` open · `[x]` done · `[~]` in progress / blocked (note why).

## Status vs vision (snapshot)

| Goal | Status | Evidence / biggest gap |
|------|--------|------------------------|
| G1 Notes survive a restart | **Done** | `.md` store + selection restore; full store test suite |
| G2 Editing that stays out of the way | **Partial** | Cap + debounced save + **clipboard + save indicator**; no undo, title edit, empty state |
| G3 Findable note set | **Not started** | Flat `List<Note>`; no search, no grouping |
| G4 Trustworthy backend | **Partial** | Solid GL renderer + key/mouse map + **cursors + OS clipboard**; gap: HiDPI runtime, non-Linux CI, renderer nits |
| G5 A UI worth looking at | **Partial** | **Docking, shortcuts, status bar landed**; theme still dark-only |
| G6 Quality gates green | **Nearly done** | Build clean, 99 tests, CI ubuntu + `--warnaserror`; gap: OS matrix |
| G7 Installable by a stranger | **Not started** | No LICENSE/icon/packaging/versioning — deliberately gated on LICENSE decision |

Principles audit: all six L1 principles held (backend stays generic; UI talks
only to `INoteStore`; no network code). Only thin spot: principle 6's "proven
by clean build on every target" — CI covers Linux only until the OS matrix lands.

## P0 — Make it real (notes that survive a restart)

- [x] `[E3,E4/G4,G5]` Starter scaffold: .NET 10 window + OpenGL 3.3 +
      `ImGuiController` backend (renderer + input) + dockspace host + notes
      shell UI (`App/`, `Core/`, `UI/`)
- [x] `[E4/G4]` Hand-written ImGui ↔ Silk.NET backend: GL 3.3 renderer with
      state backup/restore, font atlas at DPI-aware size, full keyboard/mouse
      mapping (`Core/ImGuiController.cs`); native GLFW/cimgui resolved via
      `CopyNativeLibsToOutput` MSBuild target so the app launches on distros
      where Silk's RID-graph lookup fails
- [x] `[E6/G6]` Docs set + build verified: `README.md` + `docs/` (getting
      started, architecture, backend, building UI, troubleshooting); 0 build
      errors/warnings; app smoke-tested to a clean run
- [x] `[E1/G1]` `NoteStore` abstraction: `Core/INoteStore.cs` behind the former
      in-memory `List<Note>`; UI takes the interface via constructor injection
      from `Application.OnLoad` and has no path/format knowledge
- [x] `[E1/G1]` Plain-file persistence: **one `.md` file per note** with a
      frontmatter header (chosen over a single JSON manifest; rationale +
      format rules recorded in `docs/persistence.md`); notes load at startup,
      edits save debounced (0.4 s) + flushed on note switch / quit / exit
- [x] `[E1/G1]` Safe writes: temp file + atomic rename
      (`FileNoteStore.WriteAtomic`) so a crash mid-save can't corrupt the
      store; one `.bak` of the previous state per note
- [x] `[E1/G1]` Persist selection + last-opened note via `settings.json`
      (layout still saved separately by `imgui.ini`)
- [x] `[E2/G2]` Remove the 16 KB editor cap — `InputTextMultiline` buffer is
      now sized to the note (64 KB floor + 8 KB UTF-8 slack)
- [x] `[E6/G6]` Test project (`tests/ChainNotes.Tests`, xunit, 95 tests):
      id slug/collision/length-cap contract, editor buffer sizing (UTF-8),
      selection restore, seed notes, store save→load round-trip (CRLF, emoji,
      1 MB, BOM, Windows headers), `.bak`/temp/unsafe-file handling,
      frontmatter-less files, settings fallback + forward-compat, delete

## P1 — Make it trustworthy and pleasant

### E2 — editing experience (G2)

- [x] `[E2/G2]` Clipboard interop: platform layer in `ImGuiController` sets
      `ImGuiPlatformIO.Platform_Get/SetClipboardTextFn` to
      `IKeyboard.ClipboardText`; `ClipboardInteropTests` proves the mechanism
      against real libcimgui headlessly
- [x] `[E2/G2]` Autosave indicator: save state shown in the status bar
      (`Unsaved changes — saving…` / `All changes saved`) from the existing
      `_pendingSave` debounce state
- [ ] `[E2/G2]` Undo/redo for the editor — design note: this ImGui binding has
      **no built-in InputText undo**; needs a note-level snapshot stack on
      debounce flush, with Ctrl+Z left to the OS/text input where possible
- [ ] `[E2/G2]` Editable note title — today the title is read-only text above
      the editor; rename must keep the id/filename stable (`Note.Id` never
      changes on title edit)
- [ ] `[E2/G2]` Friendly empty state — replace the `_notes.Count > 1` delete
      guard with a real "create your first note" screen when the store is
      empty (must guard `DrawEditor`, which indexes `_notes[_selectedNote]`)

### E3 — UI shell (G5)

- [x] `[E3/G5]` True docking for the panels: dropped `NoDocking` + `NoMove` +
      `NoResize`, first placement via `ImGuiCond.FirstUseEver` (after a
      one-frame warm-up so `WorkPos` accounts for the menu bar), real window
      titles (editor id stays stable via `{title}##editor`); layout persists
      via `imgui.ini`. No DockBuilder API in this binding — first run keeps
      today's regions until the user docks them
- [x] `[E3/G5]` Keyboard shortcuts: Ctrl+N new, Ctrl+S flush save, Ctrl+Q quit
      via `IsKeyChordPressed` during `Draw`; menu labels carry the real
      shortcuts (File menu also gained a Save item)
- [x] `[E3/G5]` Status bar replacing the ad-hoc stats panel: bottom bar with
      save state (autosave indicator), note count, FPS, ImGui version
- [ ] `[E3/G5]` Theme selection (dark/light/classic) persisted in settings;
      move app theming out of the generic `ImGuiController.ApplyStyle`

### E4 — backend + platform hardening (G4)

- [x] `[E4/G4]` Mouse cursor support: `ImGuiBackendFlags.HasMouseCursors` set
      and `ImGui.GetMouseCursor()` translated to `ICursor.StandardCursor`
      (diffed per frame, `Hidden` on None/software draw)
- [ ] `[E4/G4]` HiDPI runtime pass: font scale is computed once at startup —
      detect monitor DPI change and rebuild the font atlas; verify per OS
- [ ] `[E4/G4]` Backend nits: texture id narrowed via `(int)` cast and
      `ushort` index assumption in the renderer — widen/verify against 64-bit
      and 32-bit-index builds
- [x] `[E6/G6]` CI on push (GitHub Actions): `.github/workflows/ci.yml` —
      restore + `dotnet build` + `dotnet test` on ubuntu-latest, warnings as
      errors, on push to `main` and on PRs
- [ ] `[E4,G6/G4]` CI OS matrix: run build+test on ubuntu/windows/macos —
      closes principle 6's "clean build on every target" gap and backs the
      "runs identically on every desktop OS" vision claim

### E5 — search + organization (G3)

- [ ] `[E5/G3]` In-note search + a global search box over titles/bodies,
      results as a panel
- [ ] `[E5/G3]` Light grouping model (tags **or** folders — pick one) with
      persistence, replacing the single flat list once it grows

## P2 — Shippable

- [ ] `[E7/G7]` LICENSE file — decide **before** importing any third-party
      derived code or accepting contributions; unblocks the whole release track
- [ ] `[E7/G7]` App icon, display name, and `.desktop`/file-association polish
- [ ] `[E7/G7]` Packaging: pick ONE format per OS (Flatpak/AppImage/deb ·
      MSI/MSIX · DMG), automated from CI on tag
- [ ] `[E7/G7]` Versioning scheme (SemVer) + a tagged first release
- [ ] `[E6/G6]` Per-OS smoke checklist (build, launch, HiDPI, input, save/
      reload) recorded in `docs/` before each release claim
- [ ] `[E4/G4]` Gamepad / keyboard navigation input path (ImGui nav) if the
      keyboard-first experience needs it

## Backlog (do NOT promote without a goal)

- Markdown rendering / preview pane — needs a goal first, currently non-goal
  (plain text first)
- Encryption / secure notes — needs a goal first
- Tags **and** folders both — pick one model in E5, not both
- Cloud sync / accounts — explicitly out of scope until vision changes
- Real-time collaboration — explicitly out of scope
- Plugin / extension system — needs a goal first
- Mobile (Android/iOS) or web build — out of scope for a desktop-first
  immediate-mode UI
- File attachments / embedded images — needs a goal first
- Import/export (Markdown folders, other note apps) — capture as G/E first
- Onboarding / first-run tour — needs a goal first
- i18n — needs a goal first
- Multi-viewport (ImGui viewports) — not required by any goal; backend flag
  stays unset until a goal needs OS-level floating windows
- CLI flags (`Application.Run` discards args today) — needs a goal first

## Working agreement

- One task in flight at a time; check `[~]` with date + blocker when paused.
- Every completed task names its `[Ex/Gx]`; pyramid violations get re-scoped,
  not merged.
- Promote at most 2 backlog items per review; retire stale ones.
- The `Core/` backend must not gain app-specific logic — if a change only
  serves ChainNotes, it belongs in `UI/` or a new layer. Generic *platform*
  features (clipboard, cursors) belong in `ImGuiController`.
- Refresh the "Status vs vision" snapshot when a goal's status changes.
