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

    private void Update()
    {
        if (input.IsShooting)
        {
            shipManager.Fire();
        }
    }

    private void OnDestroy()
    {
        input.MoveEvent -= HandleMove;
        input.RotateEvent -= HandleRotate;
    }

    private void RegisterInputCallbacks()
    {
        input.MoveEvent += HandleMove;
        input.RotateEvent += HandleRotate;
        input.EnablePlayerActions();
    }

    private void HandleRotate(float obj)
    {
        shipManager.Rotate(obj);
    }

    //// TODO: Can make an event where the player
    ////  can listen for it and manage the player weapon list themselves.
    ////  For performance reasons
    //private void HandleShoot()
    //{
    //    if (input.IsShooting)
    //    {
    //        shipManager.Fire();
    //    }
    //}

    private void HandleMove(Vector2 direction)
    {
        shipManager.Move(direction);
    }
}