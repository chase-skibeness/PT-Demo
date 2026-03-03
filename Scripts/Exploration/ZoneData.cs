using Godot;

[GlobalClass]
public partial class ZoneData : Resource
{
	[Export] public string ZoneName { get; set; } = "Unknown Zone";
	[Export] public string Description { get; set; } = "";
	[Export] public int MinLevel { get; set; } = 1;
	[Export] public int MaxLevel { get; set; } = 5;
	[Export] public string[] MonsterPool { get; set; } = System.Array.Empty<string>();
	[Export] public string SceneBackground { get; set; } = "";
}
