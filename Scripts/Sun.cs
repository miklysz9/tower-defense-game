using Godot;
using System;

public partial class Sun : Area2D
{
	[Export] public int Value = 25; // Wartość jednego słońca
	[Export] public float Lifetime = 8.0f; // Czas (w sek.), po jakim słońce zniknie, jeśli gracz go nie kliknie

	public override void _Ready()
	{
		// Podłączamy sygnał kliknięcia myszką na ten obiekt Area2D
		InputEvent += OnInputEvent;

		// Tworzymy automatyczny zegar usuwania słońca (żeby plansza nie zasypała się starymi słońcami)
		var timer = new Timer();
		timer.WaitTime = Lifetime;
		timer.OneShot = true;
		timer.Timeout += QueueFree; // skasuj słońce po wygaśnięciu czasu
		AddChild(timer);
		timer.Start();
	}

	private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
	{
		// Sprawdzamy czy gracz kliknął LEWYM przyciskiem myszy
		if (@event is InputEventMouseButton mouseEvent 
			&& mouseEvent.Pressed 
			&& mouseEvent.ButtonIndex == MouseButton.Left)
		{
			CollectSun();
		}
	}

	private void CollectSun()
	{
		// Dodaj słońce do globalnego managera
		if (SunManager.Instance != null)
		{
			SunManager.Instance.AddSun(Value);
		}
		
		// Opcjonalnie: Tutaj możesz w przyszłości dodać dźwięk podnoszenia lub animację lotu do paska HUD
		
		// Usuń słońce ze sceny
		QueueFree();
	}
}
