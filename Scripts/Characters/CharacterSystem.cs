using Godot;
using System.Collections.Generic;
using CharacterData;
using System.Linq;
using System;

public partial class CharacterSystem : Node
{
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

    [Export]
    public static CharacterSystem Instance;
    public DataRegistry DataRegistry;
    public Dictionary<Guid, Character> CharacterLookup = new Dictionary<Guid, Character>();

    public override void _Ready()
    {
        Instance = this;
        DataRegistry = DataRegistry.Instance;
        GD.Randomize();
    }

    public Dictionary<int, PackedScene> CharacterModelDictionary = new Dictionary<int, PackedScene> {
        { 1, GD.Load<PackedScene>("res://Scenes/rogue.tscn") },
        { 2, GD.Load<PackedScene>("res://Scenes/barbarian.tscn") },
        { 3, GD.Load<PackedScene>("res://Scenes/wizard.tscn") },
        { 4, GD.Load<PackedScene>("res://Scenes/knight.tscn") },
    };
    public Character GenerateRandomCharacter()
    {
        var raceKeys = DataRegistry.Instance.Races.Keys.ToList();
        var randomRaceKey = raceKeys[GD.RandRange(0, raceKeys.Count - 1)];
        var race = DataRegistry.Instance.Races[randomRaceKey];

        var character = new Character
        {
            CharacterName = GetRandomName(race.Name),
            Race = race,
            Class = DataRegistry.Instance.Classes["Adventurer"],

        };

        foreach (Stat.StatKey stat in Stat.StatKeyNamesReverse.Values)
        {
            var min = race.BaseStats[stat].X;
            var max = race.BaseStats[stat].Y;
            character.BaseStats[stat].Value = GD.RandRange(min, max);
        }

        foreach (Stat.StatKey stat in Stat.StatKeyNamesReverse.Values)
        {
            character.GrowthRates[stat] = RollGrowthRate();
        }

        // Initialize current stats from base stats
        character.InitializeStats();

        character.CharacterModel = CharacterModelDictionary[GD.RandRange(1, CharacterModelDictionary.Count)];

        CharacterLookup.Add(character.CharacterId, character);

        return character;
    }

    public GrowthRate RollGrowthRate()
    {
        float roll = (float)GD.Randf();
        float cumulativeProbability = 0f;

        foreach (var growthRate in DataRegistry.Instance.GrowthRates.Values)
        {
            cumulativeProbability += growthRate.GrowthRateChance;
            if (roll < cumulativeProbability)
            {
                return growthRate;
            }
        }

        // Fallback in case of rounding errors
        return DataRegistry.Instance.GrowthRates[GrowthRate.GrowthRateKey.Minimal];
    }

    private string GetRandomName(string raceName)
    {
        if (RaceNames.TryGetValue(raceName, out var names) && names.Length > 0)
        {
            return names[GD.RandRange(0, names.Length - 1)];
        }
        return "Adventurer";
    }

    public Character GetPlayerCharacter()
    {
        if (CharacterLookup.TryGetValue(Guid.Empty, out var playerCharacter))
        {
            return playerCharacter;
        }

        var pc = new Character
        {
            CharacterName = "Player",
            Race = DataRegistry.Instance.Races["Gignen"],
            Class = DataRegistry.Instance.Classes["Adventurer"],
            CharacterId = Guid.Empty,
            CharacterModel = CharacterModelDictionary[4]
        };

        foreach (Stat.StatKey stat in pc.Race.BaseStats.Keys)
        {
            pc.BaseStats[stat].Value = pc.Race.BaseStats[stat].Y;
            pc.GrowthRates[stat] = DataRegistry.Instance.GrowthRates[GrowthRate.GrowthRateKey.Gradual];
        }

        pc.InitializeStats();

        CharacterLookup.Add(pc.CharacterId, pc);

        return pc;
    }

    public Character GetCharacter(Guid characterId)
    {
        if (CharacterLookup.TryGetValue(characterId, out var character))
        {
            return character;
        }
        else
        {
            GD.PrintErr($"Character with ID {characterId} not found.");
            return null;
        }
    }
    public List<Character> GetAllCharacters()
    {
        return CharacterLookup.Values.ToList();
    }

    public List<Character> GetAllNPCs()
    {
        return CharacterLookup.Values.Where(c => c.CharacterId != Guid.Empty).ToList();
    }
}

