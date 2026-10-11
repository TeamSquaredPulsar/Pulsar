using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Pulsar.Ship
{
public class ShipGridData
{
    private readonly Dictionary<Vector2Int, Tile> _cells = new();

    public IReadOnlyDictionary<Vector2Int, Tile> Cells { get; }
    public Tile Core { get; private set; }

    public ShipGridData()
    {
        Cells = new ReadOnlyDictionary<Vector2Int, Tile>(_cells);
    }

    public bool ContainsCell(Vector2Int cell) => _cells.ContainsKey(cell);

    public bool TryGetTile(Vector2Int cell, out Tile tile) =>
        _cells.TryGetValue(cell, out tile);

    internal void SetCore(Vector2Int cell, Tile tile)
    {
        SetTile(cell, tile);
        Core = tile;
    }

    internal void SetTile(Vector2Int cell, Tile tile) => _cells[cell] = tile;

    internal bool RemoveTile(Vector2Int cell) => _cells.Remove(cell);
}
}
