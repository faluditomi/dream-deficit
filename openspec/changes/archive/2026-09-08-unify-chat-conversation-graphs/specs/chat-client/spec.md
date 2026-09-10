## MODIFIED Requirements

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
