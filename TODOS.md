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
- [ ] `[E1/G1]` `NoteStore` abstraction behind today's in-memory
      `List<Note>` in `UI/MainWindow.cs`; UI depends only on the interface
- [ ] `[E1/G1]` Plain-file persistence: load notes at startup, save on change
      (debounced) + on exit; pick an on-disk format (single JSON manifest vs.
      one `.md` file per note) and record the choice in `docs/`
- [ ] `[E1/G1]` Safe writes: write to a temp file + atomic rename so a crash
      mid-save can't corrupt the store; keep one `.bak` of the previous state
- [ ] `[E1/G1]` Persist selection + last-opened note across restarts (only
      layout is saved today, via `imgui.ini`)
- [ ] `[E2/G2]` Remove the 16 KB editor cap (`InputTextMultiline(..., 16 *
      1024, ...)`) — size the buffer to the note or stream from the store
- [ ] `[E6/G6]` Test project (`dotnet new xunit`) with the first contract
      tests: store save→load round-trip, atomic-write failure path, editor
      commit-on-change

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
- [ ] `[E6/G6]` CI on push (GitHub Actions or equivalent): `dotnet build`
      + `dotnet test`, warnings as errors

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
