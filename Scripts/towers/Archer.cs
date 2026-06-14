using Godot;

/// <summary>
/// Łucznik — fuzja dwóch zwykłych Knightów.
/// Strzela dwoma strzałami jednocześnie (jedna lekko wyżej, druga lekko niżej),
/// ale wolniej niż zwykły Knight. Świetny do trafiania zombie na dwóch liniach.
/// </summary>
public partial class Archer : PlantBase
{
    [Export] public float  ShootInterval = 2.5f;  // wolniej niż Knight (1.5f)
    [Export] public int    Damage        = 20;    // obrażenia każdej strzały (40 łącznie)
    [Export] public float  ArrowXOffset  = 16f;   // odstęp poziomy między strzałami (jedna za drugą)
    [Export] public PackedScene ProjectileScene;  // przypisz Projectile.tscn

    private AnimatedSprite2D _animatedSprite;
    private Timer  _shootTimer;
    private bool   _zombieInRow = false;
    private Area2D _detectionArea;

    protected override void OnReady()
    {
        _animatedSprite = GetNode<AnimatedSprite2D>("Sprite2D");
        _animatedSprite.Play("idle");
        _animatedSprite.AnimationFinished += OnAnimationFinished;

        PlantName  = "Archer";
        MaxHealth  = 200;
        Cost       = 100; // koszt fuzji = koszt drugiego Knighta

        _shootTimer            = new Timer();
        _shootTimer.WaitTime   = ShootInterval;
        _shootTimer.Autostart  = false;
        _shootTimer.Timeout   += Shoot;
        AddChild(_shootTimer);

        _detectionArea = GetNodeOrNull<Area2D>("DetectionArea");
        if (_detectionArea != null)
        {
            _detectionArea.BodyEntered += OnZombieEntered;
            _detectionArea.BodyExited  += OnZombieExited;
        }
    }

    private void OnAnimationFinished()
    {
        if (_animatedSprite.Animation == "attack")
        {
            _animatedSprite.Play("idle");
        }
    }

    // ── Detekcja zombie ────────────────────────────────────────────────────

    private void OnZombieEntered(Node2D body)
    {
        if (body is ZombieBase)
        {
            _zombieInRow = true;
            if (_shootTimer.IsStopped())
            {
                Shoot(); // Pierwszy strzał od razu, zsynchronizowany z animacją
                _shootTimer.Start();
            }
        }
    }

    private void OnZombieExited(Node2D body) => CheckIfZombieStillInRow();

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
            _animatedSprite.Play("idle");
        }
    }

    // ── Strzał — dwie strzały z odstępem czasowym (jedna za drugą) ──────────

    private async void Shoot()
    {
        if (!_isAlive || ProjectileScene == null) return;

        CheckIfZombieStillInRow();
        if (!_zombieInRow)
        {
            _animatedSprite.Play("idle");
            return;
        }

        // Rozpoczynamy animację ataku od początku
        _animatedSprite.Play("attack");

        // Czekamy 1.1s aż animacja dojdzie do klatki wypuszczenia strzały (klatka 3 przy 5 FPS i duration)
        await ToSignal(GetTree().CreateTimer(1.1f), SceneTreeTimer.SignalName.Timeout);

        // Sprawdzamy czy nadal żyjemy i cel istnieje
        if (!_isAlive) return;
        CheckIfZombieStillInRow();
        if (!_zombieInRow)
        {
            _animatedSprite.Play("idle");
            return;
        }

        // Punkt bazowy strzału (Muzzle lub środek postaci)
        var muzzle = GetNodeOrNull<Marker2D>("Muzzle");
        Vector2 basePos = muzzle != null ? muzzle.GlobalPosition : GlobalPosition;

        // 🏹 Pierwsza strzała
        SpawnArrow(basePos);

        // Czekamy 0.15s (przy prędkości 300px/s daje to ~45px odstępu), żeby strzały nie nakładały się
        await ToSignal(GetTree().CreateTimer(0.15f), SceneTreeTimer.SignalName.Timeout);

        // Upewniamy się, że wciąż żyjemy przed wypuszczeniem drugiej strzały
        if (_isAlive && _zombieInRow)
        {
            SpawnArrow(basePos);
        }

        GD.Print($"[Archer] 🏹🏹 Wystrzelono dwie strzały z odstępem czasowym!");
    }

    private void SpawnArrow(Vector2 position)
    {
        var arrow = ProjectileScene.Instantiate<Projectile>();
        arrow.GlobalPosition = position;
        arrow.Damage         = Damage;
        GetParent().AddChild(arrow);
    }
}
