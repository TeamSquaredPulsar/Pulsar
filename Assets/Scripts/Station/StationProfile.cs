using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pulsar.Stations
{
// One wave of a station: how many guards, and how big each guard can be.
// Read-only from outside: values are set in the Inspector (or the constructor).
// FormerlySerializedAs keeps the values saved under the old field names.
[Serializable]
public struct WaveConfig
{
    [field: SerializeField]
    [field: FormerlySerializedAs("guardCount")]
    [field: Min(1)]
    public int GuardCount { get; private set; }

    [field: SerializeField]
    [field: FormerlySerializedAs("minTiles")]
    [field: Tooltip(
        "Smallest guard in this wave, counting every tile (core, body, guns, thrusters).")]
    [field: Min(1)]
    public int MinTiles { get; private set; }

    [field: SerializeField]
    [field: FormerlySerializedAs("maxTiles")]
    [field: Tooltip("Biggest guard in this wave, counting every tile.")]
    [field: Min(1)]
    public int MaxTiles { get; private set; }

    public WaveConfig(int guardCount, int minTiles, int maxTiles)
    {
        GuardCount = guardCount;
        MinTiles = minTiles;
        MaxTiles = maxTiles;
    }

    // Pushes MaxTiles up to MinTiles if it was set lower.
    public void ClampMaxToMin()
    {
        MaxTiles = Mathf.Max(MaxTiles, MinTiles);
    }
}

/// <summary>
/// Everything that makes one station different from another: its colour tiles,
/// its weapons, its waves and how close the player has to get before the
/// guards notice. One asset per station; the SpaceStation script is the same
/// on every station.
/// Every value is read-only from outside (get; private set;) because this one
/// asset is shared by every guard at the station: a change made through any
/// reference would change it for all of them, and in the Editor it would stay
/// changed in the asset. FormerlySerializedAs keeps the values saved under the
/// old field names.
/// </summary>
[CreateAssetMenu(fileName = "NewStationProfile",
    menuName = "Pulsar/Station Profile")]
public class StationProfile : ScriptableObject
{
    [field: Header("Order")]
    [field: SerializeField]
    [field: FormerlySerializedAs("stationTier")]
    [field:
        Tooltip("0 for the first station. Higher tiers are later and harder.")]
    [field: Min(0)]
    public int StationTier { get; private set; }

    [field: SerializeField]
    [field: FormerlySerializedAs("previousStationProfile")]
    [field: Tooltip(
        "The station before this one. Its weapons (and the ones before it) " +
        "are added to this station's pool. Leave empty for the first station.")]
    public StationProfile PreviousStationProfile { get; private set; }

    [field: Header("Weapons")]
    [field: SerializeField]
    [field: FormerlySerializedAs("ownWeaponPrefabs")]
    [field: Tooltip(
        "This station's own new weapon tile(s). Every guard carries at least one.")]
    public GameObject[] OwnWeaponPrefabs { get; private set; }

    [field: SerializeField]
    [field: FormerlySerializedAs("tilesPerGun")]
    [field: Tooltip(
        "About one gun for every this many tiles (always at least one gun).")]
    [field: Min(1)]
    public int TilesPerGun { get; private set; } = 3;

    [field: Header("Guard tiles (this station's colour)")]
    [field: SerializeField]
    [field: FormerlySerializedAs("coreTilePrefab")]
    public GameObject CoreTilePrefab { get; private set; }

    [field: SerializeField]
    [field: FormerlySerializedAs("bodyTilePrefabs")]
    [field: Tooltip("Square tiles that make up a guard's body.")]
    public GameObject[] BodyTilePrefabs { get; private set; }

    [field: SerializeField]
    [field: FormerlySerializedAs("cornerTilePrefabs")]
    [field: Tooltip("Triangle tiles used on the edges of a guard's body.")]
    public GameObject[] CornerTilePrefabs { get; private set; }

    [field: SerializeField]
    [field: FormerlySerializedAs("cornerChance")]
    [field: Range(0f, 1f)]
    public float CornerChance { get; private set; } = 0.3f;

    [field: SerializeField]
    [field: FormerlySerializedAs("thrusterPrefab")]
    public GameObject ThrusterPrefab { get; private set; }

    [field: Header("Guards")]
    [field: SerializeField]
    [field: FormerlySerializedAs("guardShellPrefab")]
    [field: Tooltip(
        "The empty guard ship (ShipManager, AI, behaviours) that the tiles are built onto.")]
    public GameObject GuardShellPrefab { get; private set; }

    [field: SerializeField]
    [field: FormerlySerializedAs("guardInfo")]
    [field:
        Tooltip(
            "Movement, aiming and scoring numbers for this station's guards.")]
    public EnemyInfo GuardInfo { get; private set; }

    [field: Header("Waves (the list length is the number of waves)")]
    [field: SerializeField]
    [field: FormerlySerializedAs("waves")]
    [field: ReorderableList]
    public WaveConfig[] Waves { get; private set; } =
    {
        new(guardCount: 3, minTiles: 3, maxTiles: 5),
        new(guardCount: 4, minTiles: 5, maxTiles: 7),
        new(guardCount: 5, minTiles: 7, maxTiles: 10),
    };

    [field: SerializeField]
    [field: FormerlySerializedAs("secondsBetweenWaves")]
    [field: Min(0f)]
    public float SecondsBetweenWaves { get; private set; } = 2f;

    [field: Header("Detection")]
    [field: SerializeField]
    [field: FormerlySerializedAs("detectionRange")]
    [field: Tooltip(
        "When the player is this close to the station's centre, its guards chase.")]
    [field: Min(0f)]
    public float DetectionRange { get; private set; } = 25f;

    [field: SerializeField]
    [field: FormerlySerializedAs("loseInterestMargin")]
    [field: Tooltip(
        "The guards give up once the player is this much further out than " +
        "the detection range (stops them flipping back and forth at the edge).")]
    [field: Min(0f)]
    public float LoseInterestMargin { get; private set; } = 8f;

    [field: Header("Enemies (pressure)")]
    [field: SerializeField]
    [field: Tooltip(
        "The most pressure this station's living enemies can add up to. " +
        "Each enemy counts its EnemyInfo's PressureScore.")]
    [field: Min(1)]
    public int MaxPressure { get; private set; } = 30;

    [field: SerializeField]
    [field: Tooltip(
        "The hand-built enemy prefabs this station can send. It picks one " +
        "at random from those whose PressureScore fits the room left.")]
    public EnemyController[] EnemyPrefabs { get; private set; }

    [field: SerializeField]
    [field: Tooltip("Wait between two enemy spawns, in seconds.")]
    [field: Min(0.1f)]
    public float SecondsBetweenSpawns { get; private set; } = 1.5f;

    // Runs whenever a value changes in the Inspector. NaughtyAttributes'
    // [MinValue] can't reach inside WaveConfig, so the profile keeps each
    // wave's MaxTiles at or above its MinTiles here instead.
    private void OnValidate()
    {
        if (Waves == null)
        {
            return;
        }

        for (int i = 0; i < Waves.Length; i++)
        {
            // Waves[i] is the array slot itself, so this changes it in place.
            Waves[i].ClampMaxToMin();
        }
    }

    // This station's weapons plus every earlier station's weapons.
    public List<GameObject> GetAvailableWeapons()
    {
        List<GameObject> pool = new();
        HashSet<StationProfile> visited = new();

        for (StationProfile station = this;
             station != null && visited.Add(station);
             station = station.PreviousStationProfile)
        {
            if (station.OwnWeaponPrefabs == null)
            {
                continue;
            }

            foreach (GameObject weapon in station.OwnWeaponPrefabs)
            {
                if (weapon != null && !pool.Contains(weapon))
                {
                    pool.Add(weapon);
                }
            }
        }

        return pool;
    }
}
}
