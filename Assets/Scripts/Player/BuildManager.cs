using System.Collections;
using Nrjwolf.Tools.AttachAttributes;
using Pulsar.Utilities;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    [Header("References")]
    [GetComponent]
    [SerializeField]
    private ShipManager manager;

    [SerializeField]
    private GameObject pRadialMenu;

    [SerializeField]
    private GameObject pGhostPreview;

    [Header("Scan Zone")]
    [SerializeField]
    [Min(0.1f)]
    private float scanPadding = 2.5f;

    [Header("Selection")]
    [SerializeField]
    private Color selectedTint = new(0.3f, 1f, 0.8f, 1f);

    [SerializeField]
    [Min(0.1f)]
    private float pullSpeed = 8f;

    [SerializeField]
    [Min(0.1f)]
    private float orbitDegreesPerSecond = 90f;

    private GhostPreview _ghostPreview;
    private RadialMenu _radialMenu;

    private Tile _selectedTile;
    private bool _atOutline;
    private float _orbitAngle;
    private Vector2Int _attachCell;
    private int _attachRotation;
    private Tile _highlightedTile;

    private Coroutine _updateRadialRoutine;

    private void Awake()
    {
        GameObject ghostPreview = Instantiate(pGhostPreview);
        _ghostPreview = ghostPreview.GetComponent<GhostPreview>();
        GameObject radialMenu = Instantiate(pRadialMenu);
        _radialMenu = radialMenu.GetComponent<RadialMenu>();
        SetAsChild(ghostPreview);
        SetAsChild(radialMenu);
    }

    private void SetAsChild(GameObject child)
    {
        child.transform.SetParent(transform);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
    }

    private void OnDisable()
    {
        // ClearSelection();
        // ClearDestroyHighlight();
        _radialMenu.Hide();
    }

    private void ToggleRadialMenu(bool isOn)
    {
        if (isOn)
        {
            _radialMenu.Show();
            UpdateScanZoneShape();
        }
        else
        {
            _radialMenu.Hide();
        }
    }

    private IEnumerator UpdateRadialMenu()
    {
        while (_selectedTile)
        {
            UpdateScanZoneShape();
            UpdateSelectedTile();
            yield return null;
        }
    }

    private void UpdateScanZoneShape()
    {
        // Get the ship bounds in local space
        Bounds localBounds = manager.GetLocalBounds();

        Vector3 core = manager.Core.transform.position;
        Vector3 coreLocal = manager.transform.InverseTransformPoint(core);

        Vector2 radius = Vector2.Max(localBounds.min, coreLocal);
        radius = radius.Add(scanPadding);
        _radialMenu.UpdateShape(core, radius, manager.transform.rotation);
    }

    public void OnSelectTile(Result<Tile> result)
    {
        if (result.IsSuccess)
        {
            _selectedTile = result.Value;
            _selectedTile.Select(selectedTint);

            _updateRadialRoutine = StartCoroutine(UpdateRadialMenu());
            UpdateSelectedTile();
        }
        else
        {
            DeselectTile();
            StopCoroutine(_updateRadialRoutine);
            _updateRadialRoutine = null;
        }
        ToggleRadialMenu(result.IsSuccess);
    }

    private void DeselectTile()
    {
        // It's in a spot we can build it from
        if (_selectedTile && _atOutline && _ghostPreview.enabled)
        {
            _selectedTile.transform.position
                = _ghostPreview.transform.position;
            _selectedTile.transform.rotation
                = _ghostPreview.transform.rotation;
            manager.AttachTile(
                _selectedTile.transform.position.ToVector2Int(),
                _selectedTile);
        }

        if (_selectedTile)
        {
            _selectedTile.Deselect();
        }

        _selectedTile = null;
        _ghostPreview?.Hide();
    }

    private void UpdateSelectedTile()
    {
        Vector3 target = _radialMenu.PointOnOutline(_orbitAngle);
        // Pull the tile to the outline before allowing orbit and showing a preview
        if (!_atOutline)
        {
            MoveTowardsOutline(target);
            if (!_atOutline)
            {
                return;
            }
        }

        // Move around the oval; keep the angle between 0 and 2 PI
        _orbitAngle = Mathf.Repeat(
            _orbitAngle - Mathf.Max(0.1f, orbitDegreesPerSecond)
            * Mathf.Deg2Rad * Time.deltaTime,
            Mathf.PI * 2f);


        // Keep the tile on the outline as the ship moves and rotates
        _selectedTile.transform.position
            = _radialMenu.PointOnOutline(_orbitAngle);
        UpdatePreview();
        _orbitAngle = _radialMenu.GetOutlineAngleFromTileToCore(
            manager.Core.transform.position,
            _selectedTile.transform.position
        );
    }

    private void MoveTowardsOutline(Vector3 target)
    {
        const float minimumMagnitude = 0.0001f;
        _selectedTile.transform.position = Vector3.MoveTowards(
            _selectedTile.transform.position, target,
            Mathf.Max(0.1f, pullSpeed) * Time.deltaTime);
        _atOutline = (_selectedTile.transform.position - target).sqrMagnitude <
                     minimumMagnitude;
    }

    private void UpdatePreview()
    {
        // Find a valid rotation at the nearest empty cells around the ship
        Result<(Vector2Int cell, int rotation)> result
            = manager.FindBestAttachment(
                _selectedTile.TileInfo, _selectedTile.transform.position);
        if (!result.IsSuccess)
        {
            // Hide the preview when no attachment is possible there
            _ghostPreview.Hide();
            return;
        }

        _attachCell = result.Value.cell;
        _attachRotation = result.Value.rotation;

        // apply _attachRotation to selectedTile to display the preview in world space
        Quaternion rotation = manager.transform.rotation
                              * ShipUtilities.RotateQuarterOnZAxis(
                                  _attachRotation);
        _ghostPreview.Show(_selectedTile.Sprite,
            manager.CellToWorld(_attachCell), rotation);
    }
}