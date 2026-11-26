using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Pulsar.DataClasses;
using Pulsar.Hazards;
using Pulsar.Interfaces;
using Pulsar.Ship.Data;
using Pulsar.Utilities;
using Pulsar.Weapons;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pulsar.Ship
{
[RequireComponent(typeof(CompositeCollider2D), typeof(Rigidbody2D))]
public class ShipManager : MonoBehaviour, IDamageable
{
    [Header("Refs")]
    [SerializeField]
    private CompositeCollider2D compositeCollider;

    [SerializeField]
    private Rigidbody2D rb;

    [SerializeField]
    private ThrusterManager thrusterManager;

    [Header("Detach Explosion")]
    [SerializeField]
    private Vector2 detachRadiateLinearVelocity = new(2f, 5f);

    [SerializeField]
    private Vector2 detachRadiateAngularVelocity = new(-180f, 180f);

    [SerializeField]
    private float collisionDmgMultiplier = 0.5f;

    private ShipGridData _gridData = new();
    private List<IThrustable> _thrusters = new(10);

    private List<ManualWeapon> _weapons = new(10);

    public IShipCore Core => _gridData.Core;

    public IReadOnlyDictionary<Vector2Int, Tile> Cells => _gridData.Cells;

    public Vector2 GetMaxLocalAccel(Vector2 localDir) =>
        thrusterManager.GetMaxLocalAccel(localDir);

    public float GetMaxAngleAccelDeg() =>
        thrusterManager.GetMaxAngularAccelDeg();

    private void Start()
    {
        RefreshTiles();
        _gridData.TileOrphanedEvent += DetatchTile;
    }

    private void OnDestroy()
    {
        _gridData.TileOrphanedEvent -= DetatchTile;
        foreach (Tile tile in _gridData.Tiles)
        {
            tile.DestroyEvent -= HandleTileDestroyed;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        const int contactPointIndex = 0;
        if (!_gridData.TryGetClosestTile(
                other.GetContact(contactPointIndex).point,
                out Tile tile))
        {
            return;
        }

        if (other.gameObject.TryGetComponent(out ShipManager ship))
        {
            float relativeVelocity = other.relativeVelocity.magnitude;
            float damage = CalculateDmgFromOtherShip(ship, relativeVelocity);
            tile.TakeDamage(damage);
        }
        else if (other.gameObject.TryGetComponent(
                     out ExplosiveProjectile explosiveProjectile))
        {
            Vector2 hitPoint = other.GetContact(contactPointIndex).point;
            ApplyExplosion(hitPoint, tile, explosiveProjectile);
        }
        else if (other.gameObject.TryGetComponent(out Projectile projectile))
        {
            tile.TakeDamage(projectile.Damage);
        }
    }

    [Button]
    private void DebugKillShip()
    {
        Tile core = (Tile)Core;
        core.TakeDamage(core.CurrentHp);
    }

    public event Action<ShipManager> ShipDestroyedEvent;
    public bool OwnsTile(Tile tile) => Cells.Values.Contains(tile);

    public event Action ShipGridUpdate;

    // this is only for Tutorial check for detaching tile
    public event Action OnDetachTile;

    private void RefreshTiles()
    {
        _gridData = new ShipGridData();
        _weapons = new List<ManualWeapon>();
        _thrusters = new List<IThrustable>();

        foreach (Tile tile in GetComponentsInChildren<Tile>())
        {
            Vector2Int localCell =
                ShipUtilities.LocalToGrid(
                    tile.transform.localPosition).ToVector2Int();
            TileRotation localRot =
                tile.transform.localRotation.eulerAngles.z.ToTileRotation();
            AttachTile(localCell, tile, localRot);
        }

        thrusterManager.CalculateThrusters(in _thrusters);
    }

    public void AttachTile(Vector2Int cell, Tile tile, TileRotation rotation)
    {
        if (!_gridData.TrySetTile(cell, tile))
        {
            return;
        }

        tile.gameObject.tag = gameObject.tag;
        ProcessTile(tile);
        rb.mass += tile.Mass;
        tile.Attach(cell, rotation);
        tile.transform.SetParent(transform);
        tile.transform.SetLocalPositionAndRotation(
            ShipUtilities.GridToLocal(cell),
            ShipUtilities.RotateQuarterOnZAxis(rotation.ToInt()));

        tile.DestroyEvent += HandleTileDestroyed;

        ShipGridUpdate?.Invoke();
    }

    private void ProcessTile(Tile tile)
    {
        if (tile is IThrustable thrustable)
        {
            _thrusters.Add(thrustable);
            thrusterManager.CalculateThrusters(in _thrusters);
        }

        if (tile is Weapon weapon)
        {
            weapon.SetParentCollider(compositeCollider);
        }

        if (tile is ManualWeapon manualWeapon)
        {
            _weapons.Add(manualWeapon);
        }
    }

    public void Fire()
    {
        foreach (ManualWeapon weapon in _weapons)
        {
            weapon.Fire();
        }
    }

    public void TakeDamageAt(Vector2 point, float damage)
    {
        if (_gridData.TryGetClosestTile(point, out Tile tile))
        {
            tile.TakeDamage(damage);
        }
    }

    private float CalculateDmgFromOtherShip(
        ShipManager otherShip,
        float relativeVelocity)
    {
        float m1 = rb.mass;
        float m2 = otherShip.rb.mass;
        float reducedMass = m1 * m2 / (m1 + m2);

        float kineticEnergy
            = 0.5f * reducedMass * relativeVelocity * relativeVelocity;
        float share = m2 / (m1 + m2);
        float damage = kineticEnergy * share * collisionDmgMultiplier;

        return damage;
    }

    private void ApplyExplosion(
        Vector2 hitPoint,
        Tile hitTile,
        ExplosiveProjectile explosive)
    {
        float radius = explosive.ExplosionRadius;

        foreach (Tile tile in _gridData.Tiles)
        {
            // tile thats hit takes normal damage
            if (tile == hitTile)
            {
                tile.TakeDamage(explosive.Damage);
                continue;
            }

            float distance = Vector2.Distance(
                tile.transform.position, hitPoint);

            if (distance > radius)
            {
                continue;
            }

            // this is the 0 - 1, where 0 is the hit point and 1 is at the edge
            float normalizedDistance = distance / radius;

            float damage = explosive.Damage *
                           Mathf.Lerp(1f, explosive.EdgeDamageFraction,
                               normalizedDistance);

            tile.TakeDamage(damage);
        }
    }

    public void Move(Vector2 position)
    {
        thrusterManager.HandleThrust(position);
    }

    public void Rotate(float rotation)
    {
        thrusterManager.HandleRotation(rotation);
    }

    private void HandleTileDestroyed(Tile tile)
    {
        if (tile is IShipCore)
        {
            OnCoreDestroyed();
            return;
        }

        rb.mass -= tile.Mass;
        if (!_gridData.RemoveTile(tile.Cell).IsSuccess)
        {
            Debug.LogError($"Failed to remove {tile.name}");
            return;
        }


        UnregisterTile(tile);
        ShipGridUpdate?.Invoke();
        // The build controls remove owned tiles by destroying them.
        OnDetachTile?.Invoke();
    }

    private void UnregisterTile(Tile tile)
    {
        tile.DestroyEvent -= HandleTileDestroyed;
        // [CB] Since we *could* have a tile that is a thruster AND a manual weapon
        // We need to do all checks in a row rather than using a switch statement.
        if (tile is IThrustable thrustable)
        {
            _thrusters.Remove(thrustable);
            thrusterManager.CalculateThrusters(in _thrusters);
        }

        if (tile is ManualWeapon weapon)
        {
            _weapons.Remove(weapon);
        }
    }

    private void OnCoreDestroyed()
    {
        // Spawn before the tiles detach, so the ship's size is still known.
        SpawnDeathExplosion();

        foreach (Tile tile in _gridData.Tiles)
        {
            if (tile is IShipCore)
            {
                continue;
            }

            DetatchTile(tile);
        }

        ShipDestroyedEvent?.Invoke(this);
        Destroy(gameObject);
    }

    private void SpawnDeathExplosion()
    {
        // The core holds the effect
        GameObject prefab = Core?.DeathExplosionPrefab;
        if (prefab == null)
        {
            return;
        }

        Vector3 position = Core is Component core
            ? core.transform.position
            : transform.position;
        GameObject explosion = Instantiate(prefab, position,
            Quaternion.identity);

        // Bigger ships get bigger explosions (min 1, max 2.5)
        Bounds bounds = GetLocalBounds();
        float shipSize = Mathf.Max(bounds.size.x, bounds.size.y);
        explosion.transform.localScale
            = Vector3.one * Mathf.Clamp(shipSize / 3f, 1f, 2.5f);
    }

    // returns a rectangle enclosing all ship tiles in the ship’s local space
    public Bounds GetLocalBounds()
    {
        // return compositeCollider.bounds;
        if (Cells.Count == 0)
        {
            return new Bounds(Vector3.zero,
                new Vector3(1f, 1f, 0f));
        }

        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        foreach (Vector2Int c in Cells.Keys)
        {
            min = Vector2.Min(min, c);
            max = Vector2.Max(max, c);
        }

        Vector2 size = max - min + Vector2.one;
        return new Bounds(
            ShipUtilities.GridToLocal((min + max) * 0.5f),
            new Vector3(size.x, size.y, 0f));
    }

    #region ATTACH-DETACH

    public bool CanAttach(
        Vector2Int pos,
        TileInfo info,
        TileRotation rotation)
    {
        // Cannot attach to an occupied cell
        if (_gridData.ContainsCell(pos))
        {
            return false;
        }

        return CountConnections(pos, info, rotation) > 0;
    }

    private int CountConnections(
        Vector2Int pos,
        TileInfo info,
        TileRotation rotation)
    {
        // Count all neighbours that can connect at this position and rotation
        int count = 0;
        foreach (TileRotation direction in Enum.GetValues(
                     typeof(TileRotation)))
        {
            // Get the tile edge after rotation
            int localDir = direction.Subtract(rotation).ToInt();
            if (!info.connectableEdges[localDir])
            {
                continue;
            }

            // Count only when both touching edges can connect
            Vector2Int neighborPos = pos + direction.ToVector2Int();
            if (_gridData.TryGetTile(neighborPos, out Tile neighbor)
                && neighbor.EdgeConnectable(direction.Opposite()))
            {
                count++;
            }
        }

        return count;
    }

    public Result<(Vector2Int cell, TileRotation rotation)> FindBestAttachment(
        TileInfo info,
        Vector3 worldPos)
    {
        // Convert world position to ship grid position
        Vector2 localPos = ShipUtilities.LocalToGrid(
            transform.InverseTransformPoint(worldPos));

        float bestDist = float.MaxValue;
        TileRotation? bestRot = null;
        int bestConnectionCount = 0;
        Vector2Int bestCell = default;

        HashSet<Vector2Int> candidates = new();
        foreach (Vector2Int occupied in Cells.Keys)
        {
            foreach (TileRotation direction in Enum.GetValues(
                         typeof(TileRotation)))
            {
                candidates.Add(occupied + direction.ToVector2Int());
            }
        }

        float nearestDistance = candidates
            .Where(candidate => !_gridData.ContainsCell(candidate))
            .Aggregate(float.MaxValue,
                (current, candidate) => Mathf.Min(
                    current, Vector2.Distance(localPos, candidate)
                ));

        foreach (Vector2Int candidate in candidates)
        {
            // Skip occupied cells
            if (_gridData.ContainsCell(candidate))
            {
                continue;
            }

            float dist = Vector2.Distance(localPos, candidate);
            if (dist > nearestDistance + 0.01f)
            {
                continue;
            }

            // Try all 4 rotations
            foreach (TileRotation direction in Enum.GetValues(
                         typeof(TileRotation)))
            {
                // TODO: [CB] See if we can make these functions take a TileRotation in.
                if (!CanAttach(candidate, info, direction))
                {
                    continue;
                }

                // prefer more connections
                int connectionCount
                    = CountConnections(candidate, info, direction);

                if (bestRot == null
                    || dist < bestDist - 0.01f
                    || (Mathf.Abs(dist - bestDist) < 0.01f &&
                        connectionCount > bestConnectionCount))
                {
                    bestDist = dist;
                    bestCell = candidate;
                    bestRot = direction;
                    bestConnectionCount = connectionCount;
                }
            }
        }

        return !bestRot.HasValue
            ? Result<(Vector2Int cell, TileRotation rotation)>.Fail()
            : Result<(Vector2Int cell, TileRotation rotation)>.Pass((bestCell,
                bestRot.Value));
    }

    public void DetatchTile(Tile tile)
    {
        if (!_gridData.ContainsCell(tile.Cell))
        {
            return;
        }

        // Save the ship's motion so released tiles inherit it.
        Vector2 centerOfMass = rb.worldCenterOfMass;
        Vector2 linearVelocity = rb.linearVelocity;
        float angularVelocity = rb.angularVelocity;
        Vector2 explosionCenter = ((Tile)Core).transform.position;

        float minSpeed = Mathf.Max(0f,
            Mathf.Min(detachRadiateLinearVelocity.x,
                detachRadiateLinearVelocity.y));
        float maxSpeed = Mathf.Max(0f,
            Mathf.Max(detachRadiateLinearVelocity.x,
                detachRadiateLinearVelocity.y));
        float minSpin = Mathf.Min(detachRadiateAngularVelocity.x,
            detachRadiateAngularVelocity.y);
        float maxSpin = Mathf.Max(detachRadiateAngularVelocity.x,
            detachRadiateAngularVelocity.y);

        Vector2 worldPos = tile.transform.position;

        // Inherit movement
        Vector2 offset = worldPos - centerOfMass;
        Vector2 velocity = linearVelocity +
                           new Vector2(-offset.y, offset.x) *
                           (angularVelocity * Mathf.Deg2Rad);

        // Push away from Core
        Vector2 outward = worldPos - explosionCenter;
        if (outward.sqrMagnitude < 0.0001f)
        {
            float angle = Random.Range(0f, 2f * Mathf.PI);
            outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        // Add an outward push and random spin to the inherited ship motion.
        float speed = Random.Range(minSpeed, maxSpeed);
        velocity += outward.normalized * speed;
        float spin = Random.Range(minSpin, maxSpin);

        Result<Tile> result = _gridData.RemoveTile(tile.Cell);
        if (result.Value && result.Value != tile)
        {
            throw new Exception("Removed the wrong tile!");
        }

        UnregisterTile(tile);
        tile.gameObject.tag = "Untagged";
        tile.Detatch(velocity, angularVelocity + spin);
        rb.mass -= tile.Mass;

        thrusterManager.CalculateThrusters(in _thrusters);
        ShipGridUpdate?.Invoke();
    }

    #endregion
}
}
