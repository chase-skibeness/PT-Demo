using CharacterData;
using Godot;
using ChronosSpace;
using System;
using TownResources;

public partial class GameSignalBus : Node
{

    public static GameSignalBus Instance;

    public override void _Ready()
    {
        Instance = this;
    }

    public void EmitSignalDynamic(string signalName, Variant[] signalArgs)
    {
        switch (signalArgs.Length)
        {
            case 0:
                EmitSignal(signalName);
                break;
            case 1:
                EmitSignal(signalName, signalArgs[0]);
                break;
            case 2:
                EmitSignal(signalName, signalArgs[0], signalArgs[1]);
                break;
            default:
                GD.PrintErr($"Unsupported number of args ({signalArgs.Length}) for signal: {signalName}");
                break;
        }
    }

    // Time
    [Signal]
    public delegate void RegisterTimeHookEventHandler(ChronosTimeHook timeHook);

    [Signal]
    public delegate void DayEndedEventHandler();

    [Signal]
    public delegate void DayStartedEventHandler();

    [Signal]
    public delegate void FadeOutFinishedEventHandler();

    [Signal]
    public delegate void FadeInFinishedEventHandler();

    // Quest
    [Signal]
    public delegate void ResourceCollectedEventHandler(ResourceCollectEvent resourceCollectEvent);

    [Signal]
    public delegate void PlayerStartedQuestEventHandler(Quest quest, Character character);

    [Signal]
    public delegate void NPCStartedQuestEventHandler(Quest quest, Character character);

    [Signal]
    public delegate void QuestCompletedEventHandler(Quest quest);

    // Building
    [Signal]
    public delegate void OnBuildingPlacedEventHandler(string buildingKey);

    // Town
    [Signal]
    public delegate void TownResourceUpdatedEventHandler(ResourceType resourceType);

    // Combat
    [Signal]
    public delegate void CombatStartedEventHandler();

    [Signal]
    public delegate void CombatEndedEventHandler(bool victory);

    // Day cycle (for EndOfDayReport)
    [Signal]
    public delegate void ShowEndOfDayReportEventHandler();

    [Signal]
    public delegate void EndOfDayReportClosedEventHandler();
}
