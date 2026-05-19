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

	// ── Konfiguracja fal i poziomów ──────────────────────────────────────
	[Export] public PackedScene BasicZombieScene;
	
	// Lista wszystkich poziomów w grze (wepchniesz tu pliki .tres w edytorze)
	[Export] public Array<LevelData> LevelsList;

	private int _currentLevelIndex = 0; // 0 = Poziom 1, 1 = Poziom 2 itd.
	private LevelData _currentLevelData;

	private int   _currentWave    = 0;
	private int   _aliveZombies   = 0;
	private Timer _waveTimer;

	// Te zmienne były powodem błędów CS0103 – teraz są polami klasy nadpisywanymi przez poziomy!
	public float WaveInterval { get; private set; } = 20f;
	public int   TotalWaves { get; private set; } = 5;

	// ── Sygnały ─────────────────────────────────────────────────────────
	[Signal] public delegate void GameStateChangedEventHandler(int state);
	[Signal] public delegate void WaveStartedEventHandler(int waveNumber);
	[Signal] public delegate void WaveEndedEventHandler(int waveNumber);
	[Signal] public delegate void GameOverEventHandler(bool playerWon);

	// ── Cykl życia ───────────────────────────────────────────────────────
	public override void _Ready()
	{
		Instance = this;

		// ŁADUJEMY SCENĘ ZOMBIE BEZPOŚREDNIO Z PLIKÓW PROJEKTU
		BasicZombieScene = GD.Load<PackedScene>("res://Scene/zombie_base.tscn");

		_waveTimer           = new Timer();
		// Początkowa konfiguracja timera
		_waveTimer.WaitTime  = WaveInterval;
		_waveTimer.Timeout  += StartNextWave;
		AddChild(_waveTimer);

		// Najpierw ładujemy pierwszy poziom (indeks 0), aby ustawić poprawne wartości WaveInterval i TotalWaves
		LoadLevel(0);
		StartGame(); 
	}

	// ── Kontrola gry ─────────────────────────────────────────────────────

	public void StartGame()
	{
		_currentWave  = 0;
		_aliveZombies = 0;
		State         = GameState.Playing;

		// Krótka pauza przed pierwszą falą (czas na posadzenie pierwszych Słoneczników)
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

		// NAPRAWIONE: Wyciąganie poprawnej liczby zombie ze słownika poziomu
		int zombiesToSpawn = 5; // bezpieczny fallback
		if (_currentLevelData != null && _currentLevelData.ZombiesPerWave.ContainsKey(_currentWave))
		{
			zombiesToSpawn = _currentLevelData.ZombiesPerWave[_currentWave];
		}

		// Spawnowanie z małym opóźnieniem (delay), żeby zombie nie wchodziły idealnie jeden na drugim
		for (int i = 0; i < zombiesToSpawn; i++)
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
		{
			// Aktualizujemy czas odliczania, ponieważ mógł się zmienić w zależności od poziomu
			_waveTimer.WaitTime = WaveInterval;
			_waveTimer.Start();   // odczekaj, potem kolejna fala
		}
		else
		{
			WinGame();
		}
	}

	// ── Koniec gry ───────────────────────────────────────────────────────

	private void WinGame()
	{
		State = GameState.Won;
		GD.Print("[GameManager] Wygrałeś ten poziom!");

		// Przejdź do kolejnego poziomu
		if (LevelsList != null && _currentLevelIndex + 1 < LevelsList.Count)
		{
			_currentLevelIndex++;
			LoadLevel(_currentLevelIndex);
			StartGame(); // Resetuje timery i odpala stan Playing
		}
		else
		{
			GD.Print("[GameManager] Gratulacje! Ukończyłeś całą grę!");
		}
	}

	private void LoseGame()
	{
		State = GameState.Lost;
		EmitSignal(SignalName.GameOver, false);
		GD.Print("[GameManager] ✗ Przegrana.");
	}
	
	// ── Levele ───────────────────────────────────────────────────────
	public void LoadLevel(int levelIndex)
	{
		if (LevelsList == null || levelIndex >= LevelsList.Count)
		{
			GD.PrintErr("[GameManager] Brak konfiguracji dla poziomu o indeksie: " + levelIndex);
			return;
		}

		_currentLevelIndex = levelIndex;
		_currentLevelData = LevelsList[levelIndex];

		// Nadpisujemy parametry gry danymi z poziomu
		_currentWave = 0;
		_aliveZombies = 0;
		TotalWaves = _currentLevelData.TotalWaves; 
		WaveInterval = _currentLevelData.WaveInterval;

		if (SunManager.Instance != null)
		{
			// Opcjonalnie: SunManager.Instance.ResetSun(_currentLevelData.StartingSun);
		}

		GD.Print($"[GameManager] Załadowano Poziom {_currentLevelData.LevelNumber}!");
	}
}
