using UnityEngine;
using Pulsar.Ship;

namespace Pulsar.Building
{
    public class BuildManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ShipGrid grid;
        [SerializeField] private Camera gameplayCamera;

        [Header("Scan Zone")]
        [SerializeField] private float scanPadding = 2.5f;

        [Header("Hover")]
        [SerializeField] private float hoverRadius = 0.6f;
        [SerializeField] private Color hoverTint = new(0.3f, 1f, 0.8f, 1f);

        [SerializeField] private RadialMenu _radialMenu;
        private GhostPreview _ghost;
        private bool _scanActive;

        // hover tile
        private Tile _hoveredTile;
        private Vector2Int _attachCell;
        private int _attachRotation;
        private bool _attachValid;

        // destroy tile
        private Tile _highlightedTile;
        private Vector3 _cursorWorld;


        private void Start()
        {
            if (gameplayCamera == null) gameplayCamera = Camera.main;

            // ghost preview
            GameObject ghostGO = new GameObject("GhostPreview");
            _ghost = ghostGO.AddComponent<GhostPreview>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _scanActive = !_scanActive;
                if (_scanActive)
                {
                    _radialMenu.Show();
                    Debug.Log("Activate radial menu");
                }
                else
                {
                    _radialMenu.Hide();
                    ClearHover();
                    Debug.Log("Inactivate radial menu");
                }
            }

            if (!TryGetCursorWorld(out _cursorWorld))
            {
                ClearHover();
                ClearDestroyHighlight();
                return;
            }

            if (_scanActive)
            {
                UpdateScanZoneShape();
                UpdateHover();
            }

            // destroy is always available when not hovering a floating tile
            if (_hoveredTile == null)
                UpdateDestroyMode();
        }

        private void UpdateScanZoneShape()
        {
            Bounds b = grid.GetLocalBounds();
            float rx = b.extents.x + scanPadding;
            float rz = b.extents.z + scanPadding;
            Vector3 center = grid.transform.TransformPoint(b.center);
            Quaternion rot = grid.transform.rotation;

            _radialMenu.UpdateShape(center, rx, rz, rot);
        }
        

        private void UpdateHover()
        {
            Vector2 cursor = ShipUtilities.LocalToGrid(_cursorWorld);

            // Find the floating tile closest to cursor
            Tile best = null;
            float bestDist = float.MaxValue;

            Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
            foreach (Tile t in allTiles)
            {
                if (t.IsAttached) continue;
                if (!_radialMenu.IsInside(t.transform.position)) continue;

                float d = Vector2.Distance(cursor, ShipUtilities.LocalToGrid(t.transform.position));
                if (d < bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }

            if (best == null || bestDist > hoverRadius)
            {
                ClearHover();
                return;
            }

            if (best != _hoveredTile)
            {
                ClearHover();
                ClearDestroyHighlight();
                _hoveredTile = best;
                _hoveredTile.Highlight(hoverTint);
            }
            
            (Vector2Int cell, int rotation)? result = grid.FindBestAttachment(
                _hoveredTile.TileInfo,
                _hoveredTile.transform.position);

            if (result.HasValue)
            {
                _attachCell = result.Value.cell;
                _attachRotation = result.Value.rotation;
                _attachValid = true;

                Vector3 ghostWorld = grid.CellToWorld(_attachCell);
                Quaternion ghostRot = grid.transform.rotation
                                      * ShipUtilities.RotateQuarterOnYAxis(_attachRotation);
                _ghost.Show(_hoveredTile.TileInfo.sprite, ghostWorld, true, ghostRot);
            }
            else
            {
                _attachValid = false;
                _ghost.Hide();
            }

            if (Input.GetKeyDown(KeyCode.C) && _attachValid)
            {
                TileInfoSO info = _hoveredTile.TileInfo;
                if (grid.Attach(_attachCell, _hoveredTile, _attachRotation))
                {
                    _hoveredTile = null;
                    _attachValid = false;
                    _ghost.Hide();
                    Debug.Log($"Attached {info.tileName} at {_attachCell}");
                }
            }
        }

        private void ClearHover()
        {
            if (_hoveredTile != null)
            {
                if (_hoveredTile.gameObject != null)
                    _hoveredTile.Unhighlight();
                _hoveredTile = null;
            }
            _attachValid = false;
            _ghost.Hide();
        }


        private void UpdateDestroyMode()
        {
            Vector3 cursorWorld = _cursorWorld;
            Tile tile = grid.GetTileAtWorldPos(cursorWorld);

            // un-highlight previous
            if (_highlightedTile != null && _highlightedTile != tile)
            {
                ClearDestroyHighlight();
            }

            // highlight current 
            if (tile != null && tile != grid.Core && _highlightedTile != tile)
            {
                _highlightedTile = tile;
                tile.Highlight(Color.red);
            }

            if (Input.GetKeyDown(KeyCode.X) && _highlightedTile != null)
            {
                Vector2Int cell = _highlightedTile.Cell;
                _highlightedTile = null;
                grid.DestroyTile(cell);
            }
        }


        private void ClearDestroyHighlight()
        {
            if (_highlightedTile != null) _highlightedTile.Unhighlight();
            _highlightedTile = null;
        }

        private bool TryGetCursorWorld(out Vector3 worldPoint)
        {
            worldPoint = default;
            if (gameplayCamera == null || grid == null) return false;
            Plane plane = new Plane(Vector3.up, grid.transform.position);
            Ray ray = gameplayCamera.ScreenPointToRay(Input.mousePosition);
            if (!plane.Raycast(ray, out float distance)) return false;
            worldPoint = ray.GetPoint(distance);
            return true;
        }

        private void OnDestroy()
        {
            if (_radialMenu != null) Destroy(_radialMenu.gameObject);
            if (_ghost != null) Destroy(_ghost.gameObject);
        }
    }
}
