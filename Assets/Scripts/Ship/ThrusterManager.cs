using System;
using System.Collections.Generic;
using Pulsar.DataClasses;
using Pulsar.DataClasses.Extensions;
using UnityEngine;

namespace Pulsar.Ship
{
public struct ThrustAtRotation
{
    public TileRotation Orientation;
    public float Thrust;
    public ThrusterTile[] Thrusters;
}

public class ThrusterManager : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D rb;

    //Temp field until the core has its own thruster values
    [SerializeField]
    private float coreThrust = 3;

    [SerializeField]
    private float rotationalThrust = 2;

    [SerializeField]
    private float rotationScaler = 1f;

    private ThrustAtRotation[] _directedThrust;

    private Vector2 _moveInput;
    private float _rotInput;
    private float _curRotThrust;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
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
        foreach (ThrustAtRotation dir in _directedThrust)
        {
            if (dir.Thrusters == null)
            {
                continue;
            }
            foreach (ThrusterTile t in dir.Thrusters)
            {
                t.SetActive(false);
            }
        }
        
        _moveInput = thrust;

        if (_moveInput.x < 0)
        {
            ShowActiveThrusters(_directedThrust[(int)TileRotation.Right].Thrusters);
        }
        if (_moveInput.x > 0)
        {
            ShowActiveThrusters(_directedThrust[(int)TileRotation.Left].Thrusters);
        }

        if (_moveInput.y < 0)
        {
            ShowActiveThrusters(_directedThrust[(int)TileRotation.Up].Thrusters);
        }
        if (_moveInput.y > 0)
        {
            ShowActiveThrusters(_directedThrust[(int)TileRotation.Down].Thrusters);
        }
    }

    private void ApplyRotation()
    {
        rb.AddTorque(_rotInput * _curRotThrust, ForceMode2D.Force);
    }

    private void ApplyThrust()
    {
        Vector2 localForce = Vector2.zero;
        if (_moveInput.y != 0)
        {
            ThrustAtRotation thrustY = GetThrustAtRotation(
                _moveInput.y switch
                {
                    < 0 => TileRotation.Up,
                    > 0 => TileRotation.Down,
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
                    < 0 => TileRotation.Right,
                    > 0 => TileRotation.Left,
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

    public void CalculateThrusters(ref List<ThrusterTile> thrusters)
    {
        int directions = new TileRotation().Length();
        _directedThrust = new ThrustAtRotation[directions];

        List<ThrusterTile>[] groupedThrusters = 
            new List<ThrusterTile>[directions];
        
        for (int i = 0; i < directions; i++)
        {
            groupedThrusters[i] = new List<ThrusterTile>();
            _directedThrust[i] = new ThrustAtRotation
            {
                Orientation = (TileRotation)i,
                Thrust = 0f,
            };
        }
        
        foreach (ThrusterTile thruster in thrusters)
        {
            int rotIndex = thruster.Rotation.ToInt();
            _directedThrust[rotIndex].Thrust
                += thruster.Thrust;
            groupedThrusters[rotIndex].Add(thruster);
        }
        
        for (int i = 0; i < directions; i++)
        {
            _directedThrust[i].Thrusters = groupedThrusters[i].ToArray();
        }
        
        _curRotThrust = rotationalThrust + rotationScaler * thrusters.Count;
    }

    private ThrustAtRotation
        GetThrustAtRotation(TileRotation orientation) =>
        _directedThrust[(int)orientation];

    #endregion

    private void ShowActiveThrusters(ThrusterTile[] dirThrusters)
    {
        if (dirThrusters.Length == 0) return;
        foreach (ThrusterTile t in dirThrusters)
        {
            t.SetActive(true);
        }
    }
}

}