# PT-Demo Codebase Merge — Design Document

## Context

Two independent implementations of Project Township exist:

1. **User's original codebase** (main branch, 42 commits at `ebe63a3`): Full exploration system with room generation, GameSignalBus pub/sub architecture, quest goal/event/reward framework, Chronos time hooks, party-based combat shell (CombatRunner), TownResource with 7 material types.

2. **MVP stash** (`stash@{0}` with untracked files in `^3`): Turn-based combat system (DamageCalculator, CombatSystem, CombatUI), ability system (AbilityData, AbilityResolver), monster generation (MonsterData, MonsterInstance), NPC auto-resolution (QuestResolver), EndOfDayReport, resource HUD, plus bug fixes to CharacterData/CharacterSystem/QuestMenu.

## Decision: Approach A — Transplant MVP Mechanics

**Base:** User's codebase (main at `ebe63a3`)
**Strategy:** Extract new files + fixes from stash, adapt to GameSignalBus architecture.

### Why This Approach
- User's codebase is the more complete foundation (exploration, signal bus, time hooks, quest framework)
- MVP mainly added *new* combat mechanics that slot in as additions
- Avoids messy merge conflicts from applying stash wholesale (7316 deletions)
- Preserves all user work intact

## Architecture Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Communication pattern | GameSignalBus (pub/sub) | Decoupled systems, better for growing game |
| Exploration flow | Room-based dungeon runs | User's original, more engaging than simple zone select |
| Combat mechanics | MVP turn-based + party support | MVP formulas + user's party layout |
| Central state | GameSignalBus + lightweight state | No separate GameState singleton; signals for events, TownManager for resources |

## Files to Extract From Stash

### New files (from `stash@{0}^3`):

| File | Purpose | Adaptation needed |
|------|---------|-------------------|
| `Scripts/Combat/DamageCalculator.cs` | GDD combat formulas (attack, ability, heal, crit, flee, elemental) | None — static utility |
| `Scripts/Combat/CombatantData.cs` | Runtime combat wrapper with buff system | Minor — Character references |
| `Scripts/Combat/AbilityData.cs` | `[GlobalClass]` ability resource | None |
| `Scripts/Combat/AbilityResolver.cs` | Resolves ability effects with combat log text | Minor — signal emissions |
| `Scripts/Combat/MonsterData.cs` | `[GlobalClass]` monster resource with stats/scaling | None |
| `Scripts/Combat/MonsterInstance.cs` | Level-scaled monster factory | Minor — uses MonsterData |
| `Scripts/Combat/EnemyAI.cs` | Simple aggressive AI | None |
| `Scripts/Combat/CombatSystem.cs` | Turn-based state machine | **Major** — party support + SignalBus wiring |
| `Scripts/Combat/CombatUI.cs` | Programmatic combat UI | **Major** — party display layout |
| `Scripts/Quests/QuestResolver.cs` | NPC auto-resolution (stat-based success formula) | Minor — SignalBus wiring |
| `Scripts/UI/EndOfDayReport.cs` | Day-end quest results display | Minor — SignalBus wiring |
| `Scripts/Exploration/ZoneData.cs` | `[GlobalClass]` zone resource | None |

### Files to SKIP:
- `Scripts/Exploration/WorldMap.cs` — Replaced by user's WorldSelect + ExplorationRunner
- `Scripts/Core/GameState.cs` — Replaced by GameSignalBus + TownManager pattern
- `Scripts/Core/SceneManager.cs` — User's scenes use direct ChangeScene
- `Scripts/Town/TownExit.cs` — User already has one (TownConnector pattern)

### Data files to extract:
- `Data/Abilities/` — 6 .tres files (PowerStrike, Spark, Focus, Bubble, Brace, SlimeSpit)
- `Data/Monsters/Slime.tres`
- `Data/Zones/BeginningForest.tres`
- `Data/Quests/` — Explore.tres, GatherResourcesWood.tres, GatherResourcesStone.tres

## File Modifications

### CharacterData.cs — Fix stat calculation
- Replace `CalculateCurrentStats()` with `InitializeStats()`, `LevelUp()`, `AddExperience()`
- Add `MaxHP` (END * 10) and `MaxMP` (SPI * 12) derived properties
- Fix growth formula: `rawGrowth * (1f + classMod)` instead of `rawGrowth * classMod`

### CharacterSystem.cs — Name generation
- Add static `Dictionary<string, string[]>` of names per race (120 names across 8 races)
- `GenerateRandomCharacter()` picks from race-specific name pool
- Call `InitializeStats()` instead of `CalculateCurrentStats()`

### DataRegistry.cs — Load new data types
- Add `AbilitiesFolderPath`, `MonstersFolderPath`, `ZonesFolderPath`
- Add `RegisterAbilities()`, `RegisterMonsters()`, `RegisterZones()`
- Add `Dictionary<string, AbilityData>`, `Dictionary<string, MonsterData>`, `Dictionary<string, ZoneData>`

### GameSignalBus.cs — New signals
```csharp
// Combat
[Signal] delegate void CombatStartedEventHandler();
[Signal] delegate void CombatEndedEventHandler(bool victory);
// Day cycle
[Signal] delegate void ShowEndOfDayReportEventHandler();
[Signal] delegate void EndOfDayReportClosedEventHandler();
// Resources
[Signal] delegate void ResourcesChangedEventHandler();
```

### QuestMenu.cs — Bug fixes
- Clear assigned quests list before repopulating
- Add NPC-only guard for non-combat quests
- Reset selectedQuest after assignment

### QuestManager.cs — Quest refresh
- Add `RefreshAvailableQuests()` method
- Called at day start and after combat victory

## Integration Points

### CombatRunner ↔ CombatSystem
```
ExplorationRunner → Combat room → CombatRunner scene
  CombatRunner._Ready():
    1. Get party from ExplorationManager.PartyMembers
    2. Get enemies from CurrentRoom.Monsters
    3. Convert to CombatantData (FromCharacter / FromMonster)
    4. Initialize CombatSystem with combatants
    5. CombatSystem runs turn-based battle
    6. On victory → GameSignalBus.CombatEnded(true)
    7. On escape → ChangeScene back to ExplorationRunner
```

### MonsterTemplate → MonsterData Migration
- `GeneratedRoom.Monsters` changes from `List<MonsterTemplate>` to `List<MonsterInstance>`
- `ExplorationManager.CreateRoomFromTemplate()` generates `MonsterInstance` objects
- `Location.MonsterPool` changes from `Array<MonsterTemplate>` to `Array<MonsterData>`

### QuestResolver → Day-End Flow
```
Bed interaction → Chronos.EndDay()
  → GameSignalBus.DayEnded
  → FadeOut
  → QuestResolver resolves NPC quests
  → GameSignalBus.ShowEndOfDayReport
  → EndOfDayReport displayed
  → User clicks Continue
  → GameSignalBus.EndOfDayReportClosed
  → Chronos.StartDay()
  → FadeIn
```

### Resource HUD
- Add resource tracking to TownManager (Gold, Wood, Stone, Food)
- Create HUD CanvasLayer that listens for `ResourcesChanged` signal
- Display resource counts, visible only in Town phase

## What's NOT Touched
- ExplorationManager.cs, ExplorationRunner.cs, GeneratedRoom.cs
- RoomTemplate.cs, Location.cs, LocationButton.cs, LocationLayoutDefinition.cs
- ResourceMiniGameRunner.cs, ResourceNodeDefinition.cs, WeightedResource.cs
- WorldSelect.cs (zone/location selection UI)
- Chronos time hooks system
- TownManager, BuildGrid, BuildMenu, BuildSite, Building
- Quest goal/event/reward framework (QuestGoal, QuestEvent, QuestReward, ResourceCollectGoal, ResourceCollectEvent, ResourceQuestReward)
- FadeManager, Clock, Helios
- NPCToAssignQuestData, QuestBoard, QuestPanelData

## Task Sequence (14 tasks)

1. Extract & add new combat files from stash
2. Extract & add data files from stash (.tres)
3. Cherry-pick CharacterData stat fixes
4. Cherry-pick CharacterSystem name generation
5. Update DataRegistry for new data types
6. Add new signals to GameSignalBus
7. Migrate MonsterTemplate → MonsterData in exploration
8. Integrate CombatSystem into CombatRunner (party support)
9. Adapt CombatUI for party display
10. Wire QuestResolver into day-end cycle via SignalBus
11. Add EndOfDayReport to day-end flow
12. Add resource tracking + HUD
13. Fix QuestMenu bugs
14. Build verification + smoke test
