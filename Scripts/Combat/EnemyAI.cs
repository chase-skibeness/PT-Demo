using Godot;
using System.Collections.Generic;

public static class EnemyAI
{
	/// <summary>
	/// Simple aggressive AI: attacks most of the time, occasionally uses abilities.
	/// </summary>
	public static CombatAction DecideAction(CombatantData enemy, CombatantData player)
	{
		// Try to use an ability (40% chance if MP available)
		if (enemy.Abilities.Count > 0 && GD.Randf() < 0.4f)
		{
			// Find an affordable ability
			foreach (var ability in enemy.Abilities)
			{
				if (enemy.CurrentMP >= ability.MPCost)
				{
					return new CombatAction
					{
						Type = CombatActionType.Ability,
						Ability = ability,
						Target = player
					};
				}
			}
		}

		// Default: basic attack
		return new CombatAction
		{
			Type = CombatActionType.Attack,
			Target = player
		};
	}
}

public struct CombatAction
{
	public CombatActionType Type;
	public AbilityData Ability;
	public CombatantData Target;
}

public enum CombatActionType
{
	Attack,
	Ability,
	Item,
	Run
}
