## 1. Plain Old — conversation data model

- [x] 1.1 Add conversation enums: node kind (Bubble, Choice, Wait, Entry, End), condition clause kind (event, day range, required flags), and effect operation (set flag, raise event, start day clock, end day, unlock log)
- [x] 1.2 Add flat `NodeData` POCO carrying node kind, GUID, and the union of all node fields — serialization-safe: no polymorphic base-class list, since neither the Unity asset serializer nor `JsonUtility` supports it (design D2)
- [x] 1.3 Add `ChoiceOptionData` POCO with its own GUID, preview text, posted `ChatBubble` payload, effect list, and day-blocking flag — option GUIDs keep choice edges attached when options are reordered (design D3)
- [x] 1.4 Add `EdgeData` POCO (`fromNodeGuid`, optional `fromOptionGuid`, `toNodeGuid`) and condition-clause evaluation with AND semantics shared by entry and wait nodes
- [x] 1.5 Add run-level chat state POCOs: played-history record (graph + node GUID only, no content copy), thread state (graph GUID, node GUID, parked kind), and the flag list — serialization-safe: flat `[Serializable]` public fields, `List<string>` for flags, no dictionaries, must round-trip through `JsonUtility` (design D13/D14)
- [x] 1.6 Rekey `MarkerData` from `chatBubbleIndex` to target node GUID plus graph reference — serialization-safe: old save fields simply go unread; marker restore must tolerate a missing node by skipping with an error logged
- [x] 1.7 Update `Constants`: add the player `ChatUser` enum member (display name is an open story question — use a placeholder), the `ConversationGraph` addressable prefix, and the draft-choice prefab path

## 2. Scriptable Objects — graph asset and log rewiring

- [x] 2.1 Create `ConversationGraph` ScriptableObject: node list, edge list, and `isSeed` flag, plus a GUID-assignment helper used only by editor code
- [x] 2.2 Add a direct `List<ConversationGraph>` reference to `ChatLog` (assets hold direct references; only JSON save data holds strings — design D5)
- [x] 2.3 Register `ConversationGraph` assets as Addressables under the new prefix via the Addressables Groups window — do not hand-edit `.asset` or group files
- [x] 2.4 Create the player `ChatUser` asset with username and profile picture, and register it under the existing `chat_user/` prefix via the Addressables Groups window

## 3. Managers — runner, orchestration, save, day progression

- [x] 3.1 Add `ConversationRunner` (plain C#, one per chat log): thread walk with per-bubble delay and typing timing, FIFO entry queue, single playing thread, parking at wait and choice nodes, and immediate drop for locked bonus logs
- [x] 3.2 Add a conversation manager singleton that owns and ticks all runners, routes events to them, and exposes whether any day-blocking choice is unresolved
- [x] 3.3 Rewire `SequenceEventManager` into an event bridge: on a channel raise, ask each runner to evaluate entries for that event type; keep per-event-type cooldown; stop using name decoding and random variation picking (design D11)
- [x] 3.4 Add the run-level chat state section to the save file in `SaveManager` (per-log history, thread states, flags) — serialization-safe: `JsonUtility`-friendly flat POCOs only, and loading must treat a missing section as empty so older saves still load
- [x] 3.5 Wire save/load of chat state into the existing `SaveDay` / `LoadDay` / `InitializeFromTemplate` paths so a fresh save starts with empty history and no flags
- [x] 3.6 Update `GameManager` so day start and day end happen only as choice effects, and `EndDay` defers while any day-blocking choice is unresolved, re-checking when one is answered (design D10)
- [x] 3.7 Update `MarkerManager` and `HighlightManager` to resolve markable targets by node GUID through the graph asset instead of by position in a log's message list

## 4. Controllers — view layer

- [x] 4.1 Rework `ChatLogController` to render seed graph bubbles (no timing) followed by persisted played history, resolving each record through its graph asset; remove sequence playback and the flatten-everything path
- [x] 4.2 Extend `ChatBubbleController` to render the player's identity and to support a draft presentation state
- [x] 4.3 Add a draft-choice controller and prefab that presents a pending choice's options, and on selection posts the player's bubble, applies effects, and notifies the runner to continue down the chosen edge
- [x] 4.4 Update `ChatClientController` so each roster entry previews the last delivered message from played history, falling back to the last seed bubble, and empty with no error when the log has no content at all
- [x] 4.5 Preserve unread counting and notification behavior for logs whose window is closed while content plays

## 5. Handlers

- [x] 5.1 Update `HighlightHandler` setup to take the graph-resolved bubble and its node GUID so marker placement records GUID-keyed targets
- [x] 5.2 Confirm `PointerHandler`, `DragHandler`, and `TopBarHandler` need no changes for draft rendering inside the bubble container, and adjust only if drafts break drag or scroll behavior

## 6. Editor — GraphView authoring tool

- [x] 6.1 Add the graph editor window shell: GraphView canvas, toolbar, and a thin data-to-view mapping layer that isolates every `UnityEditor.Experimental.GraphView` touch point (design D15)
- [x] 6.2 Add node views for all five kinds, with choice options rendered as output ports keyed by option GUID
- [x] 6.3 Support creating nodes, connecting and disconnecting edges, and persisting edits via `EditorUtility.SetDirty` plus `AssetDatabase.SaveAssetIfDirty`
- [x] 6.4 Add the node inspector pane covering bubble settings, choice options (preview text, posted message, effects, day-blocking), and entry/wait condition clauses
- [x] 6.5 Port the markable authoring flow from `ChatLogEditor` into the inspector: select message text, pick a marker type, create markable, re-sync indexes
- [x] 6.6 Add seed-graph marking and `ChatLog` graph-list authoring
- [x] 6.7 Add editor validation warnings for unreachable nodes, dangling edges, and day-blocking choices with no outgoing connection

## 7. Content re-authoring and verification of the new path

- [x] 7.1 Author a vertical-slice graph set: one seed graph, one event entry with a choice whose options diverge, and one wait node crossing a day boundary
- [x] 7.2 Re-author the supervisor day-start and day-end flows as day-blocking choice options carrying start-day-clock and end-day effects
- [x] 7.3 Re-author the existing assignment logs as seed graphs with their markables intact
- [x] 7.4 Verify marker placement and scoring against both a seed-graph bubble and a bubble posted by playback, and confirm both restore correctly after a reload

## 8. Removal of the old system (last, so the game stays runnable until content is re-authored)

- [x] 8.1 Delete `ChatBubbleSequence` and its assets, and remove its addressable prefix usage
- [x] 8.2 Delete `SequenceEventData.TryDecode` and the `event_sequence_...` naming convention, reducing `SequenceEventChannel` to a plain event bus
- [x] 8.3 Delete `Constants.ChatBubbleSequenceType`, `GameManager.TriggerChatBubbleSequence`, and the day-signal button spawn path
- [x] 8.4 Delete `ChatLogEditor`
- [x] 8.5 Remove `DayData.bubbleSequenceNames` and `DayData.GetSequences`, `GameTemplate.sequencedChatLogEntries` with its helpers, and `ChatLog.messages` — serialization-safe: `JsonUtility` ignores unknown fields, so older saves load and new saves omit them; verify a legacy save still loads without throwing
- [x] 8.6 Remove `SaveManager.runtimeSequencedChatLogEntries`, `MergeSequencedChatLogEntries`, and `GetSequencedChatBubblesForChatLog`
- [x] 8.7 Clean dead `Constants` entries (retired prefixes, labels, and prefab paths)

## 9. End-to-end verification

- [x] 9.1 Play mode: seed content renders immediately on first open; an entry fires on its event; a choice posts the player's bubble and follows the chosen branch; a wait node parks on day 5 and resumes on day 6
- [x] 9.2 Save/load: each log reproduces exactly what the player last saw, unchosen branches are absent, a pending choice re-presents the same drafts, and flags persist
- [x] 9.3 Locked bonus log: an eligible entry is dropped with no badge and no queueing, a parked thread does not resume while locked, and the log behaves normally once unlocked
- [x] 9.4 Closed window: content is still recorded, the unread count increases, and the content appears when the window is opened
- [x] 9.5 Concurrency: one thread plays per log, queued entries play in eligibility order, exclusive entries resolve to one while additive entries all queue, and a pending choice blocks only its own log
- [x] 9.6 Run `openspec validate unify-chat-conversation-graphs --json` and confirm it passes
