using UnityEngine;

namespace Pulsar.Stations
{
/// <summary>
/// Marks a station tile as a launch bay (the shared brown Launch rail tile).
/// Enemies come out of the bay's top edge: its transform.up points out of the
/// station. SpaceStation finds its bays and launches enemies from them.
/// </summary>
public class StationBay : MonoBehaviour
{
    // The way enemies fly out of this bay.
    public Vector2 Direction => transform.up;

    // A spot just past the bay's opening, far enough out that a ship of the
    // given radius doesn't touch the station.
    public Vector2 Mouth(float shipRadius, float gap)
    {
        float halfTile = 0.5f * transform.lossyScale.y;
        return (Vector2)transform.position + Direction * (halfTile + shipRadius + gap);
    }
}
}
