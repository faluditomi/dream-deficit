## 1. Plain Old — DayData refactor

- [x] 1.1 Add `[System.Serializable] public class ChatLogEntry` (Plain Old) with `logName` and `isBonus` fields — must stay JsonUtility-serializable
- [x] 1.2 Replace `DayData.activeChatLogNames` with `activeChatLogs: List<ChatLogEntry>` (default-initialized, null-guarded in accessors)
- [x] 1.3 Add runtime-only `DayData.unlockedChatLogNames: List<string>` — never seeded from template
- [x] 1.4 Add `DayData.GetActiveChatLogEntries()` returning resolved entries (ChatLog, isBonus, isUnlocked) via AddressableManager
- [x] 1.5 Reimplement `DayData.GetActiveChatLogs()` on top of `GetActiveChatLogEntries()` so existing consumers (MarkerManager) keep working
- [x] 1.6 Add unlock-state helpers on DayData (e.g. `IsLogUnlocked(name)`, `UnlockLog(name)` that appends to `unlockedChatLogNames`) — serialization-safe
- [x] 1.7 Add `DayData.IsLocked(name)` helper — returns `isBonus && !unlockedChatLogNames.Contains(name)`

## 2. Managers — ChatLogManager / SaveManager

- [x] 2.1 Remove button wiring from `ChatLogManager.InstantiateChatLog` (drop the `wireButton` parameter) — callers own click behavior; `SupervisorController` wires its own open listener; verify cached-controller path doesn't double-wire
- [x] 2.2 Add early-return in `ChatLogController.RunBubbleSequence` (or equivalent entry point) — if the target chat log is locked, return immediately without showing a badge, delivering, or queueing the message
- [x] 2.3 Verify `SaveManager.SaveDay` persists `unlockedChatLogNames` unchanged (no SaveManager structural change expected — DayData serializes as-is; unlock saves at day end)

## 3. Component Controllers — AssignmentEntry / AssignmentDocket

- [x] 3.1 Extend `AssignmentEntryController.Setup` to receive `(ChatLog, bool isBonus, bool isUnlocked)`; store the resolved ChatLog for later use
- [x] 3.2 Locked state: show Lock panel covering the name label; the chat window is still instantiated (closed) — content suppressed by the locked-log guards
- [x] 3.3 Unlock flow: button click on locked entry → play Lock-panel reveal animation (or hide panel) → `UnlockLog(name)` on the runtime day data (no save — happens at day end) → `Open()` the already-instantiated window (auto-open, single click)
- [x] 3.4 Unlocked/required click: open the chat log window directly without animation or save
- [x] 3.5 Update `AssignmentDocketController.LoadFromDayData` to iterate `GetActiveChatLogEntries()` and pass flags through to each entry

## 4. Editor — GameTemplateEditor

- [x] 4.1 Rework `DrawChatLogSection` rows: `ObjectField` (name round-trip preserved) + "Bonus" checkbox bound to `ChatLogEntry.isBonus`, writing into `activeChatLogs[i]`
- [x] 4.2 Add-row appends a default `ChatLogEntry { logName = "", isBonus = false }`; remove-row removes the whole entry; `AutoSave()` persists both
- [x] 4.3 Null-guard `activeChatLogs` in editor draw paths

## 5. Verification

- [x] 5.1 Play-mode check: required log shows normally and opens on click
- [x] 5.2 Play-mode check: locked bonus log shows lock covering data; single click reveals, animates, persists, and auto-opens
- [x] 5.3 Play-mode check: unlocked bonus log (after save + reload) opens directly with no re-lock
- [x] 5.4 Play-mode check: no notification badge or content leaks from a locked log receiving sequenced messages
- [x] 5.5 Play-mode check: sequence targeting a locked log returns immediately without creating a window or badge
- [x] 5.6 Editor check: Bonus toggle persists to the GameTemplate asset across editor sessions
