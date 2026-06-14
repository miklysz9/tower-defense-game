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

		// ── FUZJA: Obsydian Knight + Obsydian Knight → Magmowy Kolos ─────────
		var existingPlant = GridManager.Instance.GetPlant(row, col);
		if (existingPlant is Knight2 existingKnight2)
		{
			var tempCheck = _selectedPlantScene.Instantiate<PlantBase>();
			bool selectedIsKnight2 = tempCheck is Knight2;
			tempCheck.QueueFree();

			if (selectedIsKnight2)
			{
				TryFuseToMagmaKnight(row, col, existingKnight2);
				return;
			}
		}

		// ── FUZJA: Cannon + Cannon → Armata Szrapnelowa ───────────────────
		if (existingPlant is Cannon existingCannon)
		{
			var tempCheck = _selectedPlantScene.Instantiate<PlantBase>();
			bool selectedIsCannon = tempCheck is Cannon;
			tempCheck.QueueFree();

			if (selectedIsCannon)
			{
				TryFuseToShrapnelCannon(row, col, existingCannon);
				return;
			}
		}

		// ── FUZJA: Knight + Knight → Łucznik (Archer) ───────────────────
		if (existingPlant is Knight existingKnight)
		{
			var tempCheck = _selectedPlantScene.Instantiate<PlantBase>();
			bool selectedIsKnight = tempCheck is Knight;
			tempCheck.QueueFree();

			if (selectedIsKnight)
			{
				TryFuseToArcher(row, col, existingKnight);
				return;
			}
		}
		// ─────────────────────────────────────────────────────────────────────

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
		NotifyHud();
	}

	/// <summary>
	/// Fuzja: usuwa istniejącego Knight2 i zastępuje go Magmowym Kolosem (MagmaKnight).
	/// Koszt fuzji = koszt drugiego Obsydian Knighta (normalny koszt karty).
	/// </summary>
	private void TryFuseToMagmaKnight(int row, int col, PlantBase existingKnight)
	{
		if (!SunManager.Instance.CanAfford(_selectedPlantCost))
		{
			GD.Print("[GameBoard] Za mało słońca na fuzję!");
			return;
		}

		if (!GodotObject.IsInstanceValid(existingKnight))
			return;

		GD.Print("[GameBoard] ✨ FUZJA! Obsydian Knight + Obsydian Knight → Magmowy Kolos!");

		// Pobierz słońce za fuzję
		SunManager.Instance.SpendSun(_selectedPlantCost);

		// Usuń starego Knighta z siatki (czyści komórki w _grid[])
		GridManager.Instance.RemovePlant(row, col);
		// Usuń węzeł ze sceny (bez wywoływania Die() — nie chcemy sygnału PlantDied)
		existingKnight.QueueFree();

		// Załaduj i postaw Magmowego Kolosa
		var magmaScene = GD.Load<PackedScene>("res://Scene/towers/MagmaKnight.tscn");
		if (magmaScene == null)
		{
			GD.PrintErr("[GameBoard] Nie udało się załadować MagmaKnight.tscn!");
			return;
		}

		var magmaKnight = magmaScene.Instantiate<PlantBase>();
		AddChild(magmaKnight);
		GridManager.Instance.PlacePlant(magmaKnight, row, col);

		DeselectPlant();
		NotifyHud();
	}

	/// <summary>
	/// Fuzja: usuwa istniejącego Cannona i zastępuje go Armatą Szrapnelowa (ShrapnelCannon).
	/// Cannon zajmuje 2 kratki (GridWidth=2) — pozycja startowa z plant.GridRow/GridCol.
	/// </summary>
	private void TryFuseToShrapnelCannon(int clickedRow, int clickedCol, PlantBase existingCannon)
	{
		if (!SunManager.Instance.CanAfford(_selectedPlantCost))
		{
			GD.Print("[GameBoard] Za mało słońca na fuzję armat!");
			return;
		}

		if (!GodotObject.IsInstanceValid(existingCannon))
			return;

		GD.Print("[GameBoard] 💥 FUZJA! Cannon + Cannon → Armata Szrapnelowa!");

		// Użyj oryginalnej pozycji armaty (lewa-górna komórka) — ważne dla 2-szerokich jednostek
		int row = existingCannon.GridRow;
		int col = existingCannon.GridCol;

		SunManager.Instance.SpendSun(_selectedPlantCost);

		// Usuń stary Cannon z siatki i sceny
		GridManager.Instance.RemovePlant(clickedRow, clickedCol);
		existingCannon.QueueFree();

		// Załaduj i postaw Armatę Szrapnelowa
		var shrapnelScene = GD.Load<PackedScene>("res://Scene/towers/ShrapnelCannon.tscn");
		if (shrapnelScene == null)
		{
			GD.PrintErr("[GameBoard] Nie udało się załadować ShrapnelCannon.tscn!");
			return;
		}

		var shrapnelCannon = shrapnelScene.Instantiate<PlantBase>();
		AddChild(shrapnelCannon);
		GridManager.Instance.PlacePlant(shrapnelCannon, row, col);

		DeselectPlant();
		NotifyHud();
	}

	/// <summary>
	/// Fuzja: usuwa dwóch Knightów i zastępuje ich Łucznikiem (Archer).
	/// </summary>
	private void TryFuseToArcher(int row, int col, PlantBase existingKnight)
	{
		if (!SunManager.Instance.CanAfford(_selectedPlantCost))
		{
			GD.Print("[GameBoard] Za mało słońca na fuzję rycerzy!");
			return;
		}

		if (!GodotObject.IsInstanceValid(existingKnight))
			return;

		GD.Print("[GameBoard] 🏹 FUZJA! Knight + Knight → Łucznik!");

		SunManager.Instance.SpendSun(_selectedPlantCost);

		GridManager.Instance.RemovePlant(row, col);
		existingKnight.QueueFree();

		var archerScene = GD.Load<PackedScene>("res://Scene/towers/Archer.tscn");
		if (archerScene == null)
		{
			GD.PrintErr("[GameBoard] Nie udało się załadować Archer.tscn!");
			return;
		}

		var archer = archerScene.Instantiate<PlantBase>();
		AddChild(archer);
		GridManager.Instance.PlacePlant(archer, row, col);

		DeselectPlant();
		NotifyHud();
	}

	private void NotifyHud()
	{
		var hud = GetTree().CurrentScene.GetNodeOrNull<HUD>("%HUD");
		if (hud == null)
		{
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
			// Sprawdź czy możliwa jest jakaś fuzja (złote podświetlenie)
			var existing  = GridManager.Instance.GetPlant(row, col);
			var tempCheck = _selectedPlantScene.Instantiate<PlantBase>();

			bool fusionPossible =
				(existing is Knight2  && tempCheck is Knight2)  ||
				(existing is Cannon   && tempCheck is Cannon)   ||
				(existing is Knight   && tempCheck is Knight);

			tempCheck.QueueFree();

			if (_cellHighlight is Sprite2D highlightSprite)
				highlightSprite.Modulate = fusionPossible
					? new Color(1f, 0.8f, 0f, 1f)   // złoty = fuzja możliwa!
					: Colors.White;

			_cellHighlight.Visible        = true;
			_cellHighlight.GlobalPosition = GridManager.Instance.GridToWorld(row, col);
		}
		else
		{
			_cellHighlight.Visible = false;
		}
	}
}
