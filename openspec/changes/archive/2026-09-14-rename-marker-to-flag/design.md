## Context

See `proposal.md` — Why for motivation. This design captures the mechanical and Unity-specific decisions needed to rename the concepts safely; it does not restate the scope.

Current state that shapes the approach:
- The gameplay annotation system is code-named **marker** (`MarkerManager`, `MarkerData`, `MarkerType`, `Markers`, `Markable`) and its `.asset`/prefab references are Unity-serialized by field name and GUID.
- The on-screen raise/lower UI element is code-named **marker flag** (`MarkerFlagController`, `marker_flag` prefab, `MarkerFlag.controller`).
- The conversation graph's narrative state is code-named **flag** (`ConversationManager.Flags`, `SetFlag`, `RequiredFlag`, `ConversationChatState.flags`).
- `ChatBubble.typingFlagLength` names the typing indicator duration.
- Asset loading is string-address based via `Constants` + the Addressables group asset.
- Saves persist JSON by field name via `JsonUtility`.
- `AGENTS.md` forbids hand-editing `.asset`/`.prefab`/`.unity`. Unity resolves scripts by GUID, so files and their `.meta` must be renamed together.

## Goals / Non-Goals

**Goals:**
- Every occurrence of the four concepts has exactly one name, consistently across code, assets, addresses, docs, and editor tooling.
- GUIDs and existing authored values in project `.asset`/`.prefab`/`.unity` files survive.
- Addressables resolve after the rename (no null loads).

**Non-Goals:**
- Changing behavior, data shapes, scoring, or the graph state machine.
- Migrating or preserving old JSON saves (they are deleted).
- Editing OpenSpec behavior specs (behavior-neutral; `skip_specs: true`).
- Renaming generic prose uses of "marker" that do not refer to the domain concept (e.g. an entry node's on-canvas exclusive/repeatable badges).

## Decisions

### D1: Single concept → term map

| Concept | New term | Old terms |
|---|---|---|
| Scored text annotation | **flag** | marker |
| Scoring target span | **flaggable** | markable |
| On-screen raise/lower UI element | **indicator** | marker flag |
| Conversation narrative state | **signal** | flag |
| Typing indicator duration | **indicator** (`typingIndicatorLength`) | `typingFlagLength` |

Rationale: removes all overload; the UI already uses "Flag". Alternative considered: rename gameplay to "annotation"/"tag" — rejected as it does not match the player-facing word already in use.

### D2: Rename files together with their `.meta`, inside the Unity Editor

Rename `MarkerManager.cs`+`.meta`, `MarkerFlagController.cs`+`.meta`, `Markable.cs`+`.meta`, `MarkerFlag.prefab`+`.meta`, `MarkerCheatSheet*.prefab`+`.meta`, `MarkerFlag.controller`+`.meta`, `marker_flag_anim.anim`+`.meta`, `marker.png`+`.meta`, and move/rename the Taskbar-button prefab. Rationale: Unity references scripts/assets by GUID stored in the `.meta`; renaming the pair preserves the GUID. Alternative considered: raw filesystem rename — rejected, Unity would regenerate `.meta` and break every reference (also conflicts with the repo guardrails).

### D3: Rename serialized fields with `[FormerlySerializedAs]`

For each renamed serialized field (`markerTypeName`, `markerTypeNames`, `markerData`, `markerType` on `Markable`, `typingFlagLength`, `activeMarkerTypeCache`), add `[UnityEngine.Serialization.FormerlySerializedAs("<oldName>")]` on the renamed field so Unity maps existing serialized values in project assets. Do **not** hand-edit `.asset`/`.prefab`/`.unity` YAML. After Unity re-serializes the affected assets (open/save via the editor), remove the attributes in a follow-up. Rationale: preserves authored data without violating the guardrails. Alternative considered: manual YAML key edits — rejected (corrupts/forbidden); alternative considered: no attribute — rejected (silently resets values such as `typingFlagLength` 4/5/6/0.6/0.7 to 0).

### D4: Update Addressable addresses in lockstep

Rename addresses `prefabs/marker_flag` → `prefabs/flag_indicator`, `prefabs/marker_cheat_sheet` → `prefabs/flag_cheat_sheet`, `prefabs/marker_cheat_sheet_entry` → `prefabs/flag_cheat_sheet_entry`, `sequence_event_channel/marker_overload` → `sequence_event_channel/flag_overload`, and the `event_sequence_markeroverload_*` names if still present. Update **both** the constants in `Constants.cs` and the entries in `Assets/AddressableAssetsData/AssetGroups/Default Local Group.asset`. Rationale: a mismatch returns `null` from `RetrieveAddressable<T>`. Alternative considered: keep old addresses — rejected, contradicts the rename and leaves `marker` in data.

### D5: Delete old JSON saves; no migration

Delete saves under `Application.persistentDataPath`. Rationale: user-directed; avoids deserialization ambiguity for renamed `DayData`/`MarkerData`/`ConversationChatState` fields.

### D6: Update reflection lookups

`typeof(Markers).GetFields(...)` appears in four places (`MarkerData`, `DayData`, `GameTemplateEditor`, `ConversationGraphEditor`). Rename the static class to `Flags` and update all four consistently. Rationale: reflection couples the class name; a miss silently yields no flag types at runtime.

### D7: OpenSpec specs opt out

Set `skip_specs: true`. The rename changes no observable behavior, and the CLI's scenario-loss guard rejects renaming scenario labels inside `MODIFIED`. Rationale: keep specs coherent instead of half-renamed; record the vocabulary map here for a future behavior revision.

### D8: Execution order

1. Code renames (classes/members/fields, with `[FormerlySerializedAs]`).
2. File+`.meta` renames in Unity; let Unity reserialize assets; verify references.
3. Addressable address updates (constants + group asset).
4. Editor tooling labels/tooltips, sprite slices, animation/controller.
5. Docs (`AGENTS.md`, `openspec/config.yaml`).
6. Delete old saves; remove `[FormerlySerializedAs]` shims after a clean reserialize.

## Risks / Trade-offs

- **GUID/reference loss** → rename `.cs`/asset and its `.meta` in the Unity Editor (D2); verify no broken references after reimport.
- **Addressable mismatch → null loads** → update constants and the group asset in the same commit (D4); smoke-test every renamed address in Play Mode.
- **Serialized value loss in project assets** → `[FormerlySerializedAs]` on every renamed field (D3); verify authored `typingFlagLength` values survive.
- **Reflection miss** → update all four `typeof(...)` sites (D6); grep for `typeof(Markers)` after the pass.
- **`[FormerlySerializedAs]` residue** → schedule removal after Unity reserializes; leaving them temporarily is intentional and safe.
- **`skip_specs` leaves spec vocabulary old** → accepted; the map in D1 documents the correspondence and the spec adopts it on the next behavior change.
- **Generic "marker" prose** → deliberately untouched (Non-Goals) to avoid nonsense like "exclusive/repeatable flags".
