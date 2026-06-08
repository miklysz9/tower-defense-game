using Godot;
using System;

public partial class Cannon : PlantBase
{
	[Export] public float ShootInterval = 5.0f;
	[Export] public int Damage = 100;
	[Export] public PackedScene ProjectileScene;

	// BARDZO WAŻNE: Wpisz tutaj numer klatki (licząc od 0), w której na grafice pojawia się wybuch!
	// Jeśli Twoja animacja ma np. 5 klatek, a armata "bucha" na trzecim obrazku, wpisz tutaj 2.
	[Export] public int ShootFrameNumber = 2; 

	private Timer _shootTimer;
	private bool _zombieInRow = false;
	private Area2D _detectionArea;
	private AnimatedSprite2D _animatedSprite;

	protected override void OnReady()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("Sprite2D");
		_animatedSprite.Play("idle");
		
		// Podpinamy sygnały animacji
		_animatedSprite.AnimationFinished += OnAnimationFinished;
		_animatedSprite.FrameChanged += OnAnimationFrameChanged; // NOWOŚĆ: Reagujemy na każdą klatkę
		
		PlantName = "Heavy Cannon";
		MaxHealth = 300;
		Cost = 250;

		GridWidth = 2;
		GridHeight = 1;

		_shootTimer = new Timer();
		_shootTimer.WaitTime = ShootInterval;
		_shootTimer.Autostart = false;
		_shootTimer.Timeout += TriggerAttackAnimation; // ZMIANA: Timer teraz tylko aktywuje animację
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
			{
				// Zamiast czekać 5 sekund na pierwszy strzał, odpalamy sekwencję ataku natychmiast!
				TriggerAttackAnimation();
				_shootTimer.Start();
			}
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
			if (body is ZombieBase zombie && GodotObject.IsInstanceValid(zombie) && !zombie.IsQueuedForDeletion())
			{
				if (zombie.IsZombieAlive)
				{
					foundZombie = true; 
					break;
				}
			}
		}

		_zombieInRow = foundZombie;

		if (!_zombieInRow)
		{
			_shootTimer.Stop();
			_animatedSprite.Stop(); 
			_animatedSprite.Play("idle");
		}
	}

	// Ta metoda wykonuje się co 5 sekund (z timera)
	private void TriggerAttackAnimation()
	{
		if (!_isAlive) return;

		CheckIfZombieStillInRow();
		if (!_zombieInRow) return;

		// Timer daje tylko sygnał: "Zacznij się ruszać/ładować do strzału!"
		_animatedSprite.Play("attack");
	}

	// NOWA METODA: Wywoływana przy KAŻDEJ zmianie obrazka w animacji
	private void OnAnimationFrameChanged()
	{
		// Interesuje nas tylko moment, gdy leci animacja "attack" i doszliśmy do klatki strzału
		if (_animatedSprite.Animation == "attack" && _animatedSprite.Frame == ShootFrameNumber)
		{
			SpawnProjectile();
		}
	}

	// Logika fizycznego stworzenia kulki wyciągnięta do osobnej metody
	private void SpawnProjectile()
	{
		if (ProjectileScene == null) return;

		var projectile = ProjectileScene.Instantiate<Projectile>();
		
		var muzzle = GetNodeOrNull<Marker2D>("Muzzle");
		if (muzzle != null)
			projectile.GlobalPosition = muzzle.GlobalPosition;
		else
			projectile.GlobalPosition = GlobalPosition;

		projectile.Damage = Damage;
		GetParent().AddChild(projectile);
		
		GD.Print($"[{PlantName}] BUM! Kulka wylatuje idealnie z klatką nr {ShootFrameNumber}!");
	}

	private void OnAnimationFinished()
	{
		if (_animatedSprite.Animation == "attack")
		{
			_animatedSprite.Play("idle");
		}
	}
}
