using System;
using System.Collections.Generic;
using UnityEngine;

public struct ThrustAtRotation
{
    public Rotation Orientation;
    public float Thrust;
}

public class ThrusterManager : MonoBehaviour
{
    [SerializeField]
    private List<Thruster> thrusters;

    [SerializeField]
    private Rigidbody2D rb;

    //Temp field until the core has its own thruster values
    [SerializeField]
    private float coreThrust = 2;

    [SerializeField]
    private float rotationalThrust = 2;

    private ThrustAtRotation[] _directedThrust;

    private Vector2 _moveInput;
    private float _rotInput;

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
            ThrustAtRotation thrustY = GetThrustAtRotation(
                _moveInput.y switch
                {
                    > 0 => Rotation.Up,
                    < 0 => Rotation.Down,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(_moveInput) + ".y",
                        "Already checked Y could not be 0!"),
                });
            localForce.y = _moveInput.y * (thrustY.Thrust + coreThrust);
        }

        if (_moveInput.x != 0)
        {
            ThrustAtRotation thrustX = GetThrustAtRotation(
                _moveInput.x switch
                {
                    > 0 => Rotation.Right,
                    < 0 => Rotation.Left,
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
        int directions = Enum.GetNames(typeof(Rotation)).Length;
        _directedThrust = new ThrustAtRotation[directions];

        for (int i = 0; i < directions; i++)
        {
            _directedThrust[i] = new ThrustAtRotation
            {
                Orientation = (Rotation)i,
                Thrust = 0f,
            };
        }

        foreach (Thruster thruster in thrusters)
        {
            _directedThrust[(int)thruster.GetOrientation()].Thrust
                += thruster.GetThrust();
        }
    }

    private ThrustAtRotation
        GetThrustAtRotation(Rotation orientation) =>
        _directedThrust[(int)orientation];

    #endregion
}