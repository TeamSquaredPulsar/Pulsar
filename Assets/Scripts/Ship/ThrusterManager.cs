using System;
using System.Collections.Generic;
using System.Linq;
using Pulsar.DataClasses;
using Pulsar.DataClasses.Extensions;
using Pulsar.Interfaces;
using UnityEngine;

namespace Pulsar.Ship
{
public struct ThrustAtRotation
{
    public float Thrust;
    public IThrustable[] Thrusters;
}

public class ThrusterManager : MonoBehaviour
{
    [SerializeField]
    private Rigidbody2D rb;

    [SerializeField]
    private float rotationalThrust = 2;

    [SerializeField]
    private float rotationScaler = 1.5f;

    private ThrustAtRotation[] _directedThrust;
    private static readonly int Directions = new TileRotation().Length();

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

    public void HandleRotation(float rot) => _rotInput = rot;

    public void HandleThrust(Vector2 thrust)
    {
        foreach (ThrustAtRotation dir in _directedThrust.Where(dir =>
                     dir.Thrusters != null))
        {
            foreach (IThrustable t in dir.Thrusters)
            {
                t.DeactivateThrust();
            }
        }

        _moveInput = thrust;

        if (_moveInput.x < 0)
        {
            ShowActiveThrusters(_moveInput.x,
                in _directedThrust[(int)TileRotation.Right]
                    .Thrusters);
        }

        if (_moveInput.x > 0)
        {
            ShowActiveThrusters(_moveInput.x,
                in _directedThrust[(int)TileRotation.Left]
                    .Thrusters);
        }

        if (_moveInput.y < 0)
        {
            ShowActiveThrusters(_moveInput.y,
                in _directedThrust[(int)TileRotation.Up]
                    .Thrusters);
        }

        if (_moveInput.y > 0)
        {
            ShowActiveThrusters(_moveInput.y,
                in _directedThrust[(int)TileRotation.Down]
                    .Thrusters);
        }
    }

    private void ApplyRotation()
    {
        rb.AddTorque(_rotInput * _curRotThrust * rotationScaler,
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
        _directedThrust = new ThrustAtRotation[Directions];

        List<IThrustable>[] groupedThrusters =
            new List<IThrustable>[Directions];

        for (int i = 0; i < Directions; i++)
        {
            groupedThrusters[i] = new List<IThrustable>();
            _directedThrust[i] = new ThrustAtRotation
            {
                Thrust = 0f,
            };
        }

        foreach (IThrustable thruster in thrusters)
        {
            for (int rotIndex = 0; rotIndex < Directions; rotIndex++)
            {
                // [CB] We need to convert from the tiles rotation
                // to the relative ship rotation.
                // For example, a Thruster who faces Left should only activate
                // when the player wants to move Left.
                // But its data says it applies thrust in the Up direction,
                // so we need to subtract the TileRotation
                // to the forces to return it to a 'neutral' position.
                TileRotation worldRot
                    = thruster.Rotation.Subtract(rotIndex.ToTileRotation());
                if (!thruster.DirectionalThrust.TryGetValue(worldRot,
                        out float thrust) || thrust == 0)
                {
                    continue;
                }

                _directedThrust[rotIndex].Thrust
                    += thrust;
                groupedThrusters[rotIndex].Add(thruster);
            }

            rotationalThrust += thruster.RotationForce;
        }

        for (int i = 0; i < Directions; i++)
        {
            _directedThrust[i].Thrusters = groupedThrusters[i].ToArray();
        }

        _curRotThrust = rotationalThrust;
    }

    private ThrustAtRotation GetThrustAtRotation(TileRotation orientation) =>
        _directedThrust[(int)orientation];

    #endregion

    private void ShowActiveThrusters(
        float amount,
        in IThrustable[] dirThrusters)
    {
        foreach (IThrustable t in dirThrusters)
        {
            t.ActivateThrust(amount, in rb);
        }
    }
}
}
