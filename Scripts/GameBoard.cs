using Godot;

/// <summary>
/// Kontroler sceny GameBoard — obsługuje klikanie w siatce i sadzenie roślin.
/// Dołącz ten skrypt do węzła GameBoard (Node2D lub Control).
/// </summary>
public partial class GameBoard : Control
{
	// Aktualnie wybrana roślina do posadzenia (null = brak wyboru)
	private PackedScene _selectedPlantScene;
	private int         _selectedPlantCost;

	// Podświetlenie komórki pod kursorem (opcjonalne — Sprite2D/ColorRect)
	private Node2D _cellHighlight;

	public override void _Ready()
	{
		_cellHighlight = GetNodeOrNull<Node2D>("CellHighlight");
		if (_cellHighlight != null)
			_cellHighlight.Visible = false;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent
			&& mouseEvent.Pressed
			&& mouseEvent.ButtonIndex == MouseButton.Left)
		{
			HandleClick(mouseEvent.GlobalPosition);
		}

		
	}
	// DODAJEMY TĘ METODĘ:
	public override void _Process(double delta)
	{
		// Pobieramy globalną pozycję myszy bezpośrednio z okna gry, co klatkę
		Vector2 mouseGlobalPos = GetGlobalMousePosition();
		UpdateHighlight(mouseGlobalPos);
	}

	// ── Wybór rośliny (wywoływany przez przyciski HUD) ───────────────────

	/// <summary>Wywoływane gdy gracz kliknie kartę rośliny w HUD.</summary>
	public void SelectPlant(PackedScene plantScene, int cost)
	{
		if (!SunManager.Instance.CanAfford(cost))
		{
			GD.Print("[GameBoard] Za mało słońca!");
			return;
		}

		_selectedPlantScene = plantScene;
		_selectedPlantCost  = cost;
		GD.Print("[GameBoard] Wybrano roślinę do posadzenia.");
	}

	public void DeselectPlant()
	{
		_selectedPlantScene = null;
		_selectedPlantCost  = 0;
	}

	// ── Kliknięcie na planszę ────────────────────────────────────────────

	private void HandleClick(Vector2 worldPos)
	{
		if (_selectedPlantScene == null) return;
		if (GameManager.Instance.State != GameManager.GameState.Playing) return;

		if (!GridManager.Instance.WorldToGrid(worldPos, out int row, out int col))
		{
			GD.Print("[GameBoard] Kliknięto poza siatką.");
			return;
		}

		if (!GridManager.Instance.IsCellEmpty(row, col))
		{
			GD.Print("[GameBoard] Komórka zajęta!");
			return;
		}

		if (!SunManager.Instance.SpendSun(_selectedPlantCost))
			return;   // SpendSun wypisze błąd

		// Instancjonuj i postaw roślinę
		var plant = _selectedPlantScene.Instantiate<PlantBase>();
		AddChild(plant);
		GridManager.Instance.PlacePlant(plant, row, col);

		// Po posadzeniu odznacz (jeden zakup = jedno zasadzenie)
		DeselectPlant();
	}

	// ── Podświetlenie komórki ────────────────────────────────────────────

	private void UpdateHighlight(Vector2 worldPos)
	{
		if (_cellHighlight == null || _selectedPlantScene == null)
		{
			if (_cellHighlight != null) _cellHighlight.Visible = false;
			return;
		}

		if (GridManager.Instance.WorldToGrid(worldPos, out int row, out int col))
		{
			_cellHighlight.Visible        = true;
			_cellHighlight.GlobalPosition = GridManager.Instance.GridToWorld(row, col);
		}
		else
		{
			_cellHighlight.Visible = false;
		}
	}
}
