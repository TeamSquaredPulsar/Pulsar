using Nrjwolf.Tools.AttachAttributes;
using UnityEngine;
using UnityEngine.InputSystem;

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
    }

    private void Start()
    {
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
        input.MoveCursorEvent -= OnCursorMove;
    }

    private void OnCursorMove(Vector2 obj)
    {
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(obj);
        
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