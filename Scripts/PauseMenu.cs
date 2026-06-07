using Godot;
using System;

/// <summary>
/// Controller for the Pause Menu UI.
/// Handles pausing/resuming the game and exiting.
/// </summary>
public partial class PauseMenu : Control
{
	// Node references
	private Control _mainPanel;
	private Button _resumeButton;
	private Button _exitButton;

	public override void _Ready()
	{
		// Set process mode so this node processes even when the scene tree is paused
		ProcessMode = ProcessModeEnum.Always;

		// Get references to panel and buttons
		_mainPanel = GetNode<Control>("MenuContainer/MainMenuPanel");
		_resumeButton = GetNode<Button>("MenuContainer/MainMenuPanel/MarginContainer/VBoxContainer/ResumeButton");
		_exitButton = GetNode<Button>("MenuContainer/MainMenuPanel/MarginContainer/VBoxContainer/ExitButton");

		// Connect signals
		_resumeButton.Pressed += OnResumePressed;
		_exitButton.Pressed += OnExitPressed;

		// Start hidden
		Visible = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("ui_cancel"))
		{
			// Consume input so other nodes don't receive it
			GetViewport().SetInputAsHandled();
			TogglePause();
		}
	}

	public void TogglePause()
	{
		if (GameManager.Instance == null) return;

		if (GameManager.Instance.State == GameManager.GameState.Playing)
		{
			GameManager.Instance.PauseGame();
			ShowMenu();
		}
		else if (GameManager.Instance.State == GameManager.GameState.Paused)
		{
			GameManager.Instance.ResumeGame();
			HideMenu();
		}
	}

	private void ShowMenu()
	{
		Visible = true;
	}

	private void HideMenu()
	{
		Visible = false;
	}

	// ── Button Handlers ──────────────────────────────────────────────────

	private void OnResumePressed()
	{
		if (GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.Paused)
		{
			GameManager.Instance.ResumeGame();
		}
		HideMenu();
	}

	private void OnExitPressed()
	{
		GD.Print("[PauseMenu] Exiting game...");
		GetTree().Quit();
	}
}
