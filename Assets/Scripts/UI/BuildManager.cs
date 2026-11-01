using System.Collections;
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

    [Header("References"), SerializeField,]
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
            return;
        }

        _selectedTile.Select(selectedTint);

        //Toggles now to update the data before the coroutine
        ToggleRadialMenu(true);

        //Orbit close to where the initial block is
        _orbitAngle = radialMenu.GetOutlineAngleFromTileToCore(
            _player.CorePosition,
            _selectedTile.transform.position
        );

        _updateRadialRoutine = StartCoroutine(UpdateRadialMenu());
    }

    private void DeselectTile()
    {
        //If we were destroying a tile, stop
        if (_destroyTileCoroutine != null)
        {
            StopCoroutine(_destroyTileCoroutine);
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
            _player.CorePosition,
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
            = _player.FindBestAttachment(
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
        Quaternion rotation = _player.transform.rotation
                              * ShipUtilities.RotateQuarterOnZAxis(
                                  _attachRotation.ToInt());
        ghostPreview.Show(_selectedTile.Sprite,
            _attachCell.CellToWorld(_player.transform), rotation);

        _selectedTile.Rotate(rotation);
    }
}
}