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

[CreateAssetMenu(fileName = "NewWave", menuName = "Pulsar/Wave Definition")]
public class WaveDefinitionSO : ScriptableObject
{
    public string waveName;
    public List<WaveEntry> entries = new();
}
