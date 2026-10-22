using System.Collections.Generic;
using Pulsar.Ship.Ship.Extensions;
using UnityEngine;

public class ShipGridData
{
    private readonly Dictionary<Vector2Int, Tile> _cells = new();

    public IReadOnlyDictionary<Vector2Int, Tile> Cells => _cells;

    public CoreTile Core { get; private set; }

    public bool ContainsCell(Vector2Int cell) => _cells.ContainsKey(cell);

    public bool TryGetTile(Vector2Int cell, out Tile tile) =>
        _cells.TryGetValue(cell, out tile);

    public void SetCore(CoreTile tile)
    {
        Core = tile;
    }

    public void SetTile(Tile tile)
    {
        Vector2Int index = tile.transform.position.ToVector2Int();
        _cells[index] = tile;
    }

    public void SetTile(Vector2Int cell, Tile tile)
    {
        if (!_cells.TryAdd(cell, tile))
        {
            Debug.LogWarning(
                $"Trying to set {cell}, but already contains a tile!");
            return;
        }

        tile.SetAttached(true);
    }

    public bool RemoveTile(Vector2Int cell)
    {
        if (TryGetTile(cell, out Tile tile))
        {
            tile.SetAttached(true);
        }

        return _cells.Remove(cell);
    }

    /// <summary>
    /// Gets the closest tile to the projectiles hit point on the ship
    /// </summary>
    /// <param name="hitPoint"></param>
    /// <returns></returns>
    public Tile GetClosestTile(
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
}