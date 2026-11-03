using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WaveEntry
{
    public GameObject enemyPrefab;
    public int count = 1;
    public float spawnInterval = 1f;
}

// Where a wave's enemies appear around the player.
public enum SpawnPattern
{
    Surround, // Anywhere around the player
    OneSide,  // All from one direction
    Pincer,   // From two opposite directions, alternating
}

[CreateAssetMenu(fileName = "NewWave", menuName = "Pulsar/Wave Definition")]
public class WaveDefinitionSO : ScriptableObject
{
    public string waveName;
    public SpawnPattern pattern = SpawnPattern.Surround;

    [Tooltip("How wide (in degrees) the OneSide and Pincer patterns spread.")]
    [Range(0f, 180f)]
    public float patternSpread = 40f;

    [Tooltip("Safety net: seconds after this wave finishes spawning before " +
             "it counts as complete even if enemies are left. 0 = off.")]
    [Min(0f)]
    public float maxWaveTime = 120f;

    public List<WaveEntry> entries = new();
}
