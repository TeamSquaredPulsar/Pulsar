using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Pulsar.Hazards;
using Pulsar.Ship.Data;
using Pulsar.Ship.Extensions;
using Pulsar.Utilities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pulsar.Ship
{
[RequireComponent(typeof(CompositeCollider2D), typeof(Rigidbody2D))]
public class ShipManager : MonoBehaviour
{
    [Button]
    private void DebugKillShip()
    {
        Core.TakeDamage(Core.CurrentHp);
    }
    
    public event Action<ShipManager> ShipDestroyedEvent;
    
    [Header("Refs"), SerializeField,]
    private CompositeCollider2D compositeCollider;

    [SerializeField]
    private Rigidbody2D rb;

    [SerializeField]
    private ThrusterManager thrusterManager;

    [SerializeField]
    private List<WeaponTile> weapons = new(10);

    [SerializeField]
    private List<ThrusterTile> thrusters = new(10);

    [Header("Detach Explosion"), SerializeField,]
    private Vector2 detachRadiateLinearVelocity = new(2f, 5f);

    [SerializeField]
    private Vector2 detachRadiateAngularVelocity = new(-180f, 180f);

    private ShipGridData _gridData = new();

    public CoreTile Core => _gridData.Core;

    public IReadOnlyDictionary<Vector2Int, Tile> Cells => _gridData.Cells;
    public bool OwnsTile(Tile tile) => Cells.Values.Contains(tile);

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

    private void RefreshTiles()
    {
        _gridData = new ShipGridData();
        weapons = new List<WeaponTile>();
        thrusters = new List<ThrusterTile>();
        
        foreach (Tile tile in GetComponentsInChildren<Tile>())
        {
            Vector2Int localCell = 
                ShipUtilities.LocalToGrid(
                    tile.transform.localPosition).ToVector2Int();
            TileRotation localRot = 
                tile.transform.localRotation.eulerAngles.z.ToTileRotation();
            AttachTile(localCell, tile, localRot);
        }
    }

    public void AttachTile(Vector2Int cell, Tile tile, TileRotation rotation)
    {
        ProcessTile(tile);
        if (!_gridData.TrySetTile(cell, tile))
        {
            return;
        }

        rb.mass += tile.Mass;
        tile.Attach(cell, rotation);
        tile.transform.SetParent(transform);
        tile.transform.SetLocalPositionAndRotation(
            ShipUtilities.GridToLocal(cell),
            ShipUtilities.RotateQuarterOnZAxis(rotation.ToInt()));
        tile.DestroyEvent += HandleTileDestroyed;
    }

    private void ProcessTile(Tile tile)
    {
        switch (tile)
        {
            case ThrusterTile thruster:
                thrusters.Add(thruster);
                break;
            case WeaponTile weapon:
                weapons.Add(weapon);
                weapon.SetParentCollider(compositeCollider);
                break;
        }
    }

    public void Fire()
    {
        foreach (WeaponTile weapon in weapons)
        {
            weapon.Fire();
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

        if (other.gameObject.TryGetComponent(out Projectile projectile))
        {
            tile.TakeDamage(projectile.Damage);
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
        if (tile is CoreTile)
        {
            OnCoreDestroyed();
        }
        
        rb.mass -= tile.Mass;
        if (!_gridData.RemoveTile(tile.Cell).IsSuccess)
        {
            Debug.LogError($"Failed to remove {tile.name}");
            return;
        }

        switch (tile)
        {
            case ThrusterTile thruster:
                thrusters.Remove(thruster);
                break;
            case WeaponTile weapon:
                weapons.Remove(weapon);
                break;
        }

        tile.DestroyEvent -= HandleTileDestroyed;
    }
    
    private void OnCoreDestroyed()
    { foreach (Tile tile in _gridData.Tiles)
        {
            if (tile is CoreTile)
            {
                continue;
            }
            
            DetatchTile(tile);
        }
        
        ShipDestroyedEvent?.Invoke(this);
        Destroy(gameObject);
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

    public Vector3 CellToWorld(Vector2Int cell) =>
        transform.TransformPoint(ShipUtilities.GridToLocal(cell));

    #region ATTACH-DETACH

    public bool CanAttach(Vector2Int pos, TileInfoSO infoSo, TileRotation rotation)
    {
        // Cannot attach to an occupied cell
        if (_gridData.ContainsCell(pos))
        {
            return false;
        }

        foreach (TileRotation direction in Enum.GetValues(
                     typeof(TileRotation)))
        {
            // Get the tile edge facing this direction after rotation
            int localDir = direction.Subtract(rotation).ToInt();
            if (!infoSo.connectableEdges[localDir])
            {
                continue;
            }

            // Check the neighbour's opposite edge; both touching edges must connect
            Vector2Int neighborPos = pos + direction.ToVector2Int();
            if (_gridData.TryGetTile(neighborPos, out Tile neighbor)
                && neighbor.EdgeConnectable(direction.Opposite()))
            {
                return true;
            }
        }

        return false;
    }

    private int CountConnections(
        Vector2Int pos,
        TileInfoSO infoSo,
        TileRotation rotation)
    {
        // Count all neighbours that can connect at this position and rotation
        int count = 0;
        foreach (TileRotation direction in Enum.GetValues(
                     typeof(TileRotation)))
        {
            // Get the tile edge after rotation
            int localDir = direction.Subtract(rotation).ToInt();
            if (!infoSo.connectableEdges[localDir])
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
        TileInfoSO infoSo,
        Vector3 worldPos)
    {
        // Convert world position to ship grid position
        Vector2 localPos = ShipUtilities.LocalToGrid(
            transform.InverseTransformPoint(worldPos));

        float bestDist = float.MaxValue;
        TileRotation? bestRot = null;
        int bestConnectionCount = 0;
        Vector2Int bestCell = default;
        // public static readonly Vector2Int[] Dirs =
        // {
        //     Vector2Int.up,
        //     Vector2Int.right,
        //     Vector2Int.down,
        //     Vector2Int.left,
        // };
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
                if (!CanAttach(candidate, infoSo, direction))
                {
                    continue;
                }

                // prefer more connections
                int connectionCount
                    = CountConnections(candidate, infoSo, direction);

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
            : Result<(Vector2Int cell, TileRotation rotation)>.Pass((bestCell, bestRot.Value));
    }

    private void DetatchTile(Tile tile)
    {
        // Save the ship's motion so released tiles inherit it.
        Vector2 centerOfMass = rb.worldCenterOfMass;
        Vector2 linearVelocity = rb.linearVelocity;
        float angularVelocity = rb.angularVelocity;
        Vector2 explosionCenter = Core.transform.position;
        
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

        tile.Detatch(velocity, angularVelocity + spin);
        rb.mass -= tile.Mass;
    }

    #endregion

}
}