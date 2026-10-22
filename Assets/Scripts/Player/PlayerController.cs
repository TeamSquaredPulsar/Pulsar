using Nrjwolf.Tools.AttachAttributes;
using Pulsar.Utilities.Utilities;
using UnityEngine;

[RequireComponent(typeof(ShipManager))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private InputReader input;

    [GetComponent]
    [SerializeField]
    private ShipManager shipManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        PlayerCursor.SelectTileEvent += OnSelectTile;
        RegisterInputCallbacks();
    }

    private void OnSelectTile(Result<Tile> obj)
    {
        if (!obj.IsSuccess)
        {
            //TODO Logic for deselecting
            return;
        }
        Tile tile = obj.Value;
        
        //TODO: Logic for selecting
        //If tile is part of this ship
        if (shipManager.OwnsTile(tile))
        {
            //Start a coroutine; if the coroutine expires, destroy the tile
            
        }
        
        //else if the tile isn't attached
            // Enable the radial menu
            // Snap it to radial menu
            // Start ghost preview
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

    // TODO: Can make an event where the player
    //  can listen for it and manage the player weapon list themselves.
    //  For performance reasons
    private void HandleShoot()
    {
        shipManager.Fire();
    }

    private void HandleMove(Vector2 direction)
    {
        shipManager.Move(direction);
    }
}