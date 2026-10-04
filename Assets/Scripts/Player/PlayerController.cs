using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private InputReader input;

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

    private void HandleShoot()
    {
        Debug.Log("Pew pew");
    }

    private void HandleMove(Vector2 obj)
    {
        Debug.Log("Handling move: " + obj);
    }
}