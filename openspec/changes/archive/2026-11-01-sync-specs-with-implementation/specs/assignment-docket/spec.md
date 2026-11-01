## MODIFIED Requirements

### Requirement: Bonus chat logs start locked

A bonus chat log SHALL appear locked in the assignment list when it has not yet been unlocked. Locked entries SHALL present the lock panel over the entry; the entry's log name may still be assigned to the hidden name text while the panel covers it. Required logs SHALL never appear locked. A new save is initialized by cloning the template's per-day data, so "starts locked" holds when the template's per-day unlocked-log list is empty.

#### Scenario: Locked bonus entry is shown

- **WHEN** the day's assignment list is displayed and a bonus log has not been unlocked
- **THEN** the entry shows the lock panel covering its data and does not reveal which log it is

#### Scenario: Required entry is shown

- **WHEN** the day's assignment list is displayed for a required log
- **THEN** the entry shows the log's name normally with no lock

#### Scenario: Previously unlocked bonus entry is shown

- **WHEN** the day's assignment list is displayed for a bonus log that was unlocked earlier
- **THEN** the entry shows the log's data normally with no lock

### Requirement: Clicking a locked bonus log unlocks and opens it

Clicking a locked bonus entry SHALL unlock it as a free reward — no conditions, costs, or performance gates. The unlock SHALL record the log in the runtime `ChatLogManager` unlocked list and open the chat log window without a second click. That runtime list SHALL be written into the day's save data on the next `SaveDay`, which can happen at day start, work-clock expiry, or day end. The entry SHALL play its reveal animation when the prefab's animator provides the `Unlock` trigger; otherwise the lock panel SHALL be hidden directly. Clicking an unlocked or required entry SHALL open the log directly.

#### Scenario: Player clicks a locked bonus entry

- **WHEN** the player clicks a locked bonus entry
- **THEN** the entry reveals the log's data and opens the chat log window in the same interaction

#### Scenario: Reveal animation fallback

- **WHEN** the assignment entry prefab's animator has no `Unlock` trigger
- **THEN** the lock panel is hidden directly instead of playing an animation

#### Scenario: Player clicks an already-unlocked entry

- **WHEN** the player clicks a bonus entry that is already unlocked
- **THEN** the chat log window opens directly without replaying the unlock animation

### Requirement: Unlock state persists across saves and reloads

Unlock state SHALL be persisted as part of the day's save data. Unlocks recorded in runtime `ChatLogManager` state SHALL be copied into the day's data at the next `SaveDay`. A new game created from a template SHALL clone the template's per-day data, so every bonus log starts locked unless the template's per-day unlocked-log list already contains it. A reloaded game SHALL restore the exact unlock state saved.

#### Scenario: Unlock survives save and reload

- **WHEN** the player unlocks a bonus log and a save occurs, then the game is reloaded
- **THEN** the bonus log remains unlocked and opens directly on click

#### Scenario: Unlock before the next save

- **WHEN** the player unlocks a bonus log and no save has occurred yet
- **THEN** the log is unlocked for the running session, and the unlock is written to the day's data on the next `SaveDay`

#### Scenario: Fresh game starts locked

- **WHEN** a new game is created from a template whose per-day unlocked-log lists are empty
- **THEN** every bonus log in the day's assignment list starts locked

### Requirement: Locked logs do not leak content or notifications

A locked bonus log SHALL not surface notifications or message content before it is unlocked, and its chat window SHALL not be openable before unlock. Any graph entry, thread, or bubble targeting a locked bonus log SHALL be dropped — conversation playback for that log SHALL return immediately without showing a badge, delivering, or queueing the content, and no one-shot activation is recorded. Content dropped while locked is not queued or backfilled after unlock, but a later fresh occurrence of the same event after unlock can still activate the entry. A thread parked in a locked log SHALL NOT resume while the log remains locked. Once unlocked, the log behaves as a normal chat log.

#### Scenario: Message arrives for a locked log

- **WHEN** content is delivered to a chat log that is still locked
- **THEN** no notification badge or content is surfaced on the assignment entry

#### Scenario: Sequence targeting a locked log is dropped

- **WHEN** a graph entry becomes eligible for a chat log that is still locked
- **THEN** playback returns immediately, showing no badge, delivering no bubble, and queueing nothing

#### Scenario: Dropped content is not backfilled after unlock

- **WHEN** an event fires while a bonus log is locked and the player then unlocks it
- **THEN** the content dropped while locked does not play, and only a later fresh occurrence of that event can activate the entry

#### Scenario: Parked thread does not resume while locked

- **WHEN** a thread parked in a bonus log reaches its resume condition while that log is still locked
- **THEN** the thread does not resume and no content is delivered

#### Scenario: Unlocked log receives messages normally

- **WHEN** a bonus log has been unlocked and content is delivered to it
- **THEN** the entry surfaces notifications and opens its chat window as a normal log

### Requirement: Assignment data exposes each log's role and unlock status

The day's assignment data SHALL expose, for every active chat log that resolves, a resolved log reference, whether it is bonus, and whether it is unlocked, so downstream systems such as scoring can consume the classification. Entries whose log asset cannot be resolved SHALL be excluded from the resolved entry list. The day's data SHALL also expose the resolved entries, gradeable required assignments, and per-log bonus/unlocked/locked queries.

#### Scenario: Downstream system reads a log's classification

- **WHEN** any system queries the day's assignment data for a specific chat log
- **THEN** the response includes whether the log is bonus and whether it is unlocked, alongside the log itself

#### Scenario: Unresolvable log is excluded

- **WHEN** an assignment entry's chat log asset cannot be resolved
- **THEN** that entry is omitted from the resolved assignment data and the remaining entries remain available
