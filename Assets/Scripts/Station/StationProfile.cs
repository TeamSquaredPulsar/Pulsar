using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pulsar.Stations
{
// One wave of a station: how many guards, and how big each guard can be.
[Serializable]
public struct WaveConfig
{
    [Min(1)]
    public int guardCount;

    [Tooltip("Smallest guard in this wave, counting every tile (core, body, guns, thrusters).")]
    [Min(1)]
    public int minTiles;

    [Tooltip("Biggest guard in this wave, counting every tile.")]
    [Min(1)]
    public int maxTiles;
}

/// <summary>
/// Everything that makes one station different from another: its colour tiles,
/// its weapons, its waves and how close the player has to get before the
/// guards notice. One asset per station; the SpaceStation script is the same
/// on every station.
/// </summary>
[CreateAssetMenu(fileName = "NewStationProfile",
    menuName = "Pulsar/Station Profile")]
public class StationProfile : ScriptableObject
{
    [Header("Order")]
    [Tooltip("0 for the first station. Higher tiers are later and harder.")]
    [Min(0)]
    public int stationTier;

    [Tooltip("The station before this one. Its weapons (and the ones before it) " +
             "are added to this station's pool. Leave empty for the first station.")]
    public StationProfile previousStationProfile;

    [Header("Weapons")]
    [Tooltip("This station's own new weapon tile(s). Every guard carries at least one.")]
    public GameObject[] ownWeaponPrefabs;

    [Tooltip("About one gun for every this many tiles (always at least one gun).")]
    [Min(1)]
    public int tilesPerGun = 3;

    [Header("Guard tiles (this station's colour)")]
    public GameObject coreTilePrefab;

    [Tooltip("Square tiles that make up a guard's body.")]
    public GameObject[] bodyTilePrefabs;

    [Tooltip("Triangle tiles used on the edges of a guard's body.")]
    public GameObject[] cornerTilePrefabs;

    [Range(0f, 1f)]
    public float cornerChance = 0.3f;

    public GameObject thrusterPrefab;

    [Header("Guards")]
    [Tooltip("The empty guard ship (ShipManager, AI, behaviours) that the tiles are built onto.")]
    public GameObject guardShellPrefab;

    [Tooltip("Movement, aiming and scoring numbers for this station's guards.")]
    public EnemyInfo guardInfo;

    [Header("Waves (the list length is the number of waves)")]
    public WaveConfig[] waves =
    {
        new() { guardCount = 3, minTiles = 3, maxTiles = 5, },
        new() { guardCount = 4, minTiles = 5, maxTiles = 7, },
        new() { guardCount = 5, minTiles = 7, maxTiles = 10, },
    };

    [Min(0f)]
    public float secondsBetweenWaves = 2f;

    [Header("Detection")]
    [Tooltip("When the player is this close to the station's centre, its guards chase.")]
    [Min(0f)]
    public float detectionRange = 25f;

    [Tooltip("The guards give up once the player is this much further out than " +
             "the detection range (stops them flipping back and forth at the edge).")]
    [Min(0f)]
    public float loseInterestMargin = 8f;

    // This station's weapons plus every earlier station's weapons.
    public List<GameObject> GetAvailableWeapons()
    {
        List<GameObject> pool = new();
        HashSet<StationProfile> visited = new();

        for (StationProfile station = this;
             station != null && visited.Add(station);
             station = station.previousStationProfile)
        {
            if (station.ownWeaponPrefabs == null)
            {
                continue;
            }

            foreach (GameObject weapon in station.ownWeaponPrefabs)
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
