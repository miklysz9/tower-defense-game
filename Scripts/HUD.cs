using Godot;
using System;

public partial class HUD : Control
{
	// Sceny roślin, które ładujemy i przekazujemy do GameBoard
	private PackedScene _KnightScene;
	private PackedScene _sunflowerScene; // DODANE: Scena Słonecznika
	private PackedScene _obsidianKnightScene;

	// Referencje do węzłów UI
	private Label _sunLabel;
	private Button _KnightButton;
	private Button _sunflowerButton;    // DODANE: Przycisk Słonecznika
	private Button _obsidianKnightButton;

	public override void _Ready()
	{
		// 1. Ładowanie scen z plików projektu
		_KnightScene = GD.Load<PackedScene>("res://Scene/towers/Knight.tscn");
		
		// DODANE: Ładowanie sceny słonecznika. 
		// Upewnij się, że ścieżka i nazwa pliku dokładnie odpowiadają Twojemu projektowi!
		_sunflowerScene  = GD.Load<PackedScene>("res://Scene/towers/Sunflower.tscn"); 

		_obsidianKnightScene  = GD.Load<PackedScene>("res://Scene/towers/knight_2.tscn"); 

		// 2. Pobranie referencji do węzłów
		_sunLabel         = GetNode<Label>("SunLabel");
		_KnightButton = GetNode<Button>("HBoxContainer/KnightButton");
		
		// DODANE: Pobieramy Twój nowy przycisk z drzewa sceny.
		// Jeśli SunflowerButton nie jest w HBoxContainer, popraw ścieżkę (np. "SunflowerButton")
		_sunflowerButton  = GetNode<Button>("HBoxContainer/SunflowerButton");
		_obsidianKnightButton  = GetNode<Button>("HBoxContainer/ObsidianKnightButton");

		// 3. Podłączenie się pod sygnał zmiany ilości słońca w SunManager
		if (SunManager.Instance != null)
		{
			SunManager.Instance.SunChanged += OnSunChanged;
			OnSunChanged(SunManager.Instance.Sun);
		}

		// 4. Podłączenie kliknięć przycisków przez kod
		_KnightButton.Pressed += OnKnightButtonPressed;
		_sunflowerButton.Pressed  += OnSunflowerButtonPressed; // DODANE: Reakcja na kliknięcie słonecznika
		_obsidianKnightButton.Pressed  += OnObsidianKnightButtonPressed; // DODANE: Reakcja na kliknięcie słonecznika
	}

	// Wywołuje się automatycznie, gdy SunManager zmieni stan słońca
	private void OnSunChanged(int newAmount)
	{
		if (_sunLabel != null)
		{
			_sunLabel.Text = $"Słońce: {newAmount}";
		}
	}

	// Wywołuje się po kliknięciu przycisku Knighta
	private void OnKnightButtonPressed()
	{
		if (_KnightScene == null)
		{
			GD.PrintErr("[HUD] Nie znaleziono sceny Knighta!");
			return;
		}

		// Przekazujemy Knighta i jego koszt (100) do wyboru na planszy
		var gameBoard = GetTree().CurrentScene.GetNodeOrNull<GameBoard>("GameBoard");
		if (gameBoard != null)
		{
			gameBoard.SelectPlant(_KnightScene, 100);
		}
	}

	// DODANE: Wywołuje się po kliknięciu przycisku Słonecznika
	private void OnSunflowerButtonPressed()
	{
		if (_sunflowerScene == null)
		{
			GD.PrintErr("[HUD] Nie znaleziono sceny Słonecznika!");
			return;
		}

		// Przekazujemy Słonecznik i jego koszt (50) do wyboru na planszy
		var gameBoard = GetTree().CurrentScene.GetNodeOrNull<GameBoard>("GameBoard");
		if (gameBoard != null)
		{
			gameBoard.SelectPlant(_sunflowerScene, 50);
		}
	}

	private void OnObsidianKnightButtonPressed()
	{
		if (_obsidianKnightScene == null)
		{
			GD.PrintErr("[HUD] Nie znaleziono sceny knight2!");
			return;
		}

		var gameBoard = GetTree().CurrentScene.GetNodeOrNull<GameBoard>("GameBoard");
		if (gameBoard != null)
		{
			gameBoard.SelectPlant(_obsidianKnightScene, 300);
		}
	}
}
