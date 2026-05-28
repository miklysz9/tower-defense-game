using Godot;
using Godot.Collections;

[GlobalClass]
public partial class WaveData : Resource
{
	// Lista scen zombie, które zostaną zrodzone w tej konkretnej fali
	// Możesz tu wrzucić np. 3x zwykły zombie, 1x zombie z flagą, 1x zombie z pachołkiem
	[Export] public Array<PackedScene> ZombiesToSpawn;
}
