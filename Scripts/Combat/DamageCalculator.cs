using Godot;
using System;
using System.Collections.Generic;
using CharacterData;

public static class DamageCalculator
{
	/// <summary>
	/// Basic Attack: STR * (1 + WeaponPower/100) * (UserSTR / TargetDEF) * Crit
	/// </summary>
	public static int CalculateBasicAttack(CombatantData user, CombatantData target)
	{
		int str = GetBuffedStat(user, Stat.StatKey.STR);
		int targetDef = GetBuffedStat(target, Stat.StatKey.DEF);

		float damage = str * (1f + user.WeaponPower / 100f) * ((float)str / Math.Max(targetDef, 1));
		damage *= CalculateCrit(user, out bool wasCrit);
		user.LastActionWasCrit = wasCrit;
		return Math.Max(1, Mathf.FloorToInt(damage));
	}

	/// <summary>
	/// Ability: relevant_stat * (1 + AbilityPower/100) * (relevant/targetDef) * AttrMod * Crit
	/// </summary>
	public static int CalculateAbilityDamage(CombatantData user, CombatantData target, AbilityData ability)
	{
		bool isMagical = ability.Attribute != "Physical";
		var attackStat = isMagical ? Stat.StatKey.INT : Stat.StatKey.STR;
		var defenseStat = isMagical ? Stat.StatKey.MDF : Stat.StatKey.DEF;

		int atk = GetBuffedStat(user, attackStat);
		int def = GetBuffedStat(target, defenseStat);

		float damage = atk * (1f + ability.AbilityPower / 100f) * ((float)atk / Math.Max(def, 1));
		damage *= CalculateAttributeInteraction(ability.Attribute, target.Attribute);
		damage *= CalculateCrit(user, out bool wasCrit);
		user.LastActionWasCrit = wasCrit;
		return Math.Max(1, Mathf.FloorToInt(damage));
	}

	/// <summary>
	/// Healing: SPI * (1 + HealPower/100)
	/// </summary>
	public static int CalculateHealing(CombatantData user, AbilityData ability)
	{
		int spi = GetBuffedStat(user, Stat.StatKey.SPI);
		float healing = spi * (1f + ability.HealPower / 100f);
		return Math.Max(1, Mathf.FloorToInt(healing));
	}

	/// <summary>
	/// Hit check: ability.Accuracy + (UserACC / 10)
	/// </summary>
	public static bool RollToHit(CombatantData user, int baseAccuracy)
	{
		int acc = GetBuffedStat(user, Stat.StatKey.ACC);
		float hitChance = (baseAccuracy + acc / 10f) / 100f;
		return GD.Randf() < hitChance;
	}

	/// <summary>
	/// Crit: 5 * (1 + LCK)% chance for 1.5x damage
	/// </summary>
	public static float CalculateCrit(CombatantData user, out bool wasCrit)
	{
		int lck = GetBuffedStat(user, Stat.StatKey.LCK);
		float critRate = 5f * (1f + lck) / 100f;
		wasCrit = GD.Randf() < critRate;
		return wasCrit ? 1.5f : 1.0f;
	}

	/// <summary>
	/// Elemental interaction: Weakness = 1.5x, Resistance = 0.5x, Same = 0.5x, Neutral = 1.0x
	/// Fire > Wind > Earth > Water > Fire, Light <> Dark
	/// </summary>
	public static float CalculateAttributeInteraction(string attackAttr, string defenderAttr)
	{
		if (attackAttr == "Physical" || defenderAttr == "Physical")
			return 1.0f;

		var weaknesses = new Dictionary<string, string>
		{
			{ "Fire", "Wind" },
			{ "Wind", "Earth" },
			{ "Earth", "Water" },
			{ "Water", "Fire" },
			{ "Dark", "Light" },
			{ "Light", "Dark" }
		};

		if (weaknesses.TryGetValue(attackAttr, out string weakTo) && weakTo == defenderAttr)
			return 1.5f;

		if (weaknesses.TryGetValue(defenderAttr, out string strongVs) && strongVs == attackAttr)
			return 0.5f;

		if (attackAttr == defenderAttr)
			return 0.5f;

		return 1.0f;
	}

	/// <summary>
	/// Flee check: 50% + (UserSPD - EnemySPD) * 2% + LCK * 1%
	/// </summary>
	public static bool RollToFlee(CombatantData user, CombatantData enemy)
	{
		int userSpd = GetBuffedStat(user, Stat.StatKey.SPD);
		int enemySpd = GetBuffedStat(enemy, Stat.StatKey.SPD);
		int userLck = GetBuffedStat(user, Stat.StatKey.LCK);

		float fleeChance = 0.5f + (userSpd - enemySpd) * 0.02f + userLck * 0.01f;
		fleeChance = Mathf.Clamp(fleeChance, 0.2f, 0.95f);
		return GD.Randf() < fleeChance;
	}

	public static int GetBuffedStat(CombatantData combatant, Stat.StatKey stat)
	{
		int baseValue = combatant.Stats.TryGetValue(stat, out var val) ? val : 1;
		string statName = Stat.StatKeyNames[stat];
		float buff = combatant.Buffs.TryGetValue(statName, out var b) ? b : 0f;
		return Math.Max(1, Mathf.FloorToInt(baseValue * (1f + buff)));
	}
}
