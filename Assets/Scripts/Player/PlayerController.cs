using System.Collections;
using Pulsar.Utilities;
using UnityEngine;

[RequireComponent(typeof(ShipManager), typeof(BuildManager))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private InputReader input;

    [field: SerializeField]
    public ShipManager Ship { get; private set; }

    [SerializeField]
    private BuildManager buildManager;

    [SerializeField]
    [Min(0.1f)]
    private float tileDestroySeconds;
    
    private Coroutine _destroyTileCoroutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        PlayerCursor.SelectTileEvent += OnSelectTile;
        RegisterInputCallbacks();
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

    private void OnDestroy()
    {
        PlayerCursor.SelectTileEvent += OnSelectTile;
        
        input.MoveEvent -= HandleMove;
        input.ShootEvent -= HandleShoot;
        input.RotateEvent -= HandleRotate;
    }

    private void RegisterInputCallbacks()
    {
        input.MoveEvent += HandleMove;
        input.ShootEvent += HandleShoot;
        input.RotateEvent += HandleRotate;
        input.EnablePlayerActions();
    }

    private void HandleRotate(float obj)
    {
        Ship.Rotate(obj);
    }

    private void HandleShoot()
    {
        Ship.Fire();
    }

    private void HandleMove(Vector2 direction)
    {
        Ship.Move(direction);
    }

    private IEnumerator DestroyTileTimer(Tile tile)
    {
        yield return new WaitForSeconds(tileDestroySeconds);
        tile.TakeDamage(tile.CurrentHp);
    }
}