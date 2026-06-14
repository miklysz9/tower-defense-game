using Godot;

/// <summary>
/// Główna kula Armaty Szrapnelowej.
/// Leci w prawo i wybucha po przebyciu <see cref="ExplodeDistance"/> px
/// LUB natychmiast gdy trafi zombie — rozrzuca 5 odłamków w stożku.
/// </summary>
public partial class ShrapnelBall : Area2D
{
    [Export] public float Speed           = 350f;
    [Export] public float ExplodeDistance = 267f;  // ~3 kratki (3 × 89px)
    [Export] public int   ShrapnelDamage  = 40;    // obrażenia każdego odłamka
    [Export] public int   ShrapnelCount   = 5;

    private float            _distanceTraveled = 0f;
    private bool             _exploded         = false;
    private AnimatedSprite2D _sprite;

    // Kąty stożka odłamków (stopnie). 0 = prosto w prawo, ±15/±30 = góra/dół
    private static readonly float[] ConeAngles = { 0f, 15f, -15f, 30f, -30f };

    public override void _Ready()
    {
        _sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if (_sprite != null)
            _sprite.Play("default");

        BodyEntered += OnBodyEntered;

        var notifier = GetNodeOrNull<VisibleOnScreenNotifier2D>("Notifier");
        if (notifier != null)
            notifier.ScreenExited += QueueFree;
    }

    public override void _Process(double delta)
    {
        if (_exploded) return;

        float step = Speed * (float)delta;
        Position += new Vector2(step, 0f);
        _distanceTraveled += step;

        if (_distanceTraveled >= ExplodeDistance)
            Explode();
    }

    private void OnBodyEntered(Node2D body)
    {
        // Opcja B: wybuch przy trafieniu zombie (nawet przed 3 kratkami)
        if (body is ZombieBase && !_exploded)
            Explode();
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        // Zatrzymaj kulę i wyłącz kolizje
        Speed         = 0f;
        CollisionLayer = 0;
        CollisionMask  = 0;

        // Załaduj scenę odłamka
        var fragScene = GD.Load<PackedScene>("res://Scene/ShrapnelFragment.tscn");
        if (fragScene == null)
        {
            GD.PrintErr("[ShrapnelBall] Brak ShrapnelFragment.tscn!");
            QueueFree();
            return;
        }

        // Rozrzuć odłamki w stożku
        foreach (float angleDeg in ConeAngles)
        {
            var frag = fragScene.Instantiate<ShrapnelFragment>();
            frag.GlobalPosition = GlobalPosition;
            frag.Direction      = Vector2.FromAngle(Mathf.DegToRad(angleDeg));
            frag.Damage         = ShrapnelDamage;
            GetParent().AddChild(frag);
        }

        GD.Print($"[ShrapnelBall] 💥 WYBUCH po {_distanceTraveled:F0}px! {ConeAngles.Length} odłamków!");
        QueueFree();
    }
}
