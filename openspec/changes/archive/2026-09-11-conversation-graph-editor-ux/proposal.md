# Proposal: conversation-graph-editor-ux

## Why

The Conversation Graph Editor (`Custom Tools/Conversation Graph Editor`) is the only way to author conversation graphs, but day-to-day authoring is rough: fields have no explanations, node previews on the canvas go stale until a full "Layout Graph" rebuild, the layout leaves the graph wherever the viewport happened to be, sequence event names must be typed by hand (and a typo silently makes a condition/effect never fire), and the context-menu Copy/Duplicate actions do nothing. These papercuts slow content authoring and cause silent data errors. Separately, day/work progression is split across two APIs — `RaiseEvent` for entry/condition events and dedicated `StartDayClock`/`EndDay` effect operations that call `GameManager` directly — duplicating the event system and obscuring the Day (dream/scene boundary) vs Work (clock boundary) distinction.

## What Changes

- **Inspector tooltips**: every field in the node inspector (bubble metadata, message, markable authoring, choice options, effects, conditions, entry scheduling) shows a tooltip explaining what it does.
- **Live canvas previews**: editing a node's displayed data in the inspector (bubble message, choice option preview text, condition list, entry flags) updates that node's canvas preview immediately — no "Layout Graph" or rebuild needed.
- **Layout frames content**: running "Layout Graph" (and the automatic default-layout on open) moves/zooms the viewport so the laid-out graph is centered and fully visible.
- **Sequence event dropdowns**: event values — `Event` condition clauses and `RaiseEvent` effect values — are picked from a dropdown of `Constants.SequenceEventType` names instead of being typed as free text. Existing unrecognized values remain visible and are not silently discarded.
- **Working Copy/Duplicate**: the canvas context-menu Copy, Cut, Paste, and Duplicate actions work on node selections, duplicating node data with fresh GUIDs and preserving edges between the copied nodes.
- **Day/Work event unification (WorkStart/WorkEnd)**: remove `ConversationEffectOperation.StartDayClock` and `EndDay`; day/work progression becomes `RaiseEvent` with well-known types that `GameManager` observes. Introduce `WorkStart` (player requests work clock start → `TriggerDayTimePassing`) and `WorkEnd` (system raises at clock 0 via `TriggerEndOfDayTimePassing` → runners re-evaluate work-gated waits), and clarify `DayStart` (system raises at `StartDay` when we switch to the desktop scene — just like now, unchanged) vs `DayEnd` (player requests day end → `EndDay` with the existing day-blocking deferral). The clock-expiry raise changes from `DayEnd` to `WorkEnd` (same trigger point, renamed); `DayEnd` becomes exclusively player-initiated; `DayStart` and `WorkEnd` remain system-raised at their current points, unchanged in timing. `ConversationManager.ApplyEffects` no longer calls `GameManager` directly — everything crosses the single `SequenceEvent` bus. Includes a one-time transient migration that rewrites the single live `StartDayClock` usage (`phoebe_day_start` "enough — let's work" option) to `RaiseEvent(WorkStart)` and any `EndDay` usages to `RaiseEvent(DayEnd)`; the migration script is deleted before the change is considered done.

No save-format shape change for conversation state; event values stored on the graph remain enum-name strings, which the runtime already parses (`Enum.TryParse` / case-insensitive compare). The stored `ConversationEffectOperation` integer on the migrated node changes from `2` to `1` (`RaiseEvent`) with a new `stringValue`.

**Out of scope (non-goals):**

- Copy/paste across different graph assets or across editor sessions (clipboard is in-memory, per editor window).
- Drag-to-reorder or multi-select inspector editing.
- Any change to graph validation rules, layout algorithm (layering/spacing), or node visuals beyond preview text freshness.
- Tooltips inside the graph canvas itself (node bodies, ports) — inspector fields only.
- Undo/redo for paste/duplicate beyond what `Undo.RecordObject` on the graph asset already provides.
- New day/work event types beyond `WorkStart`/`WorkEnd` (e.g. pause, time-skip).
- Cross-session migration or save-file migration — the transient migration only touches `ConversationGraph` assets in the project; in-flight saves referencing the old operations are not retained (no live `EndDay` asset today).

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `chat-conversation-graphs`: the requirement **"Graphs are authored in a visual node editor"** is extended — the editor must explain its fields via tooltips, keep canvas previews in sync with inspector edits, frame laid-out content in the viewport, offer sequence events as a dropdown of the known event types, and support working copy/duplicate of nodes. New requirements covering these authoring-experience behaviors are added to the capability.
- `chat-conversation-graphs`: the requirement **"Choice options apply effects, including day progression"** is modified — day/work progression effects no longer exist as dedicated operations; they are authored as `RaiseEvent` effects with well-known types (`WorkStart` to start the work clock, `DayEnd` to end the day/dream, `DayStart`/`WorkEnd` as system-raised boundaries) that `GameManager` observes via the event bus. `ConversationManager` no longer calls `GameManager` directly from choice effects.

## Impact

- **Code — editor**: `Assets/Scripts/Editor/ConversationGraphEditor.cs` (inspector drawing, contextual menu, layout/framing, clipboard actions), `Assets/Scripts/Editor/ConversationNodeView.cs` (preview refresh API). Also a transient one-time editor migration script under `Assets/Scripts/Editor/` that is created, run once to patch the single `StartDayClock` node, then deleted before archiving (not shipped).
- **Code — runtime**: `Assets/Scripts/Plain Old/ConversationGraphEnums.cs` (`StartDayClock`/`EndDay` removed), `Assets/Scripts/Plain Old/Constants.cs` (`SequenceEventType` + `SequenceEventChannels` + `EventChannelMetadata` expanded with `WorkStart`/`WorkEnd`, `SequenceEventChannel` assets `work_start`/`work_end` created via editor — never by hand-editing `.asset` files), `Assets/Scripts/MonoBehaviours/Managers/ConversationManager.cs` (`ApplyEffects` reduced to `SetFlag` + `RaiseEvent`, `GameManager` coupling removed), `Assets/Scripts/MonoBehaviours/Managers/GameManager.cs` (subscribes to the `SequenceEvent` bus: `WorkStart→TriggerDayTimePassing`, `DayEnd→EndDay` keeping `isEndDayDeferred`/`OnDayBlockingChoiceResolved` deferral; `TriggerEndOfDayTimePassing` now raises `WorkEnd` instead of `DayEnd` — same clock-0 trigger, renamed; `StartDay` still raises `DayStart` when switching to the desktop scene, unchanged), `Assets/Scripts/MonoBehaviours/Managers/SequenceEventManager.cs` (cooldown metadata for new types).
- **Data**: one `ConversationGraph` asset (`Phoebe/phoebe_day_start`) has its `operation: 2` rewritten to `operation: 1` + `stringValue: WorkStart` by the transient migration; all other graph assets unchanged; event `stringValue` fields written by the new dropdown use exact enum names runtime already accepts.
- **Risk**: low-medium. Editor worst case is tool regression; runtime change is a pure indirection (direct call → event) with preserved deferral timing — `ResolveChoice` still fires effects while `pendingChoice` is set, so `DayEnd→EndDay` still defers correctly. Verified by Play Mode before archiving.
