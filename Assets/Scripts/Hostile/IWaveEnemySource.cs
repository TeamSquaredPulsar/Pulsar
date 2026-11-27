namespace Pulsar.Hostile
{
/// <summary>
/// Something a wave can switch on and off that sends enemies at the player
/// (a space station). WaveManager only talks to this, so it doesn't need to
/// know about the station code (which lives in an assembly that depends on
/// this one, so it can't be referenced from here).
/// </summary>
public interface IWaveEnemySource
{
    // How many enemies this source sent that are still alive.
    int AliveEnemies { get; }

    // Start sending enemies.
    void StartSpawning();

    // Stop sending new enemies. The ones already out keep fighting.
    void StopSpawning();
}
}
