## 1. Plain Old (data model)

- [x] 1.1 Add `activeChatClientUsers` (`List<string>`, enum names) to `DayData` with an inline initializer — serialization-safe: plain strings only, must survive `JsonUtility` round trip and default to empty for legacy saves
- [x] 1.2 Add `DayData.GetActiveChatClientUsers()` returning `List<ChatUser>` (ScriptableObjects): resolve stored names via `chat_user/<name>` through `AddressableManager`, preserving roster order, skipping and logging errors for empty/unresolvable entries

## 2. Component Controllers

- [x] 2.1 Update `ChatClientController.LoadFromDayData` to iterate the resolved `ChatUser` ScriptableObjects: per user, resolve the `ChatLog` via `chat_log/<user name lower>` through `AddressableManager`, skipping and logging errors on missing logs, then call the existing `ChatClientUserEntryController.Setup`

## 3. Editor

- [x] 3.1 Add a per-day "Active Chat Client Users" section to `GameTemplateEditor.DrawSelectedDay` modeled on `DrawChatLogSection`: `ObjectField` picking a `ChatUser` asset per row (stored as asset name), per-row remove button, "+ Add Chat User" button, `AutoSave()` on every change

## 4. Verification

- [x] 4.1 In the Game Template Editor, author rosters on the tutorial template's days (via the editor window only — no manual `.asset` edits) and confirm changes persist after reopening the window
- [x] 4.2 Play Mode: load a day and confirm the ChatClient window shows exactly one entry per roster user in roster order, with profile picture, username, and last message populated
- [x] 4.3 Play Mode: verify an empty roster shows no entries without errors, a legacy save (field absent) loads with an empty roster, and an unknown/missing asset entry is skipped with a logged error while the rest render
