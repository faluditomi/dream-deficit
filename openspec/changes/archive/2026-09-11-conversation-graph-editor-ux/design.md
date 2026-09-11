# Design: conversation-graph-editor-ux

## Context

See `proposal.md` for motivation and `specs/chat-conversation-graphs/spec.md` for the behavior contract. Current state that shapes the approach:

- The inspector is hand-drawn IMGUI (`ConversationGraphEditor.DrawInspector` and its `Draw*` helpers) editing POCO data (`ConversationNodeData`, `ChatBubble`, `ConversationConditionClause`, `ConversationEffectData`) directly on the `ConversationGraph` ScriptableObject — not `SerializedProperty`-driven.
- `ConversationNodeView` builds all its preview content (message label, condition summary labels, title, choice option port names) once in the constructor. The only refresh path is `RebuildGraphView()`, which is called on layout, node add/remove, and option add.
- `RebuildGraphView()` keeps its guid→view dictionary local; the editor only tracks `selectedNodeData`, not the view for it.
- `LayoutGraph()` writes `editorPosition` values starting at a fixed margin; the viewport (`viewTransform`) is never touched, so the graph can be laid out entirely off-screen.
- Event values (`ConversationConditionKind.Event` clause `stringValue`, `ConversationEffectOperation.RaiseEvent` effect `stringValue`) are free-text fields. Runtime parses them with `Enum.TryParse(..., true, out Constants.SequenceEventType)` (`ConversationManager`) and compares with `string.Equals(..., OrdinalIgnoreCase)` (`ConversationConditionClause.Matches`), so any exact enum name written by a dropdown is runtime-compatible.
- GraphView's built-in context menu offers Cut/Copy/Paste/Duplicate, but the default implementations round-trip through GraphView's element serialization, which carries no payload for our custom nodes — the actions silently no-op (or paste empty shells). The virtual callbacks (`CutSelectionCallback`, `CopySelectionCallback`, `PasteSelectionCallback`, `DuplicateSelectionCallback`) are overridable on `ConversationGraphView`.
- **Day/Work split**: `ConversationEffectOperation` has 4 cases (`SetFlag=0, RaiseEvent=1, StartDayClock=2, EndDay=3`). `StartDayClock`/`EndDay` call `GameManager.TriggerDayTimePassing()`/`EndDay()` directly from `ConversationManager.ApplyEffects`, while `RaiseEvent` broadcasts `Constants.SequenceEventType` via `SequenceEventManager → ConversationManager.OnSequenceEvent → runners`. Day events (`DayStart`/`DayEnd`) signal dream/scene boundaries: `StartDay()` raises `DayStart` when we switch to the desktop scene and a player choice raises `DayEnd → EndDay()` with day-blocking deferral. Work events signal the work clock (`WorkStart`/`WorkEnd`): clock expiry today raises `DayEnd` — after the split it will raise `WorkEnd` at the same system trigger point (renamed, timing unchanged); `DayStart` remains system-raised at desktop switch, unchanged. Only one live `StartDayClock` usage exists (`Phoebe/phoebe_day_start` "enough — let's work"); no live `EndDay` usage. `Constants.SequenceEventType` today is `MarkerOverload, DayStart, DayEnd, Default` and `SequenceEventManager.EventChannelMetadata` mirrors it.
- Unity 6 (6000.3.8f1), `UnityEditor.Experimental.GraphView`. AGENTS.md guardrails: never hand-edit `.asset`/`.prefab`/`.unity` files; all prefabs/data load via Addressables; managers are Singletons.

## Goals / Non-Goals

**Goals:**

- All five editor-UX items implemented within `Assets/Scripts/Editor/` (`ConversationGraphEditor.cs`, `ConversationNodeView.cs`).
- Unify day/work progression onto the single `SequenceEvent` bus: remove `StartDayClock`/`EndDay` effect operations, introduce `WorkStart`/`WorkEnd` sequence event types and channels, make `GameManager` observe `WorkStart→TriggerDayTimePassing` and `DayEnd→EndDay` via the bus, and change clock expiry to raise `WorkEnd` instead of `DayEnd`. `ConversationManager.ApplyEffects` loses its `GameManager` coupling (only `SetFlag` + `RaiseEvent`).
- Zero hand-edits of `.asset` files. The one `StartDayClock` node is patched by a transient one-time editor migration script that is deleted before the change is archived (not shipped).
- Data mutation stays in `ConversationGraphEditor` (which owns `Undo.RecordObject` + `PersistGraph`); the graph view stays a dumb shell that forwards intents.

**Non-Goals:**

- No cross-asset or cross-session clipboard (in-memory, per editor window only).
- No changes to the layout algorithm itself (layering, spacing, island placement) — only viewport framing after it runs.
- No redesign of the inspector into UIElements/SerializedProperty.
- New day/work event types beyond `WorkStart`/`WorkEnd` (e.g. pause, time-skip) — future `SequenceEventType` additions follow the same pattern.
- No cross-session or save-file migration — the transient migration only touches `ConversationGraph` assets in the project; no live `EndDay` asset and no save-file back-compat is retained.
- The transient migration script itself is not part of the shipped change — it is created, run once, then deleted.

## Decisions

### D1 — Tooltips via `GUIContent`, centralized in a static text table

Convert inspector field labels from bare strings to `new GUIContent(label, tooltip)` using the `EditorGUILayout` overloads that accept `GUIContent`. All tooltip texts live in one static class (e.g. `ConversationEditorTooltips`) next to the editor so wording is reviewable in one place.

- *Alternative considered:* `[Tooltip]` attributes on the runtime data classes + `SerializedObject`/`PropertyField` rendering. Rejected: the inspector edits nested POCOs inside a list on the ScriptableObject, not direct serialized fields; restructuring to PropertyField rendering is a rewrite, and annotating runtime types purely for editor help text couples runtime code to tooling concerns.
- Fields without a `GUIContent` overload path (section headers, read-only "Indexes"/"Resolved Text" labels) get tooltips where an overload exists; pure headers don't need one.

### D2 — Live preview via `ConversationNodeView.RefreshPreview()`, not full rebuilds

Add a public `RefreshPreview()` to `ConversationNodeView` that rebuilds only the data-dependent visuals in place:

- clears and re-adds the message preview / condition summary labels in `mainContainer` (ports live in `inputContainer`/`outputContainer`, so they are untouched);
- recomputes the title (choice day-blocking, entry exclusive/repeatable suffixes);
- updates each choice option port's `portName` from the option's current `previewText` (matched by option GUID via the existing `optionPorts` list);
- calls `RefreshExpandedState()`/`RefreshPorts()` so the node re-measures.

`ConversationGraphEditor` hoists the guid→view dictionary from `RebuildGraphView()` into a field (`nodeViews`) so it can find the view for `selectedNodeData` and call `RefreshPreview()` after any inspector edit that commits (`EditorGUI.EndChangeCheck()` / string-compare branches already mark the commit points). Structural changes (adding/removing nodes or choice options, connecting/disconnecting edges) keep using the full `RebuildGraphView()` — port counts changed, so in-place refresh is not sufficient there.

- *Alternative considered:* calling `RebuildGraphView()` on every inspector commit. Rejected: it destroys and recreates all elements, dropping GraphView selection (which drives `selectedNodeData`), and would fight IMGUI keyboard focus while typing in the message area.
- *Risk note:* refreshing while the message TextArea has IMGUI focus is safe because the node view is UIElements in a different container; no focus interaction.

### D3 — Framing computed explicitly after layout (FrameAll is timing-sensitive)

After `LayoutGraph()` + `RebuildGraphView()` (manual menu action) and after the automatic `EnsureDefaultLayout()` path in `OnSelectionChanged()`, the viewport must be moved/zoomed so the laid-out nodes are centered and visible.

**CORRECTED during implementation:** the original plan used `graphView.FrameAll()`. In practice it was effectively a no-op — deferred via `schedule.Execute`, it measured element bounds before the panel's layout pass had sized the nodes, producing a degenerate/zero rect. `GraphView.SetViewTransform` also does not exist. `CalculateRectToFitAll(VisualElement)` and `UpdateViewTransform(Vector3, Vector3)` do exist, but measuring through them remained timing-dependent; worse, a callback scheduled from a contextual-menu action often never ran, so the graph framed only once (on open) and not on subsequent Layout Graph clicks.

**Final implementation:** `FrameGraphContent()` applies the frame **synchronously** via `ApplyGraphFrame()`, then schedules one deferred re-apply. `ApplyGraphFrame()`:
1. builds `contentRect` from the **data model**, not measured layout: for each node, `nodeData.editorPosition` (the exact content-space coordinate `LayoutGraph` writes) with size `view.layout.size`, falling back to a nominal `240×120` footprint when the node has not been sized yet — so the rect is always valid and no layout pass is required. **Critical:** `view.layout.size` reads as `NaN` before layout, and `NaN <= 1f` is false, so the size test must be `!IsFinite(v) || v <= 1f`; otherwise NaN poisons `contentRect`, `CalculateFrameTransform` emits NaN, and `UpdateViewTransform` silently returns without applying (its own `IsNaN/IsInfinity` guard). This was the actual cause of the "Layout Graph does nothing" symptom;
2. passes that rect to Unity's own `public static GraphView.CalculateFrameTransform(contentRect, graphView.layout, border: 30, out translation, out scaling)` — the exact call `Frame()` uses, so fit/clamp (max scale 1.0, min `ContentZoomer.DefaultMinScale`) and centering match `FrameAll` without reimplementing the math;
3. `graphView.UpdateViewTransform(translation, scaling)` then `MarkDirtyRepaint()`.

The translation is algebraically identical to Unity's own `FrameAll` math (`−rect.position·scale + (viewport.size − rect.size·scale)/2`), so behavior matches the intended framing; only the measurement is made deterministic and independent of scheduling. The deferred re-apply refines the zoom once real node sizes exist (harmless if it never runs). Guarded for a null/empty graph and a not-yet-sized viewport. `FrameSelection()` rejected (the spec frames the whole laid-out graph, not a selection). No change to the layout algorithm itself.

- *Alternative considered:* keep calling `FrameAll()` and just retry — rejected because it gives no control over the degenerate-bounds case and silently fails; computing the transform from node data makes the behavior explicit and deterministic.
- Risk note: if a future Unity version changes node `layout.size` semantics, the worst case is framing computed with placeholder sizes (correct centering, slightly loose zoom); it does not affect graph data.

### D4 — Event dropdowns over `Enum.GetNames(typeof(Constants.SequenceEventType))`

For the two free-text event fields (`DrawConditionList` when `kind == Event`, `DrawEffects` when `operation == RaiseEvent`), replace `EditorGUILayout.TextField` with `EditorGUILayout.Popup`:

- Options = the known enum names, plus — when the stored `stringValue` matches none of them (case-insensitive) and is non-empty — the raw stored value appended as an extra final entry, selected. This satisfies "unknown legacy value stays visible and is not rewritten until the designer acts": committing the popup without touching it writes back the same string.
- Selecting a known name writes `Enum.GetName(...)` exactly (e.g. `"DayStart"`), which both runtime paths already accept (`Enum.TryParse(ignoreCase)` in `ConversationManager`, `OrdinalIgnoreCase` compare in `ConversationConditionClause.Matches`). No runtime or asset-format change. After the unification (D6) the popup automatically covers `WorkStart`/`WorkEnd` without code changes.
- *Alternative considered:* changing `stringValue` to a real `Constants.SequenceEventType` field on the data classes. Rejected: breaks existing graph assets' serialized data and JsonUtility save compatibility for anything that round-trips these strings; explicitly out of scope per proposal.
- *Alternative considered:* `EnumPopup` bound to a parsed enum. Rejected: it cannot represent unknown legacy values without discarding them.

### D5 — Custom in-window clipboard; intercept commands, not callback virtuals

**CORRECTED during implementation against the real Unity 6000.3.8f1 GraphView API** (verified from `UnityEditor.GraphViewModule` metadata). The original plan — override `CutSelectionCallback` / `CopySelectionCallback` / `PasteSelectionCallback` / `DuplicateSelectionCallback` and `canPasteSelection` — is not implementable:

| Originally assumed | Actual API in 6000.3.8f1 |
|---|---|
| `CutSelectionCallback()` overridable | `protected internal`, **non-virtual** |
| `CopySelectionCallback()` overridable | `protected internal`, **non-virtual** |
| `PasteSelectionCallback()` | **does not exist**; the member is `PasteCallback()` (non-virtual) |
| `DuplicateSelectionCallback()` overridable | `protected internal`, **non-virtual** |
| `canPasteSelection` | **does not exist**; the member is `canPaste` (virtual) |
| Ctrl+X/C/V/D arrive as `KeyDownEvent` | they arrive as **`ExecuteCommandEvent`** (`OnExecuteCommand`), with enablement via **`ValidateCommandEvent`** (`OnValidateCommand`). `OnKeyDownShortcut` handles only `a/o/[/]/space` framing, not clipboard commands. |
| `EventCommandNames.Cut/Copy/Paste/Duplicate` usable | `UnityEngine.UIElements.EventCommandNames` is **internal** in 6000.3.8f1 (CS0122) — command names are plain strings (`"Cut"`, `"Copy"`, `"Paste"`, `"Duplicate"`) held as private consts on the view |
| `DropdownMenu.MenuItem` nested type | **removed** in Unity 6 — menu items are the top-level `UnityEngine.UIElements.DropdownMenuItem` (CS0426/CS0029 confirmed; `MenuItems()` returns a generic list of it). Only `DropdownMenuAction` exposes the public `name` property, so defaults are matched via `item is DropdownMenuAction action && action.name == "Cut"` — **not** `ToString()` (which yields the type name and silently matches nothing) |

What *is* virtual and usable: `canCutSelection`, `canCopySelection`, `canDuplicateSelection`, `canPaste` (all `protected internal virtual`), and `public virtual BuildContextualMenu(ContextualMenuPopulateEvent)`.

**Decision (revised):** `ConversationGraphView` overrides `BuildContextualMenu` — calling `base.BuildContextualMenu(evt)` first, then removing the default Cut/Copy/Paste/Duplicate entries (`DropdownMenu.ClearItems()` / `RemoveItemAt()`, since `DropdownMenuAction`'s callbacks are private and cannot be retargeted in place) and appending our own actions that raise intent events — and registers a **trickle-down `ExecuteCommandEvent`** handler that maps `Cut`/`Copy`/`Paste`/`Duplicate` commands to the same intents and calls `StopImmediatePropagation()`, plus a `ValidateCommandEvent` handler so menu/shortcut enablement matches `canCut/Copy/Paste/Duplicate`. The `can*` virtuals are overridden to require a non-empty node selection (and, for paste, a non-empty clipboard). Intents are exposed as public C# events on the view (`CutRequested`, `CopyRequested`, `PasteRequested`, `DuplicateRequested`), following the existing `SelectionChanged` pattern; `ConversationGraphEditor` owns all data mutation.

The default GraphView clipboard pipeline remains unusable for custom nodes (no serializable payload), which is why the intents are handled at the data level:

- **Clipboard payload** (private class in the editor, in-memory only): deep clones of the selected `ConversationNodeData` plus the subset of `ConversationEdgeData` whose both endpoints are in the selection. Clone via `JsonUtility` round-trip per node (`JsonUtility.FromJson<ConversationNodeData>(JsonUtility.ToJson(node))`), then walk the clone assigning fresh `guid` for the node and every choice option, recording old→new maps for nodes and options. `Markable.markerType` (a ScriptableObject reference) round-trips as an instance-ID reference, which resolves because paste always happens in the same editor session with the same assets loaded — cross-session paste is a declared non-goal, and the clipboard is cleared when the target graph changes.
  - *Alternative considered:* hand-written clone method per data class. Viable fallback if the JsonUtility round-trip drops anything (verify `markables` and `editorPosition` survive); rejected as first choice to avoid maintaining parallel clone code as data classes grow.
- **Paste**: `Undo.RecordObject(targetGraph, ...)`, add cloned nodes with `editorPosition` offset (e.g. `+40,+40` from the source position stored in the clone), add the remapped internal edges via `graph.Connect(...)` (option edges carry the remapped option GUID), `PersistGraph()`, `RebuildGraphView()`, then select the new views.
- **Duplicate** = copy + paste immediately. **Cut** = copy + existing removal path (`RemoveNode` per selected node, which also drops its edges), inside one Undo step. **Copy** stores the payload without mutating the graph.
- The context menu is rebuilt in `ConversationGraphView.BuildContextualMenu` (above), so the window's existing `OnBuildContextualMenu` callback keeps only the Add-node / Layout / Validate entries and must not re-add clipboard items. Keyboard shortcuts route through `ExecuteCommandEvent`, not `KeyDownEvent`.
- Only `ConversationNodeView` selections participate; edges alone in the selection are ignored (the payload derives edges from the node set), matching the spec's "connections between copied nodes".

### D6 — Day/Work unification: melt StartDayClock/EndDay into RaiseEvent(WorkStart/DayEnd)

**Problem**: `ApplyEffects` has two calling conventions — `RaiseEvent` goes via `SequenceEventManager → OnSequenceEvent → runners`, while `StartDayClock`/`EndDay` call `GameManager` directly. `DayStart`/`DayEnd` conflate dream/scene vs work-clock boundaries; clock expiry today raises `DayEnd`, but `DayEnd` is also meant to be player-initiated (end of day → dream).

**Decision**: collapse to one bus. `ConversationEffectOperation` reduces from 4 to 2 (`SetFlag`, `RaiseEvent`). Introduce `WorkStart`/`WorkEnd` event types alongside the existing `DayStart`/`DayEnd`:

```
Constants.SequenceEventType: MarkerOverload, DayStart, DayEnd, WorkStart, WorkEnd, Default
Constants.SequenceEventChannels: + WorkStart (sequence_event_channel/work_start),
                                   WorkEnd   (sequence_event_channel/work_end)
SequenceEventManager.EventChannelMetadata.metadata: + WorkStart(0s), WorkEnd(0s)
Two new SequenceEventChannel assets (created via editor, not hand-edited per AGENTS.md)
```

- `ConversationManager.ApplyEffects`: keep only `SetFlag` and `RaiseEvent` (`Enum.TryParse` → `OnSequenceEvent`). Remove `GameManager` import/coupling from this class.
- `GameManager`: subscribes to the sequence-event bus **at `ConversationManager`, not `SequenceEventManager`**. This corrects the earlier claim that the two hook points are equivalent — they are not. `SequenceEventManager` only observes events raised through a `SequenceEventChannel` (`channel.OnSequenceEvent`), whereas `ConversationManager.ApplyEffects`' `RaiseEvent` case calls `ConversationManager.OnSequenceEvent(...)` **directly**, bypassing `SequenceEventManager` and its cooldown entirely. Since `WorkStart`/`DayEnd` are raised by *choice effects*, a `SequenceEventManager` subscription would never see them and the whole unification would silently no-op. `ConversationManager.OnSequenceEvent` is the single choke point reached by both paths, so it now raises a public `OnSequenceEventRaised` event that `GameManager` consumes. Trade-off accepted: effect-raised events skip the channel cooldown layer (as they already do today); the `0f` cooldown for `WorkStart`/`WorkEnd` makes this moot. Dispatch: `WorkStart → TriggerDayTimePassing()`, `DayEnd → EndDay()` (keeping `isEndDayDeferred` + `OnDayBlockingChoiceResolved` deferral unchanged). `OnSequenceEventRaised` is invoked **after** the runner evaluation loop so event-gated entries/parked waits are processed before a subscriber saves the day or tears down the scene. `StartDay()` still raises `DayStart` when switching to the desktop scene, unchanged. `TriggerEndOfDayTimePassing()` now raises `WorkEnd` instead of `DayEnd` — same clock-0 system trigger, just renamed. The `dayEndEventChannel` field becomes dead (nothing raises `DayEnd` through a channel anymore) and is removed.
- Clock semantics after: `WorkStart` (player choice) starts the clock; clock 0 raises `WorkEnd` (system) at the same trigger as today's `DayEnd` — work-gated waits/entries fire; `DayEnd` (player choice) ends the day → `SaveDay` → dream scene → next `StartDay() → DayStart` (desktop switch, unchanged). No double-fire: `WorkEnd` and `DayEnd` are distinct, `DayStart` and `WorkEnd` stay system-raised at their current points.
- Cooldown for `WorkStart`/`WorkEnd` is `0` like `DayStart`/`DayEnd` so `SpamProtectionCheck` never swallows a work transition.
- Ordering: `ConversationRunner.ResolveChoice` (L221) fires `ApplyEffects` while `pendingChoice` is still set. `RaiseEvent(DayEnd) → GameManager.EndDay()` therefore sees `HasUnresolvedDayBlockingChoice==true` and correctly defers — same as today, now via the bus. Verified by code inspection; task 7.x covers Play Mode verification.
- Editor impact: `DrawEffects`'s switch on `operation` collapses (no `StartDayClock`/`EndDay` cases without a value field). D4's dropdown automatically lists `WorkStart`/`WorkEnd`; no D4 code change beyond the enum growth.

*Alternatives considered:*
- Reuse `DayStart` for `WorkStart` and keep `DayEnd` for both clock and day end. Rejected: conflates entry re-evaluation — raising `DayStart` from a choice would re-fire morning entries mid-day; overloading `DayEnd` for both clock expiry and day end creates the double-fire/ordering ambiguity the user explicitly wants to avoid.
- Keep `StartDayClock`/`EndDay` operations for explicitness. Rejected: preserves the two-API split the user asked to melt; editor would need 3 operation kinds and authors must remember which API is which.

**Transient migration** (not shipped): a one-time editor script `Assets/Scripts/Editor/ConversationClockEffectMigration.cs` with a menu item `Custom Tools/Migrate Day Clock Effects` that scans `Assets/ScriptableObjects/ConversationGraphs/**/*.asset` via `AssetDatabase.FindAssets("t:ConversationGraph")`, loads each `ConversationGraph`, walks `nodes[].options[].effects[]`, and for every `operation == StartDayClock (2)` rewrites to `operation = RaiseEvent (1), stringValue = "WorkStart"` and for `operation == EndDay (3)` rewrites to `operation = RaiseEvent, stringValue = "DayEnd"`, wrapping each asset in `Undo.RecordObject` + `EditorUtility.SetDirty` + `AssetDatabase.SaveAssetIfDirty`. Expected patch: one node (`Phoebe/phoebe_day_start` option `2357f4dc-...` "enough — let's work"). The script is **deleted** before the change is considered done (tasks 8.5–8.6); no migration code ships. No `.asset` is hand-edited — the script edits via the API, satisfying the AGENTS.md guardrail.

## Risks / Trade-offs

- [`FrameAll()` frames stale bounds because UIElements layout hasn't run after `SetPosition`] → call after rebuild; if visually stale in testing, defer one frame with `schedule.Execute`. Manual `SetViewTransform` fallback documented in D3.
- [JsonUtility clone drops `Markable.markerType` references or `editorPosition`] → verify in the first implementation task; fall back to a hand-written deep clone (D5 alternative) before building paste on top of it.
- [Per-keystroke `RefreshPreview()` churn while typing a long message] → rebuild is a handful of Labels on one node; negligible. If measurable, throttle to `EndChangeCheck` commit points only (already the plan — IMGUI TextArea fires change checks per keystroke, which is exactly the "real time" the user asked for).
- [Legacy event strings with whitespace/case variants (`"daystart "`) no longer free-typable] → the appended raw entry keeps them visible and preserved; designers who want the fixed spelling pick the known type deliberately. No silent data loss.
- [Clipboard holding clones whose `MarkerType` instance IDs die after a domain reload] → clipboard is in-memory and cleared on window/target change; a domain reload clears static/instance editor state anyway. Acceptable for an authoring tool.
- [Undo granularity: cut/paste touching many nodes] → single `Undo.RecordObject` call per operation (the graph asset is one object), so one Ctrl+Z reverses the whole paste/cut.
- [D6: `DayEnd` via bus still defers while a blocking choice is pending] → preserved by `ResolveChoice` ordering (effects fire before `pendingChoice` clears). Risk is that a future change reorders `ResolveChoice`; mitigation is a Play Mode verification task that picks a `DayEnd` option while a blocking choice is pending and confirms deferral.
- [D6: `SpamProtectionCheck` swallows `WorkStart`/`WorkEnd`] → mitigation is explicit `0s` cooldown for the new types, verified in a task.
- [D6: transient migration script left behind] → tasks explicitly create, run, verify, then delete the script file and fail validation if it still exists.
- [D6: `.asset` guardrail violated by hand-editing YAML] → forbidden; migration must use `AssetDatabase`/`SetDirty` APIs only.

## Migration Plan

**Editor UX (items 1–5)**: no data migration. Existing `ConversationGraph` assets keep their format; event `stringValue` fields written by the new dropdown use exact enum names runtime already accepts. Rollback is reverting the two editor scripts.

**D6 unification**: one-time transient asset migration, not shipped. `DayStart` and `WorkEnd` trigger points are unchanged (desktop switch and clock 0), so no timing migration beyond renaming the clock-expiry type from `DayEnd` to `WorkEnd`:

1. Create `Assets/Scripts/Editor/ConversationClockEffectMigration.cs` (menu `Custom Tools/Migrate Day Clock Effects`).
2. Run it once in the editor (domain reload, invoke menu).
3. Verify `phoebe_day_start.asset` now shows `operation: 1, stringValue: WorkStart` for the "enough — let's work" option and no asset retains `operation: 2/3`.
4. Delete `ConversationClockEffectMigration.cs` (and its `.meta`) — tasks 8.5–8.6 enforce this; the change is not considered done while the file exists.
5. `GameManager` clock semantics (`WorkEnd` at clock 0, `DayStart` at `StartDay`) and `SequenceEventChannel` assets (`work_start`/`work_end`) are part of the shipped change; no further migration.

Rollback for D6 is reverting the runtime + constants commits; the patched `phoebe_day_start.asset` would need a second one-time revert (re-patched by hand in the editor, not by YAML edit) if rolling back after the migration has run.

## Open Questions

(none — all decisions above are resolvable from the codebase and the spec)
