# Pyramid of Intents — ChainNotes

How to read this file: top = stable, long-lived intent. Bottom = concrete,
changeable work. Higher levels constrain lower levels — a TODO that conflicts
with a level above it is wrong and must be re-scoped, not the other way round.

```
            L0 VISION (why this exists)
            L1 MISSION + PRINCIPLES (how we behave)
            L2 STRATEGIC GOALS (what winning looks like)
            L3 EPICS / CAPABILITIES (system must do X)
            L4 TASKS (work items) → see TODOS.md
```

---

## L0 — Vision

A fast, local-first desktop notes app that a user can trust with their writing:
their notes are plain files they own, the UI stays out of the way, and it runs
identically on every desktop OS.

- Tier 1 now: **Linux + Windows + macOS** (Silk.NET/GLFW already targets all
  three from one codebase — no per-OS forks).
- The project doubles as a **reusable ImGui desktop starter**: the backend
  (`Core/ImGuiController.cs`) stays generic and app-agnostic so it can seed
  other apps.
- Mobile / web explicitly out of scope (this is a desktop-first immediate-mode
  UI; touch is a later consideration, not a driver).

## L1 — Mission + Principles

Mission: grow the working scaffold into a real notes app that **persists
reliably and edits comfortably**, then deepen organization and polish without
abandoning immediate-mode simplicity or the portable backend.

Principles (in priority order):

1. **Files the user owns.** Notes live as plain text/markdown on disk in a
   folder the user chooses. No opaque database, no proprietary blob, no lock-in
   — the files outlive the app.
2. **UI never touches disk directly.** Widgets talk to a note store; the store
   owns serialization and IO. Panels are replaceable, persistence is not.
3. **The backend is infrastructure, not app logic.** `ImGuiController` renders
   and forwards input — nothing about ChainNotes leaks into it. Keep it generic
   so it seeds the next starter.
4. **Reference before invention.** Reuse proven ImGui idioms (docking host,
   immediate-mode state pattern) and proven .NET serialization instead of
   designing new ones.
5. **Honest, offline-by-default.** No telemetry, no accounts, no network calls
   unless the user explicitly asks for a feature that needs them.
6. **Test the contract.** Note-store round-trips and editor state transitions
   get tests; the renderer's correctness is proven by a clean build on every
   target, not by hope.

## L2 — Strategic goals

| ID | Goal | Done when |
|----|------|-----------|
| G1 | Notes survive a restart | Create/edit/delete → quit → relaunch restores every note and selection from a real file on disk |
| G2 | Editing that stays out of the way | Comfortable text editing: no artificial length cap, clipboard cut/copy/paste, undo, autosave without losing work |
| G3 | A growing note set stays findable | Search by title/body + some organization (folders/tags/star) once the list outgrows a single pane |
| G4 | Trustworthy backend | Renderer + input correct across Linux/Windows/macOS, HiDPI, and window resize/minimize; no state leaks into app rendering |
| G5 | A UI worth looking at | Theming, keyboard shortcuts, and a docking layout the user can rearrange and persist |
| G6 | Quality gates green | `dotnet build` clean, tests green, CI on push; regressions caught before merge |
| G7 | Installable by a stranger | LICENSE decided, app icon/name, single-format packaging per OS, versioned release a first-time user can install |

Non-goals (explicit): cloud sync / accounts / subscriptions, real-time
collaboration, mobile or web apps, a plugin marketplace, full WYSIWYG rich-text
(house style is plain text / markdown first), and any analytics or telemetry.

## L3 — Epics / Capabilities

- **E1 Notes domain + persistence** — `NoteStore` abstraction behind the
  current in-memory `List<Note>`; plain-file serialization (JSON or one-file-
  per-note markdown); load at startup, save on change (debounced) + on exit;
  safe write (temp file + rename); settings (last folder, window/layout) beyond
  the layout `imgui.ini` already stores.
- **E2 Editing experience** — drop the 16 KB `InputTextMultiline` cap;
  clipboard (cut/copy/paste) and undo wired to ImGui's input flags; autosave
  with a visible saved/unsaved indicator; selection restored across reload;
  guard the "never delete the last note" rule behind a friendlier empty state.
- **E3 UI shell** — make the three panels genuinely dockable (drop the
  current `NoDocking` pins so users can rearrange); keyboard shortcuts
  (new/save/search/quit) surfaced in the menu; theme selection; a status bar
  replacing the ad-hoc stats panel.
- **E4 Backend + platform hardening** — keep `ImGuiController` generic while
  closing its gaps: mouse cursors, gamepad/nav input, clipboard interop,
  correct DPI/font scaling on HiDPI, and verifying the GL state backup/restore
  against app-owned rendering on each OS.
- **E5 Search + organization** — in-note search, a global search box over
  titles/bodies, and a lightweight grouping model (tags or folders) with
  results as a first-class panel.
- **E6 Quality gates** — a test project covering the store round-trip and
  editor state; `dotnet build` + `dotnet test` in CI on push; no warnings as
  errors; a manual smoke checklist per OS before release.
- **E7 Release + distribution** — LICENSE decided **before** importing any
  third-party derived code; app icon + display name; one packaging format per
  OS (Flatpak/AppImage/deb on Linux, MSI/MSIX on Windows, DMG on macOS);
  versioning scheme; a README install section a stranger can follow.

## L4 — Tasks

Lives in `TODOS.md`. Every task carries its epic + goal tags (e.g.
`[E1/G1]`). If a task has no parent goal, either the pyramid is incomplete or
the task doesn't belong — resolve before coding.
