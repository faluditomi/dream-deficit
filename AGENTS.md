# Dream Deficit - AI Agent Guide

## Project Overview
**Dream Deficit** is a 2D Unity game where players monitor and analyze chat logs across a day-based progression system. The core gameplay involves reading messages, identifying key content, and placing timed markers on text passages to score accuracy.

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
5. **Save system uses JSON serialization** via `JsonUtility.ToJson/FromJson` - fields must be serializable
6. **AddressableManager uses synchronous loading** (`WaitForCompletion()`) - this is intentional but flagged as TODO for async migration

## Architecture

### Script Organization (`Assets/Scripts/`)
```
Scripts/
├── MonoBehaviours/
│   ├── Managers/          # Core singletons (GameManager, SaveManager, MarkerManager, AddressableManager,
│   │                      #   HighlightManager, ConversationManager, ChatLogManager, SequenceEventManager, UIFocusManager)
│   │                      # ConversationRunner also lives here but is a PLAIN C# class, not a MonoBehaviour
│   ├── Component Controllers/  # Prefab/UI controllers (ChatBubbleController, ChatLogController, ChatClientController, MarkerFlagController, etc.)
│   ├── Handlers/          # Input/event handlers (DragHandler, HighlightHandler, PointerHandler, TopBarHandler)
│   ├── BaseWindowController.cs   # Base class for draggable window UI
│   ├── NoDragScrollRect.cs       # Custom scroll rect behavior
│   └── Singleton.cs              # Generic singleton base class
├── Plain Old/             # POCOs, data classes, and interfaces
│   ├── Interfaces/        # IHighlightable, ILoadable, ISavable
│   └── (data classes)     # ChatBubble, DayData, MarkerData, MarkerType, Constants, plus conversation data
│                          #   (ConversationNodeData/EdgeData, ConversationGraphEnums, ConversationConditionClause, ConversationEffectData)
├── Scriptable Objects/    # ScriptableObject definitions
│   ├── ChatBubbleSequence.cs     # Sequence of chat bubbles for dialogue
│   ├── ChatLog.cs                # Collection of chat messages (links to related ConversationGraphs)
│   ├── ChatUser.cs               # Chat user definition
│   ├── ConversationGraph.cs      # Node/edge graph of conversation flow (Entry/Bubble/Choice/Wait/End)
│   ├── GameTemplate.cs           # Game run template (tutorial, full game)
│   └── SaveSlot.cs               # Save slot with day entries
├── Editor/              # Custom editor scripts (ConversationGraphEditor, ConversationNodeView, GameTemplateEditor)
└── Dev Hacks/           # Development utilities (FrameRateCap)
```

### Key Systems

#### Day Progression System
- Game runs in days (Day 1, Day 2, ...)
- `GameManager` controls day time: configurable start/end hours, day length in seconds
- `TriggerDayTimePassing()` starts the day clock, `EndDay()` saves and advances
- Days have associated `DayData` containing markers, active chat logs, etc.

#### Chat System
- `ChatLog` (ScriptableObject) contains ordered `ChatBubble` messages
- `ChatBubbleSequence` defines sequences of bubbles for dialogue playback
- `ChatLogController` runs bubble sequences with typing indicators
- Chat logs are Addressable assets loaded at runtime

#### Conversation Graph System
- `ConversationGraph` (ScriptableObject): nodes (`Entry`, `Bubble`, `Choice`, `Wait`, `End`) + a flat edge list; assets live under `Assets/ScriptableObjects/ConversationGraphs/<character>/`
- `ChatLog.Graphs` resolves related graphs via Addressables (`conversation_graph/` prefix, label + name match)
- `ConversationManager` (singleton) owns one `ConversationRunner` per chat log
- `ConversationRunner` (plain C#, not a MonoBehaviour): entry evaluation by `SequenceEvent`, thread walk, parking at wait/choice nodes, day-blocking choices, history/activation state for saves
- Authoring: `Custom Tools/Conversation Graph Editor` — right-click canvas to add nodes / Layout Graph / Validate Graph. Port rules: the input port is `Multi` (fan-in allowed), output and choice-option ports are `Single` (the runtime follows only the first outgoing edge — fan-out is unsupported). `ConversationGraphView.GetCompatiblePorts` is overridden because Unity 6 ships no default port adapter; ports are created with `typeof(object)`.

#### Marker System
- `MarkerManager` handles keyboard-driven marker placement on chat text
- Players hold keys to activate marker types, then select text ranges
- Markers scored by accuracy (overlap with `Markable` targets, excess penalty)
- `MarkerType` (ScriptableObject) defines marker properties including keycode
- Marker flags displayed as UI indicators on screen

#### Save System
- `SaveManager` persists game state to JSON in `Application.persistentDataPath`
- Uses `ISavable`/`ILoadable` interfaces for component registration
- `SaveSlot` (ScriptableObject) links to a `GameTemplate` and stores day entries
- `GameTemplate` defines the full game structure (days, chat sequences)
- New saves initialize from template; subsequent saves merge runtime data

### Data Flow
```
GameTemplate → SaveSlot (initialization) → JSON save file (runtime)
                ↓
           DayData per day (markers, chat logs, state)
                ↓
           ISavable components save → DayData
           ILoadable components load ← DayData
```

## Current State
- **Single scene:** `v1 prototye.unity` (note the typo in filename)
- **Core systems implemented:** Day progression, chat display, marker placement, save/load, conversation graph playback
- **TODOs in codebase:**
  - Save slot picker/creator menu (currently brute-force assigned)
  - Async Addressable loading (currently synchronous)
  - Safeguard against multiple markers per markable
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
