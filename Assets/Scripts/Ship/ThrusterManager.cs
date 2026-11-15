using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar.DataClasses;
using Pulsar.Interfaces;
using UnityEngine;

namespace Pulsar.Ship
{
public class ThrustAtRotation
{
    public float Thrust = 5;
    public readonly List<IThrustable> Thrusters = new();

    public void Reset()
    {
        Thrust = 0;
        Thrusters.Clear();
    }
}

public class ThrusterManager : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D rb;

    private readonly Dictionary<TileRotation, ThrustAtRotation> _directedThrust
        = new()
        {
            [TileRotation.Up] = new ThrustAtRotation(),
            [TileRotation.Down] = new ThrustAtRotation(),
            [TileRotation.Left] = new ThrustAtRotation(),
            [TileRotation.Right] = new ThrustAtRotation(),
        };

    private Vector2 _moveInput;
    private float _rotInput;
    private float _curRotThrust;

    private void Awake()
    {
        if (!rb)
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

    public void HandleRotation(float rot) => _rotInput = rot;

    public void HandleThrust(Vector2 thrust)
    {
        foreach (TileRotation rotation in new TileRotation().Values())
        {
            List<IThrustable> thrusters = _directedThrust[rotation].Thrusters;
            for (int i = thrusters.Count - 1; i >= 0; i--)
            {
                // Cache list
                IThrustable thruster = thrusters[i];
                //Remove null values
                if (thruster is not Tile thrustTile || !thrustTile)
                {
                    _directedThrust[rotation].Thrusters.Remove(thruster);
                }
            }

            foreach (IThrustable thruster in thrusters)
            {
                thruster.DeactivateThrust();
            }
        }

        _moveInput = thrust;

        if (_moveInput.x < 0)
        {
            ShowActiveThrusters(_moveInput.x, TileRotation.Right);
        }
        else if (_moveInput.x > 0)
        {
            ShowActiveThrusters(_moveInput.x, TileRotation.Left);
        }

        if (_moveInput.y < 0)
        {
            ShowActiveThrusters(_moveInput.y, TileRotation.Up);
        }
        else if (_moveInput.y > 0)
        {
            ShowActiveThrusters(_moveInput.y, TileRotation.Down);
        }
    }

    private void ApplyRotation()
    {
        rb.AddTorque(_rotInput * _curRotThrust,
            ForceMode2D.Force);
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
            localForce.y = _moveInput.y * thrustY.Thrust;
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
            localForce.x = _moveInput.x * thrustX.Thrust;
        }

        rb.AddRelativeForce(localForce, ForceMode2D.Force);
    }

    #endregion

    #region Thrust Calculations

    public void CalculateThrusters(in List<IThrustable> thrusters)
    {
        foreach (TileRotation rotation in new TileRotation().Values())
        {
            ThrustAtRotation direction = _directedThrust[rotation];
            direction.Reset();
        }

        foreach (IThrustable thruster in thrusters.ToList())
        {
            foreach (TileRotation rotation in new TileRotation().Values())
            {
                // [CB] We need to convert from the tiles rotation
                // to the relative ship rotation.
                // For example, a Thruster who faces Left should only activate
                // when the player wants to move Left.
                // But its data says it applies thrust in the Up direction,
                // so we need to subtract the TileRotation
                // to the forces to return it to a 'neutral' position.
                TileRotation worldRot
                    = thruster.Rotation.Subtract(rotation);
                if (!thruster.DirectionalThrust.TryGetValue(worldRot,
                        out float thrust) || thrust == 0)
                {
                    continue;
                }

                ThrustAtRotation direction
                    = _directedThrust[rotation];
                direction.Thrust += thrust;
                direction.Thrusters.Add(thruster);
            }

            _curRotThrust += thruster.RotationForce;
        }
    }

    private ThrustAtRotation GetThrustAtRotation(TileRotation orientation) =>
        _directedThrust[orientation];

    #endregion

    private void ShowActiveThrusters(
        float amount,
        TileRotation direction)
    {
        foreach (IThrustable t in _directedThrust[direction].Thrusters)
        {
            t.ActivateThrust(amount, in rb);
        }
    }
}
}
