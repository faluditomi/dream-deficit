## Why

The ChatClient window (`ChatClientController`) is meant to list the chat users the player can converse with on a given day, but its `LoadFromDayData()` already calls `DayData.GetActiveChatClientUsers()` — a method and backing list that don't exist yet. Without it the roster cannot be configured, and the controller code doesn't compile.

## What Changes

- Add a per-day `activeChatClientUsers` list to `DayData`, storing `ChatUser` asset names as strings so the field survives `JsonUtility` round-trips in the save pipeline.
- Add `DayData.GetActiveChatClientUsers()` returning `List<ChatUser>` (the ScriptableObjects), resolving the stored names through Addressables via the `chat_user/<name>` convention.
- Add a per-day "Active Chat Client Users" section to the Game Template Editor with add/remove rows, editing the list via a `ChatUser` asset picker per row.
- Update `ChatClientController.LoadFromDayData()` to iterate the resolved `ChatUser` ScriptableObjects and resolve each user's `ChatLog` through Addressables by the established naming convention (`chat_log/<name lower>`).

Non-goals:
- Entry click-to-open chat log behavior (existing TODO in `ChatClientUserEntryController`)
- Live last-message updates and notification counters (existing TODOs)
- Supervisor list ordering / default-open log
- An explicit ChatLog↔ChatUser reference; the address naming convention is kept as-is

## Capabilities

### New Capabilities

- `chat-client`: Per-day roster of active chat client users, how it is authored in the Game Template Editor, and how it is resolved at runtime into the ChatClient window's user list.

### Modified Capabilities

(none)

## Impact

- `Assets/Scripts/Plain Old/DayData.cs` — new serializable field + resolver (save-format additive: old saves deserialize with an empty list, old saves keep working)
- `Assets/Scripts/MonoBehaviours/Component Controllers/ChatClientController.cs` — loop rewritten to consume enums and resolve assets per user
- `Assets/Scripts/Editor/GameTemplateEditor.cs` — new per-day editor section
- Save pipeline: new field flows through `SaveManager` template init (JsonUtility clone), JSON save file, and slot day entries without format changes
- Content constraint: each listed user must have a `chat_user/<name>` Addressable (delilah, dave, supervisor exist) and a matching `chat_log/<name>` Addressable for the ChatClient to render its last message (supervisor exists; delilah/dave logs are not authored yet — such entries are skipped with an error until they are)
