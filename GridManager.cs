using Godot;

public partial class GridManager : Node
{
	// ── Singleton ────────────────────────────────────────────────────────
	public static GridManager Instance { get; private set; }

	// ── Konfiguracja siatki ──────────────────────────────────────────────
	[Export] public int   Rows     = 5;
	[Export] public int   Cols     = 9;
	[Export] public int   CellSize = 80;      // px, komórka jest kwadratem
	[Export] public Vector2 Origin = new Vector2(178, 120);  // lewy-górny róg siatki

	// ── Stan siatki ──────────────────────────────────────────────────────
	private PlantBase[,] _grid;

	// ── Cykl życia ───────────────────────────────────────────────────────
	public override void _Ready()
	{
		Instance = this;
		_grid    = new PlantBase[Rows, Cols];
	}

	// ── Konwersja współrzędnych ──────────────────────────────────────────

	/// <summary>Przelicza pozycję na siatce → pozycję w świecie (środek komórki).</summary>
	public Vector2 GridToWorld(int row, int col)
	{
		return Origin + new Vector2(
			col * (CellSize + 8) + CellSize / 2f,
			row * (CellSize + 4) + CellSize / 2f
		);
	}

	/// <summary>Przelicza pozycję w świecie → pozycję na siatce. Zwraca false gdy poza siatką.</summary>
	public bool WorldToGrid(Vector2 worldPos, out int row, out int col)
	{
		var local = worldPos - Origin;
		col = (int)(local.X / (CellSize+7));
		row = (int)(local.Y / CellSize);
		return IsInBounds(row, col);
	}

	// ── Operacje na siatce ───────────────────────────────────────────────

	public bool IsInBounds(int row, int col)
		=> row >= 0 && row < Rows && col >= 0 && col < Cols;

	public bool IsCellEmpty(int row, int col)
		=> IsInBounds(row, col) && _grid[row, col] == null;

	/// <summary>
	/// Postaw roślinę na siatce. Zwraca true gdy się udało.
	/// </summary>
	public bool PlacePlant(PlantBase plant, int row, int col)
	{
		if (!IsCellEmpty(row, col)) return false;

		_grid[row, col] = plant;
		plant.SetGridPosition(row, col);
		plant.GlobalPosition = GridToWorld(row, col);

		// Posprzątaj gdy roślina zginie
		plant.PlantDied += OnPlantDied;

		GD.Print($"[GridManager] Postawiono {plant.PlantName} na [{row},{col}]");
		return true;
	}

	/// <summary>Usuń roślinę z siatki (np. po jej śmierci).</summary>
	public void RemovePlant(int row, int col)
	{
		if (!IsInBounds(row, col)) return;
		_grid[row, col] = null;
	}

	public PlantBase GetPlant(int row, int col)
		=> IsInBounds(row, col) ? _grid[row, col] : null;

	// ── Pomocnicze ───────────────────────────────────────────────────────

	/// <summary>Zwraca Y środka wiersza (dla spawnowania zombie).</summary>
	public float GetRowCenterY(int row)
		=> Origin.Y + row * CellSize + CellSize / 2f;

	private void OnPlantDied(PlantBase plant)
	{
		RemovePlant(plant.GridRow, plant.GridCol);
	}
}
