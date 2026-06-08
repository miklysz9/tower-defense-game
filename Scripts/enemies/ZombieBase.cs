using Godot;

/// <summary>
/// Bazowa klasa dla wszystkich zombie.
/// Zombie idzie w lewo, atakuje rośliny które blokują mu drogę.
/// </summary>
public partial class ZombieBase : CharacterBody2D
{
	// ── Eksportowane pola ────────────────────────────────────────────────
	[Export] public int   MaxHealth    = 200;
	[Export] public float MoveSpeed    = 30f;    // piksele/sekundę
	[Export] public int   Damage       = 10;     // obrażenia na sekundę
	[Export] public float AttackRate   = 1.0f;   // ataki na sekundę
	[Export] public string ZombieName  = "Zombie";

	// ── Stan ────────────────────────────────────────────────────────────
	protected int       _currentHealth;
	protected bool      _isAlive   = true;
	protected PlantBase _targetPlant = null;     // roślina którą aktualnie atakuje
	private   float     _attackTimer = 0f;
	public bool IsZombieAlive => _isAlive;

	// ── Sygnały ─────────────────────────────────────────────────────────
	[Signal] public delegate void ZombieDiedEventHandler(ZombieBase zombie);
	[Signal] public delegate void ZombieReachedEndEventHandler();

	// ── Cykl życia ───────────────────────────────────────────────────────
	public override void _Ready()
	{
		_currentHealth = MaxHealth;

		// Wykryj kolizje z roślinami przez Area2D (skonfiguruj w scenie)
		var area = GetNodeOrNull<Area2D>("HitArea");
		if (area != null)
		{
			area.BodyEntered += OnBodyEntered;
			area.BodyExited  += OnBodyExited;
		}

		OnReady();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_isAlive) return;

		if (_targetPlant != null)
			AttackPlant(delta);
		else
			Move(delta);

		OnUpdate(delta);
	}

	// ── Ruch ─────────────────────────────────────────────────────────────

	private void Move(double delta)
	{
		// Idź w lewo (kierunek -X)
		Velocity = new Vector2(-MoveSpeed, 0);
		MoveAndSlide();

		// Sprawdź czy dotarł do lewej krawędzi ekranu (koniec planszy)
		if (GlobalPosition.X < -50)
		{
			EmitSignal(SignalName.ZombieReachedEnd);
			QueueFree();
		}
	}

	// ── Atak ─────────────────────────────────────────────────────────────

	private void AttackPlant(double delta)
	{
		Velocity = Vector2.Zero;  // stój i atakuj

		_attackTimer -= (float)delta;
		if (_attackTimer <= 0f)
		{
			_attackTimer = 1f / AttackRate;

			if (IsInstanceValid(_targetPlant))
				_targetPlant.TakeDamage(Damage);
			else
				_targetPlant = null;    // roślina już nie istnieje
		}
	}

	// ── Kolizje ──────────────────────────────────────────────────────────

	private void OnBodyEntered(Node2D body)
	{
		if (body is PlantBase plant && _targetPlant == null)
			_targetPlant = plant;
	}

	private void OnBodyExited(Node2D body)
	{
		if (body == _targetPlant)
			_targetPlant = null;
	}

	// ── Obrażenia / śmierć ───────────────────────────────────────────────

	public void TakeDamage(int amount)
	{
		if (!_isAlive) return;

		_currentHealth -= amount;

		if (_currentHealth <= 0)
			Die();
	}

	private async void Die()
	{
		if (!_isAlive) return;

		_isAlive = false;
		Velocity = Vector2.Zero;

		// Wyłącz fizyczne kolizje i maski natychmiast
		CollisionLayer = 0;
		CollisionMask = 0;

		// Wyłącz kształt kolizji CharacterBody2D
		var collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		if (collisionShape != null)
		{
			collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		}

		// Wyłącz obszar wykrywania HitArea (Area2D)
		var area = GetNodeOrNull<Area2D>("HitArea");
		if (area != null)
		{
			area.Monitoring = false;
			area.Monitorable = false;
			var areaCollision = area.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
			if (areaCollision != null)
			{
				areaCollision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
			}
		}

		OnDeath();
		EmitSignal(SignalName.ZombieDied, this);

		// Odegraj animację śmierci, jeśli AnimatedSprite2D i animacja "death" istnieją
		var sprite = GetNodeOrNull<AnimatedSprite2D>("Sprite2D");
		if (sprite != null && sprite.SpriteFrames != null && sprite.SpriteFrames.HasAnimation("death"))
		{
			sprite.Play("death");
			await ToSignal(sprite, AnimatedSprite2D.SignalName.AnimationFinished);
		}

		QueueFree();
	}

	// ── Wirtualne metody dla podklas ─────────────────────────────────────
	protected virtual void OnReady()  { }
	protected virtual void OnUpdate(double delta) { }
	protected virtual void OnDeath()  { }
}
