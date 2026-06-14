using Godot;
using System;

public partial class ShildedKnight : PlantBase
{
	private AnimatedSprite2D _animatedSprite;
	
	// Flaga sprawdzająca, czy roślina jest aktualnie gryziona
	private bool _isBeingAttacked = false;
	
	// Timer, który przywróci animację 'idle', gdy zombie przestanie gryźć
	private Timer _hurtResetTimer;

	// Progi zdrowia dla zmiany wyglądu (Rycerz ma 400 HP)
	// Gdy spadnie poniżej 200 HP, pęka mu tarcza (stan "damaged")
	private const int DamagedThreshold = 200;
	private bool _isDamagedState = false;

	protected override void OnReady()
	{
		_animatedSprite = GetNode<AnimatedSprite2D>("Sprite2D");
		
		PlantName = "Shield Knight";
		MaxHealth = 400;
		_currentHealth = MaxHealth;
		Cost = 50;
		GridWidth = 1;
		GridHeight = 1;

		// Tworzymy timer, ale nie przypisujemy mu na stałe WaitTime w tym miejscu
		_hurtResetTimer = new Timer();
		_hurtResetTimer.OneShot = true;
		_hurtResetTimer.Timeout += ResetToIdleAnimation;
		AddChild(_hurtResetTimer);

		UpdateAnimation();
	}

	// Ponieważ PlantBase w metodzie TakeDamage prawdopodobnie nie ma metody wirtualnej,
	// stworzymy nową metodę lub nadpiszemy ją (jeśli w bazie dałeś 'public virtual void TakeDamage')
	// Poniżej wersja "new", która przechwytuje obrażenia dla tego konkretnego skryptu:
	public override void TakeDamage(int amount)
	{
		if (!_isAlive) return;

		_currentHealth -= amount;

		// 1. Reakcja na ugryzienie (włączenie animacji oberwania)
		TriggerHurtAnimation();

		// 2. Sprawdzenie progu uszkodzeń tarczy
		if (!_isDamagedState && _currentHealth <= DamagedThreshold)
		{
			_isDamagedState = true;
			GD.Print($"[{PlantName}] Tarcza pękła! Zmiana wyglądu.");
		}

		// 3. Sprawdzenie śmierci
		if (_currentHealth <= 0)
		{
			Die();
		}
	}

	private void TriggerHurtAnimation()
	{
		_isBeingAttacked = true;
		UpdateAnimation();

		// SPRAWDZAMY AKTUALNĄ ANIMACJĘ:
		// Pobieramy nazwę animacji, która aktualnie leci ("hurt" lub "damaged_hurt")
		string currentAnim = _animatedSprite.Animation;

		if (_animatedSprite.SpriteFrames.HasAnimation(currentAnim))
		{
			// Obliczamy ile dokładnie sekund trwa ta animacja (klatki / FPS)
			int frameCount = _animatedSprite.SpriteFrames.GetFrameCount(currentAnim);
			float fps = (float)_animatedSprite.SpriteFrames.GetAnimationSpeed(currentAnim);
			float animationDuration = frameCount / fps; // Dla Twoich 7 klatek i 5 FPS to da 1.4 sekundy

			// Ustawiamy czas oczekiwania timera dokładnie na długość animacji!
			_hurtResetTimer.WaitTime = animationDuration;
		}
		else
		{
			// Zabezpieczenie awaryjne, gdyby nie było animacji
			_hurtResetTimer.WaitTime = 0.5f; 
		}

		// Odpalamy lub resetujemy odliczanie
		_hurtResetTimer.Start(); 
	}

	private void ResetToIdleAnimation()
	{
		_isBeingAttacked = false;
		UpdateAnimation();
	}

	// Główny system zarządzania animacjami na podstawie stanu HP oraz bycia atakowanym
	private void UpdateAnimation()
{
	if (_animatedSprite == null) return;

	if (_isDamagedState)
	{
		// STAN: USZKODZONY (HP < 200)
		if (_isBeingAttacked && _animatedSprite.SpriteFrames.HasAnimation("damaged_hurt"))
		{
			// ZABEZPIECZENIE: Jeśli ta animacja JUŻ LECI, nie włączaj jej od nowa!
			if (_animatedSprite.Animation != "damaged_hurt")
				_animatedSprite.Play("damaged_hurt");
		}
		else
		{
			if (_animatedSprite.Animation != "damaged_idle")
				_animatedSprite.Play("damaged_idle");
		}
	}
	else
	{
		// STAN: ZDROWY (HP > 200)
		if (_isBeingAttacked && _animatedSprite.SpriteFrames.HasAnimation("hurt"))
		{
			// ZABEZPIECZENIE: Zapobiega ucinaniu animacji co 1 sekundę
			if (_animatedSprite.Animation != "hurt")
				_animatedSprite.Play("hurt");
		}
		else
		{
			if (_animatedSprite.Animation != "idle")
				_animatedSprite.Play("idle");
		}
	}
}

	private void Die()
	{
		_isAlive = false;
		// Jeśli w bazie masz system usuwania z GridManagera, wywołaj go:
		// GridManager.Instance.RemovePlant(...);
		QueueFree();
	}
}
