using Godot;

/// <summary>
/// Armata Szrapnelowa — fuzja dwóch Cannonów.
/// Strzela kulą, która wybucha po przebyciu ~3 kratek LUB po trafieniu zombie,
/// rozrzucając 5 odłamków w stożku — trafia zombie na sąsiednich rzędach.
/// </summary>
public partial class ShrapnelCannon : PlantBase
{
    [Export] public float ShootInterval    = 6.0f;
    [Export] public int   ShootFrameNumber = 5;     // ta sama klatka wyrzutu co zwykły Cannon

    private Timer              _shootTimer;
    private bool               _zombieInRow = false;
    private Area2D             _detectionArea;
    private AnimatedSprite2D   _animatedSprite;

    protected override void OnReady()
    {
        _animatedSprite = GetNode<AnimatedSprite2D>("Sprite2D");
        _animatedSprite.Play("idle");

        _animatedSprite.AnimationFinished  += OnAnimationFinished;
        _animatedSprite.FrameChanged       += OnAnimationFrameChanged;

        PlantName  = "ShrapnelCannon";
        MaxHealth  = 600;
        Cost       = 250;
        GridWidth  = 2;
        GridHeight = 1;

        _shootTimer            = new Timer();
        _shootTimer.WaitTime   = ShootInterval;
        _shootTimer.Autostart  = false;
        _shootTimer.Timeout   += TriggerAttackAnimation;
        AddChild(_shootTimer);

        _detectionArea = GetNodeOrNull<Area2D>("DetectionArea");
        if (_detectionArea != null)
        {
            _detectionArea.BodyEntered += OnZombieEntered;
            _detectionArea.BodyExited  += OnZombieExited;
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
                TriggerAttackAnimation();
                _shootTimer.Start();
            }
        }
    }

    private void OnZombieExited(Node2D body) => CheckIfZombieStillInRow();

    private void CheckIfZombieStillInRow()
    {
        if (_detectionArea == null) return;

        bool found = false;
        foreach (var body in _detectionArea.GetOverlappingBodies())
        {
            if (body is ZombieBase z && GodotObject.IsInstanceValid(z)
                && !z.IsQueuedForDeletion() && z.IsZombieAlive)
            {
                found = true;
                break;
            }
        }

        _zombieInRow = found;
        if (!_zombieInRow)
        {
            _shootTimer.Stop();
            _animatedSprite.Stop();
            _animatedSprite.Play("idle");
        }
    }

    // ── Animacja + strzał ─────────────────────────────────────────────────

    private void TriggerAttackAnimation()
    {
        if (!_isAlive) return;
        CheckIfZombieStillInRow();
        if (!_zombieInRow) return;
        _animatedSprite.Play("attack");
    }

    private void OnAnimationFrameChanged()
    {
        if (_animatedSprite.Animation == "attack"
            && _animatedSprite.Frame == ShootFrameNumber)
        {
            SpawnShrapnelBall();
        }
    }

    private void SpawnShrapnelBall()
    {
        var ballScene = GD.Load<PackedScene>("res://Scene/ShrapnelBall.tscn");
        if (ballScene == null)
        {
            GD.PrintErr("[ShrapnelCannon] Brak ShrapnelBall.tscn!");
            return;
        }

        var ball = ballScene.Instantiate<ShrapnelBall>();

        var muzzle = GetNodeOrNull<Marker2D>("Muzzle");
        ball.GlobalPosition = muzzle != null ? muzzle.GlobalPosition : GlobalPosition;

        GetParent().AddChild(ball);
        GD.Print("[ShrapnelCannon] 💥 Wystrzelono kulę szrapnelową!");
    }

    private void OnAnimationFinished()
    {
        if (_animatedSprite.Animation == "attack")
            _animatedSprite.Play("idle");
    }
}
