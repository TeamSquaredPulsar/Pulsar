using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private Vector3 _moveInput;
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

    private void OnDestroy()
    {
        inputReader.RotateEvent -= HandleRotation;
        inputReader.MoveEvent -= HandleThrust;
    }

    private void FixedUpdate()
    {
        ApplyRotation();
        ApplyThrust();
    }

    private void ApplyRotation()
    {
        rb.AddRelativeTorque(Vector3.up * (_rotInput * rotationalThrust),
            ForceMode.Force);
    }

    private void ApplyThrust()
    {
        Vector3 localForce = Vector3.zero;

        if (_moveInput.z > 0)
        {
            DirectionThrust t
                = thrusterManager.GetDirectionThrust(
                    ThrusterOrientation.Forward);
            localForce.z = _moveInput.z * (t.thrust + coreThrust);
        }
        else if (_moveInput.z < 0)
        {
            DirectionThrust t =  thrusterManager.GetDirectionThrust(
                ThrusterOrientation.Backward);
            localForce.z = _moveInput.z * (t.thrust + coreThrust);
        }

        if (_moveInput.x > 0)
        {
            DirectionThrust t = thrusterManager.GetDirectionThrust(
                ThrusterOrientation.Right);
            localForce.x = _moveInput.x * (t.thrust + coreThrust);
        }
        else if (_moveInput.x < 0)
        {
            DirectionThrust t = thrusterManager.GetDirectionThrust(
                ThrusterOrientation.Left);
            localForce.x = _moveInput.x * (t.thrust + coreThrust);
        }
        
        rb.AddRelativeForce(localForce, ForceMode.Force);
    }
    
    private void HandleRotation(float rot)
    {
        _rotInput = rot;
    }

    private void HandleThrust(Vector2 thrust)
    {
        _moveInput = new Vector3(thrust.x, 0, thrust.y);
    }
}