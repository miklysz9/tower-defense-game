using Godot;
using System;

public partial class Projectile : Area2D
{
	[Export] public float Speed  = 300f;
	[Export] public int   Damage = 20;

	public override void _Ready()
	{
		// Gdy pocisk dotknie czegokolwiek
		BodyEntered += OnBodyEntered;

		// Usuń pocisk gdy wyjedzie poza ekran
		VisibleOnScreenNotifier2D notifier = GetNodeOrNull<VisibleOnScreenNotifier2D>("Notifier");
		if (notifier != null)
			notifier.ScreenExited += QueueFree;
	}

	public override void _Process(double delta)
	{
		// POPRAWKA: zmieniamy minus na plus, aby pocisk leciał w prawo
		Position += new Vector2(Speed * (float)delta, 0);
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is ZombieBase zombie)
		{
			zombie.TakeDamage(Damage);
			QueueFree();   // zniszcz pocisk po trafieniu
		}
	}
}
