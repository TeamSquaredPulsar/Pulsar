using System;
using UnityEngine;
using Pulsar.Ship;

public class BuildManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipManager manager;
    [SerializeField] private RadialMenu radialMenu;
    [SerializeField] private GhostPreview ghostPreview;
    private Cursor _cursor;

    public void SetCursor(Cursor cursor) => _cursor = cursor;

    [Header("Scan Zone")]
    [SerializeField, Min(0.1f)] private float scanPadding = 2.5f;

    [Header("Selection")]
    [SerializeField] private Color selectedTint = new(0.3f, 1f, 0.8f, 1f);
    [SerializeField] private Color detachTint = Color.red;
    [SerializeField, Min(0.1f)] private float pullSpeed = 8f;
    [SerializeField, Min(0.1f)] private float orbitDegreesPerSecond = 90f;

    private bool _scanActive;
    private Tile _selectedTile;
    private bool _atOutline;
    private float _orbitAngle;
    private Vector2Int _attachCell;
    private int _attachRotation;
    private bool _attachValid;
    private Tile _highlightedTile;

    public bool OrbitHeld { get; set; }
    
    private void Start()
    {
        radialMenu.Hide();
    }

    public void ToggleRadialMenu()
    {
            _scanActive = !_scanActive;
            if (_scanActive) radialMenu.Show();
            else
            {
                radialMenu.Hide();
                ClearSelection();
            }
        
    }

    private void Update()
    {

        if (_scanActive)
        {
            UpdateScanZoneShape();
            UpdateSelectedTile();
        }

        if (_selectedTile == null) UpdateDestroyMode();
        else ClearDestroyHighlight();
    }

    private void UpdateScanZoneShape()
    {
        // Get the ship bounds in local space
        Bounds b = manager.GetLocalBounds();

        Vector3 core = manager.Core != null
            ? manager.Core.transform.position : manager.transform.position;
        Vector3 coreLocal = manager.transform.InverseTransformPoint(core);

        float rx = Mathf.Max(Mathf.Abs(b.min.x - coreLocal.x), Mathf.Abs(b.max.x - coreLocal.x)) + scanPadding;
        float ry = Mathf.Max(Mathf.Abs(b.min.y - coreLocal.y), Mathf.Abs(b.max.y - coreLocal.y)) + scanPadding;
        radialMenu.UpdateShape(core, rx, ry, manager.transform.rotation);
    }

    public void SelectTile()
    {
        if(_selectedTile != null || !_scanActive) return;
        Tile tile = _cursor != null ? _cursor.RefreshHoveredTile() : null;
        if (tile == null || tile.IsAttached || tile.TileInfo == null || tile == _selectedTile) return;
        ClearSelection();
        ClearDestroyHighlight();
        _selectedTile = tile;
        _selectedTile.BeginBuildControl();
        _selectedTile.Highlight(selectedTint);
        Vector3 core = manager.Core != null
            ? manager.Core.transform.position : manager.transform.position;
        _orbitAngle = radialMenu.GetOutlineAngleFromTileToCore(core, tile.transform.position);
    }

    private void UpdateSelectedTile()
    {
        if (_selectedTile == null || _selectedTile.IsAttached)
        {
            ClearSelection();
            return;
        }

        Vector3 target = radialMenu.PointOnOutline(_orbitAngle);
        // Pull the tile to the outline before allowing orbit and showing a preview
        if (!_atOutline)
        {
            _selectedTile.transform.position = Vector3.MoveTowards(
                _selectedTile.transform.position, target, Mathf.Max(0.1f, pullSpeed) * Time.deltaTime);
            _atOutline = (_selectedTile.transform.position - target).sqrMagnitude < 0.0001f;
            if (!_atOutline) return;
        }

        if (OrbitHeld)
        {
            // Move around the oval; keep the angle between 0 and 2 PI
            _orbitAngle = Mathf.Repeat(_orbitAngle
                                       - Mathf.Max(0.1f, orbitDegreesPerSecond) * Mathf.Deg2Rad * Time.deltaTime,
                Mathf.PI * 2f);
        }
        
        // Keep the tile on the outline as the ship moves and rotates
        _selectedTile.transform.position = radialMenu.PointOnOutline(_orbitAngle);
        UpdatePreview();
    }

    public void TryAttachTile()
    {
            bool shouldAttach = _attachValid;
            if (shouldAttach &&
                manager.Attach(_attachCell, _selectedTile, _attachRotation))
            {
                ClearSelection();
            }
    }

    private void UpdatePreview()
    {
        // Find a valid rotation at the nearest empty cells around the ship
        (Vector2Int cell, int rotation)? result = manager.FindBestAttachment(
            _selectedTile.TileInfo, _selectedTile.transform.position);
        _attachValid = result.HasValue;
        if (!_attachValid)
        {
            // Hide the preview when no attachment is possible there
            ghostPreview.Hide();
            return;
        }

        _attachCell = result.Value.cell;
        _attachRotation = result.Value.rotation;
        
        // apply _attachRotation to selectedTile to display the preview in world space
        Quaternion rotation = manager.transform.rotation
                              * ShipUtilities.RotateQuarterOnZAxis(_attachRotation);
        ghostPreview.Show(_selectedTile.TileInfo.sprite, manager.CellToWorld(_attachCell), rotation);
    }

    private void ClearSelection()
    {
        if (_selectedTile != null)
        {
            if (!_selectedTile.IsAttached)
                _selectedTile.ReleaseToFloating(Vector2.zero, 0f);
            _selectedTile.Unhighlight();
        }
        _selectedTile = null;
        _atOutline = false;
        OrbitHeld = false;
        _attachValid = false;
        if (ghostPreview != null) ghostPreview.Hide();
    }

    private void UpdateDestroyMode()
    {
        Tile tile = _cursor != null ? _cursor.RefreshHoveredTile() : null;
        if (tile != null && (!tile.IsAttached || tile == manager.Core
                                              || !manager.Cells.TryGetValue(tile.Cell, out Tile ownedTile) || ownedTile != tile))
            tile = null;
        if (_highlightedTile != null && _highlightedTile != tile) ClearDestroyHighlight();
        if (tile != null && tile != manager.Core && _highlightedTile != tile)
        {
            _highlightedTile = tile;
            tile.Highlight(detachTint);
        }
    }

    public void DetachHoveringTile()
    {
        if (_highlightedTile != null)
        {
            Vector2Int cell = _highlightedTile.Cell;
            _highlightedTile = null;
            manager.DestroyTileAt(cell);
        }
    }

    private void ClearDestroyHighlight()
    {
        if (_highlightedTile != null) _highlightedTile.Unhighlight();
        _highlightedTile = null;
    }

    private void OnDisable()
    {
        ClearSelection();
        ClearDestroyHighlight();
        _scanActive = false;
        if (radialMenu != null) radialMenu.Hide();
    }

    private void OnDestroy()
    {
        if (radialMenu != null) Destroy(radialMenu.gameObject);
        if (ghostPreview != null) Destroy(ghostPreview.gameObject);
    }
}
