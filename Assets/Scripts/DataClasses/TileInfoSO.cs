using UnityEngine;

namespace Pulsar.DataClasses
{
[CreateAssetMenu(fileName = "NewTileInfo", menuName = "Pulsar/Tile Info")]
public class TileInfoSO : ScriptableObject
{
    [Header("Common Info")]
    public string tileName;
    public float hp = 10f;

    public bool[] connectableEdges = { true, true, true, true, };
}
}