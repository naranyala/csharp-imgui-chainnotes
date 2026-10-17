# TODOS — ChainNotes

Parent intent for every item: `PYRAMID-OF-INTENTS.md`. Tags `[Ex/Gx]` point at
the epic and goal each task serves. Phase order is intentional — finish P0
before starting P1.

Legend: `[ ]` open · `[x]` done · `[~]` in progress / blocked (note why).

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

- [ ] `[E2/G2]` Clipboard interop: wire ImGui's `ImGuiIO.SetClipboardText` /
      `GetClipboardText` to the OS clipboard through Silk.NET so
      cut/copy/paste works in the editor
- [ ] `[E2/G2]` Undo/redo for the editor (ImGui input-text undo or a
      note-level snapshot stack)
- [ ] `[E2/G2]` Autosave indicator: saved/unsaved/pending state visible in the
      status bar; never lose work on quit
- [ ] `[E2/G2]` Friendly empty state — today `Delete` is guarded by
      `_notes.Count > 1`; replace with a real "create your first note" screen
      when the store is empty
- [ ] `[E3/G5]` True docking for the three panels: drop the `NoDocking` +
      fixed-position flags so users can rearrange, split, and tab; layout
      already persists via `imgui.ini`
- [ ] `[E3/G5]` Keyboard shortcuts surfaced in the menu: New, Save, Search,
      Quit (menu currently shows `Alt+F4` text with no handler)
- [ ] `[E3/G5]` Theme selection (dark/light/classic) persisted in settings;
      move app theming out of the generic `ImGuiController.ApplyStyle`
- [ ] `[E4/G4]` Mouse cursor support: honor `ImGui.GetMouseCursor()` (text
      input, resize edges, pointing hand) — the backend currently omits
      `ImGuiBackendFlags.HasMouseCursors`
- [ ] `[E4/G4]` HiDPI verification pass per OS: confirm font scale + framebuffer
      scale on 2× displays; fix any widget that hard-codes pixels
- [ ] `[E5/G3]` In-note search + a global search box over titles/bodies,
      results as a panel
- [ ] `[E5/G3]` Light grouping model (tags **or** folders — pick one) with
      persistence, replacing the single flat list once it grows
- [x] `[E6/G6]` CI on push (GitHub Actions): `.github/workflows/ci.yml` —
      restore + `dotnet build` + `dotnet test` on ubuntu-latest, warnings as
      errors, on push to `main` and on PRs

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
- [ ] `[E3/G5]` Status bar replacing the ad-hoc stats panel (note count, saved
      state, search entry point)

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

## Working agreement

- One task in flight at a time; check `[~]` with date + blocker when paused.
- Every completed task names its `[Ex/Gx]`; pyramid violations get re-scoped,
  not merged.
- Promote at most 2 backlog items per review; retire stale ones.
- The `Core/` backend must not gain app-specific logic — if a change only
  serves ChainNotes, it belongs in `UI/` or a new layer.
