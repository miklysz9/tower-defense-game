using Godot;
using System;

public partial class Knight2 : PlantBase
{
	[Export] public float ShootInterval = 1.2f; // Fume-shroom strzela odrobinę szybciej niż groszek
	[Export] public int Damage = 20;            // Obrażenia na jedno "smagnięcie" gazem

	private Timer _shootTimer;
	private bool _zombieInRow = false;
	private Area2D _detectionArea;

	// Opcjonalnie: jeśli chcesz podpiąć animację chmury gazu (np. AnimatedSprite2D lub GPUParticles2D)
	private Node2D _fumeEffect;

	protected override void OnReady()
	{
		PlantName = "ObsidianKnight";
		MaxHealth = 400;
		Cost = 300; // Zbalansuj koszt (Fume-shroom w PvZ kosztuje 75, ale u Ciebie to potężny Rycerz)

		_fumeEffect = GetNodeOrNull<Node2D>("FumeEffect");
		if (_fumeEffect != null) _fumeEffect.Visible = false;

		_shootTimer = new Timer();
		_shootTimer.WaitTime = ShootInterval;
		_shootTimer.Autostart = false;
		_shootTimer.Timeout += ShootFume; // Zmieniamy metodę na nową logikę gazu
		AddChild(_shootTimer);

		_detectionArea = GetNodeOrNull<Area2D>("DetectionArea");
		if (_detectionArea != null)
		{
			_detectionArea.BodyEntered += OnZombieEntered;
			_detectionArea.BodyExited += OnZombieExited;
		}
	}

	private void OnZombieEntered(Node2D body)
	{
		if (body is ZombieBase)
		{
			_zombieInRow = true;
			if (_shootTimer.IsStopped())
				_shootTimer.Start();
		}
	}

	private void OnZombieExited(Node2D body)
	{
		CheckIfZombieStillInRow();
	}

	private void CheckIfZombieStillInRow()
	{
		if (_detectionArea == null) return;

		bool foundZombie = false;
		foreach (var body in _detectionArea.GetOverlappingBodies())
		{
			if (body is ZombieBase zombie && GodotObject.IsInstanceValid(zombie))
			{
				foundZombie = true;
				break;
			}
		}

		_zombieInRow = foundZombie;
		if (!_zombieInRow)
		{
			_shootTimer.Stop();
		}
	}

	// NOWA LOGIKA: Strzał chmurą oparów (AoE)
	private void ShootFume()
	{
		if (!_isAlive || _detectionArea == null) return;

		// Na wszelki wypadek upewniamy się, czy ktoś tu jeszcze stoi
		CheckIfZombieStillInRow();
		if (!_zombieInRow) return;

		GD.Print($"[{PlantName}] Puszcza chmurę oparów!");

		// Wizualny efekt ataku (jeśli go przygotujesz)
		TriggerFumeVisualEffect();

		// KLUCZ: Pobieramy WSZYSTKIE obiekty w strefie i ranimy każdego zombie!
		var targets = _detectionArea.GetOverlappingBodies();
		foreach (var body in targets)
		{
			if (body is ZombieBase zombie && GodotObject.IsInstanceValid(zombie))
			{
				zombie.TakeDamage(Damage);
			}
		}
	}

	private async void TriggerFumeVisualEffect()
	{
		if (_fumeEffect == null) return;
		
		_fumeEffect.Visible = true;

		// JEŚLI UŻYWASZ ANIMATEDSPRITE2D:
		// Rzutujemy Node2D na AnimatedSprite2D i odpalamy animację o nazwie "attack"
		if (_fumeEffect is AnimatedSprite2D animatedFume)
		{
			animatedFume.Play("attack"); // "attack" to nazwa Twojej animacji w edytorze
		}
		
		// Efekt pozostaje widoczny przez 0.3 sekundy (czas trwania "smagnięcia" gazem)
		await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
		
		// Po 0.3s ukrywamy efekt i zatrzymujemy animację
		if (GodotObject.IsInstanceValid(this))
		{
			_fumeEffect.Visible = false;
			if (_fumeEffect is AnimatedSprite2D animatedFumeStop)
			{
				animatedFumeStop.Stop();
			}
		}
	}
}
