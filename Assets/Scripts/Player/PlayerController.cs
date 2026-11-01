using System;
using System.Collections;
using Nrjwolf.Tools.AttachAttributes;
using Pulsar.DataClasses;
using Pulsar.Ship;
using Pulsar.Utilities;
using UnityEngine;

namespace Pulsar.Player
{
[RequireComponent(typeof(ShipManager))]
public class PlayerController : MonoBehaviour
{
    public static event Action<PlayerController> PlayerSpawnedEvent;
    public static event Action<PlayerController> PlayerLossEvent;

    public static event Action<Bounds> ShipGridUpdate;

    public static Vector3 PlayerPosition = Vector3.zero;

    [SerializeField]
    private InputReader input;

    [GetComponent, SerializeField,]
    private ShipManager ship;

    public Vector2 CorePosition => ship.Core.transform.position;

    private bool _isShooting;
    private Coroutine _shootingCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        ship.ShipGridUpdate += OnShipGridUpdate;
        ship.ShipDestroyedEvent += OnShipDestroyed;
        RegisterInputCallbacks();
        PlayerSpawnedEvent?.Invoke(this);
    }

    private void OnDestroy()
    {
        ship.ShipGridUpdate -= OnShipGridUpdate;
        ship.ShipDestroyedEvent -= OnShipDestroyed;

        input.MoveEvent -= HandleMove;
        input.RotateEvent -= HandleRotate;
        input.ShootEvent -= HandleShoot;
        StopAllCoroutines();
    }

    private void OnShipGridUpdate()
    {
        ShipGridUpdate?.Invoke(GetShipBounds());
    }

    public bool TryGetTile(Vector2Int index, out Tile tile) =>
        ship.Cells.TryGetValue(index, out tile);
    
    public Bounds GetShipBounds() => ship.GetLocalBounds();

    public bool OwnsTile(Tile tile) => ship.OwnsTile(tile);

    public bool CanAttach(
        Vector2Int location,
        TileInfoSO tileInfo,
        TileRotation rotation) => ship.CanAttach(location, tileInfo, rotation);

    public void AttachTile(
        Vector2Int location,
        Tile tile,
        TileRotation rotation) => ship.AttachTile(location, tile, rotation);

    public Result<(Vector2Int, TileRotation)> FindBestAttachment(
        TileInfoSO tileInfo,
        Vector3 worldPosition) =>
        ship.FindBestAttachment(tileInfo, worldPosition);
    
    private void OnShipDestroyed(ShipManager obj)
    {
        PlayerLossEvent?.Invoke(this);
        Destroy(obj.gameObject);
    }

    private void RegisterInputCallbacks()
    {
        input.MoveEvent += HandleMove;
        input.RotateEvent += HandleRotate;
        input.ShootEvent += HandleShoot;
        input.EnablePlayerActions();
    }

    private void HandleShoot(bool isShooting)
    {
        _isShooting = isShooting;
        if (_isShooting)
        {
            _shootingCoroutine = StartCoroutine(StartShooting());
        }
        else
        {
            StopCoroutine(_shootingCoroutine);
        }
    }

    private void HandleRotate(float obj)
    {
        ship.Rotate(obj);
    }

    private void HandleMove(Vector2 direction)
    {
        ship.Move(direction);
    }

    private void FixedUpdate()
    {
        PlayerPosition = gameObject.transform.position;
    }

    private IEnumerator StartShooting()
    {
        while (_isShooting)
        {
            ship.Fire();
            yield return null;
        }

        _shootingCoroutine = null;
    }
}
}