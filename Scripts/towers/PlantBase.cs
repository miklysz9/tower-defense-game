using Godot;

/// <summary>
/// Bazowa klasa dla wszystkich roślin. Dziedziczy po Node2D.
/// Każda roślina (Sunflower, Knight itp.) rozszerza tę klasę.
/// </summary>
public partial class PlantBase : Node2D
{
    // ── Eksportowane pola (edytowalne w Inspektorze Godota) ──────────────
    [Export] public int MaxHealth     = 100;
    [Export] public int Cost          = 100;   // koszt w słońcu
    [Export] public string PlantName  = "Plant";

    // ── Stan ────────────────────────────────────────────────────────────
    protected int _currentHealth;
    protected bool _isAlive = true;

    // Pozycja na siatce (ustawiana przez GridManager)
    public int GridRow { get; private set; }
    public int GridCol { get; private set; }

    // ── Sygnały ─────────────────────────────────────────────────────────
    [Signal] public delegate void PlantDiedEventHandler(PlantBase plant);
    [Signal] public delegate void HealthChangedEventHandler(int current, int max);

    // ── Cykl życia Node ─────────────────────────────────────────────────
    public override void _Ready()
    {
        _currentHealth = MaxHealth;
        OnReady();
    }

    public override void _Process(double delta)
    {
        if (!_isAlive) return;
        OnUpdate(delta);
    }

    // ── Metody do nadpisania w podklasach ────────────────────────────────

    /// <summary>Wywoływane raz przy inicjalizacji (zamiast _Ready w podklasach).</summary>
    protected virtual void OnReady() { }

    /// <summary>Wywoływane co klatkę (zamiast _Process w podklasach).</summary>
    protected virtual void OnUpdate(double delta) { }

    /// <summary>Wywoływane gdy roślina zostaje zniszczona.</summary>
    protected virtual void OnDeath() { }

    // ── API publiczne ────────────────────────────────────────────────────

    /// <summary>Ustaw pozycję na siatce (wywołuje GridManager).</summary>
    public void SetGridPosition(int row, int col)
    {
        GridRow = row;
        GridCol = col;
    }

    /// <summary>Zadaj obrażenia roślinie.</summary>
    public void TakeDamage(int amount)
    {
        if (!_isAlive) return;

        _currentHealth -= amount;
        EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);

        if (_currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        _isAlive = false;
        OnDeath();
        EmitSignal(SignalName.PlantDied, this);

        // Prosta animacja śmierci (możesz zastąpić własną)
        QueueFree();
    }
}
