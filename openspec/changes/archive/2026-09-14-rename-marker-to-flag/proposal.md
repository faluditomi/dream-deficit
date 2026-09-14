## Why

The gameplay's core annotation concept is called "marker" in code, but the player-facing UI has already drifted to "flag" (the cheat-sheet window is titled "Flag Cheat Sheet"). The term "flag" is also currently overloaded: it names the on-screen indicator element *and* conversation narrative state. Renaming now gives every concept a single unambiguous name and aligns the codebase with the player-facing vocabulary before more content and systems are authored on top of the old terms.

## What Changes

- **BREAKING** (serialized data): rename serialized fields and addressable addresses. Old JSON save files are deleted rather than migrated.
- Rename the gameplay concept **marker → flag** everywhere: `MarkerManager` → `FlagManager`, `MarkerData` → `FlagData`, `MarkerType` → `FlagType`, `Markers` → `Flags`, `markerTypeName`/`markerTypeNames`/`markerData` → `flagTypeName`/`flagTypeNames`/`flagData`, `marker_overload` → `flag_overload`, and all related members, prefabs, assets, and scenes.
- Rename the scoring target **markable → flaggable**: `Markable` → `Flaggable`, `markables` → `flaggables`, `SyncMarkables` → `SyncFlaggables`, and editor authoring controls.
- Rename the on-screen raise/lower UI element **marker flag → indicator**: `MarkerFlagController` → `FlagIndicatorController`, `marker_flag` prefab → `flag_indicator`, `marker_flag_anim` → `flag_indicator_anim`, `MarkerFlag.controller` → `FlagIndicator.controller`, and `activeMarkerFlags` → `activeFlagIndicators`.
- Rename conversation narrative-state **flag → signal**: `ConversationManager.Flags` → `Signals`, `SetFlag` → `SetSignal`, `ConversationEffectOperation.SetFlag` → `SetSignal`, `ConversationConditionKind.RequiredFlag` → `RequiredSignal`, `ConversationChatState.flags` → `signals`, and the editor's condition/effect labels and tooltips.
- Rename `typingFlagLength` → `typingIndicatorLength` on `ChatBubble` (the typing indicator duration).
- Update Addressable addresses in `Constants.cs` and the Addressable group asset in lockstep.
- Rename the corresponding `.cs`/`.prefab`/`.asset`/`.anim`/`.controller`/sprite files **with their `.meta` files** to preserve GUIDs.
- Update `AGENTS.md` and `openspec/config.yaml` terminology.
- Delete all old JSON save files under `Application.persistentDataPath`.
- Leave OpenSpec behavior specs unchanged (`skip_specs: true`) — see Capabilities below.

### Non-goals

- No gameplay, scoring, or behavior changes — this is a rename only.
- No save-file migration; old saves are discarded.
- No conversion of the ConversationGraph "signal" concept into anything new — only its name changes.
- No restructuring of the marker/flag system's responsibilities or data shapes.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This change is a behavior-neutral rename, so no capability's requirements change. It sets `skip_specs: true`. OpenSpec specs describe observable behavior, and the current `chat-conversation-graphs` requirements remain behaviorally accurate under the new vocabulary (the "marker"/"markable" concepts they describe are the same concepts now named "flag"/"flaggable"). The OpenSpec CLI's scenario-loss guard also forbids renaming scenario labels in a MODIFIED block; preserving coherent spec text rather than half-renaming it is deliberate. The terminology mapping is recorded here and in `design.md` so the main spec can adopt the new vocabulary when those requirements are next revised for behavior.

## Impact

- **Scripts (Managers):** `MarkerManager`, `SaveManager`, `SequenceEventManager`, `ConversationManager`.
- **Scripts (Component Controllers):** `MarkerFlagController`, `MarkerCheatSheetController`, `MarkerCheatSheetEntryController`.
- **Scripts (Handlers):** `HighlightHandler`.
- **Scripts (Plain Old / data):** `MarkerData`, `MarkerType`, `Markable`, `DayData`, `ChatBubble`, `Constants`, `ConversationChatState`, `ConversationConditionClause`, `ConversationEffectData`, `ConversationRunner`.
- **Scripts (Editor):** `ConversationGraphEditor`, `ConversationNodeView`, `GameTemplateEditor`.
- **Assets:** `MarkerFlag`/`MarkerCheatSheet*` prefabs (incl. `Taskbar Buttons`), scenes `v1_prototye.unity` / `tutorial_demo.unity`, `MarkerFlag.controller` + `marker_flag_anim`, `marker.png` (+ `marker_0/1/2` slices), `MarkerOverloadEventChannel.asset`, ConversationGraph/chat-log/template/save-slot `.asset` files, `Default Local Group.asset` Addressables.
- **Docs/config:** `AGENTS.md`, `openspec/config.yaml`.
- **Persistence:** existing JSON saves deleted; project `.asset` serialized field keys renamed with `[FormerlySerializedAs]` to avoid dropping authored values in Unity.
