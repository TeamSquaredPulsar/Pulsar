using System;
using System.Collections.Generic;
using Pulsar.Ship.Extensions;
using UnityEngine;

namespace Pulsar.Ship
{
public struct ThrustAtRotation
{
    public TileRotation Orientation;
    public float Thrust;
}

public class ThrusterManager : MonoBehaviour
{
    [SerializeField]
    private List<ThrusterTile> thrusters;
    
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
                    > 0 => TileRotation.Up,
                    < 0 => TileRotation.Down,
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
                    > 0 => TileRotation.Right,
                    < 0 => TileRotation.Left,
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
        int directions = Enum.GetNames(typeof(TileRotation)).Length;
        _directedThrust = new ThrustAtRotation[directions];

        for (int i = 0; i < directions; i++)
        {
            _directedThrust[i] = new ThrustAtRotation
            {
                Orientation = (TileRotation)i,
                Thrust = 0f,
            };
        }
        
        foreach (ThrusterTile thruster in thrusters)
        {
            _directedThrust[thruster.Rotation.ToInt()].Thrust
                += thruster.Thrust;
        }
    }

    private ThrustAtRotation
        GetThrustAtRotation(TileRotation orientation) =>
        _directedThrust[(int)orientation];

    #endregion
}

}