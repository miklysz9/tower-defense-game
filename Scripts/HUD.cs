using Godot;
using System;

public partial class HUD : Control
{
	// Sceny roślin, które ładujemy i przekazujemy do GameBoard
	private PackedScene _peashooterScene;
	private PackedScene _sunflowerScene; // DODANE: Scena Słonecznika

	// Referencje do węzłów UI
	private Label _sunLabel;
	private Button _peashooterButton;
	private Button _sunflowerButton;    // DODANE: Przycisk Słonecznika

	public override void _Ready()
	{
		// 1. Ładowanie scen z plików projektu
		_peashooterScene = GD.Load<PackedScene>("res://Scene/Peashooter.tscn");
		
		// DODANE: Ładowanie sceny słonecznika. 
		// Upewnij się, że ścieżka i nazwa pliku dokładnie odpowiadają Twojemu projektowi!
		_sunflowerScene  = GD.Load<PackedScene>("res://Scene/Sunflower.tscn"); 

		// 2. Pobranie referencji do węzłów
		_sunLabel         = GetNode<Label>("SunLabel");
		_peashooterButton = GetNode<Button>("HBoxContainer/PeashooterButton");
		
		// DODANE: Pobieramy Twój nowy przycisk z drzewa sceny.
		// Jeśli SunflowerButton nie jest w HBoxContainer, popraw ścieżkę (np. "SunflowerButton")
		_sunflowerButton  = GetNode<Button>("HBoxContainer/SunflowerButton");

		// 3. Podłączenie się pod sygnał zmiany ilości słońca w SunManager
		if (SunManager.Instance != null)
		{
			SunManager.Instance.SunChanged += OnSunChanged;
			OnSunChanged(SunManager.Instance.Sun);
		}

		// 4. Podłączenie kliknięć przycisków przez kod
		_peashooterButton.Pressed += OnPeashooterButtonPressed;
		_sunflowerButton.Pressed  += OnSunflowerButtonPressed; // DODANE: Reakcja na kliknięcie słonecznika
	}

	// Wywołuje się automatycznie, gdy SunManager zmieni stan słońca
	private void OnSunChanged(int newAmount)
	{
		if (_sunLabel != null)
		{
			_sunLabel.Text = $"Słońce: {newAmount}";
		}
	}

	// Wywołuje się po kliknięciu przycisku Peashootera
	private void OnPeashooterButtonPressed()
	{
		if (_peashooterScene == null)
		{
			GD.PrintErr("[HUD] Nie znaleziono sceny Peashootera!");
			return;
		}

		// Przekazujemy Peashootera i jego koszt (100) do wyboru na planszy
		var gameBoard = GetTree().CurrentScene.GetNodeOrNull<GameBoard>("GameBoard");
		if (gameBoard != null)
		{
			gameBoard.SelectPlant(_peashooterScene, 100);
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
}
