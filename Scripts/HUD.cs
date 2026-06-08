using Godot;
using System.Collections.Generic;

/// <summary>
/// Kontroler HUD — zarządza kartami postaci i wyświetla ilość słońca.
/// Dynamicznie łączy się z kartami CharacterCard w HBoxContainer.
/// </summary>
public partial class HUD : Control
{
	// Referencje do węzłów UI
	private Label _sunLabel;
	private HBoxContainer _cardContainer;

	// Lista kart w HUD
	private List<CharacterCard> _cards = new();

	// Aktualnie wybrana karta (null = brak wyboru)
	private CharacterCard _selectedCard;

	public override void _Ready()
	{
		// 1. Pobranie referencji do węzłów
		_sunLabel      = GetNode<Label>("SunLabel");
		_cardContainer = GetNode<HBoxContainer>("HBoxContainer");

		// 2. Zbieramy wszystkie karty z kontenera
		foreach (var child in _cardContainer.GetChildren())
		{
			if (child is CharacterCard card)
			{
				_cards.Add(card);
				card.CardPressed += OnCardPressed;
			}
		}

		// 3. Podłączenie się pod sygnał zmiany ilości słońca w SunManager
		if (SunManager.Instance != null)
		{
			SunManager.Instance.SunChanged += OnSunChanged;
			OnSunChanged(SunManager.Instance.Sun);
		}

		// 4. Aktualizujemy dostępność kart na start
		UpdateCardAffordability();

		GD.Print($"[HUD] Zainicjalizowano {_cards.Count} kart postaci.");
	}

	// ── Obsługa kliknięcia karty ─────────────────────────────────────────

	private void OnCardPressed(CharacterCard card)
	{
		if (card.TowerScene == null)
		{
			GD.PrintErr($"[HUD] Karta '{card.DisplayName}' nie ma załadowanej sceny wieży!");
			return;
		}

		// Jeśli klikamy tę samą kartę — odznaczamy
		if (_selectedCard == card)
		{
			DeselectCard();
			var gameBoard = GetTree().CurrentScene.GetNodeOrNull<GameBoard>("GameBoard");
			gameBoard?.DeselectPlant();
			return;
		}

		// Odznacz poprzednią
		_selectedCard?.SetSelected(false);

		// Zaznacz nową
		_selectedCard = card;
		_selectedCard.SetSelected(true);

		// Przekazujemy do GameBoard
		var board = GetTree().CurrentScene.GetNodeOrNull<GameBoard>("GameBoard");
		if (board != null)
		{
			board.SelectPlant(card.TowerScene, card.Cost);
		}
	}

	/// <summary>Odznacz aktualnie wybraną kartę.</summary>
	public void DeselectCard()
	{
		_selectedCard?.SetSelected(false);
		_selectedCard = null;
	}

	// ── Słońce ───────────────────────────────────────────────────────────

	private void OnSunChanged(int newAmount)
	{
		if (_sunLabel != null)
		{
			_sunLabel.Text = $"Słońce: {newAmount}";
		}

		UpdateCardAffordability();
	}

	/// <summary>Aktualizuj stan kart (czy gracza stać) po zmianie ilości słońca.</summary>
	private void UpdateCardAffordability()
	{
		if (SunManager.Instance == null) return;

		foreach (var card in _cards)
		{
			card.SetAffordable(SunManager.Instance.CanAfford(card.Cost));
		}
	}

	// ── API publiczne (wywoływane po posadzeniu rośliny) ─────────────────

	/// <summary>
	/// Wywoływane przez GameBoard po udanym posadzeniu rośliny.
	/// Odznacza kartę i aktualizuje dostępność.
	/// </summary>
	public void OnPlantPlaced()
	{
		DeselectCard();
		UpdateCardAffordability();
	}
}
