using Pulsar.Ship;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTileInfo", menuName = "Pulsar/Tile Info")]
public class TileInfoSO : ScriptableObject
{
    [Header("Common Info")]
    public string tileName;

    public TileType type;
    public Sprite sprite;
    public float hp = 10f;

    public bool[] connectableEdges = { true, true, true, true, };
}

namespace Pulsar.Ship
{
public enum TileType
    {
        Core,
        Chassis,
        Thruster,
        Weapon,
    }
}
