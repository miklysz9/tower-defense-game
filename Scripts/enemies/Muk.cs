using Godot;
using System;

public partial class Muk : ZombieBase
{
	protected override void OnReady()
	{
		// Nadpisujemy podstawowe statystyki z ZombieBase
		ZombieName = "Muk zombie";
		MaxHealth = 300;
		_currentHealth = MaxHealth;
		MoveSpeed = 20f;


		GD.Print($"[{ZombieName}] zrodzony z {MaxHealth} HP!");
	}
	
	
}
