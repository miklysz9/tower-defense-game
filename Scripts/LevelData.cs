using Godot;
using Godot.Collections;

[GlobalClass] // Sprawia, że Godot zobaczy ten typ w oknie tworzenia zasobów
public partial class LevelData : Resource
{
	[Export] public int LevelNumber = 1;
	[Export] public int TotalWaves = 3;
	[Export] public int StartingSun = 150;

	// Słownik określający liczbę zombie w danej fali (np. Fala 1 -> 3 zombie, Fala 2 -> 5 zombie)
	[Export] public Dictionary<int, int> ZombiesPerWave = new Dictionary<int, int>()
	{
		{ 1, 3 },
		{ 2, 5 },
		{ 3, 8 }
	};

	// Czas w sekundach między falami na tym poziomie
	[Export] public float WaveInterval = 20f;

	// Lista roślin, które gracz ma odblokowane na tym poziomie (do przekazania do HUD)
	[Export] public Array<PackedScene> AvailablePlants;
}
