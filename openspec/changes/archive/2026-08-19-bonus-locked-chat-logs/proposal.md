## Why

The assignment docket currently treats every chat log in a day identically — required content with no distinction. The design calls for **bonus** chat logs that appear locked (covered by a lock panel) and are revealed as a free reward when the player clicks them. This establishes the authoring flag and persisted unlock state that the future scoring system will build on (required logs vs bonus logs).

## What Changes

- **DayData** gains a per-log entry struct (`name`, `isBonus`) replacing the flat `activeChatLogNames` string list, plus a runtime-only `unlockedChatLogNames` set that is never seeded from the template.
- **GameTemplate Editor** shows a "Bonus" toggle on each active chat log row; toggling is saved to the template.
- **AssignmentEntry** (the docket row) shows the existing Lock panel + unlock animation for bonus logs that are still locked; clicking a locked entry unlocks it (free, no gate), records the unlock on the day's data (persisted at day end), then auto-opens the chat log window.
- A new accessor (`GetActiveChatLogEntries()`) exposes per-log flags alongside resolved `ChatLog`s; existing `GetActiveChatLogs()` consumers (`MarkerManager`) are unaffected.
- Chat windows are instantiated for every log at day start (closed). Locked logs keep their window closed until unlocked; the locked-log guards suppress notifications and sequenced content.
- If a sequenced message targets a chat log that is still locked, the sequence is a no-op (early return, no window created, no notification).
- **BREAKING:** Existing GameTemplate assets and save files must be re-authored / started fresh — the old `activeChatLogNames` field is removed, not migrated.

## Capabilities

### New Capabilities

- `assignment-docket`: Behavior of the day's assignment list — which chat logs are required vs bonus, how locked bonus entries behave on click (unlock → auto-open), and how unlock state persists across saves. Covers locked-log sequence suppression.

### Modified Capabilities

- None (no existing specs).

## Impact

- **`Assets/Scripts/Plain Old/DayData.cs`** — `activeChatLogNames` → `List<ChatLogEntry>` (name + isBonus), add `unlockedChatLogNames`, add `GetActiveChatLogEntries()`, add `IsLocked(name)` helper.
- **`Assets/Scripts/Plain Old/DayData.cs`** consumers — `AssignmentDocketController` uses the new accessor; `MarkerManager` keeps `GetActiveChatLogs()`.
- **`Assets/Scripts/Editor/GameTemplateEditor.cs`** — chat log row gets a Bonus toggle.
- **`Assets/Scripts/MonoBehaviours/Component Controllers/AssignmentEntryController.cs`** — Setup receives flags; shows the Lock panel for locked entries; triggers the unlock animation; opens the pre-instantiated window on unlock.
- **`Assets/Scripts/MonoBehaviours/Managers/ChatLogManager.cs` / `SaveManager.cs`** — locked-log sequence path returns early; DayData is JSON-serialized as-is (new fields must stay JsonUtility-serializable).
- **`Assets/Prefabs/AssignmentEntry.prefab`** — untouched (Lock panel + animation already exist).
- **Existing GameTemplate assets** — lose their chat-log assignments until re-authored in the Game Template Editor (no migration).
- **Existing save files** — incompatible; start a new game after this change.

**Out of scope (non-goals):**
- No scoring changes — `isBonus`/`isUnlocked` are stored but not consumed by scoring yet.
- No earned-unlock mechanics (no accuracy gates, no unlock conditions).
- No queued delivery of sequenced messages after unlock — suppressed messages are dropped.
- No changes to `ChatLog` asset or the prefab/serialization files.
- No migration of existing GameTemplate assets or save files.
