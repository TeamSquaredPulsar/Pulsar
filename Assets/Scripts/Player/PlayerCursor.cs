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


    private void OnTriggerEnter2D(Collider2D other)
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
        else if (other.TryGetComponent(out ShipManager ship))
        {
            Vector3 localPosition
                = ship.transform.InverseTransformPoint(transform.position);
            Vector2 gridPosition = ShipUtilities.LocalToGrid(localPosition);
            Vector2Int cell = Vector2Int.RoundToInt(gridPosition);
            ship.Cells.TryGetValue(cell, out Tile attachedTile);
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