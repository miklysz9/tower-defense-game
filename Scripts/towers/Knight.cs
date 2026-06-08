using Godot;
using System;

public partial class Knight : PlantBase
{
	[Export] public float  ShootInterval  = 1.5f;   // sekundy między strzałami
	[Export] public int    Damage         = 20;
	[Export] public PackedScene ProjectileScene;     // przypisz Projectile.tscn

	private Timer  _shootTimer;
	private bool   _zombieInRow = false;

	// Area2D wykrywający zombie w tym samym rzędzie
	private Area2D _detectionArea;

	protected override void OnReady()
	{
		PlantName = "Knight";
		MaxHealth = 150;
		Cost      = 100;

		// Timer strzelania
		_shootTimer             = new Timer();
		_shootTimer.WaitTime    = ShootInterval;
		_shootTimer.Autostart   = false;           // startuje dopiero gdy jest zombie
		_shootTimer.Timeout    += Shoot;
		AddChild(_shootTimer);

		// Znajdź lub utwórz Area2D do detekcji (skonfiguruj w scenie)
		_detectionArea = GetNodeOrNull<Area2D>("DetectionArea");
		if (_detectionArea != null)
		{
			_detectionArea.BodyEntered += OnZombieEntered;
			_detectionArea.BodyExited  += OnZombieExited;
		}
	}

	// ── Detekcja zombie ──────────────────────────────────────────────────

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
		// Po prostu wywołujemy ponowne sprawdzenie rzędu, gdy cokolwiek opuści strefę
		CheckIfZombieStillInRow();
	}
	// NOWA POMOCNICZA METODA: Dokładne sprawdzenie strefy
	private void CheckIfZombieStillInRow()
	{
		if (_detectionArea == null) return;

		bool foundZombie = false;

		// Przeszukujemy wszystkie ciała wewnątrz strefy detekcji
		foreach (var body in _detectionArea.GetOverlappingBodies())
		{
			// Sprawdzamy czy to zombie i czy przypadkiem nie jest już w trakcie usuwania z pamięci
			if (body is ZombieBase zombie && GodotObject.IsInstanceValid(zombie))
			{
				foundZombie = true;
				break; // Znaleźliśmy chociaż jednego, wystarczy
			}
		}

		_zombieInRow = foundZombie;

		// Jeśli nie ma już nikogo, uciszamy lufę
		if (!_zombieInRow)
		{
			_shootTimer.Stop();
		}
	}
	// ── Strzelanie ───────────────────────────────────────────────────────

	private void Shoot()
	{
		if (!_isAlive || ProjectileScene == null) return;

		// DODAJ TO: Zanim wystrzelisz, upewnij się, że cel wciąż tam fizycznie stoi
		CheckIfZombieStillInRow();
		if (!_zombieInRow) return; // Jeśli rząd jest już czysty, przerywamy strzał

		var projectile = ProjectileScene.Instantiate<Projectile>();
		
		var muzzle = GetNodeOrNull<Marker2D>("Muzzle");
		if (muzzle != null)
			projectile.GlobalPosition = muzzle.GlobalPosition;
		else
			projectile.GlobalPosition = GlobalPosition;

		projectile.Damage = Damage;
		GetParent().AddChild(projectile);
	}
}
