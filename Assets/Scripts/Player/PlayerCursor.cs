using System;
using Pulsar.Ship;
using Pulsar.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pulsar.Player
{
[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class PlayerCursor : MonoBehaviour
{
    public static event Action<Result<Tile>> SelectTileEvent;
    
    [SerializeField]
    private InputReader input;

    [SerializeField]
    private SpriteRenderer visual;

    [SerializeField]
    private Collider2D circleCollider;

    private Tile _hoveredTile;
    private Tile _selectedTile;

    private Camera _mainCamera;
    private Vector2 _screenPosition;

    public Vector3 WorldPosition
    {
        get
        {
            UpdateWorldPosition();
            return transform.position;
        }
    }

    private void Awake()
    {
        _mainCamera = Camera.main;
        input.MoveCursorEvent += OnCursorMove;
        input.SelectTileEvent += OnTileSelect;
        // [CB] TODO: We need to setup callbacks for when the camera changes
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;

        //Correct mouse position if the cursor isn't detected 
        Vector2 initPos = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;

        OnCursorMove(initPos);
    }

    private void OnDestroy()
    {
        input.SelectTileEvent -= OnTileSelect;
        input.MoveCursorEvent -= OnCursorMove;
    }

    private void OnTileSelect(bool isSelecting)
    {
        if (!isSelecting)
        {
            _selectedTile = null;
            SelectTileEvent?.Invoke(
                new Result<Tile>(false, null));
            return;
        }

        // The tile's collider is disabled while held. Losing hover or receiving
        // another performed callback must not release or replace the selection.
        if (_selectedTile || !_hoveredTile)
        {
            return;
        }

        _selectedTile = _hoveredTile;
        SelectTileEvent?.Invoke(new Result<Tile>(true, _selectedTile));
    }

    private void OnCursorMove(Vector2 obj)
    {
        _screenPosition = obj;
        UpdateWorldPosition();
    }

    private void LateUpdate() => UpdateWorldPosition();

    private void UpdateWorldPosition()
    {
        if (!_mainCamera)
        {
            _mainCamera = Camera.main;
        }

        if (!_mainCamera)
        {
            return;
        }

        // Reproject even when the mouse is stationary: the camera can move.
        Ray ray = _mainCamera.ScreenPointToRay(_screenPosition);
        Plane plane = new(Vector3.forward, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
        {
            return;
        }

        Vector3 worldPos = ray.GetPoint(distance);

        worldPos.z = 0f;

        transform.position = worldPos;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!TryProcessColliderTrigger(other, out Tile collidedTile))
        {
            return;
        }

        _hoveredTile = collidedTile;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!TryProcessColliderTrigger(other, out Tile collidedTile))
        {
            return;
        }

        _hoveredTile = collidedTile;
    }

    private bool TryProcessColliderTrigger(
        Collider2D other,
        out Tile collidedTile)
    {
        collidedTile = null;
        if (other.TryGetComponent(out Tile tile))
        {
            collidedTile = tile;
        }
        //Ignore EnemyAI ships
        else if (other.TryGetComponent(out PlayerController player))
        {
            ShipManager ship = player.Ship;

            // [CB] Because the collision point is on the edge of a tile, we need
            // to subtract a small amount.
            // This gets us close enough to the centre of the tile for the
            // rounding to work and get the correct index for the ship tile.
            const float tileOffset = 0.25f;

            // Get collision point relative to ship core
            Vector2 collisionPosition
                = ship.transform.InverseTransformPoint(
                    other.ClosestPoint(transform.position));

            //Modify collision point to attempt to get tile position
            switch (collisionPosition.x)
            {
                case < 0:
                    collisionPosition.x += tileOffset;
                    break;
                case > 0:
                    collisionPosition.x -= tileOffset;
                    break;
            }

            switch (collisionPosition.y)
            {
                case < 0:
                    collisionPosition.y += tileOffset;
                    break;
                case > 0:
                    collisionPosition.y -= tileOffset;
                    break;
            }

            Vector2Int gridCell = Vector2Int.RoundToInt(collisionPosition);

            ship.Cells.TryGetValue(gridCell, out Tile attachedTile);
            collidedTile = attachedTile;
        }

        return collidedTile;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // If the tile we exited from is not the tile we're currently tracking,
        // we don't care.
        if (!TryProcessColliderTrigger(other, out Tile collidedTile) ||
            collidedTile != _hoveredTile)
        {
            return;
        }

        _hoveredTile = null;
    }
}
}
