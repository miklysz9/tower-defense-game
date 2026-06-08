using Godot;
using System;

public partial class Cannon : PlantBase
{
	[Export] public float ShootInterval = 3.0f;   // Armata strzela rzadziej (np. co 3 sekundy)
	[Export] public int Damage = 100;              // Ale zadaje potężne obrażenia!
	[Export] public PackedScene ProjectileScene;

	private Timer _shootTimer;
	private bool _zombieInRow = false;
	private Area2D _detectionArea;

	protected override void OnReady()
	{
		PlantName = "Heavy Cannon";
		MaxHealth = 300; // Większa roślina ma więcej zdrowia
		Cost = 250;      // Wyższy koszt za dużą siłę ognia

		// USTAWIENIE ROZMIARU NA 2 POLA SZEROKOŚCI I 1 POLE WYSOKOŚCI
		GridWidth = 2;
		GridHeight = 1;

		// Timer strzelania
		_shootTimer = new Timer();
		_shootTimer.WaitTime = ShootInterval;
		_shootTimer.Autostart = false;
		_shootTimer.Timeout += Shoot;
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

	private void Shoot()
	{
		if (!_isAlive || ProjectileScene == null) return;

		CheckIfZombieStillInRow();
		if (!_zombieInRow) return;

		var projectile = ProjectileScene.Instantiate<Projectile>();
		
		// Szukamy punktu wylotu lufy armaty
		var muzzle = GetNodeOrNull<Marker2D>("Muzzle");
		if (muzzle != null)
			projectile.GlobalPosition = muzzle.GlobalPosition;
		else
			projectile.GlobalPosition = GlobalPosition;

		projectile.Damage = Damage;
		GetParent().AddChild(projectile);
		
		GD.Print($"[{PlantName}] BUM! Wystrzelono potężną kulę armatnią!");
	}
}
