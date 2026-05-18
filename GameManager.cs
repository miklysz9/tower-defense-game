using Godot;
using Godot.Collections;

/// <summary>
/// Główny kontroler gry: zarządza stanami, falami zombie i wynikiem.
/// Autoload: Project > Project Settings > Autoload > GameManager
/// </summary>
public partial class GameManager : Node
{
	public static GameManager Instance { get; private set; }

	// ── Stany gry ────────────────────────────────────────────────────────
	public enum GameState { Menu, Playing, Paused, Won, Lost }

	private GameState _state = GameState.Menu;
	public  GameState State
	{
		get => _state;
		private set { _state = value; EmitSignal(SignalName.GameStateChanged, (int)value); }
	}

	// ── Konfiguracja fal ─────────────────────────────────────────────────
	[Export] public PackedScene BasicZombieScene;
	[Export] public float WaveInterval     = 20f;   // sekundy między falami
	[Export] public int   ZombiesPerWave   = 5;
	[Export] public int   TotalWaves       = 5;

	private int   _currentWave    = 0;
	private int   _aliveZombies   = 0;
	private Timer _waveTimer;

	// ── Sygnały ─────────────────────────────────────────────────────────
	[Signal] public delegate void GameStateChangedEventHandler(int state);
	[Signal] public delegate void WaveStartedEventHandler(int waveNumber);
	[Signal] public delegate void WaveEndedEventHandler(int waveNumber);
	[Signal] public delegate void GameOverEventHandler(bool playerWon);

	// ── Cykl życia ───────────────────────────────────────────────────────
	public override void _Ready()
{
	Instance = this;

	// ŁADUJEMY SCENĘ ZOMBIE BEZPOŚREDNIO Z PLIKÓW PROJEKTU:
	// Upewnij się, że ścieżka i nazwa pliku (włącznie z wielkością liter) są poprawne!
	BasicZombieScene = GD.Load<PackedScene>("res://Scene/zombie_base.tscn");

	_waveTimer           = new Timer();
	_waveTimer.WaitTime  = WaveInterval;
	_waveTimer.Timeout  += StartNextWave;
	AddChild(_waveTimer);

	StartGame(); 
}

	// ── Kontrola gry ─────────────────────────────────────────────────────

	public void StartGame()
	{
		_currentWave  = 0;
		_aliveZombies = 0;
		State         = GameState.Playing;

		// Krótka pauza przed pierwszą falą (czas na posadzenie roślin)
		GetTree().CreateTimer(5.0).Timeout += StartNextWave;
		GD.Print("[GameManager] Gra startuje!");
	}

	public void PauseGame()
	{
		if (State != GameState.Playing) return;
		State = GameState.Paused;
		GetTree().Paused = true;
	}

	public void ResumeGame()
	{
		if (State != GameState.Paused) return;
		State = GameState.Playing;
		GetTree().Paused = false;
	}

	// ── Fale zombie ──────────────────────────────────────────────────────

	private void StartNextWave()
	{
		if (State != GameState.Playing) return;

		_currentWave++;
		if (_currentWave > TotalWaves)
		{
			WinGame();
			return;
		}

		GD.Print($"[GameManager] ═══ FALA {_currentWave}/{TotalWaves} ═══");
		EmitSignal(SignalName.WaveStarted, _currentWave);

		// Spawnuj zombie z opóźnieniami
		int count = ZombiesPerWave + (_currentWave) * 2;  // rośnie z każdą falą
		for (int i = 0; i < count; i++)
		{
			float delay = i * 1.5f;
			GetTree().CreateTimer(delay).Timeout += () => SpawnZombie();
		}
	}

	private void SpawnZombie()
	{
		if (BasicZombieScene == null) return;

		// Losowy rząd
		int row = GD.RandRange(0, GridManager.Instance.Rows - 1);
		float spawnX   = GetViewport().GetVisibleRect().Size.X + 40f;
		float spawnY   = GridManager.Instance.GetRowCenterY(row);

		var zombie = BasicZombieScene.Instantiate<ZombieBase>();
		zombie.GlobalPosition       = new Vector2(spawnX, spawnY);
		zombie.ZombieDied          += OnZombieDied;
		zombie.ZombieReachedEnd    += OnZombieReachedEnd;

		GetTree().CurrentScene.AddChild(zombie);
		_aliveZombies++;
	}

	// ── Zdarzenia zombie ─────────────────────────────────────────────────

	private void OnZombieDied(ZombieBase zombie)
	{
		_aliveZombies--;
		CheckWaveEnd();
	}

	private void OnZombieReachedEnd()
	{
		_aliveZombies--;
		LoseGame();           // zombie dotarł do domu → przegrana
	}

	private void CheckWaveEnd()
	{
		if (_aliveZombies > 0) return;

		GD.Print($"[GameManager] Fala {_currentWave} zakończona!");
		EmitSignal(SignalName.WaveEnded, _currentWave);

		if (_currentWave < TotalWaves)
			_waveTimer.Start();   // odczekaj, potem kolejna fala
		else
			WinGame();
	}

	// ── Koniec gry ───────────────────────────────────────────────────────

	private void WinGame()
	{
		State = GameState.Won;
		EmitSignal(SignalName.GameOver, true);
		GD.Print("[GameManager] ★ WYGRANA! ★");
	}

	private void LoseGame()
	{
		State = GameState.Lost;
		EmitSignal(SignalName.GameOver, false);
		GD.Print("[GameManager] ✗ Przegrana.");
	}
}
