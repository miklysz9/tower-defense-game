using Godot;

public partial class GridManager : Node
{
	// ── Singleton ────────────────────────────────────────────────────────
	public static GridManager Instance { get; private set; }

	// ── Konfiguracja siatki ──────────────────────────────────────────────
	[Export] public int   Rows     = 5;
	[Export] public int   Cols     = 9;
	[Export] public int   CellSize = 80;      // px, komórka jest kwadratem
	[Export] public Vector2 Origin = new Vector2(235, 181);  // lewy-górny róg siatki

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
			col * (CellSize + 9) + CellSize / 2f,
			row * (CellSize + 5) + CellSize / 2f
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

	// Sprawdza, czy cała strefa wymagana przez roślinę jest wolna
	public bool CanPlacePlantAt(int startRow, int startCol, int width, int height)
	{
		for (int r = startRow; r < startRow + height; r++)
		{
			for (int c = startCol; c < startCol + width; c++)
			{
				if (!IsInBounds(r, c) || _grid[r, c] != null)
				{
					return false; // Chociaż jedno pole jest zajęte lub poza mapą
				}
			}
		}
		return true;
	}

	public bool PlacePlant(PlantBase plant, int row, int col)
	{
		// Sprawdzamy dynamicznie gabaryty rośliny zamiast jednego pola
		if (!CanPlacePlantAt(row, col, plant.GridWidth, plant.GridHeight)) 
			return false;

		// Blokujemy WSZYSTKIE komórki w siatce, na których stoi ta roślina
		for (int r = row; r < row + plant.GridHeight; r++)
		{
			for (int c = col; c < col + plant.GridWidth; c++)
			{
				_grid[r, c] = plant;
				plant.OccupiedCells.Add(new Vector2I(r, c));
			}
		}

		plant.SetGridPosition(row, col);

		// Pozycja w świecie: dla obiektów wielopolowych obliczamy środek między zajmowanymi komórkami
		Vector2 startPos = GridToWorld(row, col);
		Vector2 endPos = GridToWorld(row + plant.GridHeight - 1, col + plant.GridWidth - 1);
		plant.GlobalPosition = (startPos + endPos) / 2f;

		plant.PlantDied += OnPlantDied;

		GD.Print($"[GridManager] Postawiono {plant.PlantName} na rozmiar {plant.GridWidth}x{plant.GridHeight}");
		return true;
	}

	// Czyszczenie wszystkich pól, które roślina zajmowała po jej śmierci
	public void RemovePlant(int row, int col)
	{
		if (!IsInBounds(row, col)) return;
		
		PlantBase plant = _grid[row, col];
		if (plant != null)
		{
			foreach (Vector2I cell in plant.OccupiedCells)
			{
				_grid[cell.X, cell.Y] = null;
			}
		}
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
