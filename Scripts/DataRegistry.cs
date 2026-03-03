using CharacterData;
using Godot;
using System.Collections.Generic;

public partial class DataRegistry : Node
{
	[Export] public static DataRegistry Instance;

	[Export] public string BuildingFolderPath = "res://Data/Buildings";

	[Export] public string RacesFolderPath = "res://Data/Characters/Races";

	[Export] public string ClassesFolderPath = "res://Data/Characters/Classes";

	[Export] public string QuestsFolderPath = "res://Data/Quests";

	[Export] public string AbilitiesFolderPath = "res://Data/Abilities";

	[Export] public string ZonesFolderPath = "res://Data/Zones";

	public Dictionary<string, Building> BuildingTemplates = new();

	public Dictionary<string, RaceData> Races = new();

	public Dictionary<string, ClassData> Classes = new();

	public Dictionary<GrowthRate.GrowthRateKey, GrowthRate> GrowthRates = new();

	public Dictionary<int, Quest> Quests = new();

	public Dictionary<string, AbilityData> Abilities = new();
	public Dictionary<string, MonsterData> Monsters = new();
	public Dictionary<string, ZoneData> Zones = new();

	public override void _Ready()
	{
		Instance = this;
		RegisterBuildings();
		RegisterRaces();
		RegisterClasses();
		RegisterGrowthRates();
		RegisterQuests();
		RegisterAbilities();
		RegisterZones();
	}

	private void RegisterBuildings()
	{
		foreach (string path in DirAccess.GetFilesAt(BuildingFolderPath))
		{
			if (!path.EndsWith(".tres"))
				continue;

			var fullPath = $"{BuildingFolderPath}/{path}";
			var resource = ResourceLoader.Load<Building>(fullPath);
			if (resource != null)
			{
				BuildingTemplates[resource.BuildingName] = resource;
			}
		}
	}

	private void RegisterRaces()
	{
		foreach (string path in DirAccess.GetFilesAt(RacesFolderPath))
		{
			if (!path.EndsWith(".tres"))
				continue;

			var fullPath = $"{RacesFolderPath}/{path}";
			var resource = ResourceLoader.Load<RaceData>(fullPath);
			if (resource != null)
			{
				Races[resource.Name] = resource;
			}
		}
	}

	private void RegisterClasses()
	{
		foreach (string path in DirAccess.GetFilesAt(ClassesFolderPath))
		{
			if (!path.EndsWith(".tres"))
				continue;

			var fullPath = $"{ClassesFolderPath}/{path}";
			var resource = ResourceLoader.Load<ClassData>(fullPath);
			if (resource != null)
			{
				Classes[resource.Name] = resource;
			}
		}
	}

	private void RegisterGrowthRates()
	{
		GrowthRate.GrowthRateKey[] growthRateKeys = (GrowthRate.GrowthRateKey[])System.Enum.GetValues(typeof(GrowthRate.GrowthRateKey));
		foreach (var growthRateKey in growthRateKeys)
		{
			GrowthRates[growthRateKey] = new GrowthRate
			{
				Key = growthRateKey,
				GrowthRateName = GrowthRate.GrowthRateNames[growthRateKey],
				Description = GrowthRate.GrowthRateMetaDescriptions[growthRateKey],
				MetaDescription = GrowthRate.GrowthRateMetaDescriptions[growthRateKey],
				GrowthRateSymbol = GrowthRate.GrowthRateSymbols[growthRateKey],
				GrowthRateChance = GrowthRate.GrowthRateChances[growthRateKey],
				CalculateGrowthRate = GrowthRate.GrowthRateCalculators[growthRateKey]
			};
		}
	}

	private void RegisterQuests()
	{
		int id = 0;
		foreach (string path in DirAccess.GetFilesAt(QuestsFolderPath))
		{
			if (!path.EndsWith(".tres"))
				continue;

			var fullPath = $"{QuestsFolderPath}/{path}";
			var resource = ResourceLoader.Load<Quest>(fullPath);
			resource.QuestID = id++;
			if (resource != null)
			{
				Quests[resource.QuestID] = resource;
				GD.Print($"Registered quest: {resource.QuestName} with ID: {resource.QuestID}");
			}
		}
	}

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
}
