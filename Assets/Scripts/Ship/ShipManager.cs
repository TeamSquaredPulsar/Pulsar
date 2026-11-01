using System;
using System.Collections.Generic;
using System.Linq;
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
        rb.mass -= tile.Mass;
        if (!_gridData.RemoveTile(tile.Cell).IsSuccess)
        {
            Debug.Log($"Failed to remove {tile.name}");
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

    /// <summary>
    /// Casts toward the core and places the preview immediately outside the
    /// entry face of the first occupied cell hit. Empty neighboring cells do
    /// not intercept the ray, and the supplied rotation is never changed.
    /// </summary>
    public Result<Vector2Int> FindAttachmentAlongRay(
        TileInfoSO infoSo,
        Vector3 worldPos,
        TileRotation rotation)
    {
        if (!Core || !infoSo)
        {
            return Result<Vector2Int>.Fail();
        }

        Vector2 origin = ShipUtilities.LocalToGrid(
            transform.InverseTransformPoint(worldPos));
        Vector2 toCore = (Vector2)Core.Cell - origin;
        if (toCore.sqrMagnitude < 0.0001f)
        {
            return Result<Vector2Int>.Fail();
        }

        Ray ray = new(ShipUtilities.GridToLocal(origin), toCore.normalized);
        const float tieTolerance = 0.0001f;
        float nearestDistance = toCore.magnitude;
        Vector2Int hitCell = default;
        bool found = false;
        foreach (Vector2Int occupied in Cells.Keys)
        {
            Bounds bounds = new(ShipUtilities.GridToLocal(occupied),
                Vector3.one);
            if (bounds.Contains(ray.origin))
            {
                return Result<Vector2Int>.Fail();
            }

            if (!bounds.IntersectRay(ray, out float distance)
                || distance > nearestDistance + tieTolerance)
            {
                continue;
            }

            // Resolve exact corner hits consistently, independent of tile order.
            bool earlierCell = occupied.x < hitCell.x
                               || (occupied.x == hitCell.x
                                   && occupied.y < hitCell.y);
            if (!found || distance < nearestDistance - tieTolerance || earlierCell)
            {
                found = true;
                nearestDistance = distance;
                hitCell = occupied;
            }
        }

        if (!found)
        {
            return Result<Vector2Int>.Fail();
        }

        Vector2 offset = (Vector2)ray.GetPoint(nearestDistance) - (Vector2)hitCell;
        // At an exact corner prefer the horizontal face (up/down).
        Vector2Int outward = Mathf.Abs(offset.y) >= Mathf.Abs(offset.x) - tieTolerance
            ? new Vector2Int(0, offset.y >= 0f ? 1 : -1)
            : new Vector2Int(offset.x >= 0f ? 1 : -1, 0);
        Vector2Int candidate = hitCell + outward;
        return CanAttach(candidate, infoSo, rotation)
            ? Result<Vector2Int>.Pass(candidate)
            : Result<Vector2Int>.Fail();
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
