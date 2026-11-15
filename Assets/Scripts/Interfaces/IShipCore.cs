namespace Pulsar.Interfaces
{
/// <summary>
/// Interface for any tile that wants to count as a ship core.
/// This lets us have cores that are thrusters, weapons, regular chassis, etc.
/// Keep in mind, each ship still can only have 1 core, so technically a
/// multicore ship will explode if <i>any</i> of its core tiles are destroyed.
/// <b> USE THIS INTERFACE SPARINGLY.</b>
/// @author Corey Buchan
/// @version 0.9
/// </summary>
public interface IShipCore
{
}
}
