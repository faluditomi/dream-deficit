# chat-client Specification

## Purpose

Defines how the ChatClient window's roster of chat users is authored per day in the game template, persisted through the save pipeline, and resolved into the user list displayed at runtime.

## Requirements

### Requirement: Per-day active chat client user roster

The game template SHALL define, for each day, an active chat client user roster chosen from the game's known chat users. The roster SHALL be editable per day in the game template editor, with add and remove operations, and each change SHALL persist to the template asset automatically.

#### Scenario: Authoring a roster

- **WHEN** a designer adds a chat user to a day's active chat client user roster in the game template editor
- **THEN** that day's template data stores the selected user in the roster and the template asset is saved

#### Scenario: Removing a user from the roster

- **WHEN** a designer removes a chat user from a day's roster
- **THEN** that user is no longer part of that day's active chat client users and the template asset is saved

### Requirement: Runtime resolution of the active roster

When a day is loaded, the system SHALL resolve the day's active chat client user roster into chat user identities, preserving roster order. Entries that are empty or do not match a known chat user SHALL be skipped with an error logged, and SHALL NOT prevent the remaining entries from resolving.

#### Scenario: Resolving a valid roster

- **WHEN** the current day's roster contains known chat users
- **THEN** each entry resolves to its chat user identity in roster order

#### Scenario: Skipping an unresolvable entry

- **WHEN** the roster contains an entry that does not match any known chat user
- **THEN** that entry is skipped with an error logged, and the remaining entries still resolve

### Requirement: ChatClient window user list

The ChatClient window SHALL display exactly one user entry per chat user resolved from the current day's roster. Each entry SHALL show the user's profile picture, username, and the last message delivered to that user's chat log — the most recent bubble in the log's persisted played history, falling back to the log's last seed bubble when nothing has played — and SHALL own that user's chat log instance.

#### Scenario: Populating the user list

- **WHEN** a day whose roster contains two chat users is loaded
- **THEN** the ChatClient window displays exactly two user entries, each showing its user's profile picture, username, and last delivered chat log message

#### Scenario: Preview reflects the branch the player took

- **WHEN** the player's choices caused different content to play in a log
- **THEN** that user's entry preview shows the last message that actually played and no message from an unplayed branch

#### Scenario: Log with no played content

- **WHEN** nothing has played into a user's chat log
- **THEN** that user's entry preview shows the log's last seed bubble

#### Scenario: Log with no content at all

- **WHEN** a user's chat log has neither played content nor seed content
- **THEN** that user's entry preview is empty and no error is logged

#### Scenario: Empty roster

- **WHEN** the current day's roster is empty
- **THEN** the ChatClient window displays no user entries and logs no error

### Requirement: Roster persistence in save data

The active chat client user roster SHALL be part of the per-day save data. It SHALL survive save slot initialization from the template and JSON save/load round trips, and saves created before the roster existed SHALL remain loadable.

#### Scenario: New save initialized from template

- **WHEN** a new save slot initializes from a template whose days have authored rosters
- **THEN** each day's save data contains the same roster as the template

#### Scenario: Loading a legacy save

- **WHEN** a save created before the roster existed is loaded
- **THEN** the affected day's roster resolves as empty and loading proceeds without error
