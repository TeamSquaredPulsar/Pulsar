using UnityEngine;
using Pulsar.Ship;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ShipManager))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private InputReader input;

    [SerializeField]
    private ShipManager shipManager;
    
    [SerializeField]
    private BuildManager buildManager;
    
    [SerializeField] private GameObject cursorPrefab;
    
    private Cursor _cursor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _cursor = Instantiate(cursorPrefab).GetComponent<Cursor>();
        _cursor.Initialize(input);
        buildManager.SetCursor(_cursor);
        RegisterInputCallbacks();
    }

    private void OnDestroy()
    {
        input.MoveEvent -= HandleMove;
        input.ShootEvent -= HandleShoot;
        input.RotateEvent -= HandleRotate;
        input.ToggleRadialMenuEvent -= HandleToggleRadialMenuEvent;
        input.SelectFloatingTileEvent -= HandleSelectFloatingTileEvent;
        input.BuildTileEvent -= HandleBuildTileEvent;
        input.DetachTileEvent -= HandleDetachTileTileEvent;
        
    }


    private void RegisterInputCallbacks()
    {
        input.MoveEvent += HandleMove;
        input.ShootEvent += HandleShoot;
        input.RotateEvent += HandleRotate;
        input.ToggleRadialMenuEvent += HandleToggleRadialMenuEvent;
        input.SelectFloatingTileEvent += HandleSelectFloatingTileEvent;
        input.BuildTileEvent += HandleBuildTileEvent;
        input.DetachTileEvent += HandleDetachTileTileEvent;
        input.EnablePlayerActions();
    }

    private void HandleSelectFloatingTileEvent(bool obj)
    {
        if (obj)
        {
            buildManager.SelectTile();
        }
    }

    private void HandleDetachTileTileEvent(bool obj)
    {
        if (obj)
        {
            buildManager.DetachHoveringTile();
        }
    }

    private void HandleBuildTileEvent(InputAction.CallbackContext obj)
    {
        if (obj.performed)
        {
            buildManager.OrbitHeld = true;
        }
        else if (obj.canceled)
        {
            buildManager.OrbitHeld = false;
            buildManager.TryAttachTile();
        }
    }

    private void HandleToggleRadialMenuEvent(bool obj)
    {
        if (obj)
        {
            buildManager.ToggleRadialMenu();
        }
    }
    // TODO: Can make an event where the player
    //  can listen for it and manage the player weapon list themselves.
    //  For performance reasons
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
