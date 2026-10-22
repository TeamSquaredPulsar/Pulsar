using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ShipManager))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private InputReader input;

    [SerializeField]
    private ShipManager shipManager;

    private bool _isShooting = false;
    private Coroutine _shootingCoroutine = null;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        RegisterInputCallbacks();
        foreach (Tile tile in shipManager.Cells.Values)
        {
            tile.gameObject.layer = LayerMask.NameToLayer("PlayerTiles");
        }
    }

    private void OnDestroy()
    {
        input.MoveEvent -= HandleMove;
        input.RotateEvent -= HandleRotate;
        input.ShootEvent -= HandleShoot;
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
        shipManager.Rotate(obj);
    }

    private void HandleMove(Vector2 direction)
    {
        shipManager.Move(direction);
    }

    private IEnumerator StartShooting()
    {
        while (_isShooting)
        {
            shipManager.Fire();
            yield return null;
        }

        _shootingCoroutine = null;
    }
}