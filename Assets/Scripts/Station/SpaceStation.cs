using System;
using System.Collections;
using System.Collections.Generic;
using Pulsar.Stations.Generation;
using Pulsar.Stations.Guards;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pulsar.Stations
{
/// <summary>
/// One script for every station. The station's StationProfile says what's
/// different (colour tiles, weapons, waves). When the station is enabled (its
/// scene loads), it builds its first wave of guards with the EnemyGenerator and spreads them on
/// a ring around itself. A new, harder wave comes once the last one is
/// destroyed, up to the profile's wave count. Guards only patrol around the
/// station; they never chase the player. It doesn't follow the game's state: OnEnable starts it from wave 1, and
/// OnDisable (which runs when its scene unloads) clears its guards. So it works
/// the same in test scenes and in the real game.
/// </summary>
public class SpaceStation : MonoBehaviour
{
    [SerializeField]
    private StationProfile profile;

    [Tooltip(
        "How far out the guards spawn and patrol, as a multiple of the station's own radius.")]
    [SerializeField]
    private float ringScale = 1.8f;

    [Header("Debug (read only)")]
    [SerializeField]
    private int currentWave = -1;

    // The guards this station spawned that are still alive.
    private readonly List<EnemyGuardAI> spawnedGuards = new();

    private bool _defending;
    private Coroutine _nextWaveRoutine;

    public StationProfile Profile => profile;

    // How far the station's furthest tile reaches (measured when it starts defending).
    public float Radius { get; private set; }
    public bool IsCleared { get; private set; }

    private void OnEnable()
    {
        BeginDefense();
    }

    private void OnDisable()
    {
        ResetStation();
    }

    // Every wave of this station was destroyed.
    public static event Action<SpaceStation> StationClearedEvent;

    // Starts the station over from wave 1.
    public void BeginDefense()
    {
        ResetStation();
        if (!profile)
        {
            Debug.LogError(
                $"{name} has no StationProfile, so it can't spawn guards.");
            return;
        }

        Radius = CalculateRadius();
        _defending = true;
        SpawnWave(0);
    }

    // Removes this station's guards and stops it (OnDisable, and before a restart).
    public void ResetStation()
    {
        if (_nextWaveRoutine != null)
        {
            StopCoroutine(_nextWaveRoutine);
            _nextWaveRoutine = null;
        }

        foreach (EnemyGuardAI guard in spawnedGuards)
        {
            if (guard)
            {
                guard.Defeated -= HandleGuardDefeated;
                Destroy(guard.gameObject);
            }
        }

        spawnedGuards.Clear();
        currentWave = -1;
        _defending = false;
        IsCleared = false;
    }

    private void SpawnWave(int waveIndex)
    {
        currentWave = waveIndex;
        int guardCount = profile.Waves[waveIndex].GuardCount;
        Vector2 centre = transform.position;
        float ringRadius = Radius * ringScale;
        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < guardCount; i++)
        {
            float angle = startAngle + i * 360f / guardCount;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 position = centre + new Vector2(Mathf.Cos(radians),
                Mathf.Sin(radians)) * ringRadius;
            // Nose pointing along the ring, the way it will patrol.
            float facing = angle;

            EnemyGuardAI guard = EnemyGenerator.GenerateEnemy(profile,
                waveIndex, position, facing);
            if (!guard)
            {
                continue;
            }

            guard.Init(this, profile.GuardInfo, ringRadius, angle);
            guard.Defeated += HandleGuardDefeated;
            spawnedGuards.Add(guard);
        }
    }

    private void HandleGuardDefeated(EnemyGuardAI guard)
    {
        guard.Defeated -= HandleGuardDefeated;
        spawnedGuards.Remove(guard);
        if (spawnedGuards.Count > 0 || !_defending)
        {
            return;
        }

        int nextWave = currentWave + 1;
        if (nextWave < profile.Waves.Length)
        {
            _nextWaveRoutine = StartCoroutine(SpawnWaveAfterDelay(nextWave));
            return;
        }

        _defending = false;
        IsCleared = true;
        Debug.Log($"{name} cleared!");
        StationClearedEvent?.Invoke(this);
    }

    private IEnumerator SpawnWaveAfterDelay(int waveIndex)
    {
        yield return new WaitForSeconds(profile.SecondsBetweenWaves);
        _nextWaveRoutine = null;
        SpawnWave(waveIndex);
    }

    // How far the station's furthest tile corner reaches from its centre.
    public float CalculateRadius()
    {
        float radius = 0f;
        Vector2 stationCentre
            = new Vector2(transform.position.x, transform.position.y);

        foreach (SpriteRenderer tile in
                 GetComponentsInChildren<SpriteRenderer>())
        {
            Bounds bounds = tile.bounds;
            Vector3 extents = bounds.extents;

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    Vector2 corner = new Vector2(
                        bounds.center.x + extents.x * x,
                        bounds.center.y + extents.y * y);
                    radius = Mathf.Max(radius,
                        Vector2.Distance(stationCentre, corner));
                }
            }
        }

        return radius;
    }
}
}
