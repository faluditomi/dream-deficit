# chat-conversation-graphs Specification

## Purpose

Defines how chat log content is authored as condition-gated conversation graphs and how that content plays, branches on player replies, parks across days, and persists so a log always appears exactly as the player left it.

## Requirements

### Requirement: Chat log content is authored as conversation graphs

Each chat log SHALL derive its content from the conversation graphs it references. A conversation graph SHALL consist of nodes with stable identities and directed connections between them. A graph MAY be marked as seed. Bubbles in a seed graph SHALL be present in the log as soon as the log is displayed, with no delay and no typing timing. Content from a non-seed graph SHALL NOT appear in a log until it has been played.

#### Scenario: Fresh log displays seed content immediately

- **WHEN** a log referencing a seed graph is displayed for the first time
- **THEN** the seed bubbles are visible immediately, with no typing indicators and no delays

#### Scenario: Non-seed content is absent until played

- **WHEN** a log references a non-seed graph whose entry has not become eligible
- **THEN** none of that graph's bubbles appear in the log

#### Scenario: Node identities survive authoring edits

- **WHEN** a designer adds, removes, or reconnects nodes in a graph
- **THEN** the identities of the remaining nodes are unchanged, so saved conversation state and markers referencing them still resolve

### Requirement: Bubble nodes carry the full bubble settings

Each bubble node SHALL carry the sender's chat identity, the message text, the pre-message delay, the typing indicator duration, and any markable targets. Playing a bubble node SHALL wait its delay, show the typing indicator for its duration while the log is visible, then post the message. Posted bubbles SHALL support highlighting, and a bubble carrying markable targets SHALL be a valid marker placement target.

#### Scenario: Bubble plays with authored timing

- **WHEN** a bubble node with a delay and a typing duration is played in a visible log
- **THEN** the delay elapses, the typing indicator shows for its duration, and the message is then posted

#### Scenario: Markable bubble is a valid marker target

- **WHEN** a bubble node carrying markable targets is posted
- **THEN** the player can place markers over its text and those markers are scored against the authored targets

### Requirement: Entry nodes gate graph content on conditions

Each non-seed graph SHALL begin its content through entry nodes. An entry SHALL declare the conditions under which it becomes eligible: a triggering event, day-number bounds, and required flags. An entry SHALL become eligible only when every declared condition is satisfied. When several entries become eligible for the same event, additive entries SHALL all be queued and entries belonging to the same exclusive group SHALL resolve to a single winner.

#### Scenario: Day-gated entry does not fire early

- **WHEN** an entry requires day 6 and the current day is 5
- **THEN** the entry does not become eligible and none of its content plays

#### Scenario: Flag-gated entry fires only after the flag is set

- **WHEN** an entry requires a flag the player has not set
- **THEN** the entry remains ineligible
- **WHEN** that flag is later set and the entry's triggering event occurs
- **THEN** the entry becomes eligible

#### Scenario: Exclusive entries resolve to one

- **WHEN** two entries in the same exclusive group become eligible for the same event
- **THEN** exactly one of them is queued

#### Scenario: Additive entries all queue

- **WHEN** two additive entries become eligible for the same event
- **THEN** both are queued for that log

### Requirement: Player replies are presented as drafted choices

When a thread reaches a choice node, the log SHALL present each option to the player as a draft reply. Selecting an option SHALL post a bubble authored under the player's own chat identity, showing the player's username and profile picture, and SHALL continue the thread along that option's connection. The text shown on a draft MAY differ from the message posted when the draft is selected. Once an option is selected, the remaining drafts SHALL be removed.

#### Scenario: Selecting a draft posts the player's bubble

- **WHEN** the player selects a draft reply
- **THEN** a bubble appears in the log under the player's identity carrying that option's posted message, and the thread continues down that option's branch

#### Scenario: Preview text differs from posted message

- **WHEN** an option is authored with preview text that differs from its posted message
- **THEN** the draft shows the preview text and the posted bubble shows the posted message

#### Scenario: Unselected drafts are removed

- **WHEN** the player selects one option at a choice
- **THEN** the drafts for the other options no longer appear in the log

#### Scenario: Player bubble renders like any other bubble

- **WHEN** a player bubble is posted
- **THEN** it displays the player's profile picture and username and supports highlighting like any other bubble

### Requirement: Choice options apply effects, including day progression

Each choice option MAY declare effects applied when it is selected: setting flags, raising sequence events, and unlocking a chat log. Well-known sequence event types represent day/work progression: `WorkStart` requests the work clock to start and `DayEnd` requests the day to end and transition to the dream scene; `GameManager` SHALL observe those requests via the `SequenceEvent` bus and SHALL NOT be called directly from choice effects. `DayStart` (system raises when we switch to the desktop scene) and `WorkEnd` (system raises when the work clock reaches zero) SHALL be system-raised boundaries and SHALL NOT be authored as player-initiated choice effects, just like now — their trigger points are unchanged, only the clock-expiry type is renamed from `DayEnd` to `WorkEnd`. Effects SHALL be applied before the thread continues. A choice MAY be marked day-blocking, and while a day-blocking choice is unresolved the day SHALL NOT end. Day and work progression signals SHALL be authored as choice options carrying `RaiseEvent` effects with the well-known types rather than as dedicated day-signal operations.

#### Scenario: Option effect sets a flag that unlocks later content

- **WHEN** the player selects an option whose effect sets a flag
- **THEN** the flag is recorded and any entry requiring that flag can become eligible

#### Scenario: Day-blocking choice holds the day

- **WHEN** the day clock reaches its end while a day-blocking choice is unresolved
- **THEN** the day does not end until the player answers that choice

#### Scenario: Starting the day is a player reply

- **WHEN** the player selects the work-start option (a choice option whose effect raises `WorkStart` — the work portion of the day)
- **THEN** the work clock begins and no separate day-signal control is used

#### Scenario: Ending the day is a player reply

- **WHEN** the player selects the day-end option (a choice option whose effect raises `DayEnd`)
- **THEN** the day ends and saves through the normal day-end flow, deferring if a day-blocking choice is still unresolved until it is answered

#### Scenario: Work clock expiry raises WorkEnd, not DayEnd

- **WHEN** the work clock reaches zero (the same system trigger as today)
- **THEN** the system raises `WorkEnd` (renamed from `DayEnd` at this trigger) and any wait or entry gated on `WorkEnd` re-evaluates, while `DayEnd` is not raised until the player requests it

#### Scenario: DayStart is system-raised at day scene start

- **WHEN** the day scene begins (we switch to the desktop scene and `StartDay` runs — just like now)
- **THEN** the system raises `DayStart` and entries gated on `DayStart` become eligible; `DayStart` is not authored as a player choice effect and its trigger point is unchanged

#### Scenario: Option unlocks a bonus log

- **WHEN** the player selects an option whose effect unlocks a chat log
- **THEN** that log becomes unlocked and behaves as a normal chat log

### Requirement: Threads park at wait nodes and resume across days

A wait node SHALL park its thread until that node's conditions are satisfied. Parking SHALL survive day transitions, closing and reopening the log window, and save/load. A parked thread SHALL resume from the node it parked at and SHALL NOT replay content it has already posted.

#### Scenario: Conversation pauses at a day boundary

- **WHEN** a thread reaches a wait node requiring day 6 while the current day is 5
- **THEN** the thread parks and posts nothing further during day 5
- **WHEN** day 6 begins and the wait node's triggering event occurs
- **THEN** the thread resumes from the parked node

#### Scenario: Parked thread survives reload

- **WHEN** the game is saved while a thread is parked at a wait node and later loaded
- **THEN** the thread is still parked at that node and resumes when its conditions are met

### Requirement: Playback is headless with per-log concurrency

Content playback SHALL be driven per chat log independently of whether that log's window is open. A log SHALL play one thread at a time, and entries that become eligible while the log is busy SHALL queue and play in eligibility order. A pending choice SHALL block its own log until it is answered and SHALL NOT block other logs. When a log's window is closed, played bubbles SHALL still be recorded and that log's unread count SHALL increase.

#### Scenario: Closed log still receives content

- **WHEN** an entry fires for a log whose window is closed
- **THEN** its bubbles are recorded, the log's unread count increases, and opening the window later shows them

#### Scenario: Second entry queues behind a playing thread

- **WHEN** an entry becomes eligible while another thread in the same log is playing
- **THEN** the new entry queues and plays after the current thread finishes or parks

#### Scenario: Pending choice blocks only its own log

- **WHEN** a choice is pending in one log
- **THEN** no other thread in that log plays, while other logs continue to play normally

### Requirement: Conversation state and log history persist

Every bubble posted to a log SHALL be appended to that log's persisted history in play order. On load, a log SHALL display its seed content followed by its persisted history and nothing else, and content from branches that were not played SHALL NOT appear. Parked thread positions, pending choices together with their options, and flags SHALL persist across saves and day transitions. An unanswered choice SHALL remain pending indefinitely and SHALL re-present the same drafts when the log is next displayed.

#### Scenario: Reload reproduces the log exactly

- **WHEN** the game is saved and reloaded
- **THEN** each log displays the same bubbles in the same order the player last saw

#### Scenario: Unchosen branch is absent after reload

- **WHEN** the player selected one option at a choice and the game is reloaded
- **THEN** the log contains only the selected branch's content and none of the unselected branches' content

#### Scenario: Pending choice survives reload

- **WHEN** the game is saved while a choice is unanswered and later loaded
- **THEN** the same drafts are presented again and selecting one continues the thread normally

#### Scenario: Flags survive reload

- **WHEN** a flag was set by an earlier choice and the game is reloaded
- **THEN** entries requiring that flag remain eligible

### Requirement: Markers target bubbles by node identity

Marker placement, scoring, and restoration SHALL identify a targeted bubble by the identity of the graph node that produced it rather than by the bubble's position in a log's message list. A marker SHALL remain valid as other content plays into the same log before or after the targeted bubble.

#### Scenario: Marker stays valid as the log grows

- **WHEN** the player marks a bubble and further content later plays into the same log
- **THEN** the marker still resolves to the same bubble and its scoring is unchanged

#### Scenario: Marker on a seed bubble resolves after reload

- **WHEN** the player marks a bubble in a log's seed content and the game is reloaded
- **THEN** the marker is restored on the same bubble and text span

#### Scenario: Marker on a played graph bubble resolves after reload

- **WHEN** the player marks a bubble posted by a graph and the game is reloaded
- **THEN** the marker is restored on that bubble

### Requirement: Graphs are authored in a visual node editor

Designers SHALL author conversation graphs in a visual node editor that displays nodes and their connections on a canvas. The editor SHALL support creating each node type, connecting and disconnecting nodes, and editing every setting a node carries, including authoring markable targets by selecting text within a bubble node's message. Edits SHALL persist to the graph asset.

#### Scenario: Authoring a branching conversation

- **WHEN** a designer adds a choice node with two options and connects each option to a different bubble node
- **THEN** the graph stores both branches and playing the choice follows the selected branch

#### Scenario: Authoring a markable from a text selection

- **WHEN** a designer selects text within a bubble node's message and creates a markable of a chosen marker type
- **THEN** the node stores that markable and the bubble becomes a valid marker target at runtime

#### Scenario: Edits persist to the asset

- **WHEN** a designer edits nodes or connections and the graph asset is saved
- **THEN** reopening the graph shows the same nodes, settings, and connections

### Requirement: Node inspector fields describe themselves with tooltips

Every editable field in the conversation graph editor's node inspector SHALL display a tooltip explaining the field's purpose when hovered. This SHALL cover bubble metadata (chat user, delay length, typing flag length), the message editor and markable authoring controls (marker type, current selection, anchor text, occurrence, index fields), choice settings (blocks day, option preview text, posted-by identity, posted message), effect rows (operation and value), condition rows (kind and value), and entry scheduling (exclusive, exclusive group id, is repeatable). A tooltip SHALL describe what the field controls at runtime, not merely restate the field's label.

#### Scenario: Hovering a field shows its explanation

- **WHEN** a designer hovers the label or control of any editable inspector field
- **THEN** a tooltip appears describing what the field does at runtime

#### Scenario: Every editable field is covered

- **WHEN** a designer inspects a node of any kind (Entry, Bubble, Choice, Wait)
- **THEN** each editable field shown for that kind has a non-empty tooltip

### Requirement: Canvas node previews update as node data is edited

When an inspector edit changes data displayed on a node's canvas view — a bubble node's message preview, a choice node's option labels or day-blocking title, a wait or entry node's condition summary, or an entry node's exclusive/repeatable markers — the canvas SHALL update that node's preview immediately, without requiring a graph layout, a node rebuild, or reopening the graph. Edits to data not displayed on the node SHALL NOT force a canvas rebuild.

#### Scenario: Message edit updates the node preview

- **WHEN** a designer edits a bubble node's message in the inspector
- **THEN** the node's message preview on the canvas reflects the new text as the edit is committed

#### Scenario: Choice option edit updates the port label

- **WHEN** a designer edits a choice option's preview text in the inspector
- **THEN** the corresponding output port's label on the canvas updates immediately

#### Scenario: Condition edit updates the summary

- **WHEN** a designer adds, removes, or changes a condition on an entry or wait node
- **THEN** the node's condition summary on the canvas updates immediately

### Requirement: Graph layout frames the laid-out content in the viewport

When the editor arranges a graph's nodes — via the manual layout action or the automatic default layout applied when overlapping nodes are detected on open — it SHALL adjust the viewport afterwards so the arranged nodes are centered and fully visible at a readable zoom, regardless of where the viewport was before.

#### Scenario: Manual layout brings the graph into view

- **WHEN** a designer runs the layout action while the viewport is panned or zoomed away from the nodes
- **THEN** the viewport moves and zooms so the whole laid-out graph is centered and visible

#### Scenario: Default layout on open frames the graph

- **WHEN** a graph with overlapping node positions is opened and the default layout runs
- **THEN** the resulting graph is centered and fully visible in the viewport

### Requirement: Sequence event values are chosen from the known event types

Wherever the editor accepts a sequence event name — the value of an event condition on entry and wait nodes, and the value of a raise-event effect on choice options — it SHALL offer a dropdown listing the known sequence event types instead of requiring free text entry. The value stored on the graph asset SHALL remain the event type's name, so runtime matching and parsing behavior is unchanged. An existing stored value that does not match any known event type SHALL remain visible and editable in the dropdown and SHALL NOT be silently discarded or rewritten until the designer chooses a known type.

#### Scenario: Event condition uses the dropdown

- **WHEN** a designer sets the value of an event condition on an entry or wait node
- **THEN** they choose from a dropdown of the known sequence event types and the node stores the chosen type's name

#### Scenario: Raise-event effect uses the dropdown

- **WHEN** a designer sets the value of a raise-event effect on a choice option
- **THEN** they choose from a dropdown of the known sequence event types and the effect stores the chosen type's name

#### Scenario: Unknown legacy value is preserved and visible

- **WHEN** a node carries an event value that matches no known sequence event type
- **THEN** the dropdown shows that raw value as the current selection, and saving the graph without touching the field keeps the value unchanged

### Requirement: Nodes can be copied, cut, pasted, and duplicated on the canvas

The graph canvas SHALL support copy, cut, paste, and duplicate for selected nodes, via both the context menu and the standard keyboard shortcuts. Duplicating or pasting SHALL create new nodes that carry copies of all the source nodes' data with fresh node identities and fresh choice-option identities, so they never alias the originals. Connections between copied nodes SHALL be preserved in the copy; connections from copied nodes to nodes outside the selection SHALL NOT be. Copied nodes SHALL be placed offset from their sources so both remain visible and selectable. Cut SHALL remove the source nodes and their connections. All of these operations SHALL be undoable and SHALL persist to the graph asset.

#### Scenario: Duplicating a node creates an independent copy

- **WHEN** a designer duplicates a selected bubble node
- **THEN** a new node appears next to it with the same bubble data but a different identity, and editing either node does not affect the other

#### Scenario: Duplicating a connected group keeps internal connections

- **WHEN** a designer selects a choice node and the bubble nodes its options connect to, and duplicates the selection
- **THEN** the copies preserve the connections between the copied choice options and the copied bubbles, and no new connections to the original nodes are created

#### Scenario: Copy and paste across the canvas

- **WHEN** a designer copies a selection of nodes, then pastes
- **THEN** pasted copies with fresh identities appear offset from the originals, with internal connections preserved

#### Scenario: Cut removes the sources

- **WHEN** a designer cuts a selection of nodes and pastes
- **THEN** the original nodes and their connections are removed from the graph and the pasted copies take their place

#### Scenario: Operations are undoable and persisted

- **WHEN** a designer duplicates nodes and then undoes the operation
- **THEN** the copies are removed, and when the operation is not undone the copies are saved with the graph asset
