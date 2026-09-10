## MODIFIED Requirements

### Requirement: Locked logs do not leak content or notifications

A locked bonus log SHALL not surface notifications or message content before it is unlocked, and its chat window SHALL not be openable before unlock. Any graph entry, thread, or bubble targeting a locked bonus log SHALL be dropped — conversation playback for that log SHALL return immediately without showing a badge, delivering, or queueing the content. Content dropped while locked is not delivered after unlock. A thread parked in a locked log SHALL NOT resume while the log remains locked. Once unlocked, the log behaves as a normal chat log.

#### Scenario: Message arrives for a locked log

- **WHEN** content is delivered to a chat log that is still locked
- **THEN** no notification badge or content is surfaced on the assignment entry

#### Scenario: Sequence targeting a locked log is dropped

- **WHEN** a graph entry becomes eligible for a chat log that is still locked
- **THEN** playback returns immediately, showing no badge, delivering no bubble, and queueing nothing

#### Scenario: Parked thread does not resume while locked

- **WHEN** a thread parked in a bonus log reaches its resume condition while that log is still locked
- **THEN** the thread does not resume and no content is delivered

#### Scenario: Unlocked log receives messages normally

- **WHEN** a bonus log has been unlocked and content is delivered to it
- **THEN** the entry surfaces notifications and opens its chat window as a normal log
