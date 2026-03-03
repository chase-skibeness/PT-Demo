using Godot;

[GlobalClass]
public partial class AbilityData : Resource
{
	public enum AbilityType
	{
		Damaging,
		Buff,
		Restorative,
		Status,
		Info
	}

	public enum TargetType
	{
		SingleEnemy,
		AllEnemies,
		SingleAlly,
		AllAllies,
		Self
	}

	[Export] public string AbilityName { get; set; } = "New Ability";
	[Export] public string Description { get; set; } = "";
	[Export] public int MPCost { get; set; } = 0;
	[Export] public AbilityType Type { get; set; } = AbilityType.Damaging;
	[Export] public string Attribute { get; set; } = "Physical";
	[Export] public int AbilityPower { get; set; } = 0;
	[Export] public int Accuracy { get; set; } = 85;
	[Export] public TargetType Target { get; set; } = TargetType.SingleEnemy;
	[Export] public string FormulaKey { get; set; } = "physical_damage";

	// For buff/debuff abilities
	[Export] public string BuffStat { get; set; } = "";
	[Export] public float BuffAmount { get; set; } = 0f;
	[Export] public int BuffDuration { get; set; } = 3;

	// For restorative abilities
	[Export] public int HealPower { get; set; } = 0;
}
