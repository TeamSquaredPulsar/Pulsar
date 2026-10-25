using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar.Ship.Extensions;
using Pulsar.Utilities;
using UnityEngine;

namespace Pulsar.Ship.Data
{
[Serializable]
public class ShipGridData
{
    public event Action<Tile> TileOrphanedEvent;

    private readonly Dictionary<Vector2Int, Tile> _cells = new();

    public IReadOnlyDictionary<Vector2Int, Tile> Cells => _cells;

    public IReadOnlyList<Tile> Tiles => _cells.Values.ToList();

    public CoreTile Core { get; private set; }

    public bool ContainsCell(Vector2Int cell) => _cells.ContainsKey(cell);

    public bool TryGetTile(Vector2Int cell, out Tile tile) =>
        _cells.TryGetValue(cell, out tile);

    public bool TrySetTile(Vector2Int cell, Tile tile)
    {
        if (tile is CoreTile coreTile)
        {
            Core = coreTile;
        }

        return _cells.TryAdd(cell, tile);
    }

    public Result<Tile> RemoveTile(Vector2Int cell)
    {
        if (!TryGetTile(cell, out Tile tile) || !_cells.Remove(cell))
        {
            return Result<Tile>.Fail();
        }

        DetachOrphans();
        return Result<Tile>.Pass(tile);
    }

    /// <summary>
    /// Gets the closest tile to the projectiles hit point on the ship
    /// </summary>
    /// <param name="hitPoint"></param>
    /// <returns></returns>
    private Tile GetClosestTile(
        Vector2 hitPoint)
    {
        Tile closestTile = null;
        float bestDistanceSquared = float.PositiveInfinity;
        float bestCenterDistanceSquared = float.PositiveInfinity;

        //? I feel like there must be a more efficient search method;
        //? like starting at the index of the hit point and searching
        //? only adjacent neighbours.
        foreach (Tile tile in _cells.Values)
        {
            Collider2D tileCollider = tile.GetComponent<Collider2D>();

            Vector2 closestPoint = tileCollider.ClosestPoint(hitPoint);
            float distanceSquared = (closestPoint - hitPoint).sqrMagnitude;
            float centerDistanceSquared =
                ((Vector2)tile.transform.position - hitPoint).sqrMagnitude;

            // Prefer the closest collider,
            // and if two tiles tie at a shared edge then
            // prefer the tile whose centre is closest to the hit point
            if (distanceSquared < bestDistanceSquared
                || (Mathf.Approximately(distanceSquared, bestDistanceSquared)
                    && centerDistanceSquared < bestCenterDistanceSquared)
               )
            {
                closestTile = tile;
                bestDistanceSquared = distanceSquared;
                bestCenterDistanceSquared = centerDistanceSquared;
            }
        }

        return closestTile;
    }

    public bool TryGetClosestTile(
        Vector2 hitPoint,
        out Tile closestTile)
    {
        closestTile = GetClosestTile(hitPoint);
        return closestTile != null;
    }

    private void DetachOrphans()
    {
        // Get all tiles still connect to Core
        HashSet<Vector2Int> reachable = Flood(Core.Cell);

        // Remove all tiles still in Cells which not connect to Core
        Cells.Keys.Where(cell => !reachable.Contains(cell))
            .ToList()
            .ForEach(cell =>
            {
                if (TryGetTile(cell, out Tile tile))
                {
                    TileOrphanedEvent?.Invoke(tile);
                }
            });
    }

    //BFS
    private HashSet<Vector2Int> Flood(Vector2Int start)
    {
        Queue<Vector2Int> open = new();
        HashSet<Vector2Int> closed = new() { start, };
        open.Enqueue(start);

        while (open.Count > 0)
        {
            Vector2Int cellIndex = open.Dequeue();
            Tile tile = Cells[cellIndex];

            foreach (TileRotation direction in Enum.GetValues(
                         typeof(TileRotation)))
            {
                Vector2Int nextIndex = cellIndex + direction.ToVector2Int();

                // If we already have the next index, no need to search
                if (closed.Contains(nextIndex))
                {
                    continue;
                }

                // If the next index isn't a tile, continue
                if (!TryGetTile(nextIndex, out Tile cell))
                {
                    continue;
                }

                if (!tile.EdgeConnectable(direction) ||
                    !cell.EdgeConnectable(direction.Opposite()))
                {
                    continue;
                }

                closed.Add(nextIndex);
                open.Enqueue(nextIndex);
            }
        }

        return closed;
    }
}
}