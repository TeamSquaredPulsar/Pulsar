using System.Collections;
using Pulsar.UI;
using Pulsar.DataClasses;
using Pulsar.DataClasses.Extensions;
using Pulsar.Utilities;
using Pulsar.Player;
using UnityEngine;

namespace Pulsar.UI
{
public class BuildManager : MonoBehaviour
{
    private PlayerController _player;

    [Header("References")]
    [SerializeField] private InputReader input;
    [SerializeField] private RadialMenu radialMenu;

    [SerializeField]
    private GhostPreview ghostPreview;

    [Header("Scan Zone"), SerializeField, Min(0.1f),]
    private float scanPadding = 2.5f;

    [Header("Selection"), SerializeField,]
    private Color selectedTint = new(0.3f, 1f, 0.8f, 1f);

    [SerializeField, Min(0.1f),]
    private float pullSpeed = 8f;

    [SerializeField, Min(0.1f),]
    private float tileDestroySeconds;

    private Tile _selectedTile;
    private bool _atOutline;
    private float _orbitAngle;
    private Vector2Int _attachCell;
    private TileRotation _attachRotation;
    private Tile _highlightedTile;

    private Coroutine _destroyTileCoroutine;
    private Coroutine _updateRadialRoutine;

    private void Awake()
    {
        PlayerController.PlayerSpawnedEvent += OnPlayerSpawned;
        PlayerCursor.PlayerSelectTile += OnPlayerSelectTile;
        input.RotateSelectedTileEvent += OnCwRotateSelectedTile;
    }

    private void Start()
    {
        ghostPreview.Hide();
        radialMenu.Hide();
    }

    private void OnDestroy()
    {
        PlayerController.PlayerSpawnedEvent -= OnPlayerSpawned;
        PlayerCursor.PlayerSelectTile -= OnPlayerSelectTile;
        input.RotateSelectedTileEvent -= OnCwRotateSelectedTile;
    }

    private void OnPlayerSpawned(PlayerController obj)
    {
        _player = obj;
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

    private IEnumerator DestroyTileTimer(Tile tile)
    {
        yield return new WaitForSeconds(tileDestroySeconds);
        tile.TakeDamage(tile.CurrentHp);
    }

    private void UpdateScanZoneShape()
    {
        // Get the ship bounds in local space
        Bounds localBounds = _player.GetShipBounds();

        Vector3 corePos = _player.CorePosition;

        Vector2 radius = localBounds.size;
        radius.Add(scanPadding);

        radialMenu.UpdateShape(corePos, radius, _player.transform.rotation);
        // radialMenu.UpdateShape(corePos, radius, controller.transform.rotation);
    }

    private void OnPlayerSelectTile(Result<Tile> result)
    {
        //Executed when the select input is disabled
        if (!result.IsSuccess)
        {
            UpdateScanZoneShape();
            // If we were selecting, stop.
            DeselectTile();
            if (_updateRadialRoutine != null)
            {
                StopCoroutine(_updateRadialRoutine);
                _updateRadialRoutine = null;
            }

            ToggleRadialMenu(false);
            return;
        }

        if (!result.Value || result.Value == _selectedTile)
        {
            return;
        }
        _selectedTile = result.Value;

        if (_player.OwnsTile(_selectedTile))
        {
            _destroyTileCoroutine
                = StartCoroutine(DestroyTileTimer(_selectedTile));
            return;
        }
        
        if (_selectedTile.IsAttached)
        {
            //Attached to an enemy ship; ignore
            _selectedTile = null;
            return;
        }

        _selectedTile.Select(selectedTint);

        //Toggles now to update the data before the coroutine
        ToggleRadialMenu(true);

        _atOutline = false;
        ghostPreview.Hide();
        Quaternion relativeRotation = Quaternion.Inverse(_player.transform.rotation)
                                      * _selectedTile.transform.rotation;

        _attachRotation = relativeRotation.eulerAngles.z.ToTileRotation();
        ApplySelectedRotation();

        // Capture the pickup ray ONCE. Cursor movement cannot redirect the
        // approach through the oval before this point has been reached.
        _orbitAngle = radialMenu.GetOutlineAngleFromTileToCore(
            _player.CorePosition,
            PlayerCursor.CursorPosition
        );

        _updateRadialRoutine = StartCoroutine(UpdateRadialMenu());
    }

    private void DeselectTile()
    {
        //If we were destroying a tile, stop
        if (_destroyTileCoroutine != null)
        {
            StopCoroutine(_destroyTileCoroutine);
            _destroyTileCoroutine = null;
            _selectedTile = null;
        }
        //If we don't have a tile for some reason
        if (!_selectedTile)
        {
            return;
        }

        // It's in a spot we can build it from
        if (_atOutline
            && ghostPreview.IsShowing
            && _player.CanAttach(
                _attachCell,
                _selectedTile.TileInfo,
                _attachRotation))
        {
            _selectedTile.transform.position
                = ghostPreview.transform.position;
            _selectedTile.transform.rotation
                = ghostPreview.transform.rotation;
            _selectedTile.Deselect();

            _player.AttachTile(
                _attachCell,
                _selectedTile,
                _attachRotation);
        }
        // We Release the selected tile.
        else
        {
            _selectedTile?.Deselect();
        }

        _selectedTile = null;
        _atOutline = false;
        _orbitAngle = 0;
        ghostPreview.Hide();
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

            _selectedTile.transform.SetParent(_player.transform);
            _selectedTile.transform.position = target;
            ApplySelectedRotation();
            UpdatePreview();
            return;
        }

        // Only after arrival does the cursor control the outline position.
        Vector3 corePosition = _player.CorePosition;
        Vector3 cursorPosition = PlayerCursor.CursorPosition;
        if (((Vector2)(cursorPosition - corePosition)).sqrMagnitude > 0.0001f)
        {
            _orbitAngle = radialMenu.GetOutlineAngleFromTileToCore(
                corePosition, cursorPosition);
        }

        // Keep the tile on the outline as the ship moves and rotates
        Vector3 newPos = radialMenu.PointOnOutline(_orbitAngle);
        _selectedTile.transform.position
            = newPos;
        UpdatePreview();
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
        Result<Vector2Int> result = FindRayAttachment();
        if (!result.IsSuccess)
        {
            // Hide the preview when no attachment is possible there
            ghostPreview.Hide();
            return;
        }

        _attachCell = result.Value;

        // Show the ghost at the manually selected rotation.
        Quaternion rotation = _player.transform.rotation
                              * ShipUtilities.RotateQuarterOnZAxis(
                                  _attachRotation.ToInt());
        ghostPreview.Show(_selectedTile.Sprite,
            _attachCell.CellToWorld(_player.transform), rotation);
    }

    private void OnCwRotateSelectedTile(int i) => RotateSelectedTile(i);

    // Positive steps turn counterclockwise, negative steps turn clockwise.
    public void RotateSelectedTile(int quarterTurns)
    {
        if (!_selectedTile || _selectedTile.IsAttached || !_player)
        {
            return;
        }
        _attachRotation = _attachRotation.PlusInt(quarterTurns % 4);
        ApplySelectedRotation();
        if (_atOutline)
        {
            UpdatePreview();
        }
    }

    private void ApplySelectedRotation()
    {
        _selectedTile.Rotate(_player.transform.rotation
                             * ShipUtilities.RotateQuarterOnZAxis(
                                 _attachRotation.ToInt()));
    }

    private Result<Vector2Int> FindRayAttachment()
    {
        Vector2 origin = _player.transform.InverseTransformPoint(
            _selectedTile.transform.position);
        Vector2 core = _player.transform.InverseTransformPoint(_player.CorePosition);
        Vector2 toCore = core - origin;
        float length = toCore.magnitude;
        if (length < 0.0001f)
        {
            return Result<Vector2Int>.Fail();
        }

        // Walk the grid along the ray, stopping at the first occupied cell.
        // Its entry face determines the empty attachment cell immediately before it.
        Vector2 direction = toCore / length;
        Vector2Int cell = new(Mathf.FloorToInt(origin.x + 0.5f),
            Mathf.FloorToInt(origin.y + 0.5f));
        if (_player.TryGetTile(cell, out _))
        {
            return Result<Vector2Int>.Fail();
        }
        int stepX = direction.x > 0f ? 1 : -1;
        int stepY = direction.y > 0f ? 1 : -1;
        float deltaX = direction.x == 0f ? float.PositiveInfinity
            : Mathf.Abs(1f / direction.x);
        float deltaY = direction.y == 0f ? float.PositiveInfinity
            : Mathf.Abs(1f / direction.y);
        float nextX = direction.x == 0f ? float.PositiveInfinity
            : (cell.x + stepX * 0.5f - origin.x) / direction.x;
        float nextY = direction.y == 0f ? float.PositiveInfinity
            : (cell.y + stepY * 0.5f - origin.y) / direction.y;

        while (Mathf.Min(nextX, nextY) <= length)
        {
            Vector2Int candidate = cell;
            if (nextX <= nextY)
            {
                cell.x += stepX;
                nextX += deltaX;
            }
            else
            {
                cell.y += stepY;
                nextY += deltaY;
            }
            if (_player.TryGetTile(cell, out _))
            {
                return _player.CanAttach(candidate, _selectedTile.TileInfo,
                    _attachRotation)
                    ? Result<Vector2Int>.Pass(candidate)
                    : Result<Vector2Int>.Fail();
            }
        }
        return Result<Vector2Int>.Fail();
    }
}
}
