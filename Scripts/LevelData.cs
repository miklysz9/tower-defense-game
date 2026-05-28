using Godot;
using Godot.Collections;

[GlobalClass]
public partial class LevelData : Resource
{
	[Export] public int LevelNumber = 1;
	[Export] public int StartingSun = 150;
	[Export] public float WaveInterval = 20f;

	// Tablica fal. Rozmiar tej tablicy automatycznie zdefiniuje TotalWaves!
	[Export] public Array<WaveData> Waves;

	// Pomocnicza właściwość zwracająca łączną liczbę fal na tym poziomie
	public int TotalWaves => Waves != null ? Waves.Count : 0;
}
