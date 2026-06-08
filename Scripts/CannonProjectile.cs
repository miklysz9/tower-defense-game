using Godot;
using System;

public partial class CannonProjectile : Projectile
{
	private AnimatedSprite2D _animatedSprite;

	public override void _Ready()
	{
		base._Ready();

		_animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		
		if (_animatedSprite != null)
		{
			_animatedSprite.Play("default");
		}
		
		GD.Print($"[{GetType().Name}] Zrodzony z prędkością {Speed} i obrażeniami {Damage}, odtwarzam animację!");
	}
	
	// W CannonProjectile.cs:
	protected override void OnBodyEntered(Node2D body)
	{
		// Zamiast wywoływać base.OnBodyEntered(body) które od razu robi QueueFree,
		// piszemy tutaj własną, asynchroniczną logikę wybuchu:
		Explode(body);
	}

	private async void Explode(Node2D body)
	{
		if (body is ZombieBase zombie)
		{
			zombie.TakeDamage(Damage);
			Speed = 0f;
			CollisionLayer = 0;
			CollisionMask = 0;
			
			if (_animatedSprite != null)
			{
				_animatedSprite.Play("hit");
				await ToSignal(_animatedSprite, AnimatedSprite2D.SignalName.AnimationFinished);
			}
			QueueFree();
		}
	}

	
}
