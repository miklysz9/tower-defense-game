using Godot;

/// <summary>
/// Uniwersalny skrypt karty postaci w HUD.
/// Obsługuje kliknięcie, hover, zaznaczenie i sprawdzanie kosztu.
/// Dołącz do głównego węzła Control w scenie karty.
/// </summary>
public partial class CharacterCard : Control
{
	// ── Eksportowane pola (ustawiane w edytorze per-karta) ──────────────
	[Export] public string TowerScenePath = "";   // np. "res://Scene/towers/Knight.tscn"
	[Export] public int    Cost           = 100;
	[Export] public string DisplayName    = "Unit";

	// ── Sygnały ─────────────────────────────────────────────────────────
	[Signal] public delegate void CardPressedEventHandler(CharacterCard card);

	// ── Stan ────────────────────────────────────────────────────────────
	private PackedScene _towerScene;
	public  PackedScene TowerScene => _towerScene;

	private bool _isSelected  = false;
	private bool _isAffordable = true;
	private bool _isHovered   = false;

	// Referencje do węzłów wizualnych (opcjonalne — obecne w kartach)
	private Sprite2D _frame;
	private Label    _nameLabel;
	private Label    _costLabel;

	// ── Kolory ──────────────────────────────────────────────────────────
	private static readonly Color ColorNormal      = Colors.White;
	private static readonly Color ColorHover       = new Color(1.2f, 1.2f, 1.2f); // lekkie rozjaśnienie
	private static readonly Color ColorSelected    = new Color(0.7f, 1.0f, 0.7f); // zielonkawe zaznaczenie
	private static readonly Color ColorUnaffordable = new Color(0.4f, 0.4f, 0.4f); // przyciemnione

	// ── Cykl życia ───────────────────────────────────────────────────────

	public override void _Ready()
	{
		// Ładujemy scenę wieży
		if (!string.IsNullOrEmpty(TowerScenePath))
		{
			_towerScene = GD.Load<PackedScene>(TowerScenePath);
			if (_towerScene == null)
				GD.PrintErr($"[CharacterCard] Nie udało się załadować sceny: {TowerScenePath}");
		}

		// Pobieramy referencje do węzłów (nazwy zgodne ze scenami kart)
		_frame     = GetNodeOrNull<Sprite2D>("Frame");
		_nameLabel = GetNodeOrNull<Label>("Unit_name");
		_costLabel = GetNodeOrNull<Label>("Label");

		// Ustawiamy wyświetlane teksty na podstawie eksportowanych pól
		if (_nameLabel != null)
			_nameLabel.Text = DisplayName;

		if (_costLabel != null)
			_costLabel.Text = Cost.ToString();

		// Ustawiamy pivot na środek karty, żeby skalowanie odbywało się symetrycznie
		PivotOffset = CustomMinimumSize / 2f;

		// Pozwalamy na przechwytywanie myszy
		MouseFilter = MouseFilterEnum.Stop;

		// Podłączamy sygnały myszy
		MouseEntered += OnMouseEntered;
		MouseExited  += OnMouseExited;

		UpdateVisuals();
	}

	// ── Input ────────────────────────────────────────────────────────────

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseBtn
			&& mouseBtn.Pressed
			&& mouseBtn.ButtonIndex == MouseButton.Left)
		{
			if (!_isAffordable)
			{
				GD.Print($"[CharacterCard] Za mało słońca na {DisplayName}!");
				return;
			}

			EmitSignal(SignalName.CardPressed, this);
			AcceptEvent(); // pochłaniamy event, żeby nie dotarł do GameBoard
		}
	}

	// ── Hover ────────────────────────────────────────────────────────────

	private void OnMouseEntered()
	{
		_isHovered = true;
		UpdateVisuals();
	}

	private void OnMouseExited()
	{
		_isHovered = false;
		UpdateVisuals();
	}

	// ── API publiczne (wywoływane przez HUD) ─────────────────────────────

	/// <summary>Zaznacz/odznacz kartę (wizualnie).</summary>
	public void SetSelected(bool selected)
	{
		_isSelected = selected;
		UpdateVisuals();
	}

	/// <summary>Aktualizuj czy gracza stać na tę kartę.</summary>
	public void SetAffordable(bool affordable)
	{
		_isAffordable = affordable;
		UpdateVisuals();
	}

	// ── Wizualizacja ─────────────────────────────────────────────────────

	private void UpdateVisuals()
	{
		Color target;

		if (!_isAffordable)
			target = ColorUnaffordable;
		else if (_isSelected)
			target = ColorSelected;
		else if (_isHovered)
			target = ColorHover;
		else
			target = ColorNormal;

		// Modulujemy ramkę (Sprite2D), jeśli istnieje
		if (_frame != null)
			_frame.Modulate = target;

		// Dodatkowo modulujemy cały control, żeby efekt był widoczny
		// na wszystkich dzieciach (ikona, tekst)
		Modulate = !_isAffordable ? new Color(0.5f, 0.5f, 0.5f) : Colors.White;

		// Skalowanie przy zaznaczeniu (delikatne powiększenie)
		Scale = _isSelected ? new Vector2(1.1f, 1.1f) : Vector2.One;
	}
}
