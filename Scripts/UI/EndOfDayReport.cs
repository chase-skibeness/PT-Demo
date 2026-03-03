using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// End-of-day report screen. Shows quest results, resources earned, and XP gains.
/// Built entirely in code — no .tscn dependency.
/// Call Show(results, day) to display, and connect OnContinue to resume the day cycle.
/// </summary>
public partial class EndOfDayReport : CanvasLayer
{
	public Action OnContinue;

	private Control _root;
	private VBoxContainer _content;
	private Label _titleLabel;
	private VBoxContainer _questResults;
	private Label _resourceSummary;
	private Button _continueButton;

	public override void _Ready()
	{
		Layer = 100;
		BuildUI();
	}

	public void Show(List<QuestResolver.QuestResult> results, int dayCompleted)
	{
		_titleLabel.Text = $"End of Day {dayCompleted}";

		// Clear previous quest results
		foreach (Node child in _questResults.GetChildren())
		{
			child.QueueFree();
		}

		if (results.Count == 0)
		{
			var noQuests = new Label
			{
				Text = "No adventurers were on quests today.",
				HorizontalAlignment = HorizontalAlignment.Center,
			};
			noQuests.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
			_questResults.AddChild(noQuests);
			_resourceSummary.Text = "";
		}
		else
		{
			var rewardTotals = new Dictionary<string, int>();
			int totalXP = 0;

			foreach (var result in results)
			{
				var panel = CreateResultPanel(result);
				_questResults.AddChild(panel);

				foreach (var reward in result.Rewards)
				{
					if (!rewardTotals.ContainsKey(reward.Label))
						rewardTotals[reward.Label] = 0;
					rewardTotals[reward.Label] += reward.Amount;
				}
				totalXP += result.XPEarned;
			}

			var parts = new List<string>();
			foreach (var kvp in rewardTotals)
			{
				if (kvp.Value > 0) parts.Add($"{kvp.Key} +{kvp.Value}");
			}
			if (totalXP > 0) parts.Add($"XP +{totalXP}");

			_resourceSummary.Text = parts.Count > 0
				? $"Total Earned: {string.Join(" | ", parts)}"
				: "No resources earned today.";
		}

		_root.Visible = true;
		_continueButton.GrabFocus();
	}

	private void BuildUI()
	{
		// Full-screen root
		_root = new Control();
		_root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_root.Visible = false;
		AddChild(_root);

		// Dark overlay
		var overlay = new ColorRect();
		overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		overlay.Color = new Color(0f, 0f, 0f, 0.85f);
		_root.AddChild(overlay);

		// Centered panel
		var panelContainer = new PanelContainer();
		panelContainer.SetAnchorsPreset(Control.LayoutPreset.Center);
		panelContainer.AnchorLeft = 0.15f;
		panelContainer.AnchorRight = 0.85f;
		panelContainer.AnchorTop = 0.1f;
		panelContainer.AnchorBottom = 0.9f;
		panelContainer.OffsetLeft = 0;
		panelContainer.OffsetRight = 0;
		panelContainer.OffsetTop = 0;
		panelContainer.OffsetBottom = 0;

		var panelStyle = new StyleBoxFlat();
		panelStyle.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
		panelStyle.BorderColor = new Color(1f, 0.85f, 0.3f);
		panelStyle.SetBorderWidthAll(2);
		panelStyle.SetCornerRadiusAll(8);
		panelContainer.AddThemeStyleboxOverride("panel", panelStyle);
		_root.AddChild(panelContainer);

		// Margin inside panel
		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 30);
		margin.AddThemeConstantOverride("margin_top", 25);
		margin.AddThemeConstantOverride("margin_right", 30);
		margin.AddThemeConstantOverride("margin_bottom", 25);
		panelContainer.AddChild(margin);

		// Main content VBox
		_content = new VBoxContainer();
		_content.AddThemeConstantOverride("separation", 15);
		margin.AddChild(_content);

		// Title
		_titleLabel = new Label
		{
			Text = "End of Day",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		_titleLabel.AddThemeFontSizeOverride("font_size", 28);
		_titleLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.3f));
		_content.AddChild(_titleLabel);

		// Separator
		var sep = new HSeparator();
		_content.AddChild(sep);

		// "Quest Results" header
		var questHeader = new Label
		{
			Text = "Quest Results",
			HorizontalAlignment = HorizontalAlignment.Left,
		};
		questHeader.AddThemeFontSizeOverride("font_size", 20);
		questHeader.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1f));
		_content.AddChild(questHeader);

		// Scrollable quest results area
		var scroll = new ScrollContainer();
		scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		_content.AddChild(scroll);

		_questResults = new VBoxContainer();
		_questResults.AddThemeConstantOverride("separation", 8);
		_questResults.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		scroll.AddChild(_questResults);

		// Separator
		var sep2 = new HSeparator();
		_content.AddChild(sep2);

		// Resource summary
		_resourceSummary = new Label
		{
			Text = "",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		_resourceSummary.AddThemeFontSizeOverride("font_size", 16);
		_resourceSummary.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.5f));
		_content.AddChild(_resourceSummary);

		// Continue button
		_continueButton = new Button();
		_continueButton.Text = "Continue to Next Day";
		_continueButton.CustomMinimumSize = new Vector2(250, 50);
		_continueButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;

		var btnNormal = new StyleBoxFlat();
		btnNormal.BgColor = new Color(0.2f, 0.35f, 0.2f);
		btnNormal.SetBorderWidthAll(1);
		btnNormal.BorderColor = new Color(0.4f, 0.6f, 0.4f);
		btnNormal.SetCornerRadiusAll(4);
		btnNormal.SetContentMarginAll(10);
		_continueButton.AddThemeStyleboxOverride("normal", btnNormal);

		var btnHover = new StyleBoxFlat();
		btnHover.BgColor = new Color(0.25f, 0.45f, 0.25f);
		btnHover.SetBorderWidthAll(1);
		btnHover.BorderColor = new Color(0.5f, 0.7f, 0.5f);
		btnHover.SetCornerRadiusAll(4);
		btnHover.SetContentMarginAll(10);
		_continueButton.AddThemeStyleboxOverride("hover", btnHover);

		var btnFocus = new StyleBoxFlat();
		btnFocus.BgColor = new Color(0.22f, 0.4f, 0.22f);
		btnFocus.SetBorderWidthAll(2);
		btnFocus.BorderColor = new Color(1f, 0.85f, 0.3f);
		btnFocus.SetCornerRadiusAll(4);
		btnFocus.SetContentMarginAll(10);
		_continueButton.AddThemeStyleboxOverride("focus", btnFocus);

		_continueButton.Pressed += OnContinuePressed;
		_content.AddChild(_continueButton);
	}

	private PanelContainer CreateResultPanel(QuestResolver.QuestResult result)
	{
		var panel = new PanelContainer();
		var style = new StyleBoxFlat();
		style.BgColor = result.Success
			? new Color(0.15f, 0.25f, 0.15f, 0.8f)
			: new Color(0.25f, 0.15f, 0.15f, 0.8f);
		style.SetBorderWidthAll(1);
		style.BorderColor = result.Success
			? new Color(0.3f, 0.5f, 0.3f)
			: new Color(0.5f, 0.3f, 0.3f);
		style.SetCornerRadiusAll(4);
		style.SetContentMarginAll(10);
		panel.AddThemeStyleboxOverride("panel", style);

		var hbox = new HBoxContainer();
		hbox.AddThemeConstantOverride("separation", 15);
		panel.AddChild(hbox);

		// Status icon
		var statusLabel = new Label
		{
			Text = result.Success ? "[OK]" : "[FAIL]",
		};
		statusLabel.AddThemeColorOverride("font_color",
			result.Success ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.9f, 0.4f, 0.4f));
		statusLabel.AddThemeFontSizeOverride("font_size", 16);
		hbox.AddChild(statusLabel);

		// Quest + Character details
		var detailVBox = new VBoxContainer();
		detailVBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		hbox.AddChild(detailVBox);

		var questName = new Label
		{
			Text = $"{result.Character.CharacterName} — {result.Quest.QuestName}",
		};
		questName.AddThemeFontSizeOverride("font_size", 16);
		detailVBox.AddChild(questName);

		// Rewards line
		var rewardParts = new System.Collections.Generic.List<string>();
		foreach (var reward in result.Rewards)
		{
			if (reward.Amount > 0) rewardParts.Add($"{reward.Label} +{reward.Amount}");
		}
		if (result.XPEarned > 0) rewardParts.Add($"XP +{result.XPEarned}");

		var rewardLabel = new Label
		{
			Text = rewardParts.Count > 0 ? string.Join("  |  ", rewardParts) : "No rewards",
		};
		rewardLabel.AddThemeFontSizeOverride("font_size", 14);
		rewardLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
		detailVBox.AddChild(rewardLabel);

		return panel;
	}

	private void OnContinuePressed()
	{
		_root.Visible = false;
		OnContinue?.Invoke();
	}
}
