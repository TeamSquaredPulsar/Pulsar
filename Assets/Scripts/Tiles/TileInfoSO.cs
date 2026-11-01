using Pulsar.Ship;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTileInfo", menuName = "Pulsar/Tile Info")]
public class TileInfoSO : ScriptableObject
{
    [Header("Common Info")]
    public string tileName;
    public TileType type;
    public float hp = 10f;

    public bool[] connectableEdges = { true, true, true, true, };
    
    [Header("Health Sprites")]
    public Sprite fullHealthSprite;
    public Sprite seventyFivePercentSprite;
    public Sprite fiftyPercentSprite;
    public Sprite twentyFivePercentSprite;
    public Sprite tenPercentSprite;
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
