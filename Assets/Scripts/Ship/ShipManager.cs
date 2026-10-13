using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar.Ship;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CompositeCollider2D))]
public class ShipManager : MonoBehaviour
{
    public static readonly Vector2Int[] Dirs =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left,
    };

    [Header("Refs")]
    [SerializeField]
    private TileInfoSO coreInfoSo;
    [SerializeField]
    private CompositeCollider2D compositeCollider;
    [field: SerializeField]
    public Rigidbody2D Rb { get; private set; }
    [field: SerializeField]
    public PlayerController PlayerController { get; private set; }
    [SerializeField]
    private List<WeaponTile> weapons = new(10);
    [SerializeField]
    private List<ThrusterTile> thrusters = new(10);

    [Header("Detach Explosion")]
    [SerializeField]
    private Vector2 detachRadiateLinearVelocity = new(2f, 5f);

    [SerializeField]
    private Vector2 detachRadiateAngularVelocity = new(-180f, 180f);

    public Tile Core => GridData.Core;


    public ShipGridData GridData { get; } = new();

    public IReadOnlyDictionary<Vector2Int, Tile> Cells => GridData.Cells;

    public event Action OnTileAdded;
    public event Action<int> OnTilesRemoved;
    public event Action OnShipDestroyed;
    // [CB] Commenting out so Unity doesn't show warnings. We can uncomment as we need them.
    // public event Action<Tile> onNewTileAdded;
    // public event Action<Weapon> onNewWeaponAdded;
    // public event Action<Thruster> onNewThrusterAdded;
    
    
    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();

        ShipUtilities.FloatingTileRbConstrain(Rb);

        if (coreInfoSo != null)
        {
            SpawnCore();
        }
    }

    private void Start()
    {
        weapons = Cells.Values.OfType<WeaponTile>().ToList();
        thrusters = Cells.Values.OfType<ThrusterTile>().ToList();
        UpdateWeaponColliders();
    }

    public void Fire()
    {
        foreach (WeaponTile weapon in weapons)
        {
            weapon.Fire();
        }
    }

    public void Move(Vector2 position)
    {
        // TODO: Add logic to figure out which thrusters to fire, and by how much
        foreach (ThrusterTile thruster in thrusters)
        {
            //! [CB] PLACEHOLDER LOGIC
            thruster.Move(position.x, Rb);
        }
    }

    public void Rotate(float rotateForce)
    {
        //TODO: Add logic for rotating
        Debug.Log("Manager moving");
    }

    private void UpdateWeaponColliders()
    {
        foreach (WeaponTile weapon in weapons)
        {
            weapon.SetParentCollider(compositeCollider);
        }
    }

    private static int Opposite(int d) => (d + 2) % 4;

    private void SpawnCore()
    {
        Tile tile = Tile.CreateTile<CoreTile>(coreInfoSo, Vector2Int.zero);
        tile.gameObject.name = "Core";
        tile.AttachTo(this, Vector2Int.zero, 0);
        GridData.SetCore(Vector2Int.zero, tile);
        tile.SetAttached(this);
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
        return new Bounds(ShipUtilities.GridToLocal((min + max) * 0.5f),
            new Vector3(size.x, size.y, 0f));
    }

    public Vector3 CellToWorld(Vector2Int cell) =>
        transform.TransformPoint(ShipUtilities.GridToLocal(cell));

    public Tile GetTileAtWorldPos(Vector3 worldPos)
    {
        Vector2 local
            = ShipUtilities.LocalToGrid(
                transform.InverseTransformPoint(worldPos));
        Vector2Int cell = Vector2Int.RoundToInt(local);
        GridData.TryGetTile(cell, out Tile tile);
        return tile;
    }

    #region ATTACH-DETACH

    public bool CanAttach(Vector2Int pos, TileInfoSO infoSo, int rotation)
    {
        // Cannot attach to an occupied cell
        if (GridData.ContainsCell(pos))
        {
            return false;
        }

        for (int d = 0; d < 4; d++)
        {
            // Get the tile edge facing this direction after rotation
            int localDir = (d + rotation) % 4;
            if (!infoSo.connectableEdges[localDir])
            {
                continue;
            }

            // Check the neighbor's opposite edge; both touching edges must connect
            Vector2Int neighborPos = pos + Dirs[d];
            if (GridData.TryGetTile(neighborPos, out Tile neighbor)
                && neighbor.EdgeConnectable(Opposite(d)))
            {
                return true;
            }
        }

        return false;
    }

    public int CountConnections(Vector2Int pos, TileInfoSO infoSo, int rotation)
    {
        // Count all neighbors that can connect at this position and rotation
        int count = 0;
        for (int d = 0; d < 4; d++)
        {
            // Get the tile edge after rotation
            int localDir = (d + rotation) % 4;
            if (!infoSo.connectableEdges[localDir])
            {
                continue;
            }

            // Count only when both touching edges can connect
            Vector2Int neighborPos = pos + Dirs[d];
            if (GridData.TryGetTile(neighborPos, out Tile neighbor)
                && neighbor.EdgeConnectable(Opposite(d)))
            {
                count++;
            }
        }

        return count;
    }

    public bool Attach(Vector2Int pos, Tile tile, int rotation)
    {
        if (tile == null || tile.IsAttached || tile.TileInfo == null)
        {
            return false;
        }

        if (!CanAttach(pos, tile.TileInfo, rotation))
        {
            return false;
        }

        tile.AttachTo(this, pos, rotation);
        GridData.SetTile(pos, tile);
        tile.SetAttached(this);
        OnTileAdded?.Invoke();
        return true;
    }

    public void DestroyTileAt(Vector2Int pos)
    {
        if (!GridData.TryGetTile(pos, out Tile tile))
        {
            return;
        }

        if (tile == Core)
        {
            OnCoreDestroyed();
            return;
        }

        GridData.RemoveTile(pos);
        tile.SetDetached(this);
        Destroy(tile.gameObject);
        int orphanTilesNum = DetachOrphans();
        OnTilesRemoved?.Invoke(orphanTilesNum + 1);
    }

    private void OnCoreDestroyed()
    {
        OnShipDestroyed?.Invoke();
    }

    public (Vector2Int cell, int rotation)? FindBestAttachment(
        TileInfoSO infoSo,
        Vector3 worldPos)
    {
        // Convert world position to ship grid position
        Vector2 localPos
            = ShipUtilities.LocalToGrid(
                transform.InverseTransformPoint(worldPos));

        float bestDist = float.MaxValue;
        int bestRot = -1; 
        int bestConnectionCount = 0;
        Vector2Int bestCell = default;

        HashSet<Vector2Int> candidates = new();
        foreach (Vector2Int occupied in Cells.Keys)
        {
            for (int d = 0; d < 4; d++)
            {
                candidates.Add(occupied + Dirs[d]);
            }
        }

        float nearestDistance = float.MaxValue;

            foreach (Vector2Int candidate in candidates)
            {
                if (!GridData.ContainsCell(candidate))
                    nearestDistance = Mathf.Min(nearestDistance, Vector2.Distance(localPos, candidate));
            }

        foreach (Vector2Int candidate in candidates)
        {
            // Skip occupied cells
            if (GridData.ContainsCell(candidate))
            {
                continue;
            }

            float dist = Vector2.Distance(localPos, candidate);
            if (dist > nearestDistance + 0.01f) continue;

            // Try all 4 rotations
            for (int rot = 0; rot < 4; rot++)
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

        if (bestRot == -1)
        {
            // No cell and rotation could connect
            return null;
        }

        return (bestCell, bestRot);
    }

    private int DetachOrphans()
    {
        // Get all tiles still connect to Core
        HashSet<Vector2Int> reachable = Flood(Core.Cell);
        
        // Remove all tiles still in Cells which not connect to Core
        List<Vector2Int> orphanKeys
            = Cells.Keys.Where(c => !reachable.Contains(c)).ToList();
        if (orphanKeys.Count == 0)
        {
            return 0;
        }

        // Save the ship's motion so released tiles inherit it.
        Vector2 centerOfMass = Rb.worldCenterOfMass;
        Vector2 linearVelocity = Rb.linearVelocity;
        float angularVelocity = Rb.angularVelocity;
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

        // Release each orphan as a free-floating tile
        int orphanTiles = 0;
        foreach (Vector2Int c in orphanKeys)
        {
            if (!GridData.TryGetTile(c, out Tile tile))
            {
                continue;
            }

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

            tile.SetDetached(this);
            GridData.RemoveTile(c);
            
            tile.ReleaseToFloating(velocity,
                angularVelocity + spin);

            orphanTiles++;
        }
        
        return orphanTiles;
    }

    
    // BFS
    private HashSet<Vector2Int> Flood(Vector2Int start)
    {
        Queue<Vector2Int> open = new();
        HashSet<Vector2Int> closed = new() { start, };
        open.Enqueue(start);

        while (open.Count > 0)
        {
            Vector2Int c = open.Dequeue();
            Tile tile = Cells[c];
            for (int direction = 0; direction < 4; direction++)
            {
                Vector2Int n = c + Dirs[direction];
                if (closed.Contains(n) || !GridData.TryGetTile(n, out Tile cell))
                {
                    continue;
                }

                if (!tile.EdgeConnectable(direction) ||
                    !cell.EdgeConnectable(Opposite(direction)))
                {
                    continue;
                }

                closed.Add(n);
                open.Enqueue(n);
            }
        }

        return closed;
    }

    #endregion


    #region TESTING

#if UNITY_EDITOR
    [SerializeField] private List<TileInfoSO> spawnTestTiles;
    [ContextMenu("SpawnRandomFloater")]
    private void SpawnRandomFloater()
    {
        TileInfoSO infor = spawnTestTiles[Random.Range(0, spawnTestTiles.Count)];

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = 6.5f;
        Vector2 shipPos = transform.position;
        Vector2 pos = shipPos + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

        Vector2 toShip = (shipPos - pos).normalized;
        Vector2 velocity = toShip + Random.insideUnitCircle * 0.3f;

        Tile.SpawnFloating<ChassisTile>(infor, pos, velocity);
    }
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 30,
            alignment = TextAnchor.UpperLeft,
            richText = true
        };

        string info =
            "<b>BUILD & DESTROY TEST</b>\n\n" +
            "<b>Tab</b>        — Toggle scan zone\n" +
            "<b>Click</b>      — Select a tile\n" +
            "<b>Hold C</b>     — Rotate selected tile \n" +
            "<b>Release C</b>  — Attach selected tile \n" +
            "<b>X</b>          — Destroy ship tile\n\n";

        GUI.Box(new Rect(10, 10, 540, 300), info, style);
    }
#endif
    

    #endregion
}
