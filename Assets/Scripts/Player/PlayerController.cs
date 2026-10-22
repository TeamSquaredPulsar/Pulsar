using Nrjwolf.Tools.AttachAttributes;
using Pulsar.Utilities;
using UnityEngine;

[RequireComponent(typeof(ShipManager), typeof(BuildManager))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private InputReader input;

    [GetComponent]
    [SerializeField]
    private ShipManager shipManager;

    [GetComponent]
    [SerializeField]
    private BuildManager buildManager;

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
            buildManager.DeselectTile();
            return;
        }

        Tile tile = result.Value;

        //TODO: Logic for selecting
        //If tile is part of this ship
        if (shipManager.OwnsTile(tile))
        {
            //Start a coroutine; if the coroutine expires, destroy the tile
        }
        else if (!tile.IsAttached)
        {
            //Tell the build manager we've selected a new tile.
            buildManager.OnSelectTile(result);
        }
    }

    private void OnDestroy()
    {
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
        shipManager.Rotate(obj);
    }

    private void HandleShoot()
    {
        shipManager.Fire();
    }

    private void HandleMove(Vector2 direction)
    {
        shipManager.Move(direction);
    }
}