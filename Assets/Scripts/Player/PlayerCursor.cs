using JetBrains.Annotations;
using Nrjwolf.Tools.AttachAttributes;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerCursor : MonoBehaviour
{
    [SerializeField]
    private InputReader input;

    [GetComponent][SerializeField]
    private SpriteRenderer visual;

    private Tile _hoveredTile;
    private Camera _mainCamera;
    private float _worldUnitsPerPixel;

    private void Awake()
    {
        input.MoveCursorEvent += OnCursorMove;
        _mainCamera = Camera.main;
        // [CB] TODO: We need to setup callbacks for when the camera changes
        // zoom so we can re-run this function to ensure the logic still works
        SetWorldUnitsPerPixel();
    }

    private void Start()
    {
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        input.MoveCursorEvent -= OnCursorMove;
    }

    private void OnCursorMove(Vector2 obj)
    {
        transform.position = obj * _worldUnitsPerPixel;
    }

    private void SetWorldUnitsPerPixel()
    {
        //OrthographicSize is 1/2 camera height
        float worldHeight = 2f * _mainCamera.orthographicSize;

        // [CB] This worked when the game is in full screen.
        // _worldUnitsPerPixel = worldHeight / Screen.height;
        _worldUnitsPerPixel = worldHeight / _mainCamera.pixelHeight;
    }

    [CanBeNull]
    public Tile RefreshHoveredTile() => _hoveredTile ? _hoveredTile : null;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (TryProcessColliderTrigger(other, out Tile collidedTile))
        {
            _hoveredTile = collidedTile;
        }
    }

    private void Update()
    {
        if(_hoveredTile)
        {
            Debug.Log($"Hovering over {_hoveredTile.name}");
        }
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
        else if (other.TryGetComponent(out ShipManager ship))
        {
            Vector3 localPosition
                = ship.transform.InverseTransformPoint(transform.position);
            Vector2 gridPosition = ShipUtilities.LocalToGrid(localPosition);
            Vector2Int cell = Vector2Int.RoundToInt(gridPosition);
            ship.Cells.TryGetValue(cell, out Tile attachedTile);
            collidedTile = attachedTile;
        }

        return collidedTile == null;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (TryProcessColliderTrigger(other, out Tile collidedTile) &&
            collidedTile == _hoveredTile)
        {
            _hoveredTile = null;
        }
    }

    // // Fire a ray from cam through ScreenPosition, then get the intersection with _plane
    // private void UpdatePosition()
    // {
    //     if (!_controllerActive && input)
    //     {
    //         // ScreenPosition = input.MouseScreenPosition;
    //     }
    //
    //     Rect viewport = _gameplayCamera.pixelRect;
    //     ScreenPosition = new Vector2(Mathf.Clamp(ScreenPosition.x, viewport.xMin, viewport.xMax),
    //         Mathf.Clamp(ScreenPosition.y, viewport.yMin, viewport.yMax));
    //     Ray ray = _gameplayCamera.ScreenPointToRay(ScreenPosition);
    //     if (!_plane.Raycast(ray, out float distance))
    //     {
    //         return;
    //     }
    //
    //     Vector3 world = ray.GetPoint(distance); 
    //     transform.position = world;
    // }
}