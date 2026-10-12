using UnityEngine;
using UnityEngine.Serialization;
using Pulsar.Ship;

namespace Pulsar.Building
{
    public class BuildManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ShipManager manager;
        [SerializeField] private RadialMenu radialMenu;

        [Header("Scan Zone")]
        [SerializeField, Min(0.1f)] private float scanPadding = 2.5f;

        [Header("Selection")]
        [SerializeField] private Color selectedTint = new(0.3f, 1f, 0.8f, 1f);
        [SerializeField] private Color detachTint = Color.red;
        [SerializeField, Min(0.1f)] private float pullSpeed = 8f;
        [SerializeField, Min(0.1f)] private float orbitDegreesPerSecond = 90f;

        private GhostPreview _ghost;
        private bool _scanActive;
        private Tile _selectedTile;
        private bool _atOutline;
        private float _orbitAngle;
        private bool _orbitHeld;
        private Vector2Int _attachCell;
        private int _attachRotation;
        private bool _attachValid;
        private Tile _highlightedTile;

        private void Start()
        {
            _ghost = new GameObject("GhostPreview").AddComponent<GhostPreview>();
            radialMenu.Hide();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _scanActive = !_scanActive;
                if (_scanActive) radialMenu.Show();
                else
                {
                    radialMenu.Hide();
                    ClearSelection();
                }
            }

            if (_scanActive)
            {
                UpdateScanZoneShape();
                if (Input.GetMouseButtonDown(0)) SelectTile();
                UpdateSelectedTile();
            }

            if (_selectedTile == null) UpdateDestroyMode();
            else ClearDestroyHighlight();
        }

        private void UpdateScanZoneShape()
        {
            Bounds b = manager.GetLocalBounds();

            Vector3 core = manager.Core != null
                ? manager.Core.transform.position : manager.transform.position;
            Vector3 coreLocal = manager.transform.InverseTransformPoint(core);
            float rx = Mathf.Max(Mathf.Abs(b.min.x - coreLocal.x), Mathf.Abs(b.max.x - coreLocal.x))
                       + Mathf.Max(0.1f, scanPadding);
            float ry = Mathf.Max(Mathf.Abs(b.min.y - coreLocal.y), Mathf.Abs(b.max.y - coreLocal.y))
                       + Mathf.Max(0.1f, scanPadding);
            radialMenu.UpdateShape(core, rx, ry, manager.transform.rotation);
        }

        private void SelectTile()
        {
            if(_selectedTile != null) return;
            Tile tile = Tile.Hovered;
            if (tile == null || tile.IsAttached || tile.TileInfo == null || tile == _selectedTile) return;
            ClearSelection();
            ClearDestroyHighlight();
            _selectedTile = tile;
            _selectedTile.BeginBuildControl();
            _selectedTile.Highlight(selectedTint);
            Vector3 core = manager.Core != null
                ? manager.Core.transform.position : manager.transform.position;
            _orbitAngle = radialMenu.OutlineAngleFromCore(core, tile.transform.position);
        }

        private void UpdateSelectedTile()
        {
            if (_selectedTile == null || _selectedTile.IsAttached)
            {
                ClearSelection();
                return;
            }

            Vector3 target = radialMenu.PointOnOutline(_orbitAngle);
            if (!_atOutline)
            {
                _selectedTile.transform.position = Vector3.MoveTowards(
                    _selectedTile.transform.position, target, Mathf.Max(0.1f, pullSpeed) * Time.deltaTime);
                _atOutline = (_selectedTile.transform.position - target).sqrMagnitude < 0.0001f;
                if (!_atOutline) return;
            }

            if (Input.GetKey(KeyCode.C))
            {
                _orbitHeld = true;
                _orbitAngle = Mathf.Repeat(_orbitAngle
                    - Mathf.Max(0.1f, orbitDegreesPerSecond) * Mathf.Deg2Rad * Time.deltaTime,
                    Mathf.PI * 2f);
            }
            _selectedTile.transform.position = radialMenu.PointOnOutline(_orbitAngle);
            UpdatePreview();

            if (Input.GetKeyUp(KeyCode.C))
            {
                bool shouldAttach = _orbitHeld && _attachValid;
                _orbitHeld = false;
                if (shouldAttach && manager.Attach(_attachCell, _selectedTile, _attachRotation))
                    ClearSelection();
            }
        }

        private void UpdatePreview()
        {
            (Vector2Int cell, int rotation)? result = manager.FindBestAttachment(
                _selectedTile.TileInfo, _selectedTile.transform.position, nearestCellOnly: true);
            _attachValid = result.HasValue;
            if (!_attachValid)
            {
                _ghost.Hide();
                return;
            }
            _attachCell = result.Value.cell;
            _attachRotation = result.Value.rotation;
            Quaternion rotation = manager.transform.rotation
                                  * ShipUtilities.RotateQuarterOnZAxis(_attachRotation);
            _ghost.Show(_selectedTile.TileInfo.sprite, manager.CellToWorld(_attachCell), true, rotation);
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
            _orbitHeld = false;
            _attachValid = false;
            if (_ghost != null) _ghost.Hide();
        }

        private void UpdateDestroyMode()
        {
            Tile tile = Tile.Hovered;
            if (tile != null && (!tile.IsAttached || tile == manager.Core
                || !manager.Cells.TryGetValue(tile.Cell, out Tile ownedTile) || ownedTile != tile))
                tile = null;
            if (_highlightedTile != null && _highlightedTile != tile) ClearDestroyHighlight();
            if (tile != null && tile != manager.Core && _highlightedTile != tile)
            {
                _highlightedTile = tile;
                tile.Highlight(detachTint);
            }
            if (Input.GetKeyDown(KeyCode.X) && _highlightedTile != null)
            {
                Vector2Int cell = _highlightedTile.Cell;
                _highlightedTile = null;
                manager.DestroyTile(cell);
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
            if (_ghost != null) Destroy(_ghost.gameObject);
        }
    }
}
