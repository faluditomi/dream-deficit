## Context

See proposal.md — Why. Current state that shapes this design:

- `DayData.activeChatLogNames` is a `List<string>` of name-based refs to `ChatLog` assets, resolved via `AddressableManager` at runtime (`GetActiveChatLogs()`), consumed by `AssignmentDocketController` and `MarkerManager` (2×).
- `DayData` already mixes authoring data and runtime state (`markerData.accuracy` is runtime-scored, sits next to template structure) — so bonus flags and unlock state both belong here, following the house pattern.
- `SaveManager.InitializeFromTemplate()` copies template `DayData` entries into slot/runtime by **reference** (the template's `DayData` object is shared until a save rewrites the slot). Any runtime mutation of a field inside the copied `DayData` also touches the template's in-memory object.
- `ChatLogManager.InstantiateChatLog(chatLog, initialiser)` creates the chat window, caches it, **wires the entry's Button onClick → `Open()`**, and hooks the notification badge. `AssignmentEntryController.Setup()` calls it eagerly.
- The `AssignmentEntry` prefab already has a Lock panel + unlock animation; no prefab/serialization edits are needed.
- Save is JSON via `JsonUtility`; new fields must stay serializable.

## Goals / Non-Goals

**Goals:**
- Store bonus classification as per-day authoring data and unlock state as persisted runtime data, with one source of truth.
- Locked bonus entries conceal data, unlock for free on click, auto-open, and persist.
- Keep `MarkerManager` (and any future scoring) able to read both flags.
- Zero edits to `.prefab` / `.asset` / `.unity` files.
- Drop sequenced messages targeting locked logs (no badge, no queue, no delivered content).

**Non-Goals:**
- No scoring logic — flags are stored/exposed, not consumed.
- No earned-unlock gates or unlock conditions.
- No changes to the `ChatLog` asset shape.
- No migration of existing GameTemplate assets or save files. Existing templates must be re-authored in the Game Template Editor.

## Decisions

### D1 — Authoring struct + runtime-only unlock set (Option 2b)

`DayData` gains:

```csharp
[System.Serializable]
public class ChatLogEntry
{
    public string logName;
    public bool isBonus;          // authoring — set in Game Template Editor
}

public List<ChatLogEntry> activeChatLogs = new List<ChatLogEntry>(); // replaces activeChatLogNames
public List<string> unlockedChatLogNames = new List<string>();       // runtime-only — never seeded from template
```

Locked state is **derived**, never stored: `entry.isBonus && !unlockedChatLogNames.Contains(entry.logName)`.

Rationale over alternatives:

- **Flags on `ChatLog` asset** — bonus-ness is per-run structure, not content; same asset could be required in one template/day and bonus in another.
- **Flags on `AssignmentEntry`** — the row is a view; `LoadFromDayData()` destroys/rebuilds it every day, so state would evaporate.
- **Option 1 (parallel `bonusChatLogNames` list)** — identity would live in two lists that must be manually kept in sync on editor removal; desync is easy and silent.
- **Option 2 (`isUnlocked` inside the struct)** — the struct is copied by reference from template at save init; mutating `isUnlocked` at runtime would also mutate the template's in-memory `DayData` (same aliasing `markerData` already has). Keeping unlock state in a separate runtime-only list reflects that it is a different kind of data (player state vs structure); the deep-copy at template init (D8) is what stops runtime mutations from reaching the template's `DayData`.

### D2 — Accessor split

Add `GetActiveChatLogEntries()` returning `List<ChatLogEntry>` with resolved logs (a small resolved-entry POCO: `ChatLog chatLog`, `bool isBonus`, `bool isUnlocked`). Reimplement `GetActiveChatLogs()` on top of it so `MarkerManager` keeps working unchanged and scoring later has one place to read both flags. `activeChatLogNames`-based lookup for a single log (used by the unlock check) becomes a `Find` over `activeChatLogs`.

### D3 — Locked entries instantiate the window closed (no deferral)

`AssignmentEntryController.Setup` **always** calls `ChatLogManager.InstantiateChatLog`, so every log's chat window exists closed from day start — locked or not. Locked entries simply show the Lock panel covering the entry. Content suppression is enforced by the D7 guards, so the closed window cannot surface content, notifications, or queued messages.

Unlock flow (single click):

1. Entry button click → controller detects `isBonus && !isUnlocked`
2. Play the existing Lock-panel reveal animation (or hide the panel when the animator trigger is not wired)
3. `dayData.UnlockLog(name)` — recorded on the runtime day data only; **the save happens at day end** (`GameManager.EndDay` → `SaveManager.SaveDay`)
4. `Open()` the already-instantiated window — auto-open without a second click

### D4 — Button wiring is the caller's responsibility

`ChatLogManager.InstantiateChatLog` no longer wires the entry button. Each caller owns its click behavior: `AssignmentEntryController` wires `OnEntryClicked` (unlock-or-open branch) in `Setup`, and `SupervisorController` wires its own open listener. This removes the `wireButton` parameter entirely and avoids double listeners.

### D5 — Editor: Bonus toggle per row

`GameTemplateEditor.DrawChatLogSection` reworks each row from a bare `ObjectField` to `ObjectField` + "Bonus" checkbox bound to `entry.isBonus` (the existing name-string round-trip via `FindAssetByName` is preserved, now writing into `activeChatLogs[i].logName`). `AutoSave()` already covers persistence. Add-row appends a default `ChatLogEntry { logName = "", isBonus = false }`.

### D6 — No migration of existing data

`activeChatLogNames` is removed from `DayData`. Existing GameTemplate assets and save files that stored chat-log names in that field are not converted. The Game Template Editor is the only way to re-author a day's chat logs after this change. This is acceptable for the prototype stage (no production saves, no save-slot picker).

### D7 — Sequenced messages targeting locked logs are dropped

If `ChatLogController.RunBubbleSequence` or `SaveManager.GetSequencedChatBubblesForChatLog` is called for a chat log that is still locked (bonus and not yet unlocked), the handler returns immediately without showing a badge, delivering, or queueing the message. The sequence is dropped — it is not delivered after unlock. This is a simple early-return check using `DayData.IsLogLocked(logName)`.

### D8 — Deep-copy DayData at template init

`SaveManager.InitializeFromTemplate` deep-copies each template `DayData` (via a `JsonUtility` round-trip) into the slot and runtime data. This prevents runtime mutations — unlock state, marker accuracy, supervisor sequences — from leaking into the template's in-memory `DayData` and accumulating across sessions (the pre-existing aliasing described in Context).

## Risks / Trade-offs

- **[Existing GameTemplate assets lose their chat-log assignments]** → D6: accepted; re-author in the Game Template Editor.
- **[Locked-log windows exist closed in memory]** → D3: accepted for simplicity; all day windows are instantiated at day load, negligible at prototype scale. Guard rails against content leaks are the D7 guards.
- **[Sequenced bubbles targeting a locked log are dropped]** → D7: intended behavior; messages dropped while locked are not delivered after unlock.
- **[`activeChatLogs` null after loading pre-change data]** → Editor and runtime accessors already null-guard the list.

## Migration Plan

- No scene/prefab edits. Runtime code + `DayData` shape change deploy as a normal script change.
- Existing GameTemplate assets and save files are not migrated. Re-author chat logs in the Game Template Editor and start a new game.
- Rollback: revert the `DayData` shape change and `GameTemplateEditor` row rework.

## Open Questions

- None that affect specs, approach, or task breakdown.
