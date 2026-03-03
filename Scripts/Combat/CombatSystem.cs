using Godot;
using System;
using System.Collections.Generic;
using CharacterData;

/// <summary>
/// Main combat controller. Manages turn-based 1v1 combat between player and enemy.
/// Uses a state machine to progress through combat phases.
/// Integrates with ExplorationManager for party/monster data and GameSignalBus for signals.
/// </summary>
public partial class CombatSystem : Node
{
	public enum CombatPhase
	{
		Setup,
		PlayerTurn,
		PlayerAction,
		EnemyTurn,
		EnemyAction,
		Victory,
		Defeat,
		Fled
	}

	public CombatPhase Phase { get; private set; } = CombatPhase.Setup;
	public CombatantData PlayerCombatant;
	public CombatantData EnemyCombatant;

	private CombatUI _combatUI;
	private int _turnNumber = 0;

	[Signal] public delegate void CombatLogEntryEventHandler(string message);
	[Signal] public delegate void CombatEndedEventHandler(bool playerWon);
	[Signal] public delegate void TurnChangedEventHandler(bool isPlayerTurn);
	[Signal] public delegate void HPChangedEventHandler();

	public override void _Ready()
	{
		// Build the UI
		_combatUI = new CombatUI();
		AddChild(_combatUI);
		_combatUI.Initialize(this);

		// Connect UI signals
		_combatUI.AttackPressed += OnAttackPressed;
		_combatUI.AbilitySelected += OnAbilitySelected;
		_combatUI.RunPressed += OnRunPressed;
		_combatUI.ContinuePressed += OnContinuePressed;

		// Start combat setup
		CallDeferred(nameof(SetupCombat));
	}

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

		// Use first party member as active combatant (1v1 for now)
		PlayerCombatant = CombatantData.FromCharacter(party[0]);

		// Get monsters from current room
		var currentRoom = ExplorationManager.Instance.GetCurrentRoom();
		if (currentRoom == null || currentRoom.Monsters.Count == 0)
		{
			GD.PrintErr("No monsters in current room!");
			GetTree().ChangeSceneToFile("res://Scenes/ExplorationRunner.tscn");
			return;
		}

		// Pick first monster, convert MonsterTemplate -> MonsterInstance
		var monsterTemplate = currentRoom.Monsters[0];
		int monsterLevel = (int)GD.RandRange(1, 5);
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

	private void StartPlayerTurn()
	{
		_turnNumber++;
		Phase = CombatPhase.PlayerTurn;
		PlayerCombatant.TickBuffs();
		EmitSignal(SignalName.TurnChanged, true);
		_combatUI.ShowActionMenu();
	}

	private void StartEnemyTurn()
	{
		Phase = CombatPhase.EnemyTurn;
		EnemyCombatant.TickBuffs();
		EmitSignal(SignalName.TurnChanged, false);
		_combatUI.HideActionMenu();

		// Small delay before enemy acts so the player can see what's happening
		var timer = GetTree().CreateTimer(0.8);
		timer.Timeout += ExecuteEnemyAction;
	}

	private void ExecuteEnemyAction()
	{
		Phase = CombatPhase.EnemyAction;

		var action = EnemyAI.DecideAction(EnemyCombatant, PlayerCombatant);

		switch (action.Type)
		{
			case CombatActionType.Attack:
				ExecuteBasicAttack(EnemyCombatant, PlayerCombatant);
				break;

			case CombatActionType.Ability:
				string result = AbilityResolver.Resolve(EnemyCombatant, PlayerCombatant, action.Ability);
				Log(result);
				break;
		}

		EmitSignal(SignalName.HPChanged);

		if (CheckCombatEnd())
			return;

		// Give player their turn next
		var timer = GetTree().CreateTimer(0.5);
		timer.Timeout += StartPlayerTurn;
	}

	// --- Player Actions ---

	private void OnAttackPressed()
	{
		if (Phase != CombatPhase.PlayerTurn) return;
		Phase = CombatPhase.PlayerAction;
		_combatUI.HideActionMenu();

		ExecuteBasicAttack(PlayerCombatant, EnemyCombatant);
		EmitSignal(SignalName.HPChanged);

		if (CheckCombatEnd())
			return;

		var timer = GetTree().CreateTimer(0.5);
		timer.Timeout += StartEnemyTurn;
	}

	private void OnAbilitySelected(AbilityData ability)
	{
		if (Phase != CombatPhase.PlayerTurn) return;
		Phase = CombatPhase.PlayerAction;
		_combatUI.HideActionMenu();
		_combatUI.HideAbilityList();

		CombatantData target;
		if (ability.Target == AbilityData.TargetType.Self ||
			ability.Target == AbilityData.TargetType.SingleAlly ||
			ability.Target == AbilityData.TargetType.AllAllies)
		{
			target = PlayerCombatant;
		}
		else
		{
			target = EnemyCombatant;
		}

		string result = AbilityResolver.Resolve(PlayerCombatant, target, ability);
		Log(result);
		EmitSignal(SignalName.HPChanged);

		if (CheckCombatEnd())
			return;

		var timer = GetTree().CreateTimer(0.5);
		timer.Timeout += StartEnemyTurn;
	}

	private void OnRunPressed()
	{
		if (Phase != CombatPhase.PlayerTurn) return;
		Phase = CombatPhase.PlayerAction;
		_combatUI.HideActionMenu();

		if (DamageCalculator.RollToFlee(PlayerCombatant, EnemyCombatant))
		{
			Log($"{PlayerCombatant.Name} escaped successfully!");
			Phase = CombatPhase.Fled;
			_combatUI.ShowResult("Escaped!", "You fled from battle.\nThe quest remains incomplete.");
		}
		else
		{
			Log($"{PlayerCombatant.Name} couldn't escape!");
			var timer = GetTree().CreateTimer(0.5);
			timer.Timeout += StartEnemyTurn;
		}
	}

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

	// --- Combat Logic ---

	private void ExecuteBasicAttack(CombatantData attacker, CombatantData defender)
	{
		if (!DamageCalculator.RollToHit(attacker, 90))
		{
			Log($"{attacker.Name} attacked but missed!");
			return;
		}

		int damage = DamageCalculator.CalculateBasicAttack(attacker, defender);
		defender.CurrentHP = Math.Max(0, defender.CurrentHP - damage);

		string critText = attacker.LastActionWasCrit ? " Critical hit!" : "";
		Log($"{attacker.Name} attacks {defender.Name} for {damage} damage!{critText}");
	}

	private bool CheckCombatEnd()
	{
		if (!EnemyCombatant.IsAlive)
		{
			Phase = CombatPhase.Victory;
			int xp = 0;
			int gold = 0;
			if (EnemyCombatant.SourceMonster != null)
			{
				xp = EnemyCombatant.SourceMonster.Template.BaseXPReward +
					 EnemyCombatant.SourceMonster.Level * 10;
				gold = EnemyCombatant.SourceMonster.Template.BaseGoldReward +
					   EnemyCombatant.SourceMonster.Level * 5;
			}
			Log($"{EnemyCombatant.Name} was defeated!");
			_combatUI.ShowResult("Victory!", $"Earned {xp} XP and {gold} Gold.");
			return true;
		}

		if (!PlayerCombatant.IsAlive)
		{
			Phase = CombatPhase.Defeat;
			Log($"{PlayerCombatant.Name} was defeated...");
			_combatUI.ShowResult("Defeat", "You have been defeated.\nReturning to town...");
			return true;
		}

		return false;
	}

	private void Log(string message)
	{
		GD.Print($"[Combat] {message}");
		EmitSignal(SignalName.CombatLogEntry, message);
	}
}
