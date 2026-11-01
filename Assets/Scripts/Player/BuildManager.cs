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

    [SerializeField]
    private PlayerCursor cursor;

    [Header("Scan Zone"), SerializeField, Min(0.1f),]
    private float scanPadding = 2.5f;

    [Header("Selection"), SerializeField,]
    private Color selectedTint = new(0.3f, 1f, 0.8f, 1f);

    [SerializeField, Min(0.1f),]
    private float pullSpeed = 8f;

    private Tile _selectedTile;
    private bool _atOutline;
    private float _outlineAngle;
    private Vector2Int _attachCell;
    private TileRotation _selectedRotation;

    private void Awake()
    {
        if (!cursor)
        {
            cursor = FindFirstObjectByType<PlayerCursor>();
        }

        ghostPreview.Hide();
        radialMenu.Hide();
    }

    private void LateUpdate()
    {
        // An idle manager must not hide visuals owned by another selection.
        // ReferenceEquals still lets us clean up a destroyed Unity tile.
        if (ReferenceEquals(_selectedTile, null))
        {
            return;
        }

        if (!_selectedTile || !controller.Ship.Core || !cursor)
        {
            EndSelection(false);
            return;
        }

        UpdateScanZoneShape();
        UpdateSelectedTile();
    }

    private void OnDisable() => EndSelection(false);

    private void UpdateScanZoneShape()
    {
        Bounds localBounds = controller.Ship.GetLocalBounds();
        Vector3 corePos = controller.Ship.Core.transform.position;
        Vector2 radius = localBounds.size;
        radius.Add(scanPadding);

        radialMenu.UpdateShape(corePos, radius,
            controller.Ship.transform.rotation);
    }

    public void OnSelectTile(Result<Tile> result)
    {
        if (!result.IsSuccess)
        {
            // Input can move and release in the same frame. Refresh the target
            // before committing so we never attach using a stale preview.
            if (_selectedTile && _atOutline && controller.Ship.Core && cursor)
            {
                UpdateScanZoneShape();
                UpdateSelectedTile();
            }

            EndSelection(true);
            return;
        }

        if (!isActiveAndEnabled || !result.Value || result.Value.IsAttached
            || result.Value == _selectedTile || !controller.Ship.Core || !cursor)
        {
            return;
        }

        EndSelection(false);
        _selectedTile = result.Value;
        _selectedTile.Select(selectedTint);

        // Preserve the picked orientation, snapped to the ship's quarter-turn
        // grid. Only RotateSelectedTile changes it during selection.
        _selectedRotation = (Quaternion.Inverse(controller.Ship.transform.rotation)
                             * _selectedTile.transform.rotation)
            .eulerAngles.z.ToTileRotation();
        _selectedTile.transform.SetParent(controller.Ship.transform, true);
        ApplySelectedRotation();

        UpdateScanZoneShape();
        _outlineAngle = radialMenu.GetOutlineAngleFromTileToCore(
            controller.Ship.Core.transform.position,
            _selectedTile.transform.position);
        radialMenu.Show();
    }

    /// <summary>
    /// Rotates the held tile in 90-degree steps relative to the ship.
    /// Positive steps turn counterclockwise; negative steps turn clockwise.
    /// </summary>
    public void RotateSelectedTile(int quarterTurns)
    {
        if (!_selectedTile)
        {
            return;
        }

        _selectedRotation = _selectedRotation.PlusInt(quarterTurns % 4);
        ApplySelectedRotation();
        if (_atOutline && controller.Ship.Core && cursor)
        {
            UpdateScanZoneShape();
            UpdateSelectedTile();
        }
    }

    private void ApplySelectedRotation()
    {
        _selectedTile.Rotate(controller.Ship.transform.rotation
                             * ShipUtilities.RotateQuarterOnZAxis(
                                 _selectedRotation.ToInt()));
    }

    private void EndSelection(bool tryAttach)
    {
        if (_selectedTile)
        {
            bool canAttach = tryAttach && _atOutline && controller.Ship.Core
                             && ghostPreview.IsShowing
                             && controller.Ship.CanAttach(_attachCell,
                                 _selectedTile.TileInfo, _selectedRotation);

            _selectedTile.Deselect();
            if (canAttach)
            {
                controller.Ship.AttachTile(_attachCell, _selectedTile,
                    _selectedRotation);
            }
        }

        _selectedTile = null;
        _atOutline = false;
        _outlineAngle = 0f;
        ghostPreview?.Hide();
        radialMenu?.Hide();
    }

    private void UpdateSelectedTile()
    {
        Vector3 corePos = controller.Ship.Core.transform.position;
        Vector3 cursorPos = cursor.WorldPosition;
        // The center has no direction: keep the previous outline point there.
        if (((Vector2)(cursorPos - corePos)).sqrMagnitude > 0.0001f)
        {
            _outlineAngle = radialMenu.GetOutlineAngleFromTileToCore(
                corePos, cursorPos);
        }

        Vector3 target = radialMenu.PointOnOutline(_outlineAngle);
        if (!_atOutline)
        {
            _selectedTile.transform.position = Vector3.MoveTowards(
                _selectedTile.transform.position, target,
                Mathf.Max(0.1f, pullSpeed) * Time.deltaTime);
            _atOutline = (_selectedTile.transform.position - target)
                .sqrMagnitude < 0.0001f;
            if (!_atOutline)
            {
                ghostPreview.Hide();
                return;
            }
        }

        _selectedTile.transform.position = target;
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        Result<Vector2Int> result = controller.Ship.FindAttachmentAlongRay(
            _selectedTile.TileInfo, _selectedTile.transform.position,
            _selectedRotation);
        if (!result.IsSuccess)
        {
            ghostPreview.Hide();
            return;
        }

        _attachCell = result.Value;
        Quaternion rotation = controller.Ship.transform.rotation
                              * ShipUtilities.RotateQuarterOnZAxis(
                                  _selectedRotation.ToInt());
        ghostPreview.Show(_selectedTile.Sprite,
            controller.Ship.CellToWorld(_attachCell), rotation);
    }
}
}
