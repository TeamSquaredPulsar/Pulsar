using System;
using UnityEngine;

namespace Pulsar.Ship
{
    [CreateAssetMenu(fileName = "NewTileInfo", menuName = "Pulsar/Tile Info")]
    public class TileInfoSO : ScriptableObject
    {
        [Header("Common Info")]
        public string tileName;

        public TileType type;
        public Sprite sprite;
        public float hp = 10f;

        public bool[] connectableEdges = { true, true, true, true };
    }



    public enum TileType
    {
        Core,
        Chassis,
        Thruster,
        Weapon
    }
}
