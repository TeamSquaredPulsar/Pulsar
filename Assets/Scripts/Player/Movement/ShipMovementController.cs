using System;
using UnityEngine;

public class ShipMovementController : MonoBehaviour
{
    [SerializeField]
    private InputReader inputReader;

    [SerializeField]
    private Rigidbody rb;

    [SerializeField]
    private float coreThrust;

    [SerializeField]
    private float rotationalThrust;

    [SerializeField]
    private ThrusterManager thrusterManager;

    private Vector2 _moveInput;
    private float _rotInput;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (thrusterManager == null)
        {
            thrusterManager = GetComponent<ThrusterManager>();
        }
    }

    private void Start()
    {
        inputReader.RotateEvent += HandleRotation;
        inputReader.MoveEvent += HandleThrust;

        inputReader.EnablePlayerActions();
    }

    private void FixedUpdate()
    {
        ApplyRotation();
        ApplyThrust();
    }

    private void OnDestroy()
    {
        inputReader.RotateEvent -= HandleRotation;
        inputReader.MoveEvent -= HandleThrust;
    }

    private void ApplyRotation()
    {
        rb.AddRelativeTorque(Vector3.forward * (_rotInput * rotationalThrust),
            ForceMode.Force);
    }

    private void ApplyThrust()
    {
        Vector2 localForce = Vector2.zero;

        if (_moveInput.y != 0)
        {
            DirectionThrust thrustY = thrusterManager.GetDirectionThrust(
                _moveInput.y switch
                {
                    > 0 => ThrusterOrientation.Forward,
                    < 0 => ThrusterOrientation.Backward,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(_moveInput) + ".y",
                        "Already checked Y could not be 0!"),
                });
            localForce.y = _moveInput.y * (thrustY.thrust + coreThrust);
        }

        if (_moveInput.x != 0)
        {
            DirectionThrust thrustX = thrusterManager.GetDirectionThrust(
                _moveInput.x switch
                {
                    > 0 => ThrusterOrientation.Forward,
                    < 0 => ThrusterOrientation.Backward,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(_moveInput) + ".X",
                        "Already checked X could not be 0!"),
                });
            localForce.x = _moveInput.x * (thrustX.thrust + coreThrust);
        }

        rb.AddRelativeForce(localForce, ForceMode.Force);
    }

    private void HandleRotation(float rot)
    {
        _rotInput = rot;
    }

    private void HandleThrust(Vector2 thrust)
    {
        _moveInput = thrust;
    }
}