using CharacterData;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TownResources;

public partial class QuestManager : Node
{
    [Export]
    public static QuestManager Instance;
    public DataRegistry DataRegistry;
    public GameSignalBus GameSignalBus;

    public struct ActiveQuest
    {
        public Quest Quest;
        public Character AssignedCharacter;
        public int Progress;
    }

    public struct CompletedQuest
    {
        public Quest Quest;
        public Character AssignedCharacter;
        public bool ResourcesCollected;
    }

    public List<ActiveQuest> ActiveQuests = new List<ActiveQuest>();
    public List<CompletedQuest> CompletedQuests = new List<CompletedQuest>();
    public List<Quest> AvailableQuests = new List<Quest>();

    public override void _Ready()
    {
        Instance = this;
        DataRegistry = DataRegistry.Instance;
        GameSignalBus = GameSignalBus.Instance;
        // Load quests or initialize quest data here
        GetAvailableQuests();

        GameSignalBus.Connect(GameSignalBus.SignalName.ResourceCollected, Callable.From<QuestEvent>((questEvent) =>
        {
            OnGameSignalReceived(GameSignalBus.SignalName.ResourceCollected, questEvent);
        }));
    }

    public void OnGameSignalReceived(string signalName, QuestEvent questEvent)
    {
        GD.Print($"Received signal: {signalName}");
        var relevantQuests = ActiveQuests.Where(quest => quest.AssignedCharacter == questEvent.AttributedCharacter).ToList();
        foreach (var activeQuest in relevantQuests)
        {
            foreach (var goal in activeQuest.Quest.Goals)
            {
                goal.OnEvent(signalName, questEvent);
            }

            if (activeQuest.Quest.Goals.All(g => g.IsComplete()))
            {
                GD.Print($"Quest {activeQuest.Quest.QuestName} is complete for character {activeQuest.AssignedCharacter.CharacterName}");
                CompleteQuest(activeQuest.Quest, activeQuest.AssignedCharacter);
            }
        }
    }

    public void GetAvailableQuests()
    {
        // Load quests from the DataRegistry or any other source
        foreach (var quest in DataRegistry.Quests.Values)
        {
            AvailableQuests.Add(quest);
            GD.Print($"Available quest: {quest.QuestName}");
        }
    }

    public void StartQuest(Quest quest, Character assignedCharacter)
    {
        GD.Print($"Starting quest: {quest.QuestName}");

        var activeQuest = new ActiveQuest
        {
            Quest = quest,
            AssignedCharacter = assignedCharacter,
            Progress = 0
        };
        // Logic to start the quest
        ActiveQuests.Add(activeQuest);

        if (IsRepeatableQuest(quest))
        {
            GD.Print($"Quest {quest.QuestName} is repeatable and already active.");
            return;
        }
        else
        {
            AvailableQuests.Remove(quest);
        }
    }

    public void AssignQuestToNPC(Quest selectedQuest, Character selectedCharacter)
    {
        GameSignalBus.EmitSignal(GameSignalBus.SignalName.NPCStartedQuest, selectedQuest, selectedCharacter);
        StartQuest(selectedQuest, selectedCharacter);
    }

    public void AssignQuestToPlayer(Quest selectedQuest, Character playerCharacter)
    {
        GameSignalBus.EmitSignal(GameSignalBus.SignalName.PlayerStartedQuest, selectedQuest, playerCharacter);
        StartQuest(selectedQuest, playerCharacter);
    }

    private bool IsRepeatableQuest(Quest quest)
    {
        bool IsRepeatable = false;
        if (quest.IsRepeatable)
        {
            IsRepeatable = true;
        }

        if (quest.RepeatableCount > 0)
        {
            if (ActiveQuests.FindAll(x => x.Quest == quest).Count >= quest.RepeatableCount)
            {
                IsRepeatable = false;
            }
        }
        else if (quest.RepeatableCount == -1)
        {
            IsRepeatable = true;
        }

        return IsRepeatable;
    }


    public void CompleteQuest(Quest quest, Character assignedCharacter)
    {
        GD.Print($"Completing quest: {quest.QuestName}");

        var completedQuest = new CompletedQuest
        {
            Quest = quest,
            AssignedCharacter = assignedCharacter,
            ResourcesCollected = false
        };

        GD.Print($"Quest {quest.QuestName} completed for character {assignedCharacter.CharacterName}");
        GameSignalBus.EmitSignal(GameSignalBus.SignalName.QuestCompleted, quest, assignedCharacter);

        CompletedQuests.Add(completedQuest);

        var activeQuest = ActiveQuests.Find(x => x.Quest == quest && x.AssignedCharacter == assignedCharacter);
        ActiveQuests.Remove(activeQuest);
    }

    internal bool IsCharacterOnQuest(Character npc)
    {
        return ActiveQuests.Any(x => x.AssignedCharacter.CharacterId == npc.CharacterId);
    }

    /// <summary>
    /// Returns all active quests assigned to NPCs (non-empty CharacterId).
    /// Used by QuestResolver for end-of-day auto-resolution.
    /// </summary>
    public List<ActiveQuest> GetNPCQuests()
    {
        return ActiveQuests.FindAll(q => q.AssignedCharacter != null && q.AssignedCharacter.CharacterId != System.Guid.Empty);
    }

    /// <summary>
    /// Re-adds quests from the DataRegistry that are neither available nor currently active.
    /// Called after end-of-day resolution to replenish the quest board.
    /// </summary>
    public void RefreshAvailableQuests()
    {
        foreach (var quest in DataRegistry.Quests.Values)
        {
            bool alreadyAvailable = AvailableQuests.Contains(quest);
            bool currentlyActive = ActiveQuests.Exists(q => q.Quest == quest);
            if (!alreadyAvailable && !currentlyActive)
            {
                AvailableQuests.Add(quest);
                GD.Print($"Quest refreshed: {quest.QuestName}");
            }
        }
    }
}
