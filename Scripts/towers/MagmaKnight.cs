using Godot;

/// <summary>
/// Magmowy Kolos — fuzja dwóch Obsydian Knightów.
/// Atakuje AoE ogniem (jak Knight2) i po każdym ataku zostawia
/// palącą się kałużę lawy, która zadaje obrażenia zombie przez 3 sekundy.
/// </summary>
public partial class MagmaKnight : PlantBase
{
    [Export] public float ShootInterval = 2.0f; // wolniejszy atak — lawa nadrabia ciągłymi obrażeniami
    [Export] public int   Damage        = 35;   // mocniejszy atak

    private Timer   _shootTimer;
    private bool    _zombieInRow = false;
    private Area2D  _detectionArea;
    private Node2D  _fumeEffect;

    protected override void OnReady()
    {
        PlantName  = "MagmaKnight";
        MaxHealth  = 800;
        Cost       = 300; // tyle samo co Knight2 (koszt fuzji = koszt drugiego Knighta)

        _fumeEffect = GetNodeOrNull<Node2D>("FumeEffect");
        if (_fumeEffect != null)
            _fumeEffect.Visible = false;

        _shootTimer            = new Timer();
        _shootTimer.WaitTime   = ShootInterval;
        _shootTimer.Autostart  = false;
        _shootTimer.Timeout   += ShootFume;
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
                _shootTimer.Start();
        }
    }

    private void OnZombieExited(Node2D body)
    {
        CheckIfZombieStillInRow();
    }

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
            _shootTimer.Stop();
    }

    // ── Atak + spawning lawy ───────────────────────────────────────────────

    private void ShootFume()
    {
        if (!_isAlive || _detectionArea == null) return;

        CheckIfZombieStillInRow();
        if (!_zombieInRow) return;

        GD.Print($"[{PlantName}] Magmowy atak! Lawa zostaje na ziemi!");

        TriggerFumeVisualEffect();

        // Obrażenia bezpośrednie wszystkim zombie w strefie
        var targets = _detectionArea.GetOverlappingBodies();
        foreach (var body in targets)
        {
            if (body is ZombieBase zombie && GodotObject.IsInstanceValid(zombie))
                zombie.TakeDamage(Damage);
        }

        // Kluczowe: zostaw kałużę lawy na ziemi
        SpawnLavaPool();
    }

    private void SpawnLavaPool()
    {
        var lavaPoolScene = GD.Load<PackedScene>("res://Scene/towers/LavaPool.tscn");
        if (lavaPoolScene == null)
        {
            GD.PrintErr("[MagmaKnight] Nie udało się załadować LavaPool.tscn!");
            return;
        }

        var lavaPool = lavaPoolScene.Instantiate<LavaPool>();

        // Lawa pojawia się w połowie strefy detekcji (przed Magma Knightem)
        // DetectionArea jest szeroka — stawiamy lawę przy Magma Knighcie
        lavaPool.GlobalPosition = GlobalPosition + new Vector2(120f, 0f);

        // Dodaj do rodzica, żeby żyła niezależnie od Magma Knighta (np. gdy przeładowuje)
        GetParent().AddChild(lavaPool);
    }

    private async void TriggerFumeVisualEffect()
    {
        if (_fumeEffect == null) return;

        _fumeEffect.Visible = true;

        if (_fumeEffect is AnimatedSprite2D animatedFume)
            animatedFume.Play("attack");

        await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);

        if (GodotObject.IsInstanceValid(this))
        {
            _fumeEffect.Visible = false;
            if (_fumeEffect is AnimatedSprite2D animatedFumeStop)
                animatedFumeStop.Stop();
        }
    }
}
