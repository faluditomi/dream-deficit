## 1. Plain Old — data classes, enums, constants

- [x] 1.1 `MarkerType.cs`: rename static class `Markers` → `Flags` and class `MarkerType` → `FlagType`; rename the file to `FlagType.cs` (file+meta handled in 6.1)
- [x] 1.2 `MarkerData.cs`: rename class `MarkerData` → `FlagData`; rename serialized field `markerTypeName` → `flagTypeName` and property `ResolvedMarkerType` → `ResolvedFlagType`
- [x] 1.3 `MarkerData.cs`: add `[FormerlySerializedAs("markerTypeName")]` to `flagTypeName` and update `typeof(Markers)` → `typeof(Flags)` (reflection; see design D3/D6)
- [x] 1.4 `Markable.cs`: rename class `Markable` → `Flaggable` and field `markerType` → `flagType`; add `[FormerlySerializedAs("markerType")]` to the field
- [x] 1.5 `DayData.cs`: rename serialized fields `markerTypeNames` → `flagTypeNames`, `markerData` → `flagData`; rename `GetMarkerTypes()` → `GetFlagTypes()`, `GetMarkerData()` → `GetFlagData()`
- [x] 1.6 `DayData.cs`: add `[FormerlySerializedAs(...)]` for both renamed fields and update `typeof(Markers)` → `typeof(Flags)`; **serialization-safe constraint: fields must stay `JsonUtility`-serializable (`List<string>` / `List<FlagData>`) and are not migrated**
- [x] 1.7 `ChatBubble.cs`: rename `markables` → `flaggables`, `SyncMarkables()` → `SyncFlaggables()`, `typingFlagLength` → `typingIndicatorLength`; add `[FormerlySerializedAs("typingFlagLength")]` (authored values must survive)
- [x] 1.8 `Constants.cs`: rename `AddressablePrefabs.MarkerFlag` → `FlagIndicator`, `MarkerCheatSheet` → `FlagCheatSheet`, `MarkerCheatSheetEntry` → `FlagCheatSheetEntry`; rename `SequenceEventChannels.MarkerOverload` → `FlagOverload`; rename enum value `SequenceEventType.MarkerOverload` → `FlagOverload`
- [x] 1.9 `Constants.cs`: update address strings `marker_flag` → `flag_indicator`, `marker_cheat_sheet` → `flag_cheat_sheet`, `marker_cheat_sheet_entry` → `flag_cheat_sheet_entry`, `marker_overload` → `flag_overload` (kept in lockstep with 7.1)
- [x] 1.10 `ConversationChatState.cs`: rename `flags` → `signals`; add `[FormerlySerializedAs("flags")]`; **serialization-safe constraint: conversation state is snapshotted into JSON saves and old saves are intentionally discarded (see 8.1)**

## 2. Plain Old — conversation narrative flag → signal

- [x] 2.1 `ConversationGraphEnums.cs`: rename `ConversationEffectOperation.SetFlag` → `SetSignal` and `ConversationConditionKind.RequiredFlag` → `RequiredSignal`
- [x] 2.2 `ConversationConditionClause.cs`: rename `Matches(..., List<string> flags)` param → `signals` and update the `RequiredFlag` case to `RequiredSignal`
- [x] 2.3 `ConversationEffectData.cs`: update any `SetFlag` references to `SetSignal` (no `SetFlag` references existed; no change required)
- [x] 2.4 `ConversationRunner.cs`: rename `flags` locals → `signals`, update `bubble.typingFlagLength` → `bubble.typingIndicatorLength`, and any `SetFlag`/`RequiredFlag` references

## 3. Managers

- [x] 3.1 `MarkerManager.cs` → `FlagManager.cs`: rename class `MarkerManager` → `FlagManager` and all members (`placedMarkers` → `placedFlags`, `activeMarkerTypeCache` → `activeFlagTypeCache`, `activeMarkerType` → `activeFlagType`, `AddMarker`/`RemoveMarker`/`AddMarkersInstantly`, `GetMarkersForChatBubble`, `CalculateMarkerAccuracy`, `MarkerOverloadEventCheck`, `SetActiveMarkerTypes`, `LoadFromDayData`, `EvaluateChatLogAccuracy`)
- [x] 3.2 `MarkerManager.cs`: rename `markerFlagPrefab` → `flagIndicatorPrefab`, `activeMarkerFlags` → `activeFlagIndicators`, `markerHoldAction` → `flagHoldAction`, `markerOverloadSequenceEventChannel` → `flagOverloadSequenceEventChannel`, `MarkerFlagController` → `FlagIndicatorController`; update `Constants.AddressablePrefabs.MarkerFlag`/`SequenceEventChannels.MarkerOverload` uses
- [x] 3.3 `MarkerManager.cs`: rename serialized field `activeMarkerTypeCache` → `activeFlagTypeCache` and add `[FormerlySerializedAs("activeMarkerTypeCache")]`; **serialization-safe constraint: this field is serialized in both scenes**
- [x] 3.4 `SaveManager.cs`: rename `GetSavedMarkersForChatLog` → `GetSavedFlagsForChatLog` and locals `allMarkers` → `allFlags`
- [x] 3.5 `SequenceEventManager.cs`: update `Constants.SequenceEventType.MarkerOverload` → `FlagOverload`
- [x] 3.6 `ConversationManager.cs`: rename `Flags` → `Signals`, `SetFlag()` → `SetSignal()`, private `flags` list → `signals`; **serialization-safe constraint: state snapshot field names feed the JSON save**

## 4. Component Controllers

- [x] 4.1 `MarkerFlagController.cs` → `FlagIndicatorController.cs`: rename class and `MoveMarkerFlagBehaviour` → `MoveFlagIndicatorBehaviour`
- [x] 4.2 `MarkerCheatSheetController.cs` → `FlagCheatSheetController.cs`: rename class; members `markerCheatSheetEntryPrefab` → `flagCheatSheetEntryPrefab`, `markerEntryContainer` → `flagEntryContainer`, `UpdateMarkers()` → `UpdateFlags()` (no such method existed), `MarkerCheatSheetEntryController` → `FlagCheatSheetEntryController`
- [x] 4.3 `MarkerCheatSheetEntryController.cs` → `FlagCheatSheetEntryController.cs`: rename class and `Setup(MarkerType)` → `Setup(FlagType)` with `markerType` → `flagType`
- [x] 4.4 `ChatLogController.cs`: rename `savedMarkers` → `savedFlags` and update `GetSavedMarkersForChatLog`/`AddMarkersInstantly` calls

## 5. Handlers and Editor

- [x] 5.1 `HighlightHandler.cs`: rename `hoveredMarker`/`previousHoveredMarker` → `hoveredFlag`/`previousHoveredFlag`, `GetMarkers`/`FindMarkerInBoundsArea`/`IsPointInMarkerBounds`/`GetMarkedText` → flag equivalents, `MarkerManager`/`MarkerData`/`MarkerType` uses
- [x] 5.2 `ConversationGraphEditor.cs`: rename `MarkerTypes`/`markerTypesCache` → `FlagTypes`/`flagTypesCache`, `markable` → `flaggable`, `newMarkableMarkerIndex` → `newFlaggableFlagIndex`, tooltip constants (`MarkerTypeNew`/`MarkerTypeExisting`/`NewMarkable`/`Markables`), and `typeof(Markers)` → `typeof(Flags)`
- [x] 5.3 `GameTemplateEditor.cs`: rename `showMarkerDataFoldout`, `markerTypeNames`, `RefreshMarkerTypeNames`, `DrawMarkerTypeSection`, `DrawMarkerDataSection`, marker labels, and `typeof(Markers)` → `typeof(Flags)`
- [x] 5.4 `ConversationNodeView.cs`: rename condition label `"flag: "` → `"signal: "`

## 6. Unity files, prefabs, animations, sprite (rename file + `.meta` together in the Editor)

- [x] 6.1 Rename scripts + `.meta`: `MarkerManager.cs`, `MarkerData.cs`, `MarkerType.cs`, `Markable.cs`, `MarkerFlagController.cs`, `MarkerCheatSheetController.cs`, `MarkerCheatSheetEntryController.cs` to their flag/indicator/flaggable names
- [x] 6.2 Rename prefabs + `.meta`: `MarkerFlag.prefab` → `FlagIndicator.prefab`, `MarkerCheatSheet.prefab` → `FlagCheatSheet.prefab`, `MarkerCheatSheetEntry.prefab` → `FlagCheatSheetEntry.prefab`; rename the Taskbar `MarkerCheatSheetButton.prefab` → `FlagCheatSheetButton.prefab` — done (files + `.meta` renamed, GameObject names via the RenameMarkerObjectsTool editor script)
- [ ] 6.3 Rename animations + `.meta`: `MarkerFlag.controller` → `FlagIndicator.controller`, `marker_flag_anim.anim` → `flag_indicator_anim.anim`; rename `marker.png` → `flag.png` — done (files + `.meta`, clip name, controller name); remaining: sprite slice names `marker_0/1/2` inside `flag.png.meta` (cosmetic, rename in the Sprite Editor; no code references slice names)
- [x] 6.4 Rename `MarkerOverloadEventChannel.asset` → `FlagOverloadEventChannel.asset` (file, `.meta`, and `m_Name`) via the Editor — done (file + `.meta` + main object name)
- [x] 6.5 Let Unity reimport/reserialize affected `.asset`/`.prefab`/`.unity` files, then fix any broken references surfaced by the Console; do **not** hand-edit YAML (guardrail) — done; 9 ScriptableObjects force-reserialized to the new field keys; the only Console error is the pre-existing missing `SupervisorButton` prefab GUID in `v1_prototye.unity` (unrelated to this change)

## 7. Addressables

- [x] 7.1 Update `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset` addresses to match 1.9 (`prefabs/flag_indicator`, `prefabs/flag_cheat_sheet`, `prefabs/flag_cheat_sheet_entry`, `sequence_event_channel/flag_overload`) and any `event_sequence_markeroverload_*` names still present (none remained) — done via the Addressables Groups window in Unity
- [x] 7.2 Verify no Addressables resolve to `null` after reimport (addresses edited only via the Addressables/Inspector UI, never raw YAML) — done statically: all four group addresses match `Constants` exactly and asset GUIDs are unchanged; runtime confirmation folds into 10.2

## 8. Saves and serialization shims

- [x] 8.1 Delete all old JSON save files under `Application.persistentDataPath` (no migration; old saves are intentionally discarded) — deleted `save_Slot 1.json` from `C:\Users\tomi\AppData\LocalLow\DefaultCompany\dream-deficit`
- [x] 8.2 After Unity has reserialized all affected assets, remove the `[FormerlySerializedAs(...)]` attributes added in 1.3/1.4/1.6/1.10/3.3 and re-verify no serialized values were lost — done; all six shims removed and authored `typingIndicatorLength` values (0.6/0.7/4/5/6) verified intact after the force-reserialize

## 9. Docs and config

- [x] 9.1 Update `AGENTS.md`: replace marker/markable/flag terminology in the overview, architecture tree, "Marker System" section, data-flow diagram, naming conventions, and TODOs with flag/flaggable/indicator/signal
- [x] 9.2 Update `openspec/config.yaml` architecture/context terminology (`MarkerManager`, `MarkerData`, `MarkerType`, `Markable`, markers) to the new terms; **note: this change itself sets `skip_specs: true` and does not modify behavior specs**

## 10. Verification

- [x] 10.1 Repository-wide search confirms no residual `marker`/`markable`/`typingFlagLength` identifiers remain in any `.cs` file (shims removed in 8.2, Addressables values updated in 1.9); only cosmetic serialized display names remain (AnimatorState `marker_flag_anim` in `FlagIndicator.controller`, sprite slices `marker_0/1/2`, and the self-healing `m_EditorClassIdentifier` in the scenes — none referenced by code)
- [ ] 10.2 Play Mode smoke test: place/remove flags, accuracy scoring, flag indicator raise/lower, cheat sheet, `flag_overload` event, conversation signal gating and persistence — **BLOCKED: requires the Unity Editor**
- [x] 10.3 Run `openspec validate rename-marker-to-flag --json` and confirm the change is valid
