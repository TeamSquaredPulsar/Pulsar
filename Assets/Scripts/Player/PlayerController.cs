using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private InputReader input;

    [SerializeField]
    private PlayerWeapon[] weapons;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        input.MoveEvent += HandleMove;
        input.ShootEvent += HandleShoot;
        input.RotateEvent += HandleRotate;
        input.EnablePlayerActions();
    }

    private void OnDestroy()
    {
        input.MoveEvent -= HandleMove;
        input.ShootEvent -= HandleShoot;
        input.RotateEvent -= HandleRotate;
    }

    private void HandleRotate(float obj)
    {
        Debug.Log($"rotate: {obj}");
    }

    // TODO: Can make an event where the player
    //  can listen for it and manage the player weapon list themselves.
    //  For performance reasons
    private void HandleShoot()
    {
        foreach (PlayerWeapon weapon in weapons)
        {
            if (weapon != null)
            {
                weapon.Fire();
            }
        }
    }

    private void HandleMove(Vector2 obj)
    {
        Debug.Log("Handling move: " + obj);
    }
}