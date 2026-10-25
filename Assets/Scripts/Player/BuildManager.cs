using System.Collections;
using Pulsar.Ship.Extensions;
using Pulsar.Utilities;
using UnityEngine;

namespace Pulsar.Player
{
public class BuildManager : MonoBehaviour
{

    [Header("References"), SerializeField,]
    private PlayerController controller;

    [SerializeField]
    private RadialMenu radialMenu;

    [SerializeField]
    private GhostPreview ghostPreview;

    [Header("Scan Zone"), SerializeField, Min(0.1f),]
    private float scanPadding = 2.5f;

    [Header("Selection"), SerializeField,]
    private Color selectedTint = new(0.3f, 1f, 0.8f, 1f);

    [SerializeField, Min(0.1f),]
    private float pullSpeed = 8f;

    [SerializeField, Min(0.1f),]
    private float orbitDegreesPerSecond = 90f;

    private Tile _selectedTile;
    private bool _atOutline;
    private float _orbitAngle;
    private Vector2Int _attachCell;
    private TileRotation _attachRotation;
    private Tile _highlightedTile;

    private Coroutine _updateRadialRoutine;

    private void Start()
    {
        ghostPreview.Hide();
        radialMenu.Hide();
    }

    private void ToggleRadialMenu(bool isOn)
    {
        if (isOn)
        {
            radialMenu.Show();
            UpdateScanZoneShape();
        }
        else
        {
            radialMenu.Hide();
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
        Bounds localBounds = controller.Ship.GetLocalBounds();

        Vector3 corePos = controller.Ship.Core.transform.position;
        
        Vector2 radius = localBounds.size;
        radius.Add(scanPadding);

        radialMenu.UpdateShape(corePos, radius, controller.transform.rotation);
        // radialMenu.UpdateShape(corePos, radius, controller.transform.rotation);
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
            if (_updateRadialRoutine != null)
            {
                StopCoroutine(_updateRadialRoutine);
                _updateRadialRoutine = null;
            }

        }

        ToggleRadialMenu(result.IsSuccess);
    }

    private void DeselectTile()
    {
        //If we don't have a tile for some reason
        if (!_selectedTile)
        {
            return;
        }

        // It's in a spot we can build it from
        if (_selectedTile 
            && _atOutline 
            && ghostPreview.IsShowing
            // [CB] Currently, tile rotation stored in the selected tile doesn't
            // match that of the _selected tile, so we cant do the following.
            // && controller.Ship.CanAttach(
            //     ghostPreview.transform.position.ToVector2Int(),
            //     _selectedTile.TileInfo,
            //     _selectedTile.Rotation)
            )
        {
            _selectedTile.transform.position
                = ghostPreview.transform.position;
            _selectedTile.transform.rotation
                = ghostPreview.transform.rotation;
            _selectedTile.Deselect();
            controller.Ship.AttachTile(
                _attachCell,
                _selectedTile);

        }
        // We Release the selected tile.
        else
        {
            _selectedTile.Deselect();
        }
        
        _selectedTile = null;
        _atOutline = false;
        _orbitAngle = 0;
        ghostPreview?.Hide();
    }

    private void UpdateSelectedTile()
    {
        Vector3 target = radialMenu.PointOnOutline(_orbitAngle);
        // Pull the tile to the outline before allowing orbit and showing a preview
        if (!_atOutline)
        {
            MoveTowardsOutline(target);
            if (!_atOutline)
            {
                return;
            }
            _selectedTile.transform.SetParent(controller.transform);
        }

        // Move around the oval; keep the angle between 0 and 2 PI
        _orbitAngle = Mathf.Repeat(
            _orbitAngle - Mathf.Max(0.1f, orbitDegreesPerSecond)
            * Mathf.Deg2Rad * Time.deltaTime,
            Mathf.PI * 2f);

        // Keep the tile on the outline as the ship moves and rotates
        Vector3 newPos = radialMenu.PointOnOutline(_orbitAngle);
        _selectedTile.transform.position
            = newPos;
        UpdatePreview();
        _orbitAngle = radialMenu.GetOutlineAngleFromTileToCore(
            controller.Ship.Core.transform.position,
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
        Result<(Vector2Int cell, TileRotation rotation)> result
            = controller.Ship.FindBestAttachment(
                _selectedTile.TileInfo, _selectedTile.transform.position);
        if (!result.IsSuccess)
        {
            // Hide the preview when no attachment is possible there
            ghostPreview.Hide();
            return;
        }

        _attachCell = result.Value.cell;
        _attachRotation = result.Value.rotation;

        // apply _attachRotation to selectedTile to display the preview in world space
        Quaternion rotation = controller.transform.rotation
                              * ShipUtilities.RotateQuarterOnZAxis(
                                  _attachRotation.ToInt());
        ghostPreview.Show(_selectedTile.Sprite,
            controller.Ship.CellToWorld(_attachCell), rotation);
        
        _selectedTile.Rotate(rotation);
    }
}
}