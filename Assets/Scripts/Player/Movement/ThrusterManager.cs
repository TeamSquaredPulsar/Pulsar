using System;
using System.Collections.Generic;
using UnityEngine;

public struct DirectionThrust
{
    public ThrusterOrientation Orientation;
    public float Thrust;
}

public class ThrusterManager : MonoBehaviour
{
    [SerializeField]
    private List<Thruster> thrusters;
    
    [SerializeField]
    private Rigidbody2D rb;

    [SerializeField]
    private float coreThrust;

    [SerializeField]
    private float rotationalThrust;

    private Vector2 _moveInput;
    private float _rotInput;
    private DirectionThrust[] _directedThrust;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        
        CalculateThrusters();
    }
    
    private void FixedUpdate()
    {
        ApplyRotation();
        ApplyThrust();
    }

    #region Thruster Use
    
    public void HandleRotation(float rot)
    {
        _rotInput = rot * -1f;
    }

    public void HandleThrust(Vector2 thrust)
    {
        _moveInput = thrust;
    }

    private void ApplyRotation()
    {
        rb.AddTorque(_rotInput * rotationalThrust, ForceMode2D.Force);
    }

    private void ApplyThrust()
    {
        Vector2 localForce = Vector2.zero;
        if (_moveInput.y != 0)
        {
            DirectionThrust thrustY = GetDirectionThrust(
                _moveInput.y switch
                {
                    > 0 => ThrusterOrientation.Forward,
                    < 0 => ThrusterOrientation.Backward,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(_moveInput) + ".y",
                        "Already checked Y could not be 0!"),
                });
            localForce.y = _moveInput.y * (thrustY.Thrust + coreThrust);
        }

        if (_moveInput.x != 0)
        {
            DirectionThrust thrustX = GetDirectionThrust(
                _moveInput.x switch
                {
                    > 0 => ThrusterOrientation.Right,
                    < 0 => ThrusterOrientation.Left,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(_moveInput) + ".X",
                        "Already checked X could not be 0!"),
                });
            localForce.x = _moveInput.x * (thrustX.Thrust + coreThrust);
        }

        rb.AddRelativeForce(localForce, ForceMode2D.Force);
    }

    #endregion
    

    #region Thrust Calculations
    private void CalculateThrusters()
    {
        int directions = Enum.GetNames(typeof(ThrusterOrientation)).Length;
        _directedThrust = new DirectionThrust[directions];
        
        for (int i = 0; i < directions; i++)
        {
            _directedThrust[i] = new DirectionThrust
            {
                Orientation = (ThrusterOrientation)i,
                Thrust = 0f,
            };
        }
        
        foreach (Thruster thruster in thrusters)
        {
            _directedThrust[(int)thruster.GetOrientation()].Thrust
                += thruster.GetThrust();
        }
    }

    public DirectionThrust
        GetDirectionThrust(ThrusterOrientation orientation) =>
        _directedThrust[(int)orientation];
    #endregion
}