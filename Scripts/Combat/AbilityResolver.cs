using Godot;
using System;

/// <summary>
/// Resolves ability effects: damage, buff/debuff, healing.
/// Returns a human-readable result string for the combat log.
/// </summary>
public static class AbilityResolver
{
	public static string Resolve(CombatantData user, CombatantData target, AbilityData ability)
	{
		// Check if user has enough MP
		if (user.CurrentMP < ability.MPCost)
		{
			return $"{user.Name} doesn't have enough MP for {ability.AbilityName}!";
		}

		// Spend MP
		user.CurrentMP -= ability.MPCost;

		switch (ability.Type)
		{
			case AbilityData.AbilityType.Damaging:
				return ResolveDamage(user, target, ability);

			case AbilityData.AbilityType.Buff:
				return ResolveBuff(user, ability);

			case AbilityData.AbilityType.Restorative:
				return ResolveHealing(user, ability);

			case AbilityData.AbilityType.Status:
				return $"{user.Name} used {ability.AbilityName}! (Status effects not yet implemented)";

			case AbilityData.AbilityType.Info:
				return $"{user.Name} used {ability.AbilityName}! (Info abilities not yet implemented)";

			default:
				return $"{user.Name} used {ability.AbilityName}!";
		}
	}

	private static string ResolveDamage(CombatantData user, CombatantData target, AbilityData ability)
	{
		// Hit check
		if (!DamageCalculator.RollToHit(user, ability.Accuracy))
		{
			return $"{user.Name} used {ability.AbilityName} but missed!";
		}

		int damage = DamageCalculator.CalculateAbilityDamage(user, target, ability);
		target.CurrentHP = Math.Max(0, target.CurrentHP - damage);

		string critText = user.LastActionWasCrit ? " Critical hit!" : "";
		string effectiveText = "";

		float attrMod = DamageCalculator.CalculateAttributeInteraction(ability.Attribute, target.Attribute);
		if (attrMod > 1f) effectiveText = " It's super effective!";
		else if (attrMod < 1f) effectiveText = " It's not very effective...";

		return $"{user.Name} used {ability.AbilityName}! Dealt {damage} damage.{critText}{effectiveText}";
	}

	private static string ResolveBuff(CombatantData user, AbilityData ability)
	{
		user.ApplyBuff(ability.BuffStat, ability.BuffAmount, ability.BuffDuration);
		return $"{user.Name} used {ability.AbilityName}! {ability.BuffStat} increased for {ability.BuffDuration} turns.";
	}

	private static string ResolveHealing(CombatantData user, AbilityData ability)
	{
		int healing = DamageCalculator.CalculateHealing(user, ability);
		int actualHeal = Math.Min(healing, user.MaxHP - user.CurrentHP);
		user.CurrentHP = Math.Min(user.MaxHP, user.CurrentHP + healing);
		return $"{user.Name} used {ability.AbilityName}! Restored {actualHeal} HP.";
	}
}
