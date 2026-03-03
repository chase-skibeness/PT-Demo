using CharacterData;
using Godot;
using System;
using System.Collections.Generic;
using TownResources;

/// <summary>
/// Resolves NPC quests at end-of-day. NPCs don't fight real combat — we simulate
/// a success/fail roll based on their stats vs quest difficulty.
///
/// Success formula:
///   base 70% + 2% per character level + 1% per average stat
///   Clamped to [30%, 95%].
///
/// On success: full rewards (elemental resources, XP, renown).
/// On failure: partial XP, no resources.
/// </summary>
public static class QuestResolver
{
	public struct AppliedReward
	{
		public string Label;
		public int Amount;
	}

	public struct QuestResult
	{
		public Quest Quest;
		public Character Character;
		public bool Success;
		public int XPEarned;
		public string Summary;
		public List<AppliedReward> Rewards;
	}

	private static readonly Random _rng = new();

	/// <summary>
	/// Resolve a single NPC quest. Returns the result with rewards.
	/// </summary>
	public static QuestResult ResolveNPCQuest(QuestManager.ActiveQuest activeQuest)
	{
		var quest = activeQuest.Quest;
		var character = activeQuest.AssignedCharacter;

		float successChance = CalculateSuccessChance(quest, character);
		float roll = (float)_rng.NextDouble() * 100f;
		bool success = roll < successChance;

		var result = new QuestResult
		{
			Quest = quest,
			Character = character,
			Success = success,
			Rewards = new List<AppliedReward>(),
		};

		if (success)
		{
			// Apply quest rewards using the existing reward system
			foreach (var reward in quest.Rewards)
			{
				if (reward is ResourceQuestReward resourceReward)
				{
					TownManager.Instance[resourceReward.ResourceKey].Amount += resourceReward.Amount;
					result.Rewards.Add(new AppliedReward
					{
						Label = resourceReward.ResourceKey.ToString(),
						Amount = resourceReward.Amount
					});
				}
				else if (reward.Type == QuestReward.RewardType.Renown)
				{
					TownManager.Instance.Renown += reward.Amount;
					result.Rewards.Add(new AppliedReward
					{
						Label = "Renown",
						Amount = reward.Amount
					});
				}
				else if (reward.Type == QuestReward.RewardType.Experience)
				{
					result.Rewards.Add(new AppliedReward
					{
						Label = "Experience",
						Amount = reward.Amount
					});
				}
			}

			result.XPEarned = 50 + character.Level * 10;
			result.Summary = $"{character.CharacterName} completed '{quest.QuestName}' successfully!";
		}
		else
		{
			result.XPEarned = 25 + character.Level * 5;
			result.Summary = $"{character.CharacterName} failed '{quest.QuestName}' but gained some experience.";
		}

		return result;
	}

	/// <summary>
	/// Resolve all NPC quests and apply rewards. Returns list of results for display.
	/// </summary>
	public static List<QuestResult> ResolveAllNPCQuests()
	{
		var results = new List<QuestResult>();
		var npcQuests = QuestManager.Instance.GetNPCQuests();

		foreach (var activeQuest in npcQuests)
		{
			var result = ResolveNPCQuest(activeQuest);
			results.Add(result);

			if (result.XPEarned > 0 && result.Character != null)
			{
				result.Character.AddExperience(result.XPEarned);
			}

			// Remove from active quests and move to completed
			QuestManager.Instance.CompleteQuest(result.Quest, result.Character);

			GD.Print(result.Summary);
		}

		return results;
	}

	private static float CalculateSuccessChance(Quest quest, Character character)
	{
		// Base 70%
		float chance = 70f;

		// +2% per character level
		chance += character.Level * 2f;

		// +1% per average stat
		float avgStat = GetAverageStat(character);
		chance += avgStat;

		return Mathf.Clamp(chance, 30f, 95f);
	}

	private static float GetAverageStat(Character character)
	{
		float total = 0;
		int count = 0;
		foreach (var stat in character.CurrentStats.Values)
		{
			total += stat.Value;
			count++;
		}
		return count > 0 ? total / count : 0;
	}
}
