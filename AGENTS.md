# Dream Deficit - AI Agent Guide

## Project Overview
**Dream Deficit** is a 2D Unity game where players monitor and analyze chat logs across a day-based progression system. The core gameplay involves reading messages, identifying key content, and placing timed flags on text passages to score accuracy.

## Spec-Driven Development (OpenSpec)
This project uses OpenSpec (spec-driven development). Planning artifacts live in `openspec/`; the `openspec` CLI (installed globally via npm as `@fission-ai/openspec`) is the source of truth.
- **Workflow cycle:** `/opsx-propose` (plan) → `/opsx-apply` (implement, ticking `tasks.md`) → `/opsx-archive` (merge deltas into `openspec/specs/`)
- Supporting commands: `/opsx-explore`, `/opsx-update`, `/opsx-sync`
- Never edit `openspec/specs/` directly — sync/archive merge delta specs
- Validate after every artifact: `openspec validate <change> --json`; keep changes small and focused
- Project context for artifact generation is in `openspec/config.yaml`

## Tech Stack
- **Unity Version:** 6000.3.8f1 (Unity 6)
- **Render Pipeline:** URP (Universal Render Pipeline) 17.3.0
- **UI System:** Unity UI (uGUI) + TextMeshPro
- **Input:** Unity New Input System 1.18.0
- **Asset Loading:** Addressables 2.8.1
- **Key Packages:** 2D Animation, 2D Tilemap Extras, Timeline, Visual Scripting

## Critical Rules for Agents
1. **NEVER edit .asset, .prefab, .unity, or other Unity serialization files directly** - they are binary/YAML and manual edits corrupt them
2. **NEVER regenerate .csproj files** - Unity handles these automatically
3. **All prefabs and data are loaded via Addressables** - use `AddressableManager.Instance.RetrieveAddressable<T>(address)` not `Resources.Load`
4. **Managers use Singleton pattern** - access via `GameManager.Instance`, `SaveManager.Instance`, etc.
5. **Save system uses JSON serialization** via `JsonUtility.ToJson/FromJson` - fields must be serializable. State is split into two scopes: **day-scoped** (`DayData`, via `IDaySavable`/`IDayLoadable`) and **run-scoped** (`RunData`, via `IRunSavable`/`IRunLoadable`). Never put run-level state in `DayData` or day-level state in `RunData`
6. **Never scene-scan for savables/loadables** - registration is automatic through `SaveLoadBehaviour.OnEnable/OnDisable`. Do not use `FindObjectsByType` to build a save registry
7. **Save methods must copy, never alias** - `SaveToDayData`/`SaveToRunData` must assign a *copy* of the owned collection (e.g. `dayData.flagData = placedFlags.ToList()`). Assigning the live list makes save data and runtime state the same object, so the next load's `Clear()` wipes both
8. **Only persistent singletons may own day/run slices** - a slice owner that is not alive at save time silently drops its data. Transient windows may be `IDayLoadable` (view-only) but must never be `IDaySavable`/`IRunSavable`
9. **AddressableManager uses synchronous loading** (`WaitForCompletion()`) - this is intentional but flagged as TODO for async migration

## Architecture

### Script Organization (`Assets/Scripts/`)
```
Scripts/
├── MonoBehaviours/
│   ├── Managers/          # Core singletons (GameManager, SaveManager, FlagManager, AddressableManager,
│   │                      #   HighlightManager, ConversationManager, ChatLogManager, SequenceEventManager, UIFocusManager)
│   ├── Component Controllers/  # Prefab/UI controllers (ChatBubbleController, ChatLogController, ChatClientController, FlagIndicatorController, etc.)
│   │   └── Abstracts/     # BaseWindowController (draggable window base), DreamController
│   ├── Handlers/          # Input/event handlers (DragHandler, HighlightHandler, PointerHandler, TopBarHandler)
│   ├── SaveLoadBehaviour.cs      # Auto-registers/unregisters IDay*/IRun* implementers (base of Singleton<T> and BaseWindowController)
│   ├── NoDragScrollRect.cs       # Custom scroll rect behavior
│   └── Singleton.cs              # Generic singleton base class; derives from SaveLoadBehaviour
├── Plain Old/             # POCOs, data classes, and interfaces
│   ├── Interfaces/        # IHighlightable, IDaySavable, IDayLoadable, IRunSavable, IRunLoadable
│   ├── ConversationRunner.cs     # PLAIN C# class (NOT a MonoBehaviour) - conversation playback engine
│   └── (data classes)     # ChatBubble, DayData, RunData, FlagData, FlagType, Flaggable, ChatLogEntry, Constants,
│                          #   plus conversation data (ConversationNodeData, ConversationGraphEnums,
│                          #   ConversationConditionClause, ConversationEffectData, ConversationChatState)
├── Scriptable Objects/    # ScriptableObject definitions
│   ├── ChatLog.cs                # Collection of chat messages (links to related ConversationGraphs)
│   ├── ChatUser.cs               # Chat user definition
│   ├── ConversationGraph.cs      # Node/edge graph of conversation flow (Entry/Bubble/Choice/Wait/End)
│   ├── SequenceEventChannel.cs   # Broadcasts SequenceEventType events to ConversationManager
│   ├── GameTemplate.cs           # Game run template (tutorial, full game); also defines DayDataEntry
│   └── SaveSlot.cs               # Save slot: links to a GameTemplate, holds RunData + day entries
├── Editor/              # Custom editor scripts (ConversationGraphEditor, ConversationNodeView, GameTemplateEditor)
└── Dev Hacks/           # Development utilities (FrameRateCap)
```

### Key Systems

#### Day Progression System
- Game runs in days (Day 1, Day 2, ...)
- `GameManager` controls day time: configurable start/end hours, day length in seconds
- `TriggerDayTimePassing()` starts the day clock; `EndDay()` saves and advances
- The current day number lives in run scope (`RunData.currentDayNumber`, surfaced as `GameManager.currentDayNumber`), NOT in `DayData` or `SaveSlot`. `EndOfDaybehaviour` increments it *before* calling `SaveDay`, so the persisted run data points at the day about to be played
- Days have associated `DayData` containing flags, active chat logs, unlock state, etc.

#### Chat System
- `ChatLog` (ScriptableObject) contains ordered `ChatBubble` messages
- `ChatLogController` renders played bubbles from its `ConversationRunner` history, with typing indicators
- Chat logs are Addressable assets loaded at runtime. The Addressable key is the ChatLog asset **`name`** (e.g. `chat_log/test_assignment`), which is also what `ChatLogEntry.logName` stores — `ChatLog.logName` is display text and is NOT an Addressable key

#### Conversation Graph System
- `ConversationGraph` (ScriptableObject): nodes (`Entry`, `Bubble`, `Choice`, `Wait`, `End`) + a flat edge list; assets live under `Assets/ScriptableObjects/ConversationGraphs/<character>/`
- `ChatLog.Graphs` resolves related graphs via Addressables (`conversation_graph/` prefix, label + name match)
- `ConversationManager` (singleton) owns one `ConversationRunner` per chat log
- `ConversationRunner` (plain C#, not a MonoBehaviour): entry evaluation by `SequenceEvent`, thread walk, parking at wait/choice nodes, day-blocking choices, history/activation state for saves
- Authoring: `Custom Tools/Conversation Graph Editor` — right-click canvas to add nodes / Layout Graph / Validate Graph. Port rules: the input port is `Multi` (fan-in allowed), output and choice-option ports are `Single` (the runtime follows only the first outgoing edge — fan-out is unsupported). `ConversationGraphView.GetCompatiblePorts` is overridden because Unity 6 ships no default port adapter; ports are created with `typeof(object)`.

#### Flag System
- `FlagManager` handles keyboard-driven flag placement on chat text
- Players hold keys to activate flag types, then select text ranges
- Flags scored by accuracy (overlap with `Flaggable` targets, excess penalty)
- `FlagType` (plain serializable data class) defines flag properties including keycode
- Flags displayed as on-screen indicators (`FlagIndicatorController`); conversation narrative state is a signal
- Placed flags are recorded **per day** into `DayData.flagData` via `FlagManager.SaveToDayData`, so a run keeps a history of what was flagged on each day

#### Save System
Two explicit scopes, each with its own save/load interface pair:

| Scope | Data class | Interfaces | Stored as |
|---|---|---|---|
| Day | `DayData` | `IDaySavable` / `IDayLoadable` | one entry per day in `SaveFileData.days` |
| Run | `RunData` | `IRunSavable` / `IRunLoadable` | `SaveFileData.runSaveData` (single object) |

- **Registration is automatic.** `SaveLoadBehaviour.OnEnable`/`OnDisable` subscribe/unsubscribe whatever `IDay*`/`IRun*` interfaces the component implements. `Singleton<T>` and `BaseWindowController` both derive from it, so managers and windows get registration for free. Never scene-scan with `FindObjectsByType`
- **Late hydration.** `AddDayLoadable`/`AddRunLoadable` immediately call `LoadFromDayData`/`LoadFromRunData` on the newcomer if that scope is already loaded, so lazily-instantiated windows still receive data
- **Slice ownership.** Only persistent singletons own slices: `FlagManager` → `DayData.flagData`, `ChatLogManager` → `DayData.unlockedChatLogNames`, `ConversationManager` → `RunData.chatLogs`/`chatSignals`, `GameManager` → `RunData.currentDayNumber`. Transient windows are `IDayLoadable`-only (view-only)
- **Copy, never alias** — save methods assign a copy of the owned collection, otherwise the next load's `Clear()` wipes both
- **Load order:** `LoadGame()` restores run scope (so runners hold history before windows are built) → `LoadDay()` restores day scope → `ConversationManager.OnDayChanged()` → DayStart event
- Persistence is JSON in `Application.persistentDataPath` (`save_{slotName}.json`) via `JsonUtility`. `SaveSlot`/`GameTemplate` are authoring assets; the JSON file is the runtime store
- New saves initialize from template; subsequent saves merge runtime data

### Data Flow
```
GameTemplate → SaveSlot (initialization) → JSON save file (runtime)
                ↓
        day scope                        run scope
   DayData per day                   RunData (currentDayNumber,
   (flags, active logs,              chatSignals, chatLogs)
    unlocks, flag types)                    ↓
        ↓                          IRunSavable → SaveToRunData
  IDaySavable → SaveToDayData       IRunLoadable ← LoadFromRunData
  IDayLoadable ← LoadFromDayData
```

## Current State
- **Scenes:** `Assets/Scenes/v1_prototye.unity` (desktop/play scene — note the typo in the filename), `Assets/Scenes/tutorial_demo.unity`, `Assets/Dreams/TestDream/dream_scene_day_1.unity`
- **Core systems implemented:** Day progression, chat display, flag placement, save/load, conversation graph playback
- **TODOs in codebase:**
  - Save slot picker/creator menu (currently brute-force assigned)
  - Async Addressable loading (currently synchronous)
  - Safeguard against multiple flags per flaggable
  - Game loading triggered from menu (currently auto-starts)

## Asset Locations
- **Prefabs:** `Assets/Prefabs/` (loaded via Addressables)
- **ScriptableObjects:** `Assets/ScriptableObjects/` (chat logs, sequences, users, save slots, conversation graphs — per-character folders under `ConversationGraphs/`)
- **Sprites:** `Assets/Sprites/`
- **Animations:** `Assets/Animations/`
- **Materials:** `Assets/Materials/`
- **Addressables paths defined in:** `Constants.AddressablePaths`

## Testing/Development
- Run in Unity Editor Play Mode to test
- Addressable assets must be built or in play mode with "Use Existing Build" disabled
- Save data persists in `%APPDATA%/../LocalLow/[CompanyName]/[ProductName]/`
- Editor tooling: "Custom Tools/Conversation Graph Editor" (graph authoring) and "Custom Tools/Game Template Editor" (template inspection)

## Naming Conventions
- **Managers:** Singleton pattern, `ClassName.Instance` access
- **Controllers:** Attached to prefabs, manage specific UI/game objects
- **Handlers:** Event/input processing, attached dynamically or via prefab
- **ScriptableObjects:** Data containers, created via Create menu or Addressables
- **POCOs:** Plain data classes in `Plain Old/` folder
