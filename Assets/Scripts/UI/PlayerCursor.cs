using System;
using Pulsar.DataClasses;
using Pulsar.Player;
using Pulsar.Utilities;
using UnityEngine;

namespace Pulsar.UI
{
[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class PlayerCursor : MonoBehaviour
{
    public static event Action<Result<Tile>> PlayerSelectTile;
    
    public static Vector3 CursorPosition = Vector3.zero;
    
    [SerializeField]
    private InputReader input;

    public InputReader Input => input;

    [SerializeField]
    private SpriteRenderer visual;

    [SerializeField]
    private Collider2D circleCollider;

    [SerializeField]
    private CameraData cameraData;

    private PlayerController _player;
    private Tile _hoveredLooseTile;
    private Tile _selectedTile;

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
        
        Vector2 initPos = cameraData.ScreenSize / 2;
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

    private void OnTileSelect(bool isSelecting)
    {
        Tile tile = isSelecting ? GetTileAtCursor() : null;
        if (!tile)
        {
            _selectedTile = null;
            PlayerSelectTile?.Invoke(Result<Tile>.Fail());
            return;
        }

        //Don't raise more events than we need to
        if (tile == _selectedTile)
        {
            return;
        }

        _selectedTile = tile;

        PlayerSelectTile?.Invoke(Result<Tile>.Pass(_selectedTile));
    }

    private void OnPlayerSpawned(PlayerController player) => _player = player;

    private Tile GetTileAtCursor()
    {
        if (_player)
        {
            // The composite collider spans the ship, so resolve the cell at press time.
            Vector3 localPosition = _player.transform.InverseTransformPoint(transform.position);
            Vector2Int cell = ShipUtilities.LocalToGrid(localPosition).ToVector2Int();
            if (_player.TryGetTile(cell, out Tile tile))
            {
                return tile;
            }
        }

        return _hoveredLooseTile && !_hoveredLooseTile.IsAttached ? _hoveredLooseTile : null;
    }

    private void OnCursorMove(Vector2 obj)
    {
        Vector3 worldPos = cameraData.MainCamera.ScreenToWorldPoint(obj);

        worldPos.z = 0f;

        transform.position = worldPos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Tile tile) && !tile.IsAttached)
        {
            _hoveredLooseTile = tile;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out Tile tile) && tile == _hoveredLooseTile)
        {
            _hoveredLooseTile = null;
        }
    }
}
}
