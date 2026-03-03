using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Builds the entire combat UI programmatically. No .tscn dependencies for UI elements.
/// Layout: Player HUD (top-left), Enemy HUD (top-right), Combat Log (bottom-left),
/// Action Menu (bottom-right), Ability List (overlay), Result Screen (centered overlay).
/// </summary>
public partial class CombatUI : CanvasLayer
{
	// Signals for CombatSystem
	[Signal] public delegate void AttackPressedEventHandler();
	[Signal] public delegate void AbilitySelectedEventHandler(AbilityData ability);
	[Signal] public delegate void RunPressedEventHandler();
	[Signal] public delegate void ContinuePressedEventHandler();

	// UI references
	private Label _playerNameLabel;
	private ProgressBar _playerHPBar;
	private Label _playerHPLabel;
	private ProgressBar _playerMPBar;
	private Label _playerMPLabel;
	private Label _playerLevelLabel;

	private Label _enemyNameLabel;
	private ProgressBar _enemyHPBar;
	private Label _enemyHPLabel;

	private RichTextLabel _combatLog;
	private VBoxContainer _actionMenu;
	private VBoxContainer _abilityList;
	private PanelContainer _abilityPanel;
	private PanelContainer _resultPanel;
	private Label _resultTitle;
	private Label _resultDetails;

	private CombatSystem _combatSystem;

	public void Initialize(CombatSystem combatSystem)
	{
		_combatSystem = combatSystem;
		_combatSystem.CombatLogEntry += OnCombatLogEntry;
		_combatSystem.HPChanged += UpdateHPBars;
	}

	public override void _Ready()
	{
		BuildUI();
	}

	private void BuildUI()
	{
		// Main container fills the screen
		var root = new Control();
		root.Name = "Root";
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);

		// Background color
		var bg = new ColorRect();
		bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		bg.Color = new Color(0.1f, 0.12f, 0.15f, 1f);
		root.AddChild(bg);

		// Title bar
		var titleLabel = new Label();
		titleLabel.Text = "-- COMBAT --";
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		titleLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
		titleLabel.OffsetTop = 10;
		titleLabel.AddThemeFontSizeOverride("font_size", 24);
		titleLabel.AddThemeColorOverride("font_color", new Color(1, 0.85f, 0.3f));
		root.AddChild(titleLabel);

		BuildPlayerHUD(root);
		BuildEnemyHUD(root);
		BuildCombatLog(root);
		BuildActionMenu(root);
		BuildAbilityList(root);
		BuildResultScreen(root);
	}

	private void BuildPlayerHUD(Control root)
	{
		var panel = CreatePanel();
		panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		panel.OffsetLeft = 20;
		panel.OffsetTop = 50;
		panel.OffsetRight = 350;
		panel.OffsetBottom = 180;
		root.AddChild(panel);

		var vbox = new VBoxContainer();
		vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		vbox.AddThemeConstantOverride("separation", 4);
		panel.AddChild(vbox);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 10);
		margin.AddThemeConstantOverride("margin_top", 8);
		margin.AddThemeConstantOverride("margin_right", 10);
		margin.AddThemeConstantOverride("margin_bottom", 8);
		margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		panel.AddChild(margin);

		var inner = new VBoxContainer();
		inner.AddThemeConstantOverride("separation", 4);
		margin.AddChild(inner);

		_playerNameLabel = new Label { Text = "Player" };
		_playerNameLabel.AddThemeFontSizeOverride("font_size", 18);
		_playerNameLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.9f, 0.4f));
		inner.AddChild(_playerNameLabel);

		_playerLevelLabel = new Label { Text = "Lv. 1" };
		_playerLevelLabel.AddThemeFontSizeOverride("font_size", 14);
		inner.AddChild(_playerLevelLabel);

		var hpRow = new HBoxContainer();
		hpRow.AddChild(new Label { Text = "HP: ", CustomMinimumSize = new Vector2(35, 0) });
		_playerHPBar = new ProgressBar { CustomMinimumSize = new Vector2(180, 20) };
		_playerHPBar.AddThemeStyleboxOverride("fill", CreateStyleBox(new Color(0.2f, 0.8f, 0.2f)));
		hpRow.AddChild(_playerHPBar);
		_playerHPLabel = new Label { Text = "100/100" };
		_playerHPLabel.AddThemeFontSizeOverride("font_size", 12);
		hpRow.AddChild(_playerHPLabel);
		inner.AddChild(hpRow);

		var mpRow = new HBoxContainer();
		mpRow.AddChild(new Label { Text = "MP: ", CustomMinimumSize = new Vector2(35, 0) });
		_playerMPBar = new ProgressBar { CustomMinimumSize = new Vector2(180, 20) };
		_playerMPBar.AddThemeStyleboxOverride("fill", CreateStyleBox(new Color(0.3f, 0.4f, 0.9f)));
		mpRow.AddChild(_playerMPBar);
		_playerMPLabel = new Label { Text = "60/60" };
		_playerMPLabel.AddThemeFontSizeOverride("font_size", 12);
		mpRow.AddChild(_playerMPLabel);
		inner.AddChild(mpRow);
	}

	private void BuildEnemyHUD(Control root)
	{
		var panel = CreatePanel();
		panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		panel.OffsetLeft = -350;
		panel.OffsetTop = 50;
		panel.OffsetRight = -20;
		panel.OffsetBottom = 160;
		root.AddChild(panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 10);
		margin.AddThemeConstantOverride("margin_top", 8);
		margin.AddThemeConstantOverride("margin_right", 10);
		margin.AddThemeConstantOverride("margin_bottom", 8);
		margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		panel.AddChild(margin);

		var inner = new VBoxContainer();
		inner.AddThemeConstantOverride("separation", 4);
		margin.AddChild(inner);

		_enemyNameLabel = new Label { Text = "Enemy" };
		_enemyNameLabel.AddThemeFontSizeOverride("font_size", 18);
		_enemyNameLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.3f, 0.3f));
		inner.AddChild(_enemyNameLabel);

		var hpRow = new HBoxContainer();
		hpRow.AddChild(new Label { Text = "HP: ", CustomMinimumSize = new Vector2(35, 0) });
		_enemyHPBar = new ProgressBar { CustomMinimumSize = new Vector2(180, 20) };
		_enemyHPBar.AddThemeStyleboxOverride("fill", CreateStyleBox(new Color(0.8f, 0.2f, 0.2f)));
		hpRow.AddChild(_enemyHPBar);
		_enemyHPLabel = new Label { Text = "100/100" };
		_enemyHPLabel.AddThemeFontSizeOverride("font_size", 12);
		hpRow.AddChild(_enemyHPLabel);
		inner.AddChild(hpRow);
	}

	private void BuildCombatLog(Control root)
	{
		var panel = CreatePanel();
		panel.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
		panel.OffsetLeft = 20;
		panel.OffsetTop = -300;
		panel.OffsetRight = 700;
		panel.OffsetBottom = -20;
		root.AddChild(panel);

		_combatLog = new RichTextLabel();
		_combatLog.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_combatLog.OffsetLeft = 10;
		_combatLog.OffsetTop = 10;
		_combatLog.OffsetRight = -10;
		_combatLog.OffsetBottom = -10;
		_combatLog.BbcodeEnabled = true;
		_combatLog.ScrollFollowing = true;
		_combatLog.AddThemeFontSizeOverride("normal_font_size", 14);
		panel.AddChild(_combatLog);
	}

	private void BuildActionMenu(Control root)
	{
		var panel = CreatePanel();
		panel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
		panel.OffsetLeft = -280;
		panel.OffsetTop = -220;
		panel.OffsetRight = -20;
		panel.OffsetBottom = -20;
		root.AddChild(panel);

		_actionMenu = new VBoxContainer();
		_actionMenu.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_actionMenu.OffsetLeft = 15;
		_actionMenu.OffsetTop = 15;
		_actionMenu.OffsetRight = -15;
		_actionMenu.OffsetBottom = -15;
		_actionMenu.AddThemeConstantOverride("separation", 8);
		panel.AddChild(_actionMenu);

		var menuTitle = new Label { Text = "Actions" };
		menuTitle.HorizontalAlignment = HorizontalAlignment.Center;
		menuTitle.AddThemeFontSizeOverride("font_size", 18);
		menuTitle.AddThemeColorOverride("font_color", new Color(1, 0.85f, 0.3f));
		_actionMenu.AddChild(menuTitle);

		var attackBtn = CreateButton("Attack", OnAttackButton);
		_actionMenu.AddChild(attackBtn);

		var abilityBtn = CreateButton("Abilities", OnAbilityButton);
		_actionMenu.AddChild(abilityBtn);

		var runBtn = CreateButton("Run", OnRunButton);
		_actionMenu.AddChild(runBtn);

		_actionMenu.Visible = false;
	}

	private void BuildAbilityList(Control root)
	{
		_abilityPanel = CreatePanel();
		_abilityPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
		_abilityPanel.OffsetLeft = -200;
		_abilityPanel.OffsetTop = -200;
		_abilityPanel.OffsetRight = 200;
		_abilityPanel.OffsetBottom = 200;
		root.AddChild(_abilityPanel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 15);
		margin.AddThemeConstantOverride("margin_top", 15);
		margin.AddThemeConstantOverride("margin_right", 15);
		margin.AddThemeConstantOverride("margin_bottom", 15);
		margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		_abilityPanel.AddChild(margin);

		var outerVBox = new VBoxContainer();
		outerVBox.AddThemeConstantOverride("separation", 8);
		margin.AddChild(outerVBox);

		var title = new Label { Text = "Select Ability" };
		title.HorizontalAlignment = HorizontalAlignment.Center;
		title.AddThemeFontSizeOverride("font_size", 18);
		title.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 1f));
		outerVBox.AddChild(title);

		var scroll = new ScrollContainer();
		scroll.CustomMinimumSize = new Vector2(0, 280);
		scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		outerVBox.AddChild(scroll);

		_abilityList = new VBoxContainer();
		_abilityList.AddThemeConstantOverride("separation", 6);
		scroll.AddChild(_abilityList);

		var backBtn = CreateButton("Back", () => HideAbilityList());
		outerVBox.AddChild(backBtn);

		_abilityPanel.Visible = false;
	}

	private void BuildResultScreen(Control root)
	{
		// Dim overlay
		var dimOverlay = new ColorRect();
		dimOverlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		dimOverlay.Color = new Color(0, 0, 0, 0.6f);

		_resultPanel = new PanelContainer();
		_resultPanel.SetAnchorsPreset(Control.LayoutPreset.Center);
		_resultPanel.OffsetLeft = -200;
		_resultPanel.OffsetTop = -120;
		_resultPanel.OffsetRight = 200;
		_resultPanel.OffsetBottom = 120;
		var resultStyle = new StyleBoxFlat();
		resultStyle.BgColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
		resultStyle.BorderColor = new Color(1, 0.85f, 0.3f);
		resultStyle.SetBorderWidthAll(2);
		resultStyle.SetCornerRadiusAll(8);
		_resultPanel.AddThemeStyleboxOverride("panel", resultStyle);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 20);
		margin.AddThemeConstantOverride("margin_top", 20);
		margin.AddThemeConstantOverride("margin_right", 20);
		margin.AddThemeConstantOverride("margin_bottom", 20);
		_resultPanel.AddChild(margin);

		var vbox = new VBoxContainer();
		vbox.AddThemeConstantOverride("separation", 15);
		margin.AddChild(vbox);

		_resultTitle = new Label { Text = "Result" };
		_resultTitle.HorizontalAlignment = HorizontalAlignment.Center;
		_resultTitle.AddThemeFontSizeOverride("font_size", 28);
		_resultTitle.AddThemeColorOverride("font_color", new Color(1, 0.85f, 0.3f));
		vbox.AddChild(_resultTitle);

		_resultDetails = new Label { Text = "" };
		_resultDetails.HorizontalAlignment = HorizontalAlignment.Center;
		_resultDetails.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		vbox.AddChild(_resultDetails);

		var continueBtn = CreateButton("Continue", () => EmitSignal(SignalName.ContinuePressed));
		vbox.AddChild(continueBtn);

		// Wrap both in a container
		var resultContainer = new Control();
		resultContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		resultContainer.AddChild(dimOverlay);
		resultContainer.AddChild(_resultPanel);
		resultContainer.Visible = false;
		resultContainer.Name = "ResultContainer";
		root.AddChild(resultContainer);
	}

	// --- Public API ---

	public void ShowActionMenu()
	{
		_actionMenu.Visible = true;
		// Focus first button
		var firstBtn = _actionMenu.GetChild<Button>(1);
		firstBtn?.GrabFocus();
	}

	public void HideActionMenu()
	{
		_actionMenu.Visible = false;
	}

	public void HideAbilityList()
	{
		_abilityPanel.Visible = false;
	}

	public void ShowResult(string title, string details)
	{
		_resultTitle.Text = title;
		_resultDetails.Text = details;
		HideActionMenu();
		var container = GetNode<Control>("Root/ResultContainer");
		if (container != null) container.Visible = true;
	}

	// --- Event Handlers ---

	private void OnAttackButton()
	{
		EmitSignal(SignalName.AttackPressed);
	}

	private void OnAbilityButton()
	{
		// Populate ability list
		foreach (var child in _abilityList.GetChildren())
		{
			((Node)child).QueueFree();
		}

		if (_combatSystem.PlayerCombatant != null)
		{
			foreach (var ability in _combatSystem.PlayerCombatant.Abilities)
			{
				bool canAfford = _combatSystem.PlayerCombatant.CurrentMP >= ability.MPCost;
				var abilityCapture = ability;
				var btn = CreateButton(
					$"{ability.AbilityName} (MP: {ability.MPCost})",
					() => {
						HideAbilityList();
						EmitSignal(SignalName.AbilitySelected, abilityCapture);
					}
				);
				btn.Disabled = !canAfford;
				if (!canAfford)
				{
					btn.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f));
				}
				_abilityList.AddChild(btn);
			}
		}

		_abilityPanel.Visible = true;
	}

	private void OnRunButton()
	{
		EmitSignal(SignalName.RunPressed);
	}

	private void OnCombatLogEntry(string message)
	{
		_combatLog.AppendText($"{message}\n");
	}

	private void UpdateHPBars()
	{
		if (_combatSystem.PlayerCombatant != null)
		{
			var p = _combatSystem.PlayerCombatant;
			_playerNameLabel.Text = p.Name;
			_playerLevelLabel.Text = $"Lv. {p.SourceCharacter?.Level ?? 1}";
			_playerHPBar.MaxValue = p.MaxHP;
			_playerHPBar.Value = p.CurrentHP;
			_playerHPLabel.Text = $"{p.CurrentHP}/{p.MaxHP}";
			_playerMPBar.MaxValue = p.MaxMP;
			_playerMPBar.Value = p.CurrentMP;
			_playerMPLabel.Text = $"{p.CurrentMP}/{p.MaxMP}";
		}

		if (_combatSystem.EnemyCombatant != null)
		{
			var e = _combatSystem.EnemyCombatant;
			_enemyNameLabel.Text = $"{e.Name}";
			_enemyHPBar.MaxValue = e.MaxHP;
			_enemyHPBar.Value = e.CurrentHP;
			_enemyHPLabel.Text = $"{e.CurrentHP}/{e.MaxHP}";
		}
	}

	// --- Helpers ---

	private PanelContainer CreatePanel()
	{
		var panel = new PanelContainer();
		var style = new StyleBoxFlat();
		style.BgColor = new Color(0.12f, 0.12f, 0.18f, 0.9f);
		style.BorderColor = new Color(0.4f, 0.4f, 0.5f);
		style.SetBorderWidthAll(1);
		style.SetCornerRadiusAll(4);
		panel.AddThemeStyleboxOverride("panel", style);
		return panel;
	}

	private Button CreateButton(string text, Action onPressed)
	{
		var btn = new Button();
		btn.Text = text;
		btn.CustomMinimumSize = new Vector2(0, 36);

		var normalStyle = new StyleBoxFlat();
		normalStyle.BgColor = new Color(0.2f, 0.2f, 0.3f);
		normalStyle.SetBorderWidthAll(1);
		normalStyle.BorderColor = new Color(0.4f, 0.4f, 0.5f);
		normalStyle.SetCornerRadiusAll(4);
		normalStyle.SetContentMarginAll(8);
		btn.AddThemeStyleboxOverride("normal", normalStyle);

		var hoverStyle = new StyleBoxFlat();
		hoverStyle.BgColor = new Color(0.3f, 0.3f, 0.45f);
		hoverStyle.SetBorderWidthAll(1);
		hoverStyle.BorderColor = new Color(0.6f, 0.6f, 0.8f);
		hoverStyle.SetCornerRadiusAll(4);
		hoverStyle.SetContentMarginAll(8);
		btn.AddThemeStyleboxOverride("hover", hoverStyle);

		var focusStyle = new StyleBoxFlat();
		focusStyle.BgColor = new Color(0.25f, 0.25f, 0.4f);
		focusStyle.SetBorderWidthAll(2);
		focusStyle.BorderColor = new Color(1f, 0.85f, 0.3f);
		focusStyle.SetCornerRadiusAll(4);
		focusStyle.SetContentMarginAll(8);
		btn.AddThemeStyleboxOverride("focus", focusStyle);

		btn.Pressed += onPressed;
		return btn;
	}

	private StyleBoxFlat CreateStyleBox(Color color)
	{
		var style = new StyleBoxFlat();
		style.BgColor = color;
		style.SetCornerRadiusAll(2);
		return style;
	}
}
