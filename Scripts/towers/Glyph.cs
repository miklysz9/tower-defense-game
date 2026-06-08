using Godot;
using System;

public partial class Glyph : PlantBase
{
	private AnimatedSprite2D _animatedSprite;
	[Export] public float Speed = 400f; // piksele/sekundę

	private bool _isMoving = false;
	private Area2D _detectionArea;

	protected override void OnReady()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("Sprite2D");
		_animatedSprite.Play("idle");
		
		PlantName = "Glyph";
		MaxHealth = 999999;
		_currentHealth = MaxHealth;
		Cost = 0;
		GridWidth = 1;
		GridHeight = 1;

		_detectionArea = GetNodeOrNull<Area2D>("DetectionArea");
		if (_detectionArea != null)
		{
			_detectionArea.BodyEntered += OnZombieEntered;
			_detectionArea.AreaEntered += OnAreaEntered;
		}
		else
		{
			GD.PrintErr($"[{PlantName}] Brak DetectionArea!");
		}
	}

	protected override void OnUpdate(double delta)
	{
		if (!_isAlive) return;

		if (_isMoving)
		{
			// Poruszaj się w prawą stronę
			GlobalPosition += new Vector2(Speed * (float)delta, 0);

			// Zabijaj wszystkich przeciwników na swojej drodze
			KillZombiesInArea();

			// Jeśli dotrze do końca mapy, zniknij
			float screenWidth = GetViewport().GetVisibleRect().Size.X;
			if (GlobalPosition.X > screenWidth + 100f)
			{
				QueueFree();
			}
		}
	}

	private void OnZombieEntered(Node2D body)
	{
		if (!_isAlive) return;

		if (body is ZombieBase zombie)
		{
			TriggerGlyph(zombie);
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		if (!_isAlive) return;

		if (area.GetParent() is ZombieBase zombie)
		{
			TriggerGlyph(zombie);
		}
	}

	private void TriggerGlyph(ZombieBase zombie)
	{
		if (!_isMoving)
		{
			GD.Print($"[{PlantName}] Aktywacja przez {zombie.ZombieName}!");
			_isMoving = true;
			_animatedSprite.Play("attack");
			
			// Wyłączamy kolizję w StaticBody2D, aby nie blokować innych obiektów
			var collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (collisionShape != null)
			{
				collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
			}
		}

		// Zabij zombie natychmiast
		KillZombie(zombie);
	}

	private void KillZombiesInArea()
	{
		if (_detectionArea == null) return;

		// Sprawdzamy i zabijamy ciała fizyczne zombie
		foreach (var body in _detectionArea.GetOverlappingBodies())
		{
			if (body is ZombieBase zombie && GodotObject.IsInstanceValid(zombie))
			{
				KillZombie(zombie);
			}
		}

		// Sprawdzamy i zabijamy strefy (HitArea) zombie
		foreach (var area in _detectionArea.GetOverlappingAreas())
		{
			if (area.GetParent() is ZombieBase zombie && GodotObject.IsInstanceValid(zombie))
			{
				KillZombie(zombie);
			}
		}
	}

	private void KillZombie(ZombieBase zombie)
	{
		if (GodotObject.IsInstanceValid(zombie))
		{
			GD.Print($"[{PlantName}] Rozjeżdżam {zombie.ZombieName}!");
			zombie.TakeDamage(999999); // Zadaj krytyczne obrażenia, aby zabić
		}
	}
}
