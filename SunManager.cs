using Godot;

/// <summary>
/// Zarządza walutą słońca — dodawanie, odejmowanie, sprawdzanie.
/// Autoload: Project > Project Settings > Autoload > SunManager
/// </summary>
public partial class SunManager : Node
{
	public static SunManager Instance { get; private set; }

	[Export] public int StartingSun = 300;

	private int _sun;
	public  int Sun
	{
		get => _sun;
		private set
		{
			_sun = Mathf.Max(0, value);          // nigdy poniżej 0
			EmitSignal(SignalName.SunChanged, _sun);
		}
	}

	[Signal] public delegate void SunChangedEventHandler(int newAmount);

	public override void _Ready()
	{
		Instance = this;
		Sun      = StartingSun;
	}

	public void AddSun(int amount)
	{
		Sun += amount;
		GD.Print($"[SunManager] +{amount} słońca → łącznie: {Sun}");
	}

	/// <summary>Próba wydania słońca. Zwraca true gdy udało się zapłacić.</summary>
	public bool SpendSun(int amount)
	{
		if (_sun < amount)
		{
			GD.Print($"[SunManager] Za mało słońca! Potrzeba {amount}, jest {_sun}");
			return false;
		}

		Sun -= amount;
		GD.Print($"[SunManager] -{amount} słońca → łącznie: {Sun}");
		return true;
	}

	public bool CanAfford(int amount) => _sun >= amount;
}
