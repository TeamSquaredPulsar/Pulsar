using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D),
    typeof(Rigidbody2D))]
public class Cursor : MonoBehaviour
{
    private const int CursorSortingOrder = 100;
    
    [SerializeField] private float controllerCursorSpeed = 800f;
    [SerializeField][Range(0f, 1f)] private float stickDeadZone = 0.1f;

    private CircleCollider2D _cursorCollider;
    private Camera _gameplayCamera;
    private InputReader _input;
    private Vector2 _stick;
    private Vector2 _lastMouse;
    private bool _controllerActive;
    private SpriteRenderer _visual;
    private Tile _hoveredTile;
    private Plane _plane;

    private Vector2 ScreenPosition { get; set; }

    private void Awake()
    {
        _visual = GetComponent<SpriteRenderer>();
        _visual.sortingOrder = CursorSortingOrder;
        _cursorCollider = GetComponent<CircleCollider2D>();
        _cursorCollider.isTrigger = true;
        _gameplayCamera = Camera.main;
        ScreenPosition = new Vector2(Screen.width, Screen.height) * 0.5f;
        _plane = new Plane(Vector3.forward, new Vector3(0f, 0f));
    }

    public void Initialize(InputReader reader)
    {
        Unsubscribe();
        _input = reader;
        if (isActiveAndEnabled)
        {
            Subscribe();
        }
    }

    private void OnEnable()
    {
        UnityEngine.Cursor.visible = false;
        Subscribe();
    }

    private void OnDisable()
    {
        _visual.enabled = false;
        Unsubscribe();
        _stick = Vector2.zero;
        _hoveredTile = null;
        UnityEngine.Cursor.visible = true;
    }

    private void Subscribe()
    {
        if (_input == null) return;
        _input.MouseCursorEvent += OnMouse;
        _input.ControllerRightStickCursor += OnStick;
        _lastMouse = _input.MouseScreenPosition;
    }

    private void Unsubscribe()
    {
        if (_input == null) return;
        _input.MouseCursorEvent -= OnMouse;
        _input.ControllerRightStickCursor -= OnStick;
    }

    private void OnMouse(Vector2 position)
    {
        if ((position - _lastMouse).sqrMagnitude >= 1f)
        {
            _controllerActive = false;
            ScreenPosition = position;
            _lastMouse = position;
        }
    }

    private void OnStick(Vector2 direction)
    {
        _stick = direction.magnitude > stickDeadZone ? direction : Vector2.zero;
        if (_stick != Vector2.zero)
        {
            _controllerActive = true;
        }
    }

    private void Update()
    {
        if (_controllerActive)
        {
            ScreenPosition += _stick * (controllerCursorSpeed * Time.unscaledDeltaTime);
        }
        UpdatePosition();
    }

    public Tile RefreshHoveredTile()
    {
        return _hoveredTile != null ? _hoveredTile : null;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.TryGetComponent(out Tile tile))
        {
            _hoveredTile = tile;
        }
        else if (other.TryGetComponent(out ShipManager ship))
        {
            Vector3 localPosition = ship.transform.InverseTransformPoint(transform.position);
            Vector2 gridPosition = ShipUtilities.LocalToGrid(localPosition);
            Vector2Int cell = Vector2Int.RoundToInt(gridPosition);
            ship.Cells.TryGetValue(cell, out Tile attachedTile);
            _hoveredTile = attachedTile;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out Tile tile) && _hoveredTile == tile)
        {
            _hoveredTile = null;
        }
        else if (other.TryGetComponent(out ShipManager ship)
                 && _hoveredTile != null
                 && ship.Cells.TryGetValue(_hoveredTile.Cell, out Tile attachedTile)
                 && attachedTile == _hoveredTile)
        {
            _hoveredTile = null;
        }
    }

    // Fire a ray from cam through ScreenPosition, then get the intersection with _plane
    private void UpdatePosition()
    {
        if (!_controllerActive && _input != null) ScreenPosition = _input.MouseScreenPosition;
        Rect viewport = _gameplayCamera.pixelRect;
        ScreenPosition = new Vector2(Mathf.Clamp(ScreenPosition.x, viewport.xMin, viewport.xMax),
            Mathf.Clamp(ScreenPosition.y, viewport.yMin, viewport.yMax));
        Ray ray = _gameplayCamera.ScreenPointToRay(ScreenPosition);
        if (!_plane.Raycast(ray, out float distance)) return; 
        Vector3 world = ray.GetPoint(distance); 
        transform.position = world;
    }
}
