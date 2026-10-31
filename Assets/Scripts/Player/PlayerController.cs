using System;
using System.Collections;
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

    public static Vector3 PlayerPosition = Vector3.zero;
    
    [SerializeField]
    private InputReader input;

    [field: SerializeField]
    public ShipManager Ship { get; private set; }

    [SerializeField]
    private BuildManager buildManager;

    [SerializeField, Min(0.1f),]
    private float tileDestroySeconds;

    private Coroutine _destroyTileCoroutine;

    private bool _isShooting = false;
    private Coroutine _shootingCoroutine = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        PlayerCursor.SelectTileEvent += OnSelectTile;
        Ship.ShipDestroyedEvent += OnShipDestroyed;
        RegisterInputCallbacks();
        PlayerSpawnedEvent?.Invoke(this);
    }

    private void OnShipDestroyed(ShipManager obj)
    {
        PlayerLossEvent?.Invoke(this);
        Destroy(obj.gameObject);
    }

    private void OnDestroy()
    {
        PlayerCursor.SelectTileEvent -= OnSelectTile;
        Ship.ShipDestroyedEvent -= OnShipDestroyed;

        input.MoveEvent -= HandleMove;
        input.RotateEvent -= HandleRotate;
        input.ShootEvent -= HandleShoot;
        StopAllCoroutines();
    }

    private void OnSelectTile(Result<Tile> result)
    {
        if (!result.IsSuccess)
        {
            //TODO Logic for deselecting
            if (_destroyTileCoroutine != null)
            {
                StopCoroutine(_destroyTileCoroutine);
            }

            buildManager.OnSelectTile(result);
            return;
        }

        Tile tile = result.Value;

        //TODO: Logic for selecting
        //If tile is part of this ship
        if (Ship.OwnsTile(tile))
        {
            //Start a coroutine
            _destroyTileCoroutine = StartCoroutine(DestroyTileTimer(tile));
        }
        else if (!tile.IsAttached)
        {
            //Tell the build manager we've selected a new tile.
            buildManager.OnSelectTile(result);
        }
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
        Ship.Rotate(obj);
    }

    private void HandleMove(Vector2 direction)
    {
        Ship.Move(direction);
    }

    private void FixedUpdate()
    {
        PlayerPosition = gameObject.transform.position;
    }

    private IEnumerator DestroyTileTimer(Tile tile)
    {
        yield return new WaitForSeconds(tileDestroySeconds);
        tile.TakeDamage(tile.CurrentHp);
    }

    private IEnumerator StartShooting()
    {
        while (_isShooting)
        {
            Ship.Fire();
            yield return null;
        }

        _shootingCoroutine = null;
    }
}
}