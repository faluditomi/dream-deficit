## ADDED Requirements

### Requirement: Conversation effects apply with optional delays

A conversation effect SHALL perform exactly one of two operations: setting a signal (`SetSignal`) or raising a sequence event (`RaiseEvent`). There is no unlock-log effect, and signals are run-scoped strings distinct from scoring flags. Effects are authored on choice options and on bubble nodes. A choice option's effects fire when the option is selected; a bubble node's effects fire when the bubble posts. An effect SHALL apply before its thread continues, unless the effect declares a delay.

Every effect MAY declare a non-negative delay in seconds. An effect with no delay (or a zero delay) SHALL apply synchronously at its trigger moment. An effect with a positive delay SHALL be queued and applied only after that delay has elapsed. Delayed effects with equal delays SHALL apply in their authored order. Pending delayed effects are runtime-only: they SHALL be discarded when a run is loaded and when the day changes, and SHALL NOT survive save/load.

Well-known sequence event types represent day/work progression: `WorkStart` requests the work clock to start and `DayEnd` requests the day to end and transition to the dream scene; `GameManager` SHALL observe those requests via the `SequenceEvent` bus and SHALL NOT be called directly from effects. `DayStart` (system raises when we switch to the desktop scene) and `WorkEnd` (system raises when the work clock reaches zero) SHALL be system-raised boundaries and SHALL NOT be authored as player-initiated effects. Day and work progression signals SHALL be authored as choice options carrying `RaiseEvent` effects with the well-known types rather than as dedicated day-signal operations.

#### Scenario: Zero-delay effect applies immediately

- **WHEN** a choice option or bubble carries a zero-delay effect
- **THEN** the effect applies synchronously at the moment the option is selected or the bubble posts

#### Scenario: Delayed effect applies after its delay

- **WHEN** an effect declares a positive delay and its trigger occurs
- **THEN** the effect does not apply immediately and applies once the delay has elapsed

#### Scenario: Pending delayed effect is discarded on load or day change

- **WHEN** a delayed effect is queued and the run is loaded or the day changes before the delay elapses
- **THEN** the pending effect is discarded and never applied

#### Scenario: Option effect sets a signal that unlocks later content

- **WHEN** the player selects an option whose effect sets a signal
- **THEN** the signal is recorded and any entry or wait requiring that signal can become eligible

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

#### Scenario: Delayed option effect applies after the delay

- **WHEN** the player selects an option whose effect declares a positive delay
- **THEN** the option's bubble posts and the thread continues first, and the effect applies once the delay has elapsed

### Requirement: Bubble nodes carry authored content, timing, and effects

Each bubble node SHALL carry the sender's chat identity, the message text, the pre-message delay, the typing indicator duration, any flaggable targets, and an optional list of effects. Playing a bubble node SHALL wait its delay, show the typing indicator for its duration while the log is visible, then post the message. Posted bubbles SHALL support highlighting, and a bubble carrying flaggable targets SHALL be a valid flag placement target. Effects declared on a bubble SHALL fire once when the message posts — not when the delay or typing phase begins — and each effect may carry its own delay. Seed-graph bubbles are rendered statically and are never played, so they SHALL NOT fire effects.

#### Scenario: Bubble plays with authored timing

- **WHEN** a bubble node with a delay and a typing duration is played in a visible log
- **THEN** the delay elapses, the typing indicator shows for its duration, and the message is then posted

#### Scenario: Flaggable bubble is a valid flag target

- **WHEN** a bubble node carrying flaggable targets is posted
- **THEN** the player can place flags over its text and those flags are scored against the authored targets

#### Scenario: Bubble effect fires when the message posts

- **WHEN** a bubble node with an effect is played through its delay and typing phases
- **THEN** the effect fires once when the message is posted, not before

#### Scenario: Seed bubble does not fire effects

- **WHEN** a seed graph contains a bubble node with effects
- **THEN** the bubble is displayed statically and no effect fires

### Requirement: Entry nodes gate graph content and schedule activation

Each non-seed graph SHALL begin its content through entry nodes. An entry SHALL declare the conditions under which it becomes eligible: a triggering sequence event, day-number bounds, and required signals. An entry SHALL become eligible only when every declared condition is satisfied. When several entries become eligible for the same event, additive entries SHALL all be queued and entries belonging to the same exclusive group SHALL resolve to a random single winner. An entry's exclusive group is its authored group id, or the node's own identity when the group id is empty. A repeatable entry SHALL be eligible every time its event fires; a non-repeatable entry SHALL activate at most once per event type per day, and its activation SHALL be recorded when it is enqueued even if it has not started playing. Queued entries are runtime-only and SHALL NOT be persisted. Additive winners are enqueued before exclusive winners.

#### Scenario: Day-gated entry does not fire early

- **WHEN** an entry requires day 6 and the current day is 5
- **THEN** the entry does not become eligible and none of its content plays

#### Scenario: Signal-gated entry fires only after the signal is set

- **WHEN** an entry requires a signal the player has not set
- **THEN** the entry remains ineligible
- **WHEN** that signal is later set and the entry's triggering event occurs
- **THEN** the entry becomes eligible

#### Scenario: Exclusive entries resolve to one

- **WHEN** two entries in the same exclusive group become eligible for the same event
- **THEN** exactly one of them is queued and the other is not consumed

#### Scenario: Additive entries all queue

- **WHEN** two additive entries become eligible for the same event
- **THEN** both are queued for that log

#### Scenario: Non-repeatable entry does not re-fire the same day

- **WHEN** a non-repeatable entry has already activated for an event type on the current day and that event fires again
- **THEN** the entry is not queued again until the next day

#### Scenario: Repeatable entry re-fires

- **WHEN** a repeatable entry's event fires again on the same day
- **THEN** the entry is queued again

### Requirement: Player replies are presented as drafted choice options

When a thread reaches a choice node, the log SHALL present each option to the player as a draft reply. Selecting an option SHALL post a bubble authored with that option's chat identity — normally the player's, showing that identity's username and profile picture — and SHALL continue the thread along that option's connection. The runtime SHALL NOT enforce that the authored identity is the player's. The text shown on a draft MAY differ from the message posted when the draft is selected. Once an option is selected, the remaining drafts SHALL be removed.

#### Scenario: Selecting a draft posts the authored bubble

- **WHEN** the player selects a draft reply
- **THEN** a bubble appears in the log under that option's authored identity carrying its posted message, and the thread continues down that option's branch

#### Scenario: Preview text differs from posted message

- **WHEN** an option is authored with preview text that differs from its posted message
- **THEN** the draft shows the preview text and the posted bubble shows the posted message

#### Scenario: Unselected drafts are removed

- **WHEN** the player selects one option at a choice
- **THEN** the drafts for the other options no longer appear in the log

#### Scenario: Authored reply bubble renders like any other bubble

- **WHEN** a reply bubble is posted
- **THEN** it displays its authored identity's profile picture and username and supports highlighting like any other bubble

### Requirement: Conversation state and log history persist across sessions

Every bubble posted to a log SHALL be appended to that log's persisted history in play order. On load, a log SHALL display its seed content, then its persisted history, then the re-presented drafts of any unanswered choice, and nothing else. Content from branches that were not played SHALL NOT appear. Parked thread positions, pending choices together with their options, one-shot entry activations, signals, and flags SHALL persist across saves and day transitions. Queued-but-unstarted entries and pending delayed effects are runtime-only and SHALL NOT be persisted; an entry whose activation was recorded while queued stays consumed for that event type for the rest of the day. An unanswered choice SHALL remain pending indefinitely and SHALL re-present the same drafts when the log is next displayed.

#### Scenario: Reload reproduces the log exactly

- **WHEN** the game is saved and reloaded
- **THEN** each log displays the same bubbles in the same order the player last saw

#### Scenario: Pending drafts follow history on display

- **WHEN** a log with an unanswered choice is reopened
- **THEN** the window shows seed content, then played history, then the pending draft options

#### Scenario: Unchosen branch is absent after reload

- **WHEN** the player selected one option at a choice and the game is reloaded
- **THEN** the log contains only the selected branch's content and none of the unselected branches' content

#### Scenario: Pending choice survives reload

- **WHEN** the game is saved while a choice is unanswered and later loaded
- **THEN** the same drafts are presented again and selecting one continues the thread normally

#### Scenario: Signals survive reload

- **WHEN** a signal was set by an earlier effect and the game is reloaded
- **THEN** entries or waits requiring that signal remain eligible

### Requirement: Conversation graphs are authored in a visual node editor

Designers SHALL author conversation graphs in a visual node editor that displays nodes and their connections on a canvas. The editor SHALL support creating each of the five node kinds — Bubble, Choice, Wait, Entry, and End — connecting and disconnecting nodes, and editing every setting a node carries, including authoring flaggable targets by selecting text within a bubble node's message and authoring effects (with per-effect delays) on bubble nodes and choice options. Edits SHALL persist to the graph asset.

#### Scenario: Authoring a branching conversation

- **WHEN** a designer adds a choice node with two options and connects each option to a different bubble node
- **THEN** the graph stores both branches and playing the choice follows the selected branch

#### Scenario: Authoring a flaggable from a text selection

- **WHEN** a designer selects text within a bubble node's message and creates a flaggable of a chosen flag type
- **THEN** the node stores that flaggable and the bubble becomes a valid flag target at runtime

#### Scenario: Authoring a bubble effect with a delay

- **WHEN** a designer adds an effect to a bubble node and gives it a delay
- **THEN** the graph stores the effect and its delay, and the effect fires that long after the bubble posts at runtime

#### Scenario: Edits persist to the asset

- **WHEN** a designer edits nodes or connections and the graph asset is saved
- **THEN** reopening the graph shows the same nodes, settings, and connections

### Requirement: Flags target bubbles by node identity

Flag placement, scoring, and restoration SHALL identify a targeted bubble by the identity of the graph node that produced it rather than by the bubble's position in a log's message list. A flag SHALL remain valid as other content plays into the same log before or after the targeted bubble.

#### Scenario: Flag stays valid as the log grows

- **WHEN** the player flags a bubble and further content later plays into the same log
- **THEN** the flag still resolves to the same bubble and its scoring is unchanged

#### Scenario: Flag on a seed bubble resolves after reload

- **WHEN** the player flags a bubble in a log's seed content and the game is reloaded
- **THEN** the flag is restored on the same bubble and text span

#### Scenario: Flag on a played graph bubble resolves after reload

- **WHEN** the player flags a bubble posted by a graph and the game is reloaded
- **THEN** the flag is restored on that bubble

## MODIFIED Requirements

### Requirement: Chat log content is authored as conversation graphs

Each chat log SHALL derive its content from the conversation graphs resolved for it. A log SHALL load conversation-graph assets and keep those whose asset name begins with the log's own asset name. A conversation graph SHALL consist of nodes with stable identities and directed connections between them. A graph MAY be marked as seed. Bubbles in a seed graph SHALL be present in the log as soon as the log is displayed, with no delay and no typing timing. Content from a non-seed graph SHALL NOT appear in a log until it has been played.

#### Scenario: Fresh log displays seed content immediately

- **WHEN** a log referencing a seed graph is displayed for the first time
- **THEN** the seed bubbles are visible immediately, with no typing indicators and no delays

#### Scenario: Non-seed content is absent until played

- **WHEN** a log references a non-seed graph whose entry has not become eligible
- **THEN** none of that graph's bubbles appear in the log

#### Scenario: Node identities survive authoring edits

- **WHEN** a designer adds, removes, or reconnects nodes in a graph
- **THEN** the identities of the remaining nodes are unchanged, so saved conversation state and flags referencing them still resolve

### Requirement: Threads park at wait nodes and resume across days

A wait node SHALL park its thread until that node's conditions are satisfied. A wait gated on a sequence event SHALL be satisfied when that event has already been raised on the current day, including when the raise happened before the thread reached the wait. If a satisfied wait cannot resume because its log is busy with another thread, the system SHALL remember it and resume it once the log is free; this pending-resume state is runtime-only. Parking SHALL survive day transitions, closing and reopening the log window, and save/load. A parked thread SHALL resume from the node it parked at and SHALL NOT replay content it has already posted.

#### Scenario: Conversation pauses at a day boundary

- **WHEN** a thread reaches a wait node requiring day 6 while the current day is 5
- **THEN** the thread parks and posts nothing further during day 5
- **WHEN** day 6 begins and the wait node's triggering event occurs
- **THEN** the thread resumes from the parked node

#### Scenario: Wait sees an event already raised today

- **WHEN** a wait requires an event that has already been raised on the current day, even if the thread reached the wait afterwards
- **THEN** the wait is satisfied and the thread resumes

#### Scenario: Satisfied wait resumes when the busy log becomes free

- **WHEN** a wait's conditions are satisfied while another thread is playing in the same log
- **THEN** the wait is remembered and resumes once the log becomes free

#### Scenario: Parked thread survives reload

- **WHEN** the game is saved while a thread is parked at a wait node and later loaded
- **THEN** the thread is still parked at that node and resumes when its conditions are met

### Requirement: Playback is headless with per-log concurrency

Content playback SHALL be driven per chat log independently of whether that log's window is open. A log SHALL play one thread at a time, and entries that become eligible while the log is busy SHALL queue and play in queue order (additive winners first, then one winner per exclusive group). A pending choice SHALL block its own log until it is answered and SHALL NOT block other logs. When a log is not focused — including when its window is closed or open but unfocused — its unread count SHALL increase for each posted bubble, counted through the instantiated log controller. Since a log's controller exists only once its window has been instantiated, unread counting requires that controller.

#### Scenario: Closed log still receives content

- **WHEN** an entry fires for a log whose window is closed
- **THEN** its bubbles are recorded, the log's unread count increases, and opening the window later shows them

#### Scenario: Second entry queues behind a playing thread

- **WHEN** an entry becomes eligible while another thread in the same log is playing
- **THEN** the new entry queues and plays after the current thread finishes or parks

#### Scenario: Pending choice blocks only its own log

- **WHEN** a choice is pending in one log
- **THEN** no other thread in that log plays, while other logs continue to play normally

#### Scenario: Unfocused open log counts as unread

- **WHEN** a bubble posts to a log whose window is open but not focused
- **THEN** that log's unread count increases

### Requirement: Node inspector fields describe themselves with tooltips

Every editable field in the conversation graph editor's node inspector SHALL display a tooltip explaining the field's purpose when hovered. This SHALL cover bubble metadata (chat user, delay length, typing indicator length), the message editor and flaggable authoring controls (flag type, current selection, anchor text, occurrence, index fields), choice settings (blocks day, option preview text, posted-by identity, posted message), effect rows (operation, value, and delay), bubble and choice effect headers, condition rows (kind and value), and entry scheduling (exclusive, exclusive group id, is repeatable). A tooltip SHALL describe what the field controls at runtime, not merely restate the field's label.

#### Scenario: Hovering a field shows its explanation

- **WHEN** a designer hovers the label or control of any editable inspector field
- **THEN** a tooltip appears describing what the field does at runtime

#### Scenario: Every editable field is covered

- **WHEN** a designer inspects a node of any kind (Entry, Bubble, Choice, Wait, End)
- **THEN** each editable field shown for that kind has a non-empty tooltip

### Requirement: Sequence event values are chosen from the known event types

Wherever the editor accepts a sequence event name — the value of an event condition on entry and wait nodes, and the value of a raise-event effect on choice options and bubble nodes — it SHALL offer a dropdown listing the known sequence event types instead of requiring free text entry. The value stored on the graph asset SHALL remain the event type's name, so runtime matching and parsing behavior is unchanged. An existing stored value that does not match any known event type SHALL remain visible and editable in the dropdown and SHALL NOT be silently discarded or rewritten until the designer chooses a known type.

#### Scenario: Event condition uses the dropdown

- **WHEN** a designer sets the value of an event condition on an entry or wait node
- **THEN** they choose from a dropdown of the known sequence event types and the node stores the chosen type's name

#### Scenario: Raise-event effect uses the dropdown

- **WHEN** a designer sets the value of a raise-event effect on a choice option or bubble node
- **THEN** they choose from a dropdown of the known sequence event types and the effect stores the chosen type's name

#### Scenario: Unknown legacy value is preserved and visible

- **WHEN** a node carries an event value that matches no known sequence event type
- **THEN** the dropdown shows that raw value as the current selection, and saving the graph without touching the field keeps the value unchanged

## REMOVED Requirements

### Requirement: Bubble nodes carry the full bubble settings

**Reason**: Replaced by "Bubble nodes carry authored content, timing, and effects", which adds bubble effects and per-effect delays and corrects flag terminology.

**Migration**: Authors keep the same bubble data (chat user, message, delay, typing duration, flaggables); the node additionally exposes an effects list.

### Requirement: Entry nodes gate graph content on conditions

**Reason**: Replaced by "Entry nodes gate graph content and schedule activation", which names all condition kinds, documents repeatable/exclusive scheduling and queue ordering, and corrects signal terminology.

**Migration**: Existing entry nodes keep their authored conditions, exclusive flags, group ids, and repeatable flags; no data migration is required.

### Requirement: Player replies are presented as drafted choices

**Reason**: Replaced by "Player replies are presented as drafted choice options", which clarifies that reply identity is authored rather than runtime-enforced.

**Migration**: Existing choice options keep their authored posted bubbles and identities.

### Requirement: Choice options apply effects, including day progression

**Reason**: Replaced by "Conversation effects apply with optional delays", which covers both choice options and bubble nodes, corrects the supported operations (no unlock-log effect), and documents zero-delay versus delayed application.

**Migration**: Existing choice effects keep their operation and value; the new delay field defaults to zero, preserving prior behavior.

### Requirement: Conversation state and log history persist

**Reason**: Replaced by "Conversation state and log history persist across sessions", which adds display composition, pending drafts, one-shot activations, and the runtime-only status of queued entries and delayed effects.

**Migration**: No data migration; the save format is unchanged.

### Requirement: Graphs are authored in a visual node editor

**Reason**: Replaced by "Conversation graphs are authored in a visual node editor", which lists all five node kinds and documents authoring bubble effects with delays.

**Migration**: Existing graphs open unchanged in the editor.

### Requirement: Markers target bubbles by node identity

**Reason**: Replaced by "Flags target bubbles by node identity" to match the implemented flag/flaggable naming and avoid colliding with the separate DoubleMarker scoring concept.

**Migration**: No data migration; node identities are unchanged and existing placed flags continue to resolve.
