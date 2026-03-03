using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using CharacterData;

public class CombatantData
{
	public string Name;
	public int CurrentHP;
	public int MaxHP;
	public int CurrentMP;
	public int MaxMP;
	public Dictionary<Stat.StatKey, int> Stats;
	public string Attribute;
	public List<AbilityData> Abilities;
	public int WeaponPower;
	public Dictionary<string, float> Buffs = new();
	public Dictionary<string, int> BuffDurations = new();
	public bool IsPlayer;
	public bool LastActionWasCrit;

	// Reference to source data for XP/reward calculation
	public Character SourceCharacter;
	public MonsterInstance SourceMonster;

	public static CombatantData FromCharacter(Character c)
	{
		var stats = c.CurrentStats.ToDictionary(
			kvp => kvp.Key,
			kvp => kvp.Value.Value
		);

		// Compute HP/MP from stats (Character has no MaxHP/MaxMP properties)
		int maxHP = stats.TryGetValue(Stat.StatKey.END, out var endVal) ? endVal * 10 : 50;
		int maxMP = stats.TryGetValue(Stat.StatKey.SPI, out var spiVal) ? spiVal * 12 : 24;

		var combatant = new CombatantData
		{
			Name = c.CharacterName,
			MaxHP = maxHP,
			CurrentHP = maxHP,
			MaxMP = maxMP,
			CurrentMP = maxMP,
			Stats = stats,
			Attribute = "Physical",
			Abilities = new List<AbilityData>(),
			WeaponPower = 30,
			IsPlayer = true,
			SourceCharacter = c
		};

		// Load abilities from DataRegistry based on character's ability list
		if (c.Abilities != null)
		{
			foreach (var abilityName in c.Abilities)
			{
				if (DataRegistry.Instance.Abilities.TryGetValue(abilityName, out var ability))
				{
					combatant.Abilities.Add(ability);
				}
			}
		}

		// If no abilities assigned, give the Adventurer defaults
		if (combatant.Abilities.Count == 0)
		{
			var defaultAbilities = new[] { "Power Strike", "Spark", "Focus", "Brace", "Bubble" };
			foreach (var name in defaultAbilities)
			{
				if (DataRegistry.Instance.Abilities.TryGetValue(name, out var ability))
				{
					combatant.Abilities.Add(ability);
				}
			}
		}

		return combatant;
	}

	public static CombatantData FromMonster(MonsterInstance m)
	{
		var combatant = new CombatantData
		{
			Name = m.Template.MonsterName,
			CurrentHP = m.CurrentHP,
			MaxHP = m.MaxHP,
			CurrentMP = m.CurrentMP,
			MaxMP = m.MaxMP,
			Stats = new Dictionary<Stat.StatKey, int>(m.CurrentStats),
			Attribute = m.Template.Attribute,
			Abilities = new List<AbilityData>(m.Abilities),
			WeaponPower = m.Template.NaturalWeaponPower,
			IsPlayer = false,
			SourceMonster = m
		};

		// Load abilities from DataRegistry
		foreach (var abilityName in m.Template.AbilityKeys)
		{
			if (DataRegistry.Instance.Abilities.TryGetValue(abilityName, out var ability))
			{
				combatant.Abilities.Add(ability);
			}
		}

		return combatant;
	}

	/// <summary>
	/// Tick buff durations down. Remove expired buffs.
	/// </summary>
	public void TickBuffs()
	{
		var expired = new List<string>();
		foreach (var key in BuffDurations.Keys.ToList())
		{
			BuffDurations[key]--;
			if (BuffDurations[key] <= 0)
			{
				expired.Add(key);
			}
		}

		foreach (var key in expired)
		{
			Buffs.Remove(key);
			BuffDurations.Remove(key);
		}
	}

	public void ApplyBuff(string statName, float amount, int duration)
	{
		if (Buffs.ContainsKey(statName))
		{
			Buffs[statName] = Math.Max(Buffs[statName], amount);
			BuffDurations[statName] = Math.Max(BuffDurations[statName], duration);
		}
		else
		{
			Buffs[statName] = amount;
			BuffDurations[statName] = duration;
		}
	}

	public bool IsAlive => CurrentHP > 0;
}
