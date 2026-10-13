using System.Collections.Generic;
using System.Collections.ObjectModel;
using Pulsar.Ship;
using UnityEngine;

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

    public void SetCore(Vector2Int cell, Tile tile)
    {
        SetTile(cell, tile);
        Core = tile;
    }

    public void SetTile(Vector2Int cell, Tile tile) => _cells[cell] = tile;

    public bool RemoveTile(Vector2Int cell) => _cells.Remove(cell);
}