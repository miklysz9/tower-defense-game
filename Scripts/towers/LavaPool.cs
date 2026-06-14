using Godot;
using System.Collections.Generic;

/// <summary>
/// Kałuża lawy zostawiana przez Magmowego Kolosa.
/// Zadaje obrażenia DoT wszystkim zombie w zasięgu przez <see cref="Duration"/> sekund,
/// niezależnie od tego czy Magma Knight przeładowuje broń.
/// </summary>
public partial class LavaPool : Node2D
{
    [Export] public float Duration       = 3.0f;   // ile sekund lawa płonie
    [Export] public int   DamagePerTick  = 15;     // obrażenia co sekundę
    [Export] public float TickInterval   = 1.0f;   // jak często bije (sekundy)

    private Area2D                _area;
    private Timer                 _lifetimeTimer;
    private Timer                 _damageTimer;
    private Sprite2D              _sprite;
    private readonly List<ZombieBase> _zombiesInPool = new List<ZombieBase>();

    // Licznik do animacji migotania (flicker)
    private float _flickerTime = 0f;

    public override void _Ready()
    {
        ZIndex = 1; // Renderuj ponad tłem, ale pod zombie i roślinami

        _sprite = GetNodeOrNull<Sprite2D>("Sprite2D");

        _area = GetNodeOrNull<Area2D>("Area2D");
        if (_area != null)
        {
            _area.BodyEntered += OnBodyEntered;
            _area.BodyExited  += OnBodyExited;
        }
        else
        {
            GD.PrintErr("[LavaPool] Brak Area2D!");
        }

        // Timer czasu życia lawy
        _lifetimeTimer          = new Timer();
        _lifetimeTimer.WaitTime = Duration;
        _lifetimeTimer.OneShot  = true;
        _lifetimeTimer.Timeout  += OnLifetimeExpired;
        AddChild(_lifetimeTimer);
        _lifetimeTimer.Start();

        // Timer obrażeń (co sekundę)
        _damageTimer           = new Timer();
        _damageTimer.WaitTime  = TickInterval;
        _damageTimer.Autostart = true;
        _damageTimer.Timeout  += DamageZombiesInPool;
        AddChild(_damageTimer);
    }

    public override void _Process(double delta)
    {
        // Efekt migotania lawy
        _flickerTime += (float)delta * 4f;

        if (GodotObject.IsInstanceValid(_sprite))
        {
            // Oblicz czas jaki pozostał do wygaśnięcia (efekt zanikania)
            float lifeLeft = _lifetimeTimer.TimeLeft > 0
                ? (float)_lifetimeTimer.TimeLeft / Duration
                : 1f;

            // Migotanie — pseudo-losowe za pomocą sinusa
            float flicker = (Mathf.Sin(_flickerTime * 2.3f) * 0.15f)
                          + (Mathf.Sin(_flickerTime * 5.7f) * 0.10f)
                          + 0.75f;
            flicker = Mathf.Clamp(flicker, 0.5f, 1.0f);

            float alpha = lifeLeft * flicker;

            // Modyfikujemy przeźroczystość sprite'a z lawą
            _sprite.Modulate = new Color(1f, 1f, 1f, alpha);
        }
    }

    // ── Detekcja zombie ───────────────────────────────────────────────────

    private void OnBodyEntered(Node2D body)
    {
        if (body is ZombieBase zombie && !_zombiesInPool.Contains(zombie))
        {
            _zombiesInPool.Add(zombie);
            GD.Print($"[LavaPool] Zombie wchodzi w lawę! (łącznie: {_zombiesInPool.Count})");
        }
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is ZombieBase zombie)
            _zombiesInPool.Remove(zombie);
    }

    // ── Obrażenia od lawy ─────────────────────────────────────────────────

    private void DamageZombiesInPool()
    {
        var toRemove = new List<ZombieBase>();

        foreach (var zombie in _zombiesInPool)
        {
            if (GodotObject.IsInstanceValid(zombie) && zombie.IsZombieAlive)
            {
                zombie.TakeDamage(DamagePerTick);
                GD.Print($"[LavaPool] 🔥 Lawa zadaje {DamagePerTick} obrażeń!");
            }
            else
            {
                toRemove.Add(zombie); // martwy zombie — wyczyść listę
            }
        }

        foreach (var z in toRemove)
            _zombiesInPool.Remove(z);
    }

    // ── Wygaśnięcie ───────────────────────────────────────────────────────

    private void OnLifetimeExpired()
    {
        GD.Print("[LavaPool] Lawa wygasła.");
        QueueFree();
    }
}
