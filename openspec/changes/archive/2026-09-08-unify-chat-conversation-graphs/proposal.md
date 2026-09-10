## Why

Chat content today is a flat `ChatBubbleSequence` list resolved by a string-encoded naming convention (`event_sequence_{event}_{log}_{type}_{variation}`), which cannot express player replies, branching, day-gated continuation, or content that parks across days. The player is a passive observer: the only interactive chat elements are hardcoded Start Day / End Day buttons spawned from `GameManager` with a self-described "shit code" TODO asking for a central chat-response solution. Branching dialogue is that solution, and because all existing chat data is dummy content, replacing the sequence system outright is cheaper than maintaining two parallel content models.

## What Changes

- Add a `ConversationGraph` asset holding content nodes with stable GUIDs, directed connections, and entry points. Node types: `Bubble`, `Choice`, `Wait`, `Entry`, `End`.
- Every bubble node carries the full existing `ChatBubble` settings (chat user, message, delay, typing length, markables), so no authored per-bubble behavior is lost.
- A graph may be marked as **seed**: its bubbles render immediately when a log is created, with no timing. Non-seed content only ever appears after being played.
- Entry nodes gate on conditions (sequence event type, day number, flags). Entries replace the string-decoded scheduling and the random variation picker; each entry is marked exclusive (one wins) or additive (all queue).
- The player becomes a regular `ChatUser` identity with their own profile picture and username.
- `Choice` nodes present reply **drafts** in the chat window. Each option carries preview text, a separately authored posted bubble, effects, and a next node. Preview text and posted message MAY differ. Picking an option instantiates the player's own bubble in the log.
- Options carry **effects**: set flag, raise event, start the day clock, end the day, unlock a chat log.
- **BREAKING** — The Start Day and End Day signal buttons are removed. Day progression becomes day-blocking choice options whose effects start the clock / end the day. `Constants.ChatBubbleSequenceType` and `GameManager.TriggerChatBubbleSequence` are deleted.
- Add a headless per-log conversation runner that owns playback, parking, and queueing. `ChatLogController` stops playing sequences and renders only seed bubbles plus persisted played history. Playback no longer depends on a window being open.
- Per-log concurrency: one active thread at a time, later eligible entries queue, and a pending choice blocks its log until answered.
- Threads park at `Wait` and `Choice` nodes and resume across day boundaries and save/load. Choices that are never answered remain pending forever and re-render as drafts on load.
- Add run-level chat save state: played history per log, parked thread cursors, pending choices, and flags. A loaded log SHALL look exactly as the player last saw it, and unchosen branches SHALL NOT appear.
- **BREAKING** — Remove `ChatBubbleSequence`, `SequenceEventData` string decoding, `DayData.bubbleSequenceNames`, and `GameTemplate.sequencedChatLogEntries`. `SequenceEventChannel` survives as a plain event bus with no name decoding.
- **BREAKING** — `MarkerData` is rekeyed from a bubble index into a log's message list to a bubble node GUID, removing index-drift bugs permanently.
- Replace `ChatLogEditor` with a GraphView-based graph editor window whose node inspector absorbs bubble settings and the text-selection markable authoring flow.

### Non-goals

- Free-text player input; replies remain authored choices.
- New group-chat window types; logs keep today's one-window-per-log shape.
- Migrating or preserving existing dummy chat content; it will be re-authored in graphs.
- Async Addressable loading, the save slot picker menu, and the multi-marker safeguard (pre-existing TODOs, untouched).
- Graph editor niceties beyond what authoring needs (blackboard, minimap are optional).

## Capabilities

### New Capabilities
- `chat-conversation-graphs`: Authoring and runtime of chat log content as condition-gated node graphs — node types, entry conditions, player reply choices with preview/posted text and effects, day-blocking, cross-day parking, headless playback, per-log concurrency, run-level persistence, and GUID-based marker targeting.

### Modified Capabilities
- `assignment-docket`: The locked-log content suppression requirement is expressed in terms of `ChatBubbleSequence` delivery. It changes to graph entry and thread targeting — a locked bonus log drops graph entries and threads rather than sequences, with the same no-badge, no-delivery, no-queueing outcome.
- `chat-client`: "Last message of the user's chat log" becomes ambiguous once content is choice-dependent. It changes to the last message actually delivered to that log (from played history, falling back to seed), so the roster preview reflects the player's own conversation state.

## Impact

**New code**
- `Assets/Scripts/Scriptable Objects/ConversationGraph.cs` and node/condition/effect data classes under `Assets/Scripts/Plain Old/`
- Conversation runner (headless, per-log) plus a manager that owns and ticks runners
- Draft/choice UI controller for the chat window
- `Assets/Scripts/Editor/` GraphView editor window, node views, and node inspector

**Modified code**
- `ChatLog` (identity plus graph references, seed handling), `ChatLogController` (render from seed + history), `ChatClientController` (last delivered message), `ChatBubbleController` (draft state, player identity)
- `SaveManager` and the save file shape (run-level chat state), `DayData` (drop sequence names), `GameTemplate` (drop sequenced chat log entries)
- `MarkerData`, `MarkerManager`, `HighlightHandler` (GUID-keyed targets), `GameManager` (day progression driven by choice effects), `SequenceEventChannel` and `SequenceEventManager` (event bus, no decoding), `Constants` (new prefixes/enums, remove dead ones)

**Assets and Addressables**
- New `ConversationGraph` assets and addressable prefix; `ChatBubbleSequence` assets retired. Graph assets load via `AddressableManager` like all other content.
- New player `ChatUser` asset and a new `Constants.ChatUser` enum member.
- Start/End Day button prefab usage removed from the chat flow.

**Save compatibility**
- Save file shape changes. Existing saves are dummy data and are not required to load; fresh saves initialize from the template.
