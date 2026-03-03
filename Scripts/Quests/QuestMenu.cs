using CharacterData;
using Godot;
using System;

public partial class QuestMenu : Control
{
    public Action OnClose;
    public PackedScene QuestPanelScene = GD.Load<PackedScene>("res://Scenes/UI/QuestPanel.tscn");
    public PackedScene NPCToAssignQuestPanelScene = GD.Load<PackedScene>("res://Scenes/UI/NPCToAssignQuest.tscn");
    public Quest selectedQuest;
    public Character selectedCharacter;
    public Player Player;
    private VBoxContainer AvailableQuestsList;
    private VBoxContainer AssignedQuestsList;
    private Panel NPCListPanel;
    private VBoxContainer NPCList;
    private Button AssignPlayerQuestButton;
    private Button AssignNPCQuestButton;

    public override void _Ready()
    {
        AssignPlayerQuestButton = GetNode<Button>("QuestMenuRoot/AvailableQuestsList/AssignPlayerQuestButton");
        AssignNPCQuestButton = GetNode<Button>("QuestMenuRoot/AvailableQuestsList/AssignNPCQuestButton");
        AvailableQuestsList = GetNode<VBoxContainer>("QuestMenuRoot/AvailableQuestsList/AvailableQuestList");
        AssignedQuestsList = GetNode<VBoxContainer>("QuestMenuRoot/AssignedQuestsList/AssignedQuestList");
        NPCListPanel = GetNode<Panel>("QuestMenuRoot/SelectNPCsList");
        NPCList = GetNode<VBoxContainer>("QuestMenuRoot/SelectNPCsList/SelectNPCList");

        AssignPlayerQuestButton.Pressed += OnPlayerQuestSelected;
        AssignNPCQuestButton.Pressed += OnNPCQuestSelected;

        this.VisibilityChanged += () => { if (Visible) AssignPlayerQuestButton.GrabFocus(); };
    }

    private void OnNPCQuestSelected()
    {
        if (NPCListPanel.Visible)
            return;

        if (selectedQuest == null)
            return;

        if (selectedQuest.Availability == Quest.QuestAvailability.Player)
        {
            GD.Print("Quest is only available for the player");
            return;
        }

        var npcs = CharacterSystem.Instance.GetAllNPCs();
        foreach (var npc in npcs)
        {
            if (!QuestManager.Instance.IsCharacterOnQuest(npc))
            {
                var npcPanel = NPCToAssignQuestPanelScene.Instantiate();
                var npcPanelData = npcPanel as NPCToAssignQuestData;
                npcPanelData.Initialize(npc);
                npcPanelData.OnCharacterSelected = (Character selectedCharacter) =>
                {
                    this.selectedCharacter = selectedCharacter;
                    QuestManager.Instance.AssignQuestToNPC(selectedQuest, selectedCharacter);
                    UpdateQuestsList();
                    AvailableQuestsList.FindNextValidFocus().GrabFocus();
                    foreach (NPCToAssignQuestData child in NPCList.GetChildren())
                    {
                        child._Toggled(child.AssignedCharacter == selectedCharacter);
                        child.QueueFree();
                    }
                    NPCListPanel.Visible = false;
                };
                NPCList.AddChild(npcPanel);
            }
        }
        NPCListPanel.Visible = true;
        NPCList.FindNextValidFocus().GrabFocus();
    }


    private void OnPlayerQuestSelected()
    {
        if (selectedQuest == null)
            return;

        if (selectedQuest.Availability == Quest.QuestAvailability.NPC)
        {
            GD.Print("Quest is only available for the NPC");
            return;
        }
        QuestManager.Instance.AssignQuestToPlayer(selectedQuest, Player.PlayerCharacter);
        UpdateQuestsList();
        selectedQuest = null;
    }

    private void UpdateQuestsList()
    {
        foreach (QuestPanelData child in AvailableQuestsList.GetChildren())
        {
            if (child.Quest == selectedQuest)
            {
                PopulateAssignedQuests();
                child.QueueFree();
                selectedCharacter = null;
                selectedQuest = null;
                break;
            }
        }
    }

    public void Open(Player player)
    {
        Visible = true;
        SetProcessInput(true);
        Player = player;
        PopulateAvailableQuests();
        PopulateAssignedQuests();
    }

    private void PopulateAvailableQuests()
    {
        foreach (var quest in QuestManager.Instance.AvailableQuests)
        {
            GD.Print($"Quest: {quest.QuestName}");
            GD.Print("Creating quest panel");
            var questPanel = QuestPanelScene.Instantiate();
            var questPanelData = questPanel as QuestPanelData;
            questPanelData._Toggled(false);
            questPanelData.Initialize(quest);
            questPanelData.OnQuestSelected = (Quest selectedQuest) =>
            {
                this.selectedQuest = selectedQuest;
                foreach (QuestPanelData child in AssignedQuestsList.GetChildren())
                {
                    child._Toggled(child.Quest == selectedQuest);
                }
            };
            AvailableQuestsList.AddChild(questPanel);
        }
    }

    private void PopulateAssignedQuests()
    {
        foreach (Node child in AssignedQuestsList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var quest in QuestManager.Instance.ActiveQuests)
        {
            GD.Print($"Quest: {quest.Quest.QuestName}");
            GD.Print("Creating quest panel");
            var questPanel = QuestPanelScene.Instantiate();
            var questPanelData = questPanel as QuestPanelData;
            questPanelData._Toggled(false);
            questPanelData.InitializeActiveQuest(quest.Quest, quest.AssignedCharacter);
            AssignedQuestsList.AddChild(questPanel);
        }
    }

    public void Close()
    {
        Visible = false;
        NPCListPanel.Visible = false;
        SetProcessInput(false);
        OnClose?.Invoke();
        foreach (Node child in AvailableQuestsList.GetChildren())
        {
            child.QueueFree();
        }
        foreach (Node child in AssignedQuestsList.GetChildren())
        {
            child.QueueFree();
        }
        foreach (Node child in NPCList.GetChildren())
        {
            child.QueueFree();
        }
        selectedQuest = null;
        selectedCharacter = null;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            Close();
        }
    }
}

