using Godot;
using Godot.Collections;
using CharacterData;

[GlobalClass]
public partial class MonsterData : Resource
{
	[Export] public string MonsterName { get; set; } = "Monster";
	[Export] public string Description { get; set; } = "";
	[Export] public string Behavior { get; set; } = "Aggressive";
	[Export] public string Attribute { get; set; } = "Physical";
	[Export] public int NaturalWeaponPower { get; set; } = 30;
	[Export] public Dictionary<Stat.StatKey, int> BaseStats { get; set; } = new();
	[Export] public Dictionary<Stat.StatKey, float> ScalingMultipliers { get; set; } = new();
	[Export] public string[] AbilityKeys { get; set; } = System.Array.Empty<string>();
	[Export] public int BaseXPReward { get; set; } = 50;
	[Export] public int BaseGoldReward { get; set; } = 10;
}
