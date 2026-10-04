using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Pulsar.Ship
{
    [RequireComponent(typeof(Rigidbody))]
    public class ShipGrid : MonoBehaviour
    {
        public static readonly Vector2Int[] Dirs =
        {
            Vector2Int.up,    
            Vector2Int.right, 
            Vector2Int.down,  
            Vector2Int.left  
        };

        public static int Opposite(int d) => (d + 2) % 4;

        [SerializeField] private TileInfoSO coreInfoSo;

        [Header("Detach Explosion")]
        [SerializeField] private Vector2 detachRadiateLinearVelocity = new(2f, 5f);
        [SerializeField] private Vector2 detachRadiateAngularVelocity = new(-180f, 180f);

        private readonly Dictionary<Vector2Int, Tile> _cells = new();
        private Tile _core;
        private Rigidbody _rb;

        public IReadOnlyDictionary<Vector2Int, Tile> Cells => _cells;
        public Tile Core => _core;
        public Rigidbody Rb => _rb;

        public UnityEvent OnShipChanged;
        public UnityEvent OnShipDestroyed;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody>();

        ShipUtilities.Constrain(Rb);

        if (coreInfoSo != null)
        {
            SpawnCore();
        }
    }

    public static int Opposite(int d) => (d + 2) % 4;

        private void SpawnCore()
        {
            Tile tile = Tile.CreateTile(coreInfoSo, Vector2Int.zero, 0);
            tile.gameObject.name = "Core";
            tile.AttachTo(this, Vector2Int.zero, 0);
            _cells[Vector2Int.zero] = tile;
            _core = tile;
            tile.SetAttached(this);
        }

        #region ATTACH-DETACH

        public bool CanAttach(Vector2Int pos, TileInfoSO infoSo, int rotation)
        {
            if (_cells.ContainsKey(pos)) return false;

            for (int d = 0; d < 4; d++)
            {
                int localDir = (d + rotation) % 4;
                if (!infoSo.connectableEdges[localDir]) continue;

                Vector2Int neighborPos = pos + Dirs[d];
                if (_cells.TryGetValue(neighborPos, out Tile neighbor)
                    && neighbor.EdgeConnectable(Opposite(d)))
                    return true;
            }
            return false;
        }
        
        public int CountConnections(Vector2Int pos, TileInfoSO infoSo, int rotation)
        {
            int count = 0;
            for (int d = 0; d < 4; d++)
            {
                int localDir = (d + rotation) % 4;
                if (!infoSo.connectableEdges[localDir]) continue;

                Vector2Int neighborPos = pos + Dirs[d];
                if (_cells.TryGetValue(neighborPos, out Tile neighbor)
                    && neighbor.EdgeConnectable(Opposite(d)))
                    count++;
            }
            return count;
        }

        public bool Attach(Vector2Int pos, Tile tile, int rotation)
        {
            if (tile == null || tile.IsAttached || tile.TileInfo == null) return false;
            if (!CanAttach(pos, tile.TileInfo, rotation)) return false;
            tile.AttachTo(this, pos, rotation);
            _cells[pos] = tile;
            tile.SetAttached(this);
            OnShipChanged?.Invoke();
            return true;
        }

        public void DestroyTile(Vector2Int pos)
        {
            if (!_cells.TryGetValue(pos, out Tile tile)) return;
            if (tile == _core) { OnCoreDestroyed(); return; }

            _cells.Remove(pos);
            tile.SetDetached(this);
            Destroy(tile.gameObject);
            DetachOrphans();
            OnShipChanged?.Invoke();
        }

        private void OnCoreDestroyed()
        {
            OnShipDestroyed?.Invoke();
        }
        
        public (Vector2Int cell, int rotation)? FindBestAttachment(TileInfoSO infoSo, Vector3 worldPos)
        {
            Vector2 localPos = ShipUtilities.LocalToGrid(transform.InverseTransformPoint(worldPos));

            float bestDist = float.MaxValue;
            int bestRot = -1;
            int bestConns = 0;
            Vector2Int bestCell = default;

            // gather all candidate empty cells adjacent to existing tiles
            HashSet<Vector2Int> candidates = new();
            foreach (Vector2Int occupied in _cells.Keys)
                for (int d = 0; d < 4; d++)
                    candidates.Add(occupied + Dirs[d]);

            foreach (Vector2Int candidate in candidates)
            {
                if (_cells.ContainsKey(candidate)) continue;

                float dist = Vector2.Distance(localPos, (Vector2)candidate);

                for (int rot = 0; rot < 4; rot++)
                {
                    if (!CanAttach(candidate, infoSo, rot)) continue;

                    int conns = CountConnections(candidate, infoSo, rot);

                    if (bestRot == -1
                        || dist < bestDist - 0.01f
                        || (Mathf.Abs(dist - bestDist) < 0.01f && conns > bestConns))
                    {
                        bestDist = dist;
                        bestCell = candidate;
                        bestRot = rot;
                        bestConns = conns;
                    }
                }
            }

            if (bestRot == -1) return null;
            return (bestCell, bestRot);
        }



        private void DetachOrphans()
        {
            HashSet<Vector2Int> reachable = Flood(_core.Cell);
            List<Vector2Int> orphanKeys = _cells.Keys.Where(c => !reachable.Contains(c)).ToList();
            if (orphanKeys.Count == 0) return;


            Vector3 centerOfMass = _rb.worldCenterOfMass;
            Vector3 linearVelocity = _rb.linearVelocity;
            Vector3 angularVelocity = _rb.angularVelocity;
            Vector3 explosionCenter = _core.transform.position;
            float minSpeed = Mathf.Max(0f, Mathf.Min(detachRadiateLinearVelocity.x, detachRadiateLinearVelocity.y));
            float maxSpeed = Mathf.Max(0f, Mathf.Max(detachRadiateLinearVelocity.x, detachRadiateLinearVelocity.y));
            float minSpin = Mathf.Min(detachRadiateAngularVelocity.x, detachRadiateAngularVelocity.y) * Mathf.Deg2Rad;
            float maxSpin = Mathf.Max(detachRadiateAngularVelocity.x, detachRadiateAngularVelocity.y) * Mathf.Deg2Rad;

            // Release each orphan as a free-floating tile
            foreach (Vector2Int c in orphanKeys)
            {
                if (!_cells.TryGetValue(c, out Tile tile)) continue;

                Vector3 worldPos = tile.transform.position;

                Vector3 velocity = linearVelocity + Vector3.Cross(angularVelocity, worldPos - centerOfMass);
                Vector3 outward = worldPos - explosionCenter;
                outward.y = 0f;
                if (outward.sqrMagnitude < 0.0001f)
                {
                    float angle = Random.Range(0f, 2f * Mathf.PI);
                    outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                }
                float speed = Random.Range(minSpeed, maxSpeed);
                velocity += outward.normalized * speed;
                float spin = Random.Range(minSpin, maxSpin);

                tile.SetDetached(this);
                _cells.Remove(c);
                
                tile.ReleaseToFloating(velocity, angularVelocity + Vector3.up * spin);
            }
        }

        private HashSet<Vector2Int> Flood(Vector2Int start)
        {
            Queue<Vector2Int> open = new Queue<Vector2Int>();
            HashSet<Vector2Int> closed = new HashSet<Vector2Int> { start };
            open.Enqueue(start);

            while (open.Count > 0)
            {
                Vector2Int c = open.Dequeue();
                Tile tile = _cells[c];
                for (int d = 0; d < 4; d++)
                {
                    Vector2Int n = c + Dirs[d];
                    if (closed.Contains(n) || !_cells.ContainsKey(n)) continue;
                    if (!tile.EdgeConnectable(d) || !_cells[n].EdgeConnectable(Opposite(d))) continue;
                    closed.Add(n);
                    open.Enqueue(n);
                }
            }
            return closed;
        }
        #endregion


        public Bounds GetLocalBounds()
        {
            if (_cells.Count == 0) return new Bounds(Vector3.zero, new Vector3(1f, ShipUtilities.TileColliderHeight, 1f));

            Vector2 min = new(float.MaxValue, float.MaxValue);
            Vector2 max = new(float.MinValue, float.MinValue);
            foreach (Vector2Int c in _cells.Keys)
            {
                min = Vector2.Min(min, c);
                max = Vector2.Max(max, c);
            }
            Vector2 size = max - min + Vector2.one;
            return new Bounds(ShipUtilities.GridToLocal((min + max) * 0.5f),
                new Vector3(size.x, ShipUtilities.TileColliderHeight, size.y));
        }

        public Vector3 CellToWorld(Vector2Int cell) => transform.TransformPoint(ShipUtilities.GridToLocal(cell));

        public Tile GetTileAtWorldPos(Vector3 worldPos)
        {
            Vector2 local = ShipUtilities.LocalToGrid(transform.InverseTransformPoint(worldPos));
            Vector2Int cell = Vector2Int.RoundToInt(local);
            _cells.TryGetValue(cell, out Tile tile);
            return tile;
        }
    }
}
