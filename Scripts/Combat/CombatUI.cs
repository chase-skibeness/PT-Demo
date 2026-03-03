using Godot;
using System;

/// <summary>
/// Stub for CombatUI. Will be fully implemented in Task 57.
/// Provides the signal and method signatures that CombatSystem depends on.
/// </summary>
public partial class CombatUI : CanvasLayer
{
	[Signal] public delegate void AttackPressedEventHandler();
	[Signal] public delegate void AbilitySelectedEventHandler(AbilityData ability);
	[Signal] public delegate void RunPressedEventHandler();
	[Signal] public delegate void ContinuePressedEventHandler();

	private CombatSystem _combatSystem;

	public void Initialize(CombatSystem combatSystem)
	{
		_combatSystem = combatSystem;
	}

	public void ShowActionMenu() { }
	public void HideActionMenu() { }
	public void HideAbilityList() { }
	public void ShowResult(string title, string details) { }
}
