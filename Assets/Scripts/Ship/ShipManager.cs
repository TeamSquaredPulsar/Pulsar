using System;
using System.Collections.Generic;
using System.Linq;
using Nrjwolf.Tools.AttachAttributes;
using Pulsar.Ship.Extensions;
using Pulsar.Utilities;
using UnityEngine;

[RequireComponent(typeof(CompositeCollider2D), typeof(Rigidbody2D))]
public class ShipManager : MonoBehaviour
{
    [Header("Refs")]
    [GetComponent]
    [SerializeField]
    private CompositeCollider2D compositeCollider;

    [GetComponent]
    [SerializeField]
    private Rigidbody2D rb;

    [GetComponent]
    [SerializeField]
    private ThrusterManager thrusterManager;

    [SerializeField]
    private List<WeaponTile> weapons = new(10);

    [SerializeField]
    private List<ThrusterTile> thrusters = new(10);

    [Header("Detach Explosion")]
    [SerializeField]
    private Vector2 detachRadiateLinearVelocity = new(2f, 5f);

    [SerializeField]
    private Vector2 detachRadiateAngularVelocity = new(-180f, 180f);

    private ShipGridData _gridData = new();

    [field: SerializeField]
    public CoreTile Core { get; private set; }

    public IReadOnlyDictionary<Vector2Int, Tile> Cells => _gridData.Cells;
    public bool OwnsTile(Tile tile) => Cells.Values.Contains(tile);

    private void Start()
    {
        RefreshTiles();
    }

    private void RefreshTiles()
    {
        _gridData = new ShipGridData();
        weapons = new List<WeaponTile>();
        thrusters = new List<ThrusterTile>();
        foreach (Tile tile in GetComponentsInChildren<Tile>())
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
                case CoreTile core:
                    _gridData.SetCore(core);
                    Core = core;
                    break;
            }

            _gridData.SetTile(
                tile.transform.position.ToVector2Int(),
                tile,
                rb);
            tile.DestroyEvent += HandleTileDestroyed;
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
        //TODO implement tile destruction logic here
    }
    

    // returns a rectangle enclosing all ship tiles in the ship’s local space
    public Bounds GetLocalBounds()
    {
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

    public bool CanAttach(Vector2Int pos, TileInfoSO infoSo, int rotation)
    {
        // Cannot attach to an occupied cell
        if (_gridData.ContainsCell(pos))
        {
            return false;
        }
        
        foreach(TileRotationEnum direction in Enum.GetValues(typeof(TileRotationEnum)))
        {
            // Get the tile edge facing this direction after rotation
            int localDir = direction.Add(rotation).ToInt();
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

    private int CountConnections(Vector2Int pos, TileInfoSO infoSo, int rotation)
    {
        // Count all neighbours that can connect at this position and rotation
        int count = 0;
        foreach(TileRotationEnum direction in Enum.GetValues(typeof(TileRotationEnum)))
        {
            // Get the tile edge after rotation
            int localDir = direction.Add(rotation).ToInt();
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

    // public bool Attach(Vector2Int pos, Tile tile, int rotation)
    // {
    //     if (tile == null || tile.IsAttached || tile.TileInfo == null)
    //     {
    //         return false;
    //     }
    //
    //     if (!CanAttach(pos, tile.TileInfo, rotation))
    //     {
    //         return false;
    //     }
    //
    //     tile.AttachTo(this, pos, rotation);
    //     GridData.SetTile(pos, tile);
    //     tile.SetAttached(this);
    //     OnTileAdded?.Invoke();
    //     return true;
    // }

    // public void DestroyTileAt(Vector2Int pos)
    // {
    //     if (!GridData.TryGetTile(pos, out Tile tile))
    //     {
    //         return;
    //     }
    //
    //     if (tile == Core)
    //     {
    //         OnCoreDestroyed();
    //         return;
    //     }
    //
    //     GridData.RemoveTile(pos);
    //     tile.SetDetached(this);
    //     Destroy(tile.gameObject);
    //     int orphanTilesNum = DetachOrphans();
    //     OnTilesRemoved?.Invoke(orphanTilesNum + 1);
    // }

    // private void OnCoreDestroyed()
    // {
    //     OnShipDestroyed?.Invoke();
    // }

    public Result<(Vector2Int cell, int rotation)> FindBestAttachment(
        TileInfoSO infoSo,
        Vector3 worldPos)
    {
        // Convert world position to ship grid position
        Vector2 localPos = ShipUtilities.LocalToGrid(
                transform.InverseTransformPoint(worldPos));

        float bestDist = float.MaxValue;
        int bestRot = -1;
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
        int directions = Enum.GetNames(typeof(TileRotationEnum)).Length;
        foreach (Vector2Int occupied in Cells.Keys)
        {
            for (int d = 0; d < directions; d++)
            {
                candidates.Add(occupied + d.ToRotationEnum().ToVector2Int());
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
            for (int rot = 0; rot < directions; rot++)
            {
                if (!CanAttach(candidate, infoSo, rot))
                {
                    continue;
                }
                
                // prefer more connections
                int connectionCount = CountConnections(candidate, infoSo, rot);

                if (bestRot == -1
                    || dist < bestDist - 0.01f
                    || (Mathf.Abs(dist - bestDist) < 0.01f &&
                        connectionCount > bestConnectionCount))
                {
                    bestDist = dist;
                    bestCell = candidate;
                    bestRot = rot;
                    bestConnectionCount = connectionCount;
                }
            }
        }

        return new Result<(Vector2Int cell, int rotation)>(
            bestRot != -1,
            (bestCell, bestRot));
    }

    // private int DetachOrphans()
    // {
    //     // Get all tiles still connect to Core
    //     HashSet<Vector2Int> reachable = Flood(Core.Cell);
    //     
    //     // Remove all tiles still in Cells which not connect to Core
    //     List<Vector2Int> orphanKeys
    //         = Cells.Keys.Where(c => !reachable.Contains(c)).ToList();
    //     if (orphanKeys.Count == 0)
    //     {
    //         return 0;
    //     }
    //
    //     // Save the ship's motion so released tiles inherit it.
    //     Vector2 centerOfMass = Rb.worldCenterOfMass;
    //     Vector2 linearVelocity = Rb.linearVelocity;
    //     float angularVelocity = Rb.angularVelocity;
    //     Vector2 explosionCenter = Core.transform.position;
    //
    //     float minSpeed = Mathf.Max(0f,
    //         Mathf.Min(detachRadiateLinearVelocity.x,
    //             detachRadiateLinearVelocity.y));
    //     float maxSpeed = Mathf.Max(0f,
    //         Mathf.Max(detachRadiateLinearVelocity.x,
    //             detachRadiateLinearVelocity.y));
    //     float minSpin = Mathf.Min(detachRadiateAngularVelocity.x,
    //         detachRadiateAngularVelocity.y);
    //     float maxSpin = Mathf.Max(detachRadiateAngularVelocity.x,
    //         detachRadiateAngularVelocity.y);
    //
    //     // Release each orphan as a free-floating tile
    //     int orphanTiles = 0;
    //     foreach (Vector2Int c in orphanKeys)
    //     {
    //         if (!GridData.TryGetTile(c, out Tile tile))
    //         {
    //             continue;
    //         }
    //
    //         Vector2 worldPos = tile.transform.position;
    //
    //         // Inherit movement 
    //         Vector2 offset = worldPos - centerOfMass;
    //         Vector2 velocity = linearVelocity +
    //                            new Vector2(-offset.y, offset.x) *
    //                            (angularVelocity * Mathf.Deg2Rad);
    //         
    //         // Push away from Core
    //         Vector2 outward = worldPos - explosionCenter;
    //         if (outward.sqrMagnitude < 0.0001f)
    //         {
    //             float angle = Random.Range(0f, 2f * Mathf.PI);
    //             outward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    //         }
    //
    //         // Add an outward push and random spin to the inherited ship motion.
    //         float speed = Random.Range(minSpeed, maxSpeed);
    //         velocity += outward.normalized * speed;
    //         float spin = Random.Range(minSpin, maxSpin);
    //
    //         tile.SetDetached(this);
    //         GridData.RemoveTile(c);
    //         
    //         tile.ReleaseToFloating(velocity,
    //             angularVelocity + spin);
    //
    //         orphanTiles++;
    //     }
    //     
    //     return orphanTiles;
    // }

    // BFS
    // private HashSet<Vector2Int> Flood(Vector2Int start)
    // {
    //     Queue<Vector2Int> open = new();
    //     HashSet<Vector2Int> closed = new() { start, };
    //     open.Enqueue(start);
    //
    //     while (open.Count > 0)
    //     {
    //         Vector2Int c = open.Dequeue();
    //         Tile tile = Cells[c];
    //         for (int direction = 0; direction < 4; direction++)
    //         {
    //             Vector2Int n = c + Dirs[direction];
    //             if (closed.Contains(n) ||
    //                 !GridData.TryGetTile(n, out Tile cell))
    //             {
    //                 continue;
    //             }
    //
    //             if (!tile.EdgeConnectable(direction) ||
    //                 !cell.EdgeConnectable(Opposite(direction)))
    //             {
    //                 continue;
    //             }
    //
    //             closed.Add(n);
    //             open.Enqueue(n);
    //         }
    //     }
    //
    //     return closed;
    // }

    #endregion

    #region TESTING

#if UNITY_EDITOR
    // [SerializeField] private List<TileInfoSO> spawnTestTiles;
    // [ContextMenu("SpawnRandomFloater")]
    // private void SpawnRandomFloater()
    // {
    //     TileInfoSO info = spawnTestTiles[Random.Range(0, spawnTestTiles.Count)];
    //
    //     float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
    //     float dist = 6.5f;
    //     Vector2 shipPos = transform.position;
    //     Vector2 pos = shipPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
    //
    //     Vector2 toShip = (shipPos - pos).normalized;
    //     Vector2 velocity = toShip + Random.insideUnitCircle * 0.3f;
    //
    //     Tile.SpawnFloating<ChassisTile>(info, pos, velocity);
    // }
#endif

    #endregion
}