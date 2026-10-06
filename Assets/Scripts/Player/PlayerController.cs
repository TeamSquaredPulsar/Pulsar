using UnityEngine;

[RequireComponent(typeof(ShipManager))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private InputReader input;

    [SerializeField]
    private ShipManager shipManager;

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
    }

    private void RegisterInputCallbacks()
    {
        input.MoveEvent += HandleMove;
        input.ShootEvent += HandleShoot;
        input.RotateEvent += HandleRotate;
        input.EnablePlayerActions();
    }

    private void HandleRotate(float amount)
    {
        Debug.Log($"rotate: {amount}");
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