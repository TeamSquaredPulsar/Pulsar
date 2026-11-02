using Pulsar.Ship;
using Pulsar.Tiles;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewTileInfo", menuName = "Pulsar/Tile Info")]
public class TileInfoSO : ScriptableObject
{
    [Header("Common Info")]
    public string tileName;
    public TileType type;
    public float hp = 10f;

    public bool[] connectableEdges = { true, true, true, true, };
    
    [Header("Health Sprites (default)")]
    public TileSpritesSO sprites;
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
