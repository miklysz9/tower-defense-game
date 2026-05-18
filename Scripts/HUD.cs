using Godot;
using System;

public partial class HUD : Control
{
	// Scena Peashootera, którą załadujemy i przekażemy do GameBoard
	private PackedScene _peashooterScene;

	// Referencje do węzłów UI
	private Label _sunLabel;
	private Button _peashooterButton;

	public override void _Ready()
	{
		// 1. Ładowanie sceny Peashootera z plików projektu
		_peashooterScene = GD.Load<PackedScene>("res://Scene/Peashooter.tscn"); // Upewnij się, że ścieżka jest poprawna!

		// 2. Pobranie referencji do węzłów
		_sunLabel = GetNode<Label>("SunLabel");
		_peashooterButton = GetNode<Button>("HBoxContainer/PeashooterButton");

		// 3. Podłączenie się pod sygnał zmiany ilości słońca w SunManager
		if (SunManager.Instance != null)
		{
			SunManager.Instance.SunChanged += OnSunChanged;
			// Ustawiamy początkową wartość słońca na starcie
			OnSunChanged(SunManager.Instance.Sun);
		}

		// 4. Podłączenie kliknięcia przycisku przez kod (zamiast edytora)
		_peashooterButton.Pressed += OnPeashooterButtonPressed;
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

		// Pobieramy instancję GameBoard i wywołujemy Twoją metodę wyboru rośliny
		var gameBoard = GetTree().CurrentScene.GetNodeOrNull<GameBoard>("GameBoard");
		
		if (gameBoard != null)
		{
			// Przekazujemy scenę i koszt (100) do Twojego skryptu GameBoard.cs
			gameBoard.SelectPlant(_peashooterScene, 100);
		}
		else
		{
			GD.PrintErr("[HUD] Nie znaleziono węzła GameBoard na scenie głównej!");
		}
	}
}
