## 1. Audit current behavior

- [x] 1.1 Audit conversation-graph runtime, data, and editor behavior against the existing spec
- [x] 1.2 Audit chat-client roster and preview behavior against the existing spec
- [x] 1.3 Audit assignment-docket classification, unlock, and persistence behavior against the existing spec

## 2. Update capability specs

- [x] 2.1 Write the `chat-conversation-graphs` delta (bubble effects, effect delays, corrected operations/terminology, scheduling, wait latching, persistence caveats)
- [x] 2.2 Write the `chat-client` delta (asset-name roster, silent skipping, broadened preview fallback)
- [x] 2.3 Write the `assignment-docket` delta (runtime unlock state, save timing, reveal fallback, lock coverage, cloning semantics, data accessors)

## 3. Update project context

- [x] 3.1 Refresh `openspec/config.yaml` project context (scenes, `ConversationRunner` location, save interfaces, effect model)

## 4. Validate

- [x] 4.1 Run `openspec validate sync-specs-with-implementation` and fix all errors
- [x] 4.2 Confirm no code, asset, prefab, scene, or save-schema files are part of this change
