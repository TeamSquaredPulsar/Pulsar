using UnityEngine;

[RequireComponent(typeof(ShipManager))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private InputReader input;

    [SerializeField]
    private ShipManager shipManager;

    [SerializeField]
    private BuildManager buildManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
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

    private void HandleBuildTileEvent(bool obj)
    {
        buildManager.OrbitHeld = obj;
        if (!buildManager.OrbitHeld)
        {
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
    //  can listen for it and manage the player thruster list themselves.
    //  For performance reasons
    private void HandleRotate(float obj)
    {
        Debug.Log($"rotate: {obj}");
    }

    // TODO: Can make an event where the player
    //  can listen for it and manage the player weapon list themselves.
    //  For performance reasons
    private void HandleShoot()
    {
        shipManager.Fire();
    }

    // TODO: Can make an event where the player
    //  can listen for it and manage the player thruster list themselves.
    //  For performance reasons
    private void HandleMove(Vector2 direction)
    {
        shipManager.Move(direction);
    }
}