using Godot;

/// <summary>
/// Odłamek po wybuchu kuli szrapnelowej.
/// Leci w zadanym kierunku <see cref="Direction"/>, niszczy się po trafieniu
/// zombie lub wyjściu poza ekran. Rysuje się przez _Draw() — brak potrzeby grafiki.
/// </summary>
public partial class ShrapnelFragment : Area2D
{
    // Ustawiane przez ShrapnelBall przed AddChild
    public Vector2 Direction = Vector2.Right;
    public int     Damage    = 40;

    [Export] public float Speed       = 520f;
    [Export] public float MaxDistance = 900f;  // automatyczne usunięcie gdy daleko

    private float _distanceTraveled = 0f;
    private float _animTime         = 0f;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask  = 2;   // wykrywa zombie (warstwa 2)

        BodyEntered += OnBodyEntered;

        var notifier = GetNodeOrNull<VisibleOnScreenNotifier2D>("Notifier");
        if (notifier != null)
            notifier.ScreenExited += QueueFree;

        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        float step = Speed * (float)delta;
        Position += Direction * step;
        _distanceTraveled += step;
        _animTime         += (float)delta * 10f;

        if (_distanceTraveled >= MaxDistance)
            QueueFree();

        QueueRedraw();
    }

    public override void _Draw()
    {
        // Pulsujący odłamek — żółto-pomarańczowa świecąca kulka
        float lifeRatio = Mathf.Clamp(1f - (_distanceTraveled / MaxDistance), 0.1f, 1f);
        float pulse     = (Mathf.Sin(_animTime) * 0.2f) + 0.8f;
        float alpha     = lifeRatio * pulse;

        // Zewnętrzna poświata
        DrawCircle(Vector2.Zero, 9f,  new Color(1f, 0.5f, 0f,  alpha * 0.35f));
        // Główny odłamek
        DrawCircle(Vector2.Zero, 5f,  new Color(1f, 0.75f, 0.1f, alpha * 0.9f));
        // Jasne centrum
        DrawCircle(Vector2.Zero, 2f,  new Color(1f, 1f,   0.6f, alpha));
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is ZombieBase zombie && GodotObject.IsInstanceValid(zombie) && zombie.IsZombieAlive)
        {
            zombie.TakeDamage(Damage);
            GD.Print($"[ShrapnelFragment] Odłamek trafił zombie! -{Damage} HP");
            QueueFree();
        }
    }
}
