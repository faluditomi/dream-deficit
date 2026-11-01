## Context

See `proposal.md` — Why. The implementation already exists; this change is a spec reconciliation. The main constraint is OpenSpec's delta semantics: a `MODIFIED` requirement replaces the whole block and archive refuses to silently drop any existing scenario name. Scenarios also cannot be renamed through a delta header.

## Goals / Non-Goals

**Goals:**
- Make every capability spec describe behavior that matches the shipped game, including the recently added bubble effects and per-effect delays.
- Keep the spec set internally consistent in terminology (flags/flaggables vs signals vs DoubleMarker).

**Non-Goals:**
- Changing runtime behavior or data formats.
- Backfilling features the specs previously promised but the code never implemented (for example an unlock-log effect).
- Reworking the capability boundaries or splitting specs.

## Decisions

### Decision: Use REMOVED + ADDED for requirements whose scenario names must change

`MODIFIED` requires preserving every existing scenario name, and delta headers cannot rename a scenario. Where the old scenario names encode stale terminology ("markable", "marker", "flag-gated" for signals) or a nonexistent behavior ("Option unlocks a bonus log"), the requirement is removed and re-added under a corrected requirement name with corrected scenario names. Requirements whose scenario names remain accurate are updated with `MODIFIED`.

*Alternative considered:* keep the old scenario names and only edit their bodies. Rejected because names carrying "marker" would continue to imply the separate `DoubleMarker` scoring concept and would keep a false unlock-effect scenario in the spec.

### Decision: Record current behavior, even where it is a known limitation

Queued entries are not persisted, delayed effects are discarded on load/day change, invalid roster entries are skipped silently, and new saves clone the template's per-day data (including its unlocked-log list). These are documented as current status rather than turned into new requirements or silent omissions, so future changes have an accurate baseline.

*Alternative considered:* write aspirational requirements (persist queued entries, log roster errors). Rejected because the requested scope is spec/implementation alignment, not new behavior.

### Decision: Keep derived glossary-like facts out of specs

Class names and accessor names are only used where the spec already commits to an externally consumed contract (for example the resolved assignment entry accessors). Internal method names (such as the delayed-effect queue) are described by behavior, not by identifier.

## Risks / Trade-offs

- **Requirement identity churn** → seven requirements are removed and re-added. Archive keeps the spec readable but history now shows removals; the proposal and this design explain why.
- **Spec/implementation drift can recur** → the root cause was specs not being updated when runtime behavior changed. Mitigation: this change also refreshes `openspec/config.yaml` project context so future artifact generation starts from the current architecture.
- **Documenting limitations as behavior** → readers may treat them as intended. Each is worded as current status ("runtime-only", "skipped silently") so a future change can supersede it explicitly.
