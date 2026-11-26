using System;
using Pulsar.DataClasses;
using Pulsar.Player;
using Pulsar.Settings.Input;
using Pulsar.Utilities;
using UnityEngine;

namespace Pulsar.UI
{
[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class PlayerCursor : MonoBehaviour
{
    public static Vector3 CursorPosition = Vector3.zero;

    [SerializeField]
    private InputReader input;

    [SerializeField]
    private SpriteRenderer visual;

    [SerializeField]
    private Collider2D circleCollider;

    private Tile _hoveredLooseTile;

    private PlayerController _player;
    private Tile _selectedTile;

    public InputReader Input => input;

    private void Awake()
    {
        PlayerController.PlayerSpawnedEvent += OnPlayerSpawned;
        input.MoveCursorEvent += OnCursorMove;
        input.SelectTileEvent += OnTileSelect;
    }

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;

        Vector2 initPos = CameraData.ScreenSize / 2;
        OnCursorMove(initPos);

        input.EnablePlayerActions();
    }

    private void Update()
    {
        CursorPosition = transform.position;
    }

    private void OnDestroy()
    {
        PlayerController.PlayerSpawnedEvent -= OnPlayerSpawned;
        input.SelectTileEvent -= OnTileSelect;
        input.MoveCursorEvent -= OnCursorMove;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Tile tile) && !tile.IsAttached)
        {
            SetHoveredLooseTile(tile);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out Tile tile) && tile == _hoveredLooseTile)
        {
            SetHoveredLooseTile(null);
        }
    }

    public static event Action<Result<Tile>> PlayerSelectTile;
    public static event Action<Tile> LooseTileHoverChanged;

    private void OnTileSelect(bool isSelecting)
    {
        Result<Tile> result
            = isSelecting ? GetTileAtCursor() : Result<Tile>.Fail();
        if (!result.IsSuccess)
        {
            _selectedTile = null;
            PlayerSelectTile?.Invoke(Result<Tile>.Fail());
            return;
        }

        Tile tile = result.Value;
        if (tile == _selectedTile)
        {
            return;
        }

        _selectedTile = tile;

        PlayerSelectTile?.Invoke(Result<Tile>.Pass(_selectedTile));
    }

    private void OnPlayerSpawned(PlayerController player) => _player = player;

    private Result<Tile> GetTileAtCursor()
    {
        if (_player)
        {
            // The composite collider spans the ship, so resolve the cell at press time.
            Vector3 localPosition
                = _player.transform.InverseTransformPoint(transform.position);
            Vector2Int cell = ShipUtilities.LocalToGrid(localPosition)
                .ToVector2Int();
            if (_player.TryGetTile(cell, out Tile tile))
            {
                return tile ? Result<Tile>.Pass(tile) : Result<Tile>.Fail();
            }
        }

        return _hoveredLooseTile && !_hoveredLooseTile.IsAttached
            ? Result<Tile>.Pass(_hoveredLooseTile)
            : Result<Tile>.Fail();
    }

    private void SetHoveredLooseTile(Tile tile)
    {
        // This is basically to check if the same tile was hovered over,
        if (tile == _hoveredLooseTile)
        {
            return;
        }

        _hoveredLooseTile = tile;
        LooseTileHoverChanged?.Invoke(tile);
    }

    private void OnCursorMove(Vector2 obj)
    {
        if (!CameraData.MainCamera)
        {
            // Debug.LogWarning("No MainCamera");
            return;
        }

        Vector3 worldPos = CameraData.MainCamera.ScreenToWorldPoint(obj);

        worldPos.z = 0f;

        transform.position = worldPos;
    }
}
}
