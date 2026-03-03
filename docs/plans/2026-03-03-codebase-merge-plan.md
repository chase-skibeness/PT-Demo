# PT-Demo Codebase Merge Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Transplant MVP combat mechanics, ability system, NPC auto-resolution, and day-end reporting into the user's existing codebase, wired through GameSignalBus.

**Architecture:** Extract 12 new files from git stash (`stash@{0}^3` untracked files), adapt references from GameState/SceneManager singletons to GameSignalBus pub/sub + existing singletons (TownManager, ExplorationManager, QuestManager). Keep the user's full exploration/room-generation/quest-goal/time-hook systems intact.

**Tech Stack:** Godot 4.5, C# (.NET 8), GameSignalBus pub/sub pattern

**Important:** This is a Godot C# project — there is no unit test framework. Verification is `dotnet build "Project Township Demo.sln"` (0 errors) + in-engine smoke test. Each task ends with a build check and commit.

**Repo:** `C:\Users\ChaseSkibeness\Projects\PT-Demo`
**Stash:** `stash@{0}` — MVP work with untracked files in `stash@{0}^3`
**Extraction command:** `git show "stash@{0}^3:<path>" > <path>`

---

### Task 1: Extract Combat Data Layer

Extract the pure Resource classes that define abilities, zones, and monsters. These have zero dependencies on GameState/SceneManager.

**Files:**
- Create: `Scripts/Combat/AbilityData.cs`
- Create: `Scripts/Combat/MonsterData.cs`
- Create: `Scripts/Combat/MonsterInstance.cs`
- Create: `Scripts/Exploration/ZoneData.cs`

**Step 1: Extract AbilityData.cs from stash**

```bash
cd /c/Users/ChaseSkibeness/Projects/PT-Demo
git show "stash@{0}^3:Scripts/Combat/AbilityData.cs" > Scripts/Combat/AbilityData.cs
```

This file is used as-is — it's a `[GlobalClass] Resource` with no external dependencies.

**Step 2: Extract ZoneData.cs from stash**

```bash
git show "stash@{0}^3:Scripts/Exploration/ZoneData.cs" > Scripts/Exploration/ZoneData.cs
```

Used as-is — `[GlobalClass] Resource` with string arrays.

**Step 3: Extract MonsterData.cs from stash**

```bash
git show "stash@{0}^3:Scripts/Combat/MonsterData.cs" > Scripts/Combat/MonsterData.cs
```

Used as-is — `[GlobalClass] Resource` with `Dictionary<Stat.StatKey, int>`.

**Step 4: Extract MonsterInstance.cs and add adapter for MonsterTemplate**

```bash
git show "stash@{0}^3:Scripts/Combat/MonsterInstance.cs" > Scripts/Combat/MonsterInstance.cs
```

Then add a `FromTemplate` factory method so the exploration system can convert its `MonsterTemplate` objects into `MonsterInstance` for combat. Add this method to the class:

```csharp
/// <summary>
/// Create a MonsterInstance from a MonsterTemplate (used by exploration system).
/// MonsterTemplate uses Stat objects; this converts to int-based stats.
/// </summary>
public static MonsterInstance FromTemplate(MonsterTemplate template, int level)
{
    var instance = new MonsterInstance
    {
        Template = new MonsterData
        {
            MonsterName = template.MonsterName,
            Description = template.Description,
            Attribute = template.Attributes?.Count > 0 ? template.Attributes[0].AttributeName : "Physical",
            NaturalWeaponPower = 30,
            AbilityKeys = template.Abilities ?? System.Array.Empty<string>(),
            BaseXPReward = 50 + level * 10,
            BaseGoldReward = 10 + level * 5,
        },
        Level = level,
        CurrentStats = new()
    };

    foreach (var stat in template.BaseStats.Keys)
    {
        int baseValue = template.BaseStats[stat].Value;
        float scaling = template.ScalingMultipliers.TryGetValue(stat, out var mult) ? (float)mult : 1f;
        float randomMod = (float)GD.RandRange(0.8, 1.2);
        instance.CurrentStats[stat] = Math.Max(1, baseValue + (int)(level * scaling * randomMod));
    }

    // Fill missing stats
    foreach (Stat.StatKey key in Enum.GetValues(typeof(Stat.StatKey)))
    {
        if (!instance.CurrentStats.ContainsKey(key))
            instance.CurrentStats[key] = 5 + level;
    }

    instance.MaxHP = instance.CurrentStats[Stat.StatKey.END] * 10;
    instance.CurrentHP = instance.MaxHP;
    instance.MaxMP = instance.CurrentStats[Stat.StatKey.SPI] * 12;
    instance.CurrentMP = instance.MaxMP;

    return instance;
}
```

Note: `MonsterInstance.FromTemplate` creates a temporary `MonsterData` wrapper so the rest of the combat system works unchanged.

**Step 5: Verify build**

```bash
cd /c/Users/ChaseSkibeness/Projects/PT-Demo && dotnet build "Project Township Demo.sln"
```

Expected: 0 errors. There may be warnings about unused files — that's fine, these are standalone data classes.

**Step 6: Commit**

```bash
git add Scripts/Combat/AbilityData.cs Scripts/Combat/MonsterData.cs Scripts/Combat/MonsterInstance.cs Scripts/Exploration/ZoneData.cs
git commit -m "feat: add combat data layer (AbilityData, MonsterData, MonsterInstance, ZoneData)"
```

---

### Task 2: Extract Combat Utilities

Extract the combat math, AI, and ability resolution systems. These depend on Task 1's data classes but not on any singletons.

**Files:**
- Create: `Scripts/Combat/DamageCalculator.cs`
- Create: `Scripts/Combat/EnemyAI.cs` (includes CombatAction struct and CombatActionType enum)
- Create: `Scripts/Combat/CombatantData.cs`
- Create: `Scripts/Combat/AbilityResolver.cs`

**Step 1: Extract DamageCalculator.cs**

```bash
git show "stash@{0}^3:Scripts/Combat/DamageCalculator.cs" > Scripts/Combat/DamageCalculator.cs
```

Used as-is — static utility class. All formulas (basic attack, ability damage, healing, crit, flee, elemental weakness) work with `CombatantData`.

**Step 2: Extract EnemyAI.cs**

```bash
git show "stash@{0}^3:Scripts/Combat/EnemyAI.cs" > Scripts/Combat/EnemyAI.cs
```

Used as-is — contains `EnemyAI` static class, `CombatAction` struct, and `CombatActionType` enum.

**Step 3: Extract CombatantData.cs**

```bash
git show "stash@{0}^3:Scripts/Combat/CombatantData.cs" > Scripts/Combat/CombatantData.cs
```

Used as-is. Key points:
- `FromCharacter(Character c)` — converts a Character into a combat-ready wrapper. Reads abilities from `DataRegistry.Instance.Abilities`.
- `FromMonster(MonsterInstance m)` — converts a MonsterInstance into a combat-ready wrapper.
- Buff system: `ApplyBuff()`, `TickBuffs()` handle timed stat modifications.

**Step 4: Extract AbilityResolver.cs**

```bash
git show "stash@{0}^3:Scripts/Combat/AbilityResolver.cs" > Scripts/Combat/AbilityResolver.cs
```

Used as-is — resolves Damaging/Buff/Restorative ability types using DamageCalculator.

**Step 5: Verify build**

```bash
dotnet build "Project Township Demo.sln"
```

Expected: 0 errors. `CombatantData.FromCharacter` references `DataRegistry.Instance.Abilities` which doesn't exist yet — this will cause a build error. To fix, add a temporary empty Abilities dictionary to DataRegistry (we'll properly implement it in Task 5):

Modify `Scripts/DataRegistry.cs` — add after line 25 (`public Dictionary<int, Quest> Quests = new();`):

```csharp
public Dictionary<string, AbilityData> Abilities = new();
public Dictionary<string, MonsterData> Monsters = new();
public Dictionary<string, ZoneData> Zones = new();
```

Then rebuild. Expected: 0 errors.

**Step 6: Commit**

```bash
git add Scripts/Combat/DamageCalculator.cs Scripts/Combat/EnemyAI.cs Scripts/Combat/CombatantData.cs Scripts/Combat/AbilityResolver.cs Scripts/DataRegistry.cs
git commit -m "feat: add combat utilities (DamageCalculator, EnemyAI, CombatantData, AbilityResolver)"
```

---

### Task 3: Fix CharacterData Stat Calculation

The current `CalculateCurrentStats()` has bugs: growth formula multiplies by `classMod` (0 = no growth), always uses base stat (not accumulated). Replace with proper `InitializeStats()`, `LevelUp()`, `AddExperience()`.

**Files:**
- Modify: `Scripts/Characters/CharacterData.cs:211-240`
- Modify: `Scripts/Characters/CharacterSystem.cs:54,103`

**Step 1: Replace CalculateCurrentStats in CharacterData.cs**

In `Scripts/Characters/CharacterData.cs`, replace the entire `CalculateCurrentStats()` method (lines 211-240) with:

```csharp
// Derived combat stats
public int MaxHP => CurrentStats[Stat.StatKey.END].Value * 10;
public int MaxMP => CurrentStats[Stat.StatKey.SPI].Value * 12;

/// <summary>
/// Copies BaseStats into CurrentStats. Call once after setting base stats during character creation.
/// </summary>
public void InitializeStats()
{
    foreach (var stat in BaseStats.Keys)
    {
        CurrentStats[stat] = new Stat(stat)
        {
            Value = BaseStats[stat].Value,
            Overflow = 0f
        };
    }
    SnapshotStats();
}

/// <summary>
/// Applies one level of growth to CurrentStats using growth rates and class modifiers.
/// </summary>
public void LevelUp()
{
    Level++;
    foreach (var stat in CurrentStats.Keys)
    {
        var current = CurrentStats[stat];
        var growthRate = GrowthRates[stat];
        var classMod = Class.Modifiers.TryGetValue(stat, out var modifier) ? modifier : 0f;

        float rawGrowth = growthRate.CalculateGrowthRate(Level);
        float adjustedGrowth = rawGrowth * (1f + classMod) + current.Overflow;

        int intGrowth = Mathf.FloorToInt(adjustedGrowth);
        float newOverflow = adjustedGrowth - intGrowth;

        CurrentStats[stat] = new Stat(stat)
        {
            Value = current.Value + intGrowth,
            Overflow = newOverflow
        };
    }
    SnapshotStats();
    GD.Print($"{CharacterName} leveled up to {Level}!");
}

public void AddExperience(int amount)
{
    Experience += amount;
    while (TryLevelUp()) { }
}

private bool TryLevelUp()
{
    int xpNeeded = Level * 100;
    if (Experience >= xpNeeded)
    {
        Experience -= xpNeeded;
        LevelUp();
        return true;
    }
    return false;
}

private void SnapshotStats()
{
    var snapshot = new Dictionary<Stat.StatKey, Stat>();
    foreach (var kvp in CurrentStats)
    {
        snapshot[kvp.Key] = new Stat(kvp.Key) { Value = kvp.Value.Value, Overflow = kvp.Value.Overflow };
    }
    StatHistory.Add(snapshot);
}
```

**Step 2: Update CharacterSystem.cs to use InitializeStats**

In `Scripts/Characters/CharacterSystem.cs`:

- Line 54: Replace `character.CalculateCurrentStats();` with `character.InitializeStats();`
- Line 103: Replace `pc.CalculateCurrentStats();` with `pc.InitializeStats();`

**Step 3: Add race-based name generation to CharacterSystem.cs**

Add a static name dictionary before `GenerateRandomCharacter()` and update the method. After line 26 (end of `CharacterModelDictionary`), add:

```csharp
private static readonly Dictionary<string, string[]> RaceNames = new()
{
    { "Gignen", new[] { "Aldric", "Brynn", "Cedric", "Dara", "Elowen", "Finn", "Gwen", "Harlan", "Isolde", "Jareth", "Keira", "Leif", "Mira", "Nolan", "Orin" } },
    { "Draconid", new[] { "Azraxis", "Brimscale", "Cindrath", "Drakara", "Embera", "Fyrthos", "Galdrix", "Hexara", "Ignatius", "Jyrath", "Kaldrix", "Lysscale", "Mordrath", "Nystrix", "Obsidix" } },
    { "Felinara", new[] { "Amberwhisk", "Bristle", "Clover", "Dewpaw", "Echo", "Fawn", "Glimmer", "Hazel", "Ivy", "Jasper", "Kit", "Luna", "Misty", "Nimble", "Onyx" } },
    { "Elphyn", new[] { "Aelindra", "Brightleaf", "Caelum", "Dawnmist", "Eirlys", "Faeryn", "Galadris", "Hespera", "Illyria", "Juniper", "Kalindra", "Loriel", "Mirael", "Nymeria", "Orellia" } },
    { "Konstrukt", new[] { "Anvil", "Bolts", "Copperjaw", "Dynamo", "Ember-Core", "Forge", "Gearwright", "Hexbolt", "Ironclad", "Jolt", "Kinetic", "Lodestone", "Magnet", "Nickle", "Oxid" } },
    { "Golemkin", new[] { "Boulder", "Cobble", "Duststone", "Earthen", "Flint", "Granite", "Hearthstone", "Ironore", "Jade", "Keystone", "Limestone", "Mortar", "Nugget", "Obsidian", "Pumice" } },
    { "Verdani", new[] { "Ashwood", "Bramble", "Canopy", "Dewdrop", "Elm", "Fern", "Grove", "Hickory", "Iris", "Juniper", "Kudzu", "Lichen", "Moss", "Nettle", "Oleander" } },
    { "Luminari", new[] { "Astra", "Beacon", "Celeste", "Dawnlight", "Eclipse", "Flare", "Gleam", "Halo", "Iridea", "Jewel", "Kindle", "Lantern", "Moonbeam", "Nebula", "Opal" } },
};
```

Then in `GenerateRandomCharacter()` (line 35), replace `CharacterName = "Jerry",` with:

```csharp
CharacterName = GetRandomName(race.Name),
```

And add the helper method:

```csharp
private string GetRandomName(string raceName)
{
    if (RaceNames.TryGetValue(raceName, out var names) && names.Length > 0)
    {
        return names[GD.RandRange(0, names.Length - 1)];
    }
    return "Adventurer";
}
```

**Step 4: Verify build**

```bash
dotnet build "Project Township Demo.sln"
```

Expected: 0 errors. If `SnapshotStats` causes issues due to missing reference, ensure it's defined inside the `Character` class.

**Step 5: Commit**

```bash
git add Scripts/Characters/CharacterData.cs Scripts/Characters/CharacterSystem.cs
git commit -m "fix: replace buggy stat calculation with InitializeStats/LevelUp/AddExperience, add race-based name generation"
```

---

### Task 4: Add .tres Data Files

Extract ability and zone data files from the stash.

**Files:**
- Create: `Data/Abilities/` (6 files)
- Create: `Data/Zones/` (1 file)

**Step 1: Create directories and extract files**

```bash
mkdir -p Data/Abilities Data/Zones
git show "stash@{0}^3:Data/Abilities/PowerStrike.tres" > Data/Abilities/PowerStrike.tres
git show "stash@{0}^3:Data/Abilities/Spark.tres" > Data/Abilities/Spark.tres
git show "stash@{0}^3:Data/Abilities/Focus.tres" > Data/Abilities/Focus.tres
git show "stash@{0}^3:Data/Abilities/Bubble.tres" > Data/Abilities/Bubble.tres
git show "stash@{0}^3:Data/Abilities/Brace.tres" > Data/Abilities/Brace.tres
git show "stash@{0}^3:Data/Abilities/SlimeSpit.tres" > Data/Abilities/SlimeSpit.tres
git show "stash@{0}^3:Data/Zones/BeginningForest.tres" > Data/Zones/BeginningForest.tres
```

**Step 2: Commit**

```bash
git add Data/Abilities/ Data/Zones/
git commit -m "feat: add ability and zone .tres data files"
```

---

### Task 5: Update DataRegistry to Load New Data Types

Add registration methods for abilities, monsters, and zones.

**Files:**
- Modify: `Scripts/DataRegistry.cs`

**Step 1: Add folder path exports and registration**

Add after line 15 (`[Export] public string QuestsFolderPath`):

```csharp
[Export] public string AbilitiesFolderPath = "res://Data/Abilities";
[Export] public string ZonesFolderPath = "res://Data/Zones";
```

Note: `Monsters` dict was added in Task 2 as a temporary stub. The exploration system uses `MonsterTemplate` in `.tres` files under `Data/Exploration/`. We don't need a separate Monsters folder — `MonsterInstance.FromTemplate()` handles conversion at runtime.

In `_Ready()`, add after `RegisterQuests();` (line 35):

```csharp
RegisterAbilities();
RegisterZones();
```

Add registration methods:

```csharp
private void RegisterAbilities()
{
    foreach (string path in DirAccess.GetFilesAt(AbilitiesFolderPath))
    {
        if (!path.EndsWith(".tres"))
            continue;

        var fullPath = $"{AbilitiesFolderPath}/{path}";
        var resource = ResourceLoader.Load<AbilityData>(fullPath);
        if (resource != null)
        {
            Abilities[resource.AbilityName] = resource;
            GD.Print($"Registered ability: {resource.AbilityName}");
        }
    }
}

private void RegisterZones()
{
    foreach (string path in DirAccess.GetFilesAt(ZonesFolderPath))
    {
        if (!path.EndsWith(".tres"))
            continue;

        var fullPath = $"{ZonesFolderPath}/{path}";
        var resource = ResourceLoader.Load<ZoneData>(fullPath);
        if (resource != null)
        {
            Zones[resource.ZoneName] = resource;
            GD.Print($"Registered zone: {resource.ZoneName}");
        }
    }
}
```

**Step 2: Verify build**

```bash
dotnet build "Project Township Demo.sln"
```

Expected: 0 errors.

**Step 3: Commit**

```bash
git add Scripts/DataRegistry.cs
git commit -m "feat: register abilities and zones in DataRegistry"
```

---

### Task 6: Add Combat Signals to GameSignalBus

Add signals for combat events, day-end reporting, and resource changes.

**Files:**
- Modify: `Scripts/GameSignalBus.cs`

**Step 1: Add new signal declarations**

Add after the existing `// Town` section (line 71):

```csharp
// Combat
[Signal]
public delegate void CombatStartedEventHandler();

[Signal]
public delegate void CombatEndedEventHandler(bool victory);

// Day cycle (for EndOfDayReport)
[Signal]
public delegate void ShowEndOfDayReportEventHandler();

[Signal]
public delegate void EndOfDayReportClosedEventHandler();
```

**Step 2: Verify build**

```bash
dotnet build "Project Township Demo.sln"
```

Expected: 0 errors.

**Step 3: Commit**

```bash
git add Scripts/GameSignalBus.cs
git commit -m "feat: add combat and day-end signals to GameSignalBus"
```

---

### Task 7: Adapt and Add CombatSystem

This is the core integration task. The MVP's CombatSystem was standalone (1v1, GameState-driven). We need to adapt it to:
1. Work within the exploration room system (get combatants from ExplorationManager)
2. Support party-based combat (multiple player characters)
3. Use GameSignalBus instead of GameState/SceneManager

**Files:**
- Create: `Scripts/Combat/CombatSystem.cs` (adapted from stash)

**Step 1: Create adapted CombatSystem.cs**

Extract from stash, then rewrite `SetupCombat()` and `OnContinuePressed()`:

```bash
git show "stash@{0}^3:Scripts/Combat/CombatSystem.cs" > Scripts/Combat/CombatSystem.cs
```

Then make these adaptations:

**Replace the `SetupCombat()` method** — instead of reading from `GameState.Instance`, read from `ExplorationManager.Instance`:

```csharp
private void SetupCombat()
{
    Phase = CombatPhase.Setup;

    // Get party from ExplorationManager
    var party = ExplorationManager.Instance.PartyMembers;
    if (party == null || party.Count == 0)
    {
        GD.PrintErr("No party members found in ExplorationManager!");
        GetTree().ChangeSceneToFile("res://Scenes/Town.tscn");
        return;
    }

    // Use first party member as active combatant (1v1 for now, party support later)
    PlayerCombatant = CombatantData.FromCharacter(party[0]);

    // Get monsters from current room
    var currentRoom = ExplorationManager.Instance.GetCurrentRoom();
    if (currentRoom == null || currentRoom.Monsters.Count == 0)
    {
        GD.PrintErr("No monsters in current room!");
        GetTree().ChangeSceneToFile("res://Scenes/ExplorationRunner.tscn");
        return;
    }

    // Pick first monster, convert MonsterTemplate → MonsterInstance
    var monsterTemplate = currentRoom.Monsters[0];
    int monsterLevel = 1; // Use location level if available
    if (ExplorationManager.Instance.CurrentLocation != null)
    {
        // Estimate level from location context
        monsterLevel = GD.RandRange(1, 5);
    }
    var monsterInstance = MonsterInstance.FromTemplate(monsterTemplate, monsterLevel);
    EnemyCombatant = CombatantData.FromMonster(monsterInstance);

    _turnNumber = 0;

    Log($"A wild {EnemyCombatant.Name} (Lv.{monsterLevel}) appeared!");

    GameSignalBus.Instance.EmitSignal(GameSignalBus.SignalName.CombatStarted);
    EmitSignal(SignalName.HPChanged);

    // Determine who goes first based on SPD
    int playerSpd = DamageCalculator.GetBuffedStat(PlayerCombatant, Stat.StatKey.SPD);
    int enemySpd = DamageCalculator.GetBuffedStat(EnemyCombatant, Stat.StatKey.SPD);

    if (playerSpd >= enemySpd)
    {
        Log($"{PlayerCombatant.Name} is faster!");
        StartPlayerTurn();
    }
    else
    {
        Log($"{EnemyCombatant.Name} strikes first!");
        StartEnemyTurn();
    }
}
```

**Replace `OnContinuePressed()`** — remove GameState/SceneManager references:

```csharp
private void OnContinuePressed()
{
    switch (Phase)
    {
        case CombatPhase.Victory:
            // Award XP
            if (EnemyCombatant.SourceMonster != null)
            {
                int xp = EnemyCombatant.SourceMonster.Template.BaseXPReward +
                          EnemyCombatant.SourceMonster.Level * 10;
                PlayerCombatant.SourceCharacter?.AddExperience(xp);
            }

            // Mark room as cleared
            var currentRoom = ExplorationManager.Instance.GetCurrentRoom();
            if (currentRoom != null) currentRoom.IsCleared = true;

            GameSignalBus.Instance.EmitSignal(GameSignalBus.SignalName.CombatEnded, true);

            // Return to exploration
            GetTree().ChangeSceneToFile("res://Scenes/ExplorationRunner.tscn");
            break;

        case CombatPhase.Defeat:
            GameSignalBus.Instance.EmitSignal(GameSignalBus.SignalName.CombatEnded, false);
            // Return to town on defeat
            GetTree().ChangeSceneToFile("res://Scenes/Town.tscn");
            break;

        case CombatPhase.Fled:
            GameSignalBus.Instance.EmitSignal(GameSignalBus.SignalName.CombatEnded, false);
            // Return to exploration (can try another room)
            GetTree().ChangeSceneToFile("res://Scenes/ExplorationRunner.tscn");
            break;
    }
}
```

**Remove `using` references** to `GameState` and `SceneManager` at the top of the file. These classes don't exist in this codebase.

**Step 2: Verify build**

```bash
dotnet build "Project Township Demo.sln"
```

Expected: 0 errors. If there are references to `GameState` or `SceneManager` elsewhere in the file, remove them.

**Step 3: Commit**

```bash
git add Scripts/Combat/CombatSystem.cs
git commit -m "feat: add CombatSystem adapted for exploration-based combat"
```

---

### Task 8: Adapt and Add CombatUI

The CombatUI builds all UI programmatically (no .tscn needed). Extract as-is — it works with CombatSystem signals.

**Files:**
- Create: `Scripts/Combat/CombatUI.cs`

**Step 1: Extract CombatUI**

```bash
git show "stash@{0}^3:Scripts/Combat/CombatUI.cs" > Scripts/Combat/CombatUI.cs
```

This file is used as-is. It:
- Creates HP/MP bars for player and enemy
- Shows combat log, action menu, ability list, result screen
- Emits `AttackPressed`, `AbilitySelected`, `RunPressed`, `ContinuePressed` signals
- CombatSystem connects to these in its `_Ready()`

**Step 2: Verify build**

```bash
dotnet build "Project Township Demo.sln"
```

Expected: 0 errors.

**Step 3: Commit**

```bash
git add Scripts/Combat/CombatUI.cs
git commit -m "feat: add programmatic CombatUI"
```

---

### Task 9: Update CombatRunner to Use CombatSystem

The existing `CombatRunner.cs` places 3D models for party and enemies but has no combat mechanics. Wire it to use CombatSystem.

**Files:**
- Modify: `Scripts/Combat/CombatRunner.cs`

**Step 1: Add CombatSystem as a child in _Ready()**

Replace the current `CombatRunner._Ready()` body. Keep the 3D model placement, but add CombatSystem initialization:

After the existing model placement code (line 65, before `}`), add:

```csharp
// Initialize the combat system
var combatSystem = new CombatSystem();
combatSystem.Name = "CombatSystem";
AddChild(combatSystem);
```

**Remove the old escape button handler** since CombatSystem handles escape now. Remove line 41: `EscapeButton.Pressed += OnEscapePressed;` and remove the `OnEscapePressed()` method entirely.

**Step 2: Verify build**

```bash
dotnet build "Project Township Demo.sln"
```

Expected: 0 errors.

**Step 3: Commit**

```bash
git add Scripts/Combat/CombatRunner.cs
git commit -m "feat: wire CombatSystem into CombatRunner for turn-based combat"
```

---

### Task 10: Add QuestResolver for NPC Auto-Resolution

The QuestResolver simulates NPC quest outcomes at end-of-day. Adapt for the existing resource system (elemental resources via TownManager, not gold/wood/stone/food).

**Files:**
- Create: `Scripts/Quests/QuestResolver.cs`
- Modify: `Scripts/Quests/QuestManager.cs` (add helper methods)

**Step 1: Create adapted QuestResolver.cs**

Extract from stash then adapt:

```bash
git show "stash@{0}^3:Scripts/Quests/QuestResolver.cs" > Scripts/Quests/QuestResolver.cs
```

**Adapt the `QuestResult` struct** — replace `GoldEarned/WoodEarned/StoneEarned/FoodEarned` with a generic reward list that works with the existing `QuestReward` system:

Replace the struct with:

```csharp
public struct QuestResult
{
    public Quest Quest;
    public Character Character;
    public bool Success;
    public int XPEarned;
    public string Summary;
    public List<AppliedReward> Rewards;
}

public struct AppliedReward
{
    public string Label;
    public int Amount;
}
```

**Adapt `ResolveNPCQuest`** — use the Quest's `Rewards` array instead of flat resource fields:

```csharp
public static QuestResult ResolveNPCQuest(QuestManager.ActiveQuest activeQuest)
{
    var quest = activeQuest.Quest;
    var character = activeQuest.AssignedCharacter;

    float successChance = CalculateSuccessChance(quest, character);
    float roll = (float)_rng.NextDouble() * 100f;
    bool success = roll < successChance;

    var result = new QuestResult
    {
        Quest = quest,
        Character = character,
        Success = success,
        Rewards = new List<AppliedReward>(),
    };

    if (success)
    {
        // Apply quest rewards
        foreach (var reward in quest.Rewards)
        {
            if (reward is ResourceQuestReward resourceReward)
            {
                TownManager.Instance[resourceReward.Resource].Amount += resourceReward.Amount;
                result.Rewards.Add(new AppliedReward
                {
                    Label = resourceReward.Resource.ToString(),
                    Amount = resourceReward.Amount
                });
            }
        }
        result.XPEarned = 50 + character.Level * 10;
        result.Summary = $"{character.CharacterName} completed '{quest.QuestName}' successfully!";
    }
    else
    {
        result.XPEarned = 25 + character.Level * 5;
        result.Summary = $"{character.CharacterName} failed '{quest.QuestName}' but gained some experience.";
    }

    return result;
}
```

**Adapt `ResolveAllNPCQuests`** — remove `GameState.Instance.AddResources()` (rewards applied inline above):

```csharp
public static List<QuestResult> ResolveAllNPCQuests()
{
    var results = new List<QuestResult>();
    var npcQuests = QuestManager.Instance.GetNPCQuests();

    foreach (var activeQuest in npcQuests)
    {
        var result = ResolveNPCQuest(activeQuest);
        results.Add(result);

        if (result.XPEarned > 0 && result.Character != null)
        {
            result.Character.AddExperience(result.XPEarned);
        }

        // Remove from active quests
        QuestManager.Instance.CompleteQuest(result.Quest, result.Character);

        GD.Print(result.Summary);
    }

    return results;
}
```

**Remove** the zone-level-gap check in `CalculateSuccessChance` that references `DataRegistry.Instance.Zones` (since quests don't have `ZoneName` in this codebase). Simplify to:

```csharp
private static float CalculateSuccessChance(Quest quest, Character character)
{
    float chance = 70f;
    chance += character.Level * 2f;
    chance += GetAverageStat(character);
    return Mathf.Clamp(chance, 30f, 95f);
}
```

**Step 2: Add GetNPCQuests to QuestManager.cs**

Add to `Scripts/Quests/QuestManager.cs`:

```csharp
public List<ActiveQuest> GetNPCQuests()
{
    return ActiveQuests.FindAll(q => q.AssignedCharacter.CharacterId != System.Guid.Empty);
}

public void RefreshAvailableQuests()
{
    foreach (var quest in DataRegistry.Quests.Values)
    {
        bool alreadyAvailable = AvailableQuests.Contains(quest);
        bool currentlyActive = ActiveQuests.Exists(q => q.Quest == quest);
        if (!alreadyAvailable && !currentlyActive)
        {
            AvailableQuests.Add(quest);
            GD.Print($"Quest refreshed: {quest.QuestName}");
        }
    }
}
```

**Step 3: Add `using TownResources;`** to the top of QuestResolver.cs (needed for `ResourceQuestReward`).

**Step 4: Verify build**

```bash
dotnet build "Project Township Demo.sln"
```

If `ResourceQuestReward` doesn't exist, check if the quest reward system uses a different name. The existing `QuestReward.cs` may need inspection. If rewards are generic, adapt the resolver to just print summary text without applying specific resource types.

**Step 5: Commit**

```bash
git add Scripts/Quests/QuestResolver.cs Scripts/Quests/QuestManager.cs
git commit -m "feat: add NPC quest auto-resolution with stat-based success formula"
```

---

### Task 11: Add EndOfDayReport

**Files:**
- Create: `Scripts/UI/EndOfDayReport.cs`

**Step 1: Extract and adapt**

```bash
mkdir -p Scripts/UI
git show "stash@{0}^3:Scripts/UI/EndOfDayReport.cs" > Scripts/UI/EndOfDayReport.cs
```

**Adapt the result panel** to use `QuestResult.Rewards` list instead of hardcoded gold/wood/stone/food. In `CreateResultPanel()`, replace the reward label logic with:

```csharp
var rewardParts = new System.Collections.Generic.List<string>();
foreach (var reward in result.Rewards)
{
    if (reward.Amount > 0) rewardParts.Add($"{reward.Label} +{reward.Amount}");
}
if (result.XPEarned > 0) rewardParts.Add($"XP +{result.XPEarned}");

var rewardLabel = new Label
{
    Text = rewardParts.Count > 0 ? string.Join("  |  ", rewardParts) : "No rewards",
};
```

Also adapt the `Show()` method's resource summary the same way.

**Step 2: Verify build**

```bash
dotnet build "Project Township Demo.sln"
```

**Step 3: Commit**

```bash
git add Scripts/UI/EndOfDayReport.cs
git commit -m "feat: add EndOfDayReport UI for day-end quest results"
```

---

### Task 12: Wire Day-End Flow

Connect Chronos → FadeOut → QuestResolver → EndOfDayReport → FadeIn → New Day.

**Files:**
- Modify: `Scripts/Time/Chronos.cs`

**Step 1: Add EndOfDayReport and QuestResolver to EndDay flow**

In `Scripts/Time/Chronos.cs`, modify the `EndDay()` method to:

```csharp
public void EndDay()
{
    DayTimer.Paused = true;
    GameSignalBus.Instance.EmitSignal(GameSignalBus.SignalName.DayEnded);

    // Resolve NPC quests
    var results = QuestResolver.ResolveAllNPCQuests();

    // Show end-of-day report
    var report = new EndOfDayReport();
    AddChild(report);
    report.Show(results, CurrentDay);
    report.OnContinue = () =>
    {
        report.QueueFree();
        QuestManager.Instance.RefreshAvailableQuests();
        StartDay();
    };
}
```

Remove the existing `GameSignalBus.Instance.Connect(GameSignalBus.SignalName.FadeOutFinished, ...)` from `_Ready()` (line 41) since we no longer auto-start the next day on fade-out — the report screen handles that.

**Step 2: Verify build**

```bash
dotnet build "Project Township Demo.sln"
```

**Step 3: Commit**

```bash
git add Scripts/Time/Chronos.cs
git commit -m "feat: wire day-end flow through QuestResolver and EndOfDayReport"
```

---

### Task 13: Fix QuestMenu Bugs

Fix panel duplication, add NPC-only quest guard, reset selection state.

**Files:**
- Modify: `Scripts/Quests/QuestMenu.cs`

**Step 1: Fix PopulateAssignedQuests to clear children first**

In `PopulateAssignedQuests()` (line 138), add at the start of the method:

```csharp
foreach (Node child in AssignedQuestsList.GetChildren())
{
    child.QueueFree();
}
```

**Step 2: Reset selectedQuest after assignment**

In `OnPlayerQuestSelected()` (after line 89 `UpdateQuestsList();`), add:

```csharp
selectedQuest = null;
```

In the `OnNPCQuestSelected()` lambda (after `UpdateQuestsList();`), it already sets `selectedQuest = null` via the close logic, but verify it does.

**Step 3: Add NPC-only guard for non-combat quests**

In `OnPlayerQuestSelected()`, add at the start (after null check):

```csharp
// Non-combat quests (gathering, etc.) can only be done by NPCs
if (selectedQuest.Availability == Quest.QuestAvailability.NPC)
{
    GD.Print("This quest is only available for NPCs.");
    return;
}
```

**Step 4: Verify build**

```bash
dotnet build "Project Township Demo.sln"
```

**Step 5: Commit**

```bash
git add Scripts/Quests/QuestMenu.cs
git commit -m "fix: quest menu duplication, NPC-only guard, selection reset"
```

---

### Task 14: Add Resource HUD

Add a CanvasLayer HUD that shows elemental resource amounts, visible in Town phase.

**Files:**
- Modify: `Scripts/Town/TownManager.cs`

**Step 1: Add HUD creation in TownManager._Ready()**

Add a resource display HUD. After `InitializeTownResources();` in `_Ready()`:

```csharp
CreateResourceHUD();
```

Add the HUD methods:

```csharp
private Label _resourceLabel;
private CanvasLayer _hudLayer;

private void CreateResourceHUD()
{
    _hudLayer = new CanvasLayer();
    _hudLayer.Layer = 10;
    AddChild(_hudLayer);

    var panel = new PanelContainer();
    panel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
    panel.OffsetBottom = 32;
    var style = new StyleBoxFlat();
    style.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.85f);
    panel.AddThemeStyleboxOverride("panel", style);
    _hudLayer.AddChild(panel);

    _resourceLabel = new Label();
    _resourceLabel.HorizontalAlignment = HorizontalAlignment.Center;
    _resourceLabel.AddThemeFontSizeOverride("font_size", 14);
    panel.AddChild(_resourceLabel);

    UpdateResourceHUD();
}

public void UpdateResourceHUD()
{
    if (_resourceLabel != null)
    {
        _resourceLabel.Text = $"Urum: {Urum.Amount}  Terratite: {Terratite.Amount}  Aquatite: {Aquatite.Amount}  Ventite: {Ventite.Amount}  Ignitite: {Ignitite.Amount}  Lumia: {Lumia.Amount}  Tenebria: {Tenebria.Amount}";
    }
}
```

**Step 2: Call UpdateResourceHUD when resources change**

In `OnResourceCollected()` and `OnQuestCompleted()`, add `UpdateResourceHUD();` after modifying resource amounts.

**Step 3: Verify build**

```bash
dotnet build "Project Township Demo.sln"
```

**Step 4: Commit**

```bash
git add Scripts/Town/TownManager.cs
git commit -m "feat: add resource HUD to TownManager"
```

---

### Task 15: Build Verification and Final Cleanup

Full build check, fix any remaining issues.

**Step 1: Clean build**

```bash
cd /c/Users/ChaseSkibeness/Projects/PT-Demo
dotnet clean "Project Township Demo.sln"
dotnet build "Project Township Demo.sln"
```

Expected: 0 errors, 0 warnings (or minimal warnings).

**Step 2: Check for any remaining GameState/SceneManager references**

```bash
grep -r "GameState" Scripts/ --include="*.cs" | grep -v ".uid"
grep -r "SceneManager" Scripts/ --include="*.cs" | grep -v ".uid"
```

Expected: No results. If any remain, they're in files that need updating.

**Step 3: Check for any remaining CalculateCurrentStats references**

```bash
grep -r "CalculateCurrentStats" Scripts/ --include="*.cs"
```

Expected: No results (all replaced with `InitializeStats()`).

**Step 4: Final commit if any cleanup was needed**

```bash
git add -A
git commit -m "chore: final cleanup after codebase merge"
```

---

## Smoke Test Checklist (Manual, In-Engine)

After all tasks, open the project in Godot 4.5 and verify:

1. **Town phase:** Player moves with WASD, NPCs have random names (not "Jerry")
2. **Quest board:** Quests display, NPC assignment works, no duplication
3. **Resource HUD:** Shows all 7 elemental resources at top of screen
4. **Exploration:** Exit town → choose location → rooms generate → combat rooms trigger combat scene
5. **Combat:** Turn-based battle with HP bars, combat log, abilities, flee option
6. **Combat end:** Victory returns to exploration, defeat returns to town
7. **Day end:** Sleep in bed → NPC quests resolve → EndOfDayReport shows → Continue starts new day
8. **Quest refresh:** Completed quests reappear on quest board after day end
