# chat-conversation-graphs delta — conversation-graph-editor-ux

## MODIFIED Requirements

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

## ADDED Requirements

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
