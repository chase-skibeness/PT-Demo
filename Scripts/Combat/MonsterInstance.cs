using Godot;
using System;
using System.Collections.Generic;
using CharacterData;

public class MonsterInstance
{
	public MonsterData Template;
	public int Level;
	public Dictionary<Stat.StatKey, int> CurrentStats;
	public int CurrentHP;
	public int MaxHP;
	public int CurrentMP;
	public int MaxMP;
	public List<AbilityData> Abilities = new();

	/// <summary>
	/// Generate a scaled monster instance from a template at the given level.
	/// Stats scale using base + level * (scaling * random modifier).
	/// </summary>
	public static MonsterInstance Generate(MonsterData template, int level)
	{
		var instance = new MonsterInstance
		{
			Template = template,
			Level = level,
			CurrentStats = new()
		};

		foreach (var stat in template.BaseStats.Keys)
		{
			float scaling = template.ScalingMultipliers.TryGetValue(stat, out var mult) ? mult : 1f;
			float randomMod = (float)GD.RandRange(0.8, 1.2);
			int value = template.BaseStats[stat] + (int)(level * scaling * randomMod);
			instance.CurrentStats[stat] = Math.Max(1, value);
		}

		// Fill in any missing stats with defaults
		foreach (Stat.StatKey key in Enum.GetValues(typeof(Stat.StatKey)))
		{
			if (!instance.CurrentStats.ContainsKey(key))
			{
				instance.CurrentStats[key] = 5 + level;
			}
		}

		instance.MaxHP = instance.CurrentStats[Stat.StatKey.END] * 10;
		instance.CurrentHP = instance.MaxHP;
		instance.MaxMP = instance.CurrentStats[Stat.StatKey.SPI] * 12;
		instance.CurrentMP = instance.MaxMP;

		return instance;
	}

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
				Attribute = template.Attributes?.Count > 0 ? template.Attributes[0].ToString() : "Physical",
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
}
