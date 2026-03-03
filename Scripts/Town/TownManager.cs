using System;
using System.Collections.Generic;
using CharacterData;
using Godot;
using TownResources;

public partial class TownManager : Node
{
	public static TownManager Instance { get; private set; }
	public int TownLevel { get; set; } = 1;
	public int Renown { get; set; } = 0;
	public TownResource Urum { get; set; }
	public TownResource Terratite { get; set; }
	public TownResource Aquatite { get; set; }
	public TownResource Ventite { get; set; }
	public TownResource Ignitite { get; set; }
	public TownResource Lumia { get; set; }
	public TownResource Tenebria { get; set; }

	public TownResource this[ResourceType resourceKey]
	{
		get
		{
			return resourceKey switch
			{
				ResourceType.Urum => Urum,
				ResourceType.Terratite => Terratite,
				ResourceType.Aquatite => Aquatite,
				ResourceType.Ventite => Ventite,
				ResourceType.Ignitite => Ignitite,
				ResourceType.Lumia => Lumia,
				ResourceType.Tenebria => Tenebria,
				_ => throw new ArgumentOutOfRangeException(nameof(resourceKey), "Invalid resource type")
			};
		}
	}

	private Label _resourceLabel;
	private CanvasLayer _hudLayer;

	public GameSignalBus GameSignalBus;

	public override void _Ready()
	{
		Instance = this;
		GameSignalBus = GameSignalBus.Instance;
		GameSignalBus.Connect(GameSignalBus.SignalName.OnBuildingPlaced, Callable.From<string>(OnBuildingPlaced));
		GameSignalBus.Connect(GameSignalBus.SignalName.ResourceCollected, Callable.From<ResourceCollectEvent>(OnResourceCollected));
		InitializeTownResources();
		CreateResourceHUD();
		GameSignalBus.Connect(GameSignalBus.SignalName.QuestCompleted, Callable.From<Quest, Character>(OnQuestCompleted));
	}

	private void OnResourceCollected(ResourceCollectEvent @event)
	{
		if (@event != null)
		{
			GD.Print($"Resource collected: {@event.EventData.ResourceKey} - {@event.EventData.Amount}");
			this[@event.EventData.ResourceKey].Amount += @event.EventData.Amount;
			GD.Print($"New amount: {this[@event.EventData.ResourceKey].Amount}");
			UpdateResourceHUD();
		}
	}

	private void InitializeTownResources()
	{
		Urum = new TownResource { ResourceKey = ResourceType.Urum, Amount = 0 };
		Terratite = new TownResource { ResourceKey = ResourceType.Terratite, Amount = 0 };
		Aquatite = new TownResource { ResourceKey = ResourceType.Aquatite, Amount = 0 };
		Ventite = new TownResource { ResourceKey = ResourceType.Ventite, Amount = 0 };
		Ignitite = new TownResource { ResourceKey = ResourceType.Ignitite, Amount = 0 };
		Lumia = new TownResource { ResourceKey = ResourceType.Lumia, Amount = 0 };
		Tenebria = new TownResource { ResourceKey = ResourceType.Tenebria, Amount = 0 };
	}

	private void CreateResourceHUD()
	{
		_hudLayer = new CanvasLayer();
		_hudLayer.Layer = 10;
		AddChild(_hudLayer);

		var panel = new PanelContainer();
		panel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		panel.OffsetBottom = 32;
		var style = new StyleBoxFlat();
		style.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.85f);
		panel.AddThemeStyleboxOverride("panel", style);
		_hudLayer.AddChild(panel);

		_resourceLabel = new Label();
		_resourceLabel.HorizontalAlignment = HorizontalAlignment.Center;
		_resourceLabel.AddThemeFontSizeOverride("font_size", 14);
		panel.AddChild(_resourceLabel);

		UpdateResourceHUD();
	}

	public void UpdateResourceHUD()
	{
		if (_resourceLabel != null)
		{
			_resourceLabel.Text = $"Urum: {Urum.Amount}  Terratite: {Terratite.Amount}  Aquatite: {Aquatite.Amount}  Ventite: {Ventite.Amount}  Ignitite: {Ignitite.Amount}  Lumia: {Lumia.Amount}  Tenebria: {Tenebria.Amount}";
		}
	}

	private void OnQuestCompleted(Quest completedQuest, Character assignedCharacter)
	{
		if (completedQuest != null)
		{
			GD.Print($"Quest completed: {completedQuest.QuestName}");
			foreach (var reward in completedQuest.Rewards)
			{
				switch (reward.Type)
				{
					case QuestReward.RewardType.Resource:
						break;
					case QuestReward.RewardType.Item:
						// Handle item rewards
						break;
					case QuestReward.RewardType.Renown:
						Renown += reward.Amount;
						break;
				}
			}
			GD.Print("Quest rewards distributed!");
			UpdateResourceHUD();
		}
	}

	private void OnBuildingPlaced(string buildingKey)
	{
		if (buildingKey == "GuildHall")
		{
			TownLevel++;
		}
	}

	public bool HasNecessaryResources(Godot.Collections.Array<TownResource> buildingRequirements)
	{
		foreach (TownResource requirement in buildingRequirements)
		{
			if (requirement.Amount > this[requirement.ResourceKey].Amount)
			{
				return false;
			}
		}
		return true;
	}

	public void SpendResources(Godot.Collections.Array<TownResource> buildingRequirements)
	{
		foreach (TownResource requirement in buildingRequirements)
		{
			this[requirement.ResourceKey].Amount -= requirement.Amount;
		}
	}
}
