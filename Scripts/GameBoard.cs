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

		SpawnGlyphs();
	}

	private void SpawnGlyphs()
	{
		var glyphScene = GD.Load<PackedScene>("res://Scene/towers/Glyph.tscn");
		if (glyphScene == null)
		{
			GD.PrintErr("[GameBoard] Nie można załadować sceny Glyph.tscn!");
			return;
		}

		for (int r = 0; r < GridManager.Instance.Rows; r++)
		{
			var glyph = glyphScene.Instantiate<PlantBase>();
			AddChild(glyph);

			// Ustaw pozycję po lewej stronie siatki
			Vector2 firstCellWorldPos = GridManager.Instance.GridToWorld(r, 0);
			float glyphX = GridManager.Instance.Origin.X - GridManager.Instance.CellSize / 2f - 20f;
			glyph.GlobalPosition = new Vector2(glyphX, firstCellWorldPos.Y);
			
			glyph.SetGridPosition(r, -1);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
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

			// Tworzymy tymczasową instancję (lub czytamy parametry ze sceny), aby poznać jej rozmiar przed postawieniem
		var tempPlant = _selectedPlantScene.Instantiate<PlantBase>();
		int width = tempPlant.GridWidth;
		int height = tempPlant.GridHeight;
		tempPlant.QueueFree(); // Usuwamy obiekt tymczasowy

		// Sprawdzamy czy cały obszar dla tej rośliny jest pusty
		if (!GridManager.Instance.CanPlacePlantAt(row, col, width, height))
		{
			GD.Print("[GameBoard] Nie ma wystarczająco dużo miejsca dla tej rośliny!");
			return;
		}

		if (!SunManager.Instance.SpendSun(_selectedPlantCost))
			return;

		// Instancjonuj i postaw roślinę (PlacePlant zajmie się resztą)
		var plant = _selectedPlantScene.Instantiate<PlantBase>();
		AddChild(plant);
		GridManager.Instance.PlacePlant(plant, row, col);

		DeselectPlant();

		// Powiadom HUD o posadzeniu — odznacz kartę i zaktualizuj dostępność
		var hud = GetTree().CurrentScene.GetNodeOrNull<HUD>("%HUD");
		if (hud == null)
		{
			// Szukaj w CanvasLayer/UI
			var ui = GetTree().CurrentScene.GetNodeOrNull<CanvasLayer>("UI");
			hud = ui?.GetNodeOrNull<HUD>("HUD");
		}
		hud?.OnPlantPlaced();
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
