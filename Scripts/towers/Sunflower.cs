using Godot;
using System;

public partial class Sunflower : PlantBase
{
	[Export] public float SunInterval = 10.0f; // Co ile sekund rodzi się słońce
	[Export] public int SunAmount = 25;       // Ile warte jest słońce z tej rośliny

	// Przeciągniemy tu naszą scenę Sun.tscn w edytorze
	[Export] public PackedScene SunScene;

	private Timer _sunTimer;

	protected override void OnReady()
	{
		PlantName = "Sunflower";
		MaxHealth = 100;
		Cost = 50;

		// Konfiguracja timera generowania waluty
		_sunTimer = new Timer();
		_sunTimer.WaitTime = SunInterval;
		_sunTimer.Autostart = true;
		_sunTimer.Timeout += SpawnSun;
		AddChild(_sunTimer);
	}

	private void SpawnSun()
	{
		if (!_isAlive || SunScene == null) return;

		// Tworzymy obiekt słońca w pamięci
		var sunInstance = SunScene.Instantiate<Sun>();
		
		// Ustalamy pozycję słońca. Przepychamy je delikatnie (np. losowo o parę pikseli), 
		// żeby nie spawnowało się idealnie pod łodygą
		Vector2 spawnOffset = new Vector2(GD.RandRange(-20, 20), GD.RandRange(10, 30));
		sunInstance.GlobalPosition = GlobalPosition + spawnOffset;
		
		// Przekazujemy słońcu ile ma być warte
		sunInstance.Value = SunAmount;

		// Dodajemy słońce do głównej sceny gry (do rodzica, żeby nie zginęło, gdy zombie zje słonecznik)
		GetParent().AddChild(sunInstance);
		GD.Print("[Sunflower] Wygenerowano słońce!");
	}
}
