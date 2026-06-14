using Godot;
using System;

/// <summary>
/// Kontroler ekranu wygranej/przegranej.
/// Zarządza animacjami, zmianą stylów (czerwony/zielony) i interakcją z przyciskami.
/// </summary>
public partial class GameResultScreen : Control
{
	private ColorRect _backgroundDim;
	private PanelContainer _panelContainer;
	private Label _titleLabel;
	private Label _subtitleLabel;
	private Button _nextLevelButton;
	private Button _restartButton;
	private Button _exitButton;

	public override void _Ready()
	{
		// Ten węzeł musi przetwarzać nawet przy wstrzymanej grze
		ProcessMode = ProcessModeEnum.Always;

		// Pobranie referencji do węzłów UI
		_backgroundDim = GetNode<ColorRect>("BackgroundDim");
		_panelContainer = GetNode<PanelContainer>("CenterContainer/PanelContainer");
		_titleLabel = GetNode<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/Title");
		_subtitleLabel = GetNode<Label>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/Subtitle");
		
		_nextLevelButton = GetNode<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonsContainer/NextLevelButton");
		_restartButton = GetNode<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonsContainer/RestartButton");
		_exitButton = GetNode<Button>("CenterContainer/PanelContainer/MarginContainer/VBoxContainer/ButtonsContainer/ExitButton");

		// Podłączenie sygnałów przycisków
		_nextLevelButton.Pressed += OnNextLevelPressed;
		_restartButton.Pressed += OnRestartPressed;
		_exitButton.Pressed += OnExitPressed;

		// Dodanie mikro-animacji najechania myszką
		SetupButtonHover(_nextLevelButton);
		SetupButtonHover(_restartButton);
		SetupButtonHover(_exitButton);

		// Nasłuchiwanie sygnału końca gry z GameManager
		if (GameManager.Instance != null)
		{
			GameManager.Instance.GameOver += OnGameOver;
		}

		// Początkowy stan — niewidoczny
		Visible = false;
	}

	public override void _ExitTree()
	{
		// Odpięcie sygnału w celu uniknięcia wycieków pamięci
		if (GameManager.Instance != null)
		{
			GameManager.Instance.GameOver -= OnGameOver;
		}
	}

	private void OnGameOver(bool playerWon)
	{
		bool hasNext = false;
		int currentLevel = 1;

		if (GameManager.Instance != null)
		{
			hasNext = GameManager.Instance.HasNextLevel();
			currentLevel = GameManager.Instance.CurrentLevelNumber;
		}

		ShowScreen(playerWon, currentLevel, hasNext);
	}

	/// <summary>
	/// Pokazuje ekran końcowy z odpowiednim motywem i animacją.
	/// </summary>
	public void ShowScreen(bool won, int levelNum, bool hasNextLevel)
	{
		Visible = true;

		// Konfiguracja stylu panelu (StyleBoxFlat) na podstawie wygranej/przegranej
		StyleBoxFlat panelStyle = new StyleBoxFlat();
		panelStyle.CornerRadiusTopLeft = 16;
		panelStyle.CornerRadiusTopRight = 16;
		panelStyle.CornerRadiusBottomLeft = 16;
		panelStyle.CornerRadiusBottomRight = 16;
		panelStyle.ShadowSize = 16;
		panelStyle.ShadowColor = new Color(0, 0, 0, 0.6f);
		panelStyle.BorderWidthLeft = 4;
		panelStyle.BorderWidthTop = 4;
		panelStyle.BorderWidthRight = 4;
		panelStyle.BorderWidthBottom = 4;

		if (won)
		{
			// Szmaragdowa zieleń (glassmorphism bg + jasne obramowanie)
			panelStyle.BgColor = new Color(0.06f, 0.16f, 0.08f, 0.95f);
			panelStyle.BorderColor = new Color(0.15f, 0.68f, 0.37f); // emerald green

			_titleLabel.Text = "ZWYCIĘSTWO!";
			_titleLabel.AddThemeColorOverride("font_color", new Color(0.18f, 0.8f, 0.44f));
			_subtitleLabel.Text = $"Poziom {levelNum} ukończony pomyślnie!";
			
			_nextLevelButton.Visible = hasNextLevel;
		}
		else
		{
			// Karmazynowa czerwień (glassmorphism bg + jasne obramowanie)
			panelStyle.BgColor = new Color(0.16f, 0.06f, 0.06f, 0.95f);
			panelStyle.BorderColor = new Color(0.75f, 0.15f, 0.15f); // crimson red

			_titleLabel.Text = "PORAŻKA";
			_titleLabel.AddThemeColorOverride("font_color", new Color(0.9f, 0.22f, 0.13f));
			_subtitleLabel.Text = "Zombiaki wdarły się do twojego domu!";
			
			_nextLevelButton.Visible = false;
		}

		_panelContainer.AddThemeStyleboxOverride("panel", panelStyle);

		// Resetowanie pozycji/skali do celów animacyjnych
		_backgroundDim.Color = new Color(0, 0, 0, 0f);
		_panelContainer.Modulate = new Color(1, 1, 1, 0f);
		_panelContainer.Scale = new Vector2(0.7f, 0.7f);

		// Ustawienie punktu obrotu (pivot) na środek panelu (potrzebne do ładnego skalowania)
		// Deferujemy to o jedną klatkę, aby rozmiar panelu zdążył się przeliczyć w layoucie
		Callable.From(() => {
			_panelContainer.PivotOffset = _panelContainer.Size / 2f;
		}).CallDeferred();

		// Płynna animacja przyciemniania tła
		Tween dimTween = CreateTween();
		dimTween.TweenProperty(_backgroundDim, "color", new Color(0, 0, 0, 0.75f), 0.5f)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);

		// Płynna animacja wyskakiwania i rozjaśniania panelu (Back Out)
		Tween panelTween = CreateTween().SetParallel(true);
		panelTween.TweenProperty(_panelContainer, "modulate:a", 1f, 0.4f);
		panelTween.TweenProperty(_panelContainer, "scale", new Vector2(1f, 1f), 0.5f)
				  .SetTrans(Tween.TransitionType.Back)
				  .SetEase(Tween.EaseType.Out);
	}

	// ── Mikro-animacje przycisków ──────────────────────────────────────────

	private void SetupButtonHover(Button button)
	{
		button.MouseEntered += () =>
		{
			// Ustawienie punktu pivot na środek przycisku
			button.PivotOffset = button.Size / 2f;
			
			Tween t = CreateTween();
			t.TweenProperty(button, "scale", new Vector2(1.06f, 1.06f), 0.15f)
			 .SetTrans(Tween.TransitionType.Quad)
			 .SetEase(Tween.EaseType.Out);
		};

		button.MouseExited += () =>
		{
			button.PivotOffset = button.Size / 2f;
			
			Tween t = CreateTween();
			t.TweenProperty(button, "scale", new Vector2(1.0f, 1.0f), 0.15f)
			 .SetTrans(Tween.TransitionType.Quad)
			 .SetEase(Tween.EaseType.Out);
		};
	}

	// ── Obsługa kliknięć przycisków ───────────────────────────────────────

	private void OnNextLevelPressed()
	{
		if (GameManager.Instance != null)
		{
			GameManager.Instance.LoadNextLevel();
		}
	}

	private void OnRestartPressed()
	{
		if (GameManager.Instance != null)
		{
			GameManager.Instance.RestartLevel();
		}
	}

	private void OnExitPressed()
	{
		GD.Print("[GameResultScreen] Zamykanie gry...");
		GetTree().Quit();
	}
}
