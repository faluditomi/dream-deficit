## Why

The capability specs have drifted from the shipped implementation. Conversation effects are now authored on bubble nodes and every effect carries a per-effect delay, but the specs still describe effects as choice-only. Separate drift exists in terminology (markers vs flags, flags vs signals), in the roster and unlock persistence details, and in behaviors that were never described (event latching, queued entries, fallbacks). Specs that describe behavior that does not exist mislead both designers and future agents.

## What Changes

- Document bubble-node effects and per-effect `delaySeconds` on every effect (choice options and bubbles), including the runtime queue, zero-delay synchronous semantics, and the fact that pending delayed effects are discarded on load/day change.
- Correct effect operations: effects are `SetSignal` and `RaiseEvent` only. Remove the nonexistent "unlock a chat log" effect and the "setting flags" wording (signals are run-scoped strings, separate from scoring flags).
- Rename marker/markable terminology to flag/flaggable to match the implementation and avoid colliding with the separate `DoubleMarker` scoring concept.
- Document how a chat log actually discovers its graphs (addressable name prefix), all five node kinds, all four condition kinds, entry scheduling (`isRepeatable`, exclusive groups, additive-first queue order), and the fact that queued-but-unstarted entries are not persisted.
- Document wait-node event latching and the busy-log pending-resume retry; broaden unread counting to "log not focused"; clarify that re-presented drafts follow history in the window.
- Clarify that choice option identities are authored, not runtime-enforced.
- Chat client: state that the per-day roster is a list of chat-user asset-name strings resolved through Addressables; empty/unresolvable entries are skipped silently; preview falls back to the last seed bubble also when the last played bubble cannot be resolved or is empty.
- Assignment docket: state that unlock is recorded in runtime `ChatLogManager` state and written to `DayData` on the next `SaveDay` (day start, work-clock expiry, or day end); document the direct-reveal fallback when the prefab has no `Unlock` trigger; clarify lock-panel concealment; note that new saves clone the template `DayData` (including `unlockedChatLogNames`); clarify dropped-while-locked content can still play on a later fresh occurrence of the same event; name the data-exposure accessors.
- Update the project context in `openspec/config.yaml` so artifact generation sees the current architecture (scenes, `ConversationRunner` location, save interfaces, effect model).

**No runtime code changes.** This is a documentation/spec reconciliation of already-shipped behavior.

## Capabilities

### New Capabilities
- None.

### Modified Capabilities
- `chat-conversation-graphs`: bubble-node effects, per-effect delays, corrected effect operations, flag terminology, graph discovery, node/condition kinds, entry scheduling and queue persistence, wait latching, unread counting, display composition, authored reply identity.
- `chat-client`: roster storage as asset-name strings resolved via Addressables, silent skipping of invalid entries, broadened preview fallback.
- `assignment-docket`: unlock persistence timing, reveal-animation fallback, lock concealment, fresh-save cloning semantics, dropped-content timing, data-exposure accessors.

## Impact

- Specs only: `openspec/specs/chat-conversation-graphs/spec.md`, `openspec/specs/chat-client/spec.md`, `openspec/specs/assignment-docket/spec.md`.
- Project context: `openspec/config.yaml`.
- No code, assets, prefabs, scenes, or save schema are touched.
- Non-goals: changing runtime behavior, adding the missing unlock effect, adding error logging for invalid roster entries, persisting the entry queue or pending delayed effects, or fixing the template's `unlockedChatLogNames` cloning quirk. Those are recorded as current-status facts, not as new requirements.
