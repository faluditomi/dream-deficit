## Context

See proposal.md for motivation and specs/chat-client for the behavior contract. Two constraints shape the approach:

1. `DayData` is cloned via a `JsonUtility.ToJson`/`FromJson` round trip when a save slot initializes from its template (`SaveManager.InitializeFromTemplate`), and is written to the JSON save file. Any roster field on `DayData` must survive this round trip, which rules out storing `ScriptableObject` references.
2. `ChatUser` ScriptableObjects are the display/content key: `ChatBubbleController` resolves them via `chat_user/<name lower>` for profile picture and username, and chat logs are addressed `chat_log/<name lower>`. Assets confirm the convention: `chat_user/delilah`, `chat_user/dave`, `chat_user/supervisor`, and `chat_log/supervisor` exist. The ChatClient consumes the `ChatUser` SO directly and derives the ChatLog address from its asset name.

## Goals / Non-Goals

**Goals:**

- Roster authored per day, resolved at runtime into `ChatUser` ScriptableObjects consumed by the ChatClient user list, end to end through the save pipeline.
- Reuse existing conventions (Addressables naming, `ChatBubbleController` resolution pattern) rather than introducing new ones.

**Non-Goals:**

- Reworking how entries open/chat with their chat logs (existing TODOs in `ChatClientUserEntryController`).
- Changing the ChatLog↔ChatUser association mechanism (address naming convention stays).
- Async Addressables or batching the per-entry loads.

## Decisions

### 1. Store roster entries as `ChatUser` asset-name strings on `DayData`

`DayData.activeChatClientUsers` is `List<string>`, each entry the name of a `ChatUser` asset (e.g. `"delilah"`).

- **Why:** strings survive the JsonUtility clone and JSON save round trip; this matches the established pattern of `markerTypeNames`, `activeChatLogs.logName`, and `supervisorBubbleSequenceNames`.
- **Alternative — `List<ChatUser>` (ScriptableObject refs):** rejected; JsonUtility serializes object references as instance IDs that do not survive round trips, so the template-clone step would null them.
- **Alternative — roster on `GameTemplate` globally:** rejected; the load call site is per-day (`GetDayData(CurrentDayNumber)`), and per-day rosters enable "user joins on day N" gameplay.

### 2. `GetActiveChatClientUsers()` returns `List<ChatUser>` (ScriptableObjects)

The resolver maps each stored name to its `ChatUser` asset via `AddressableManager` (`chat_user/<lower>`), preserving roster order, skipping empty/unresolvable entries with a logged error.

- **Why:** the ChatClient consumes the SO directly — `ChatClientUserEntryController.Setup` needs `profilePicture` and `username` — and the controller derives the ChatLog address from the SO's asset name. Resolving here puts asset loading in one place and matches `ChatBubbleController`'s resolution pattern.
- **Alternative — return `List<Constants.ChatUser>` enums:** rejected. It was the initial design, but it forced the controller to resolve the SO anyway for display and re-derive the log address from the enum, adding indirection without benefit.
- **Lookup:** `AddressableManager.Instance.RetrieveAddressable<ChatUser>(Constants.AddressablePrefixes.ChatUser + name.ToLower())`; a null result is logged and skipped.

### 3. Editor section uses `ChatUser` asset picker rows

`GameTemplateEditor` gains a per-day "Active Chat Client Users" section inside `DrawSelectedDay`, structured like `DrawChatLogSection`: an `ObjectField` over `ChatUser` per row (stores `asset.name`), a per-row remove button, and a "+ Add Chat User" button, with `AutoSave()` on every change.

- **Why:** the roster's catalog is `ChatUser` assets, so picking the asset directly is the most faithful editing experience; `FindAssetByName<ChatUser>` is an existing shared helper.
- **Alternative — `EnumPopup` over `Constants.ChatUser`:** rejected with the SO-return design; the popup's catalog would be the enum, but the roster stores assets, so a picked enum value could name a `ChatUser` asset that does not exist.
- **Alternative — name popup over static fields (marker types pattern):** rejected; there is no static registry for chat users (unlike `Markers`), so an asset picker is the natural fit.

### 4. Controller resolves the ChatLog per entry

`ChatClientController.LoadFromDayData` iterates the resolved `ChatUser` SOs; for each it retrieves the `ChatLog` via `chat_log/<asset name lower>` through `AddressableManager` and calls the existing `ChatClientUserEntryController.Setup`.

- **Why:** mirrors `ChatBubbleController`'s convention; keeps `Setup`'s signature unchanged.
- Missing ChatLogs are skipped with an error log so one missing log cannot break the whole list.

## Risks / Trade-offs

- [New `ChatUser` asset added without a matching `chat_log/` Addressable] → controller logs an error and skips the entry until the log exists (currently true for delilah/dave).
- [`ChatUser` asset renamed after being added to a roster] → stored name no longer resolves; the entry is skipped with an error at load; re-picking the asset in the editor fixes it.
- [Legacy saves/templates lack the roster field] → JsonUtility deserializes it as an empty list; the empty-roster path must be exercised in verification.
- [Synchronous Addressables calls, 1–2 per entry] → roster is tiny (currently 3 users); acceptable, consistent with the existing sync-loading TODO.
- [Display order = roster order; "supervisor first" is not addressed] → intentionally deferred to the existing `ChatClientController` TODO; the spec only requires roster-order fidelity.

## Migration Plan

Additive only: new field with default empty list, no save-format break. Rollback is removing the field, resolver, editor section, and reverting the controller loop.
