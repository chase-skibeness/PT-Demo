using System.Linq;
using Godot;
using Godot.Collections;
using System.Collections.Generic;

namespace ChronosSpace
{
	public partial class ChronosTimeHook : Resource
	{
		public int TriggerDay;
		public double TriggerTime;
		public string TriggerName;
		public string ReturnSignal;
		public Array<Variant> ReturnSignalArgs = new();
	}

	public partial class Chronos : Node
	{
		[Export]
		public static Chronos Instance;
		[Export]
		public double CurrentTime = 0f;
		[Export]
		public int TimeInDay = 120;
		[Export]
		public int CurrentDay = 0;

		public Timer DayTimer;

		public Array<ChronosTimeHook> TimeHooks = new();

		public override void _Ready()
		{
			Instance = this;
			DayTimer = new Timer();
			DayTimer.WaitTime = TimeInDay;
			DayTimer.Timeout += EndDay;
			DayTimer.OneShot = false;
			DayTimer.Autostart = false;
			AddChild(DayTimer);
			StartDay();
			GameSignalBus.Instance.Connect(GameSignalBus.SignalName.RegisterTimeHook, Callable.From<ChronosTimeHook>(RegisterTimeHook));
		}

		private void RegisterTimeHook(ChronosTimeHook hook)
		{
			GD.Print($"Registering time hook: {hook.TriggerName} for day {hook.TriggerDay} at time {hook.TriggerTime}");
			TimeHooks.Add(hook);
		}

		public override void _PhysicsProcess(double delta)
		{
			// Update current time
			CurrentTime = TimeInDay - DayTimer.TimeLeft;
			foreach (var timeHook in TimeHooks)
			{
				if ((CurrentDay == timeHook.TriggerDay && CurrentTime >= timeHook.TriggerTime) || CurrentDay > timeHook.TriggerDay)
				{
					GD.Print($"Triggering time hook: {timeHook.TriggerName} for day {timeHook.TriggerDay} at time {timeHook.TriggerTime}");
					GameSignalBus.Instance.EmitSignalDynamic(timeHook.ReturnSignal, timeHook.ReturnSignalArgs.ToArray());
					TimeHooks.Remove(timeHook);
				}
			}
		}

		public void EndDay()
		{
			DayTimer.Paused = true;
			GameSignalBus.Instance.EmitSignal(GameSignalBus.SignalName.DayEnded);

			// Resolve NPC quests
			var results = QuestResolver.ResolveAllNPCQuests();

			// Show end-of-day report
			var report = new EndOfDayReport();
			AddChild(report);
			report.Show(results, CurrentDay);
			report.OnContinue = () =>
			{
				report.QueueFree();
				QuestManager.Instance.RefreshAvailableQuests();
				StartDay();
			};
		}

		public void StartDay()
		{
			CurrentDay += 1;
			CurrentTime = 0f;
			GameSignalBus.Instance.EmitSignal(GameSignalBus.SignalName.DayStarted);
			DayTimer.Paused = false;
			DayTimer.Start();
		}

		public void AdvanceTime(int time)
		{
			var prevTimeLeft = DayTimer.TimeLeft;
			GD.Print($"Advancing time by {time} seconds. Previous time left: {prevTimeLeft}");
			DayTimer.QueueFree();
			DayTimer = new Timer();
			DayTimer.WaitTime = prevTimeLeft - time < 0.01 ? 0.01 : prevTimeLeft - time;
			DayTimer.Timeout += EndDay;
			DayTimer.OneShot = false;
			DayTimer.Autostart = false; // Set to false, we'll start manually
			AddChild(DayTimer);
			DayTimer.Start(); // Start the timer so TimeLeft is set
			GD.Print($"New time left: {DayTimer.TimeLeft}");
		}
	}
}
