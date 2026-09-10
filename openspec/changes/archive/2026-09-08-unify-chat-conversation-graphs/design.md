## Context

See `proposal.md` — Why. The constraints that shape this design:

- **Content is authored data, state is JSON.** The codebase already splits these: `GameTemplate` holds direct asset references, while `DayData` and the save file hold *names* because `JsonUtility` cannot serialize asset references or dictionaries. Every new type has to respect that split.
- **`JsonUtility` is the save serializer.** No polymorphism, no dictionaries, no `List<List<T>>`. New save types must be flat `[Serializable]` POCOs with public fields and lists.
- **Unity's asset serializer also rejects polymorphic lists.** A `List<BaseNode>` of derived node classes will not serialize in a ScriptableObject without `[SerializeReference]`, which carries undo and duplicate-reference pitfalls.
- **All content loads through `AddressableManager`** with synchronous `WaitForCompletion()`.
- **Playback currently lives inside the window.** `ChatLogController.RunBubbleSequenceBehaviour` is a coroutine on the log's own UI object, which is why a new sequence hard-cuts the running one and why nothing can survive the window closing.
- **Markers are keyed positionally.** `MarkerData.chatBubbleIndex` indexes into a log's message list, which is only stable because assignment content is static.
- **Editor code must not leak into runtime assemblies.** GraphView lives in `UnityEditor.Experimental.GraphView`; the runtime interpreter cannot reference it.

## Goals / Non-Goals

**Goals:**
- One content model, one authoring tool, one playback path, one save story for every chat log.
- A runtime data model that is independent of the editor framework, so the canvas can be replaced without touching gameplay.
- Playback that outlives windows, day boundaries, and save/load.
- Stable identity for content, so markers and saved cursors never drift.

**Non-Goals:**
- A general-purpose scripting or expression language for conditions; the vocabulary stays a fixed, enumerable set.
- Editor conveniences beyond authoring needs (minimap, blackboard, copy/paste, multi-graph diffing).
- Preserving existing dummy content or existing save files.
- Changing day-length semantics, marker scoring math, or the assignment/docket and roster systems beyond the two modified requirements.

## Decisions

### Data model

**D1 — Unify both log kinds onto graphs; drop the `ChatLog.messages` seed list.**
Assignments become a seed graph (linear chain of markable bubbles) plus optional event graphs; live chats become a seed graph plus event graphs containing choices. No `chatLogType` flag is introduced: the systems that genuinely care about the assignment/live distinction (docket classification, chat client roster) already key off `DayData`, and marker scoring keys off whether a bubble carries markables.
*Alternative considered:* keep `ChatLog.messages` for assignments and add graphs only for live chats. Rejected — it preserves two authoring tools, two render paths, and two save paths, and markable authoring would have to exist in both editors. With dummy data the migration cost of unifying is zero.

**D2 — Nodes are flat POCOs with a kind enum, not a polymorphic hierarchy.**
One `NodeData` class carries `nodeKind` plus the union of all fields (bubble payload, choice options, condition clauses, effects). Five node kinds and a modest field count make this cheap.
*Alternative considered:* `[SerializeReference] List<BaseNode>`. Rejected — managed references complicate Undo, can silently duplicate instances on copy/paste, and make the GraphView mapping layer harder to reason about. The flat union is boring, robust, and trivially inspectable.

**D3 — Every node and every choice option gets a GUID assigned at creation.**
Node GUIDs are the identity used by save state and markers. Choice *options* also get GUIDs so that edges leaving a choice stay attached to the right option when options are reordered in the editor. Edges are stored as `{ fromNodeGuid, fromOptionGuid?, toNodeGuid }` in a flat list on the graph, not as pointers inside nodes.

**D4 — `MarkerData` is rekeyed from bubble index to node GUID.**
A marker stores the log, the target node GUID, and the markable/span it covers. Resolution walks the graph asset by GUID. This removes the entire class of index-drift bugs and makes markers on dynamically played content work with no special casing.
*Alternative considered:* keep indexes and snapshot the rendered order into the save. Rejected — branching makes rendered order player-dependent, so any positional key is inherently fragile.

**D5 — Assets hold direct references; JSON holds strings.**
`ChatLog` gains a direct `List<ConversationGraph>` reference (authored asset → authored asset, exactly as `GameTemplate` already references `ChatLog` assets). Save data holds only GUID/name strings. Graph nodes are embedded in the graph asset rather than stored as sub-assets, so one graph is one file.

**D6 — Seed is a graph-level flag, rendered statically by the view.**
A graph marked `isSeed` has its bubbles drawn when the log window is created, with no delay and no typing indicator, and they are *not* appended to played history. Non-seed content only ever appears via playback.
*Alternative considered:* a runtime "play on init with zero timing" entry. Rejected — it turns static backstory into mutable runtime state and needs a first-load special case.

### Runtime

**D7 — Split simulation from presentation with a headless runner per log.**
A plain-C# `ConversationRunner` per chat log owns thread state, the entry queue, playback timing, and the pending choice. A singleton manager owns all runners, drives their timing, subscribes to event channels, and bridges to `SaveManager`. `ChatLogController` becomes a view: it renders seed + history, shows and hides drafts, and keeps its unread/notification behavior.
*Alternative considered:* keep playback in `ChatLogController` and special-case choices. Rejected — a pending-forever choice, cross-day parking, and content arriving at a closed window are all impossible if playback dies with the UI object. This split is also what removes the existing hard-cut bug where a new sequence stops the running one.

**D8 — Threads, parking, and one-active-thread-per-log.**
A thread is a walk through a graph from an entry. The runner holds at most one *playing* thread; parked threads (at a `Wait` or a `Choice`) are alive but do not occupy the playing slot. Newly eligible entries queue FIFO. While a choice is pending, nothing else plays in that log — queued entries wait and parked threads do not resume — but other logs are unaffected.

**D9 — Conditions and effects are enumerable data, not code.**
A condition is a list of clauses (`event`, `dayMin`/`dayMax`, `requiredFlags`) evaluated with AND semantics; entry nodes and wait nodes use the same clause type. An effect is a list of operations from a fixed set: set flag, raise event, start day clock, end day, unlock log.
*Alternative considered:* `UnityEvent` or delegates on nodes. Rejected — not JSON-serializable, not visually authorable, and it would hide gameplay behavior inside assets where it cannot be inspected or validated.

**D10 — Day progression becomes authored effects, with a global blocking gate.**
`Constants.ChatBubbleSequenceType` and `GameManager.TriggerChatBubbleSequence` are deleted; the Start Day / End Day button spawn goes with them. Day start and day end are choice options carrying `StartDayClock` / `EndDay` effects. The clock reaching zero still raises the `DayEnd` event exactly as today — what changes is that `EndDay` only happens as an effect. Because a day-blocking choice can be pending in a log other than the one carrying the day-end option, `EndDay` additionally consults the runner manager and defers while *any* day-blocking choice is unresolved, re-checking when one is answered.
*Alternative considered:* freeze the clock while a choice is pending. Rejected — it changes day length and the player-visible clock for no benefit; withholding the transition preserves today's semantics.

**D11 — Keep `SequenceEventChannel` as a dumb bus; delete the name decoding.**
Channels stay Addressable assets raised by `GameManager`/`MarkerManager` as today. `SequenceEventManager` becomes the bridge: on a raise, it asks every runner to evaluate its entries for that event type. `SequenceEventData.TryDecode`, the `event_sequence_...` naming convention, and the random variation picker are removed — variation is now expressed as several entries in one exclusive group. Per-event-type cooldown (currently `MarkerOverload` at 10s) is retained at the bridge level.

### Persistence

**D12 — History is the single render source; graphs are never rendered directly.**
Each posted bubble appends a record to that log's run-level history. The view renders seed bubbles followed by history. Unchosen branches cannot appear on reload because they were never appended — the branching-replay problem is solved by construction rather than by filtering.

**D13 — History stores identity, not content.**
A history record holds the node GUID and the graph it came from; message text, sender, and markables are resolved from the graph asset at render time. Pending choices store only the choice node GUID — the drafts are rebuilt from the asset, so option text can never drift between save and asset.
*Trade-off:* if an author edits a node's text after a save exists, the reloaded log shows the edited text. Accepted: the asset is the source of truth for *content*, the save is the source of truth for *what played*. If a played node is deleted from its graph, the record is skipped with an error logged — the same failure mode and handling as a marker whose target node disappears.
*Alternative considered:* store full `ChatBubble` copies in history. Rejected — duplicates authored data into every save, bloats the file, and allows save/asset drift.

**D14 — Chat state is run-level, not day-level.**
Conversation state must outlive days (a thread parked on day 5 resumes on day 6), so it lives in a new run-level section of the save file beside `days`: per-log history, per-log thread states, and the flag set. `DayData.bubbleSequenceNames`, `GameTemplate.sequencedChatLogEntries`, and `SaveManager.runtimeSequencedChatLogEntries` / `GetSequencedChatBubblesForChatLog` are removed. All new types are flat `[Serializable]` POCOs; flags are a `List<string>` rather than a set or dictionary. Loading tolerates a missing chat-state section by treating it as empty.

### Editor

**D15 — GraphView, with the canvas as a pure view over the data model.**
GraphView ships with Unity 6, so there is no third-party dependency to patch across editor versions, and the runtime data model stays framework-free POCOs — required anyway because graph assets load at runtime through `AddressableManager` while GraphView is editor-only. All GraphView code lives under `Assets/Scripts/Editor/` behind a thin data↔view mapping layer.
*Alternative considered:* xNode (MIT, free, canvas in hours). Rejected — upstream has been dormant since 2020, it is IMGUI-styled against a Unity 6 editor, and it makes nodes ScriptableObject sub-assets, coupling the runtime interpreter to a third-party serialization model. Hand-rolled IMGUI was rejected as the most work for the least result.

**D16 — One editor window: canvas plus a node inspector that absorbs `ChatLogEditor`.**
Selecting a node shows its full settings in an inspector pane, including the text-selection markable authoring flow ported from `ChatLogEditor` (select text in the message, pick a marker type, create markable, re-sync indexes). `ChatLogEditor` is deleted; `ChatLog` itself needs no custom window — its identity fields and graph list are edited in the default inspector. Graph edits write back to the data model and persist via `EditorUtility.SetDirty` + `AssetDatabase.SaveAssetIfDirty`, matching the existing editor convention. Editor validation warns on unreachable nodes, dangling edges, and day-blocking choices with no outgoing connection.

## Risks / Trade-offs

- **GraphView sits in an `Experimental` namespace and could change between Unity versions** → Isolate every GraphView touch point behind the data↔view mapping layer so an API change is contained to editor code and cannot reach gameplay.
- **The refactor spans many files at once and could leave the chat flow broken mid-change** → Sequence deletion last: land the data model, runner, save state, and choice UI additively while the old sequence system still runs, re-author a minimal vertical slice (one seed graph, one choice, one wait) as the first playable proof, and only then delete `ChatBubbleSequence` and friends.
- **Rekeying markers touches scoring and could regress silently** → Verify scoring in one session against both a seed-graph bubble and a bubble posted by playback, and confirm saved markers restore across a reload for each.
- **A day-blocking choice that is unreachable or unanswerable soft-locks day progression** → Author-side: editor validation flags day-blocking choices with no outgoing connection. Runtime-side: the blocking gate defers rather than discards, so answering always unblocks.
- **Deleting played nodes or graphs strands history records and markers** → Resolve-or-skip with an error logged, applied identically to history rendering and marker resolution, so a missing node degrades one bubble rather than the whole log.
- **Per-log blocking could feel unresponsive if many entries queue behind one pending choice** → This is the intended, spec'd behavior (a conversation waits for your reply). Queued entries are not lost, and other logs keep playing, so the player is never globally stuck.
- **Save shape change invalidates existing saves** → Accepted: all data is dummy. Fresh saves initialize from the template, and `LoadGame` treats a missing chat-state section as empty so an older file still loads without throwing.
- **Editor scope creep on the canvas** → Ship create / connect / disconnect / inspect / markable-authoring first. Minimap, search window, copy-paste, and grouping are deferred and do not block any requirement.

## Migration Plan

No data migration is required — existing chat content and saves are dummy and will be re-authored. The ordering below keeps the game runnable at every step:

1. Land the data model: node/condition/effect POCOs, `ConversationGraph` asset, `ChatLog` graph references, player `ChatUser` identity.
2. Land the runner and manager; rewire `ChatLogController` to render seed + history. Old sequence playback still works in parallel.
3. Land run-level chat save state and the `MarkerData` GUID rekey.
4. Land the draft/choice UI and day progression via effects; remove the Start/End Day button path.
5. Land the GraphView editor and re-author the dummy content as graphs.
6. Delete the old system: `ChatBubbleSequence`, `SequenceEventData` decoding, `ChatBubbleSequenceType`, `ChatLogEditor`, `DayData.bubbleSequenceNames`, `GameTemplate.sequencedChatLogEntries`.

**Rollback:** revert the change branch; step 6 is the only destructive one, so reverting before it restores the old system intact.

## Open Questions

- The player's chat username and display name — a story decision; the enum member and `ChatUser` asset name follow from it.
- Visual treatment of drafts (styling, and whether they sit inline at the end of the bubble list or in a dedicated strip above it).
- Whether exclusive groups are authored as a free-text group name or a per-graph group index.
- Whether the existing per-event cooldown values (notably `MarkerOverload` at 10s) stay as they are now that variation picking is gone.
