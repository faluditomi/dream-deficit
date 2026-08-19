## Purpose

Defines how the day's assignment list distinguishes required from bonus chat logs, how bonus logs present as locked rewards, and how unlock state persists across saves.

## Requirements

### Requirement: Chat logs are marked required or bonus per day

Each day's assignment structure SHALL classify every active chat log as either required or bonus. The classification is authoring data set in the game template and applies to that day only, not to the log asset itself. Logs default to required.

#### Scenario: Bonus log is configured for a day

- **WHEN** an author marks a chat log as bonus in a day's assignment list
- **THEN** that log is classified bonus for that day while all other active logs remain required

#### Scenario: Same log used in different roles

- **WHEN** the same chat log asset is used in two different days, marked bonus in one and left required in the other
- **THEN** each day's classification is independent of the other

### Requirement: Bonus chat logs start locked

A bonus chat log SHALL appear locked in the assignment list when it has not yet been unlocked. Locked entries SHALL conceal the log's data behind the lock panel. Required logs SHALL never appear locked.

#### Scenario: Locked bonus entry is shown

- **WHEN** the day's assignment list is displayed and a bonus log has not been unlocked
- **THEN** the entry shows the lock covering its data and does not reveal which log it is

#### Scenario: Required entry is shown

- **WHEN** the day's assignment list is displayed for a required log
- **THEN** the entry shows the log's name normally with no lock

#### Scenario: Previously unlocked bonus entry is shown

- **WHEN** the day's assignment list is displayed for a bonus log that was unlocked earlier
- **THEN** the entry shows the log's data normally with no lock

### Requirement: Clicking a locked bonus log unlocks and opens it

Clicking a locked bonus entry SHALL unlock it as a free reward — no conditions, costs, or performance gates. The unlock SHALL play the reveal animation, record the unlock on the day's data, and open the chat log window without a second click. The unlock SHALL persist when the day is saved (saves occur at day end). Clicking an unlocked or required entry SHALL open the log directly.

#### Scenario: Player clicks a locked bonus entry

- **WHEN** the player clicks a locked bonus entry
- **THEN** the entry plays the unlock animation, reveals the log's data, and opens the chat log window

#### Scenario: Player clicks an already-unlocked entry

- **WHEN** the player clicks a bonus entry that is already unlocked
- **THEN** the chat log window opens directly without replaying the unlock animation

### Requirement: Unlock state persists across saves and reloads

Unlock state SHALL be persisted as part of the day's save data. A new game created from a template SHALL start with all bonus logs locked. A reloaded game SHALL restore the exact unlock state saved.

#### Scenario: Unlock survives save and reload

- **WHEN** the player unlocks a bonus log and the day is saved, then the game is reloaded
- **THEN** the bonus log remains unlocked and opens directly on click

#### Scenario: Fresh game starts locked

- **WHEN** a new game is created from a template
- **THEN** every bonus log in the day's assignment list starts locked

### Requirement: Locked logs do not leak content or notifications

A locked bonus log SHALL not surface notifications or message content before it is unlocked, and its chat window SHALL not be openable before unlock. Any sequenced message or sequence targeting a locked bonus log SHALL be dropped — the sequence handler SHALL return immediately without showing a badge, delivering, or queueing the message. Messages dropped while locked are not delivered after unlock. Once unlocked, the log behaves as a normal chat log.

#### Scenario: Message arrives for a locked log

- **WHEN** a sequenced message is delivered to a chat log that is still locked
- **THEN** no notification badge or content is surfaced on the assignment entry

#### Scenario: Sequence targeting a locked log is dropped

- **WHEN** a bubble sequence is started for a chat log that is still locked
- **THEN** the handler returns immediately, showing no badge, delivering no message, and queueing nothing

#### Scenario: Unlocked log receives messages normally

- **WHEN** a bonus log has been unlocked and a sequenced message is delivered to it
- **THEN** the entry surfaces notifications and opens its chat window as a normal log

### Requirement: Assignment data exposes each log's role and unlock status

The day's assignment data SHALL expose, for every active chat log, its resolved log reference, whether it is bonus, and whether it is unlocked, so downstream systems such as scoring can consume the classification.

#### Scenario: Downstream system reads a log's classification

- **WHEN** any system queries the day's assignment data for a specific chat log
- **THEN** the response includes whether the log is bonus and whether it is unlocked, alongside the log itself
