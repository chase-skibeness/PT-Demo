using Godot;
using Godot.Collections;
using System;

public partial class CombatRunner : Node3D
{
    [Export]
    public Array<Node3D> PartyZones;

    [Export]
    public Array<Node3D> EnemyZones;

    public Button AttackButton;
    public Button AbilityButton;
    public Button EscapeButton;
    public Button ItemButton;

    public GeneratedRoom CurrentRoom => ExplorationManager.Instance.GetCurrentRoom();

    public override void _Ready()
    {
        PartyZones = new Array<Node3D>();
        EnemyZones = new Array<Node3D>();
        foreach (Node3D zone in GetNode<Node3D>("PartyZones").GetChildren())
        {
            PartyZones.Add(zone);
        }

        foreach (Node3D zone in GetNode<Node3D>("EnemyZones").GetChildren())
        {
            EnemyZones.Add(zone);
        }

        AttackButton = GetNode<Button>("CombatMenu/HSC/ActionMenu/AttackButton");
        AbilityButton = GetNode<Button>("CombatMenu/HSC/ActionMenu/AbilityButton");
        EscapeButton = GetNode<Button>("CombatMenu/HSC/ActionMenu/EscapeButton");
        ItemButton = GetNode<Button>("CombatMenu/HSC/ActionMenu/ItemButton");

        AttackButton.GrabFocus();

        GD.Print($"{CurrentRoom.Monsters.Count} monsters in room.");
        for (int i = 0; i < CurrentRoom.Monsters.Count; i++)
        {
            var monster = CurrentRoom.Monsters[i];
            if (i < EnemyZones.Count)
            {
                var zone = EnemyZones[i];
                var monsterNode = monster.MonsterModel.Instantiate() as Node3D;
                zone.AddChild(monsterNode);
            }
        }

        for (int i = 0; i < ExplorationManager.Instance.PartyMembers.Count; i++)
        {
            var character = ExplorationManager.Instance.PartyMembers[i];
            if (i < PartyZones.Count)
            {
                var zone = PartyZones[i];
                var characterNode = character.CharacterModel.Instantiate() as Node3D;
                zone.AddChild(characterNode);
            }
        }

        // Initialize the combat system
        var combatSystem = new CombatSystem();
        combatSystem.Name = "CombatSystem";
        AddChild(combatSystem);
    }

}
