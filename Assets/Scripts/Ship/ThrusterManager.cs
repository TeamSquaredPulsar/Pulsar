using System;
using System.Collections.Generic;
using Codice.Client.Commands.WkTree;
using Pulsar.DataClasses;
using Pulsar.DataClasses.Extensions;
using Pulsar.Ship.Data;
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
    private const float ThrustThresh = 0.01f;
    private const float TorqueThresh = 0.01f;
    
    [SerializeField]
    private ShipManager shipManager;

    //Temp field until the core has its own thruster values
    [SerializeField]
    private float coreThrust = 2;

    [SerializeField]
    private float rotationalThrust = 2;
    
    private ThrustAtRotation[] _directedThrust;

    private Rigidbody2D _rb;
    private List<ThrusterTile> _thrusters;
    
    private Vector2 _moveInput;
    private float _rotInput;

    private void Awake()
    {
        _rb = shipManager.GetComponent<Rigidbody2D>();
        
        UpdateThrusterList();
    }

    private void Start()
    {
        shipManager.ShipGridUpdate += UpdateThrusterList;
    }

    private void OnDestroy()
    {
        shipManager.ShipGridUpdate -= UpdateThrusterList;
    }

    private void FixedUpdate()
    {
        ApplyRotation();
        ApplyThrust();
    }

    private void UpdateThrusterList()
    {
        _thrusters = shipManager.thrusters;
    }

    #region Thruster Use

    public void HandleRotation(float rot)
    {
        //D rotates right, A rotates left
        _rotInput = rot * -1f;
    }

    public void HandleThrust(Vector2 thrust)
    {
        _moveInput = thrust;
    }

    private void ApplyRotation()
    {
        _rb.AddTorque(_rotInput * rotationalThrust, ForceMode2D.Force);
    }

    private void ApplyThrust()
    {
        //Vector2 localForce = Vector2.zero;
        //if (_moveInput.y != 0)
        //{
        //    ThrustAtRotation thrustY = GetThrustAtRotation(
        //        _moveInput.y switch
        //        {
        //            > 0 => TileRotation.Up,
        //            < 0 => TileRotation.Down,
        //            _ => throw new ArgumentOutOfRangeException(
        //                nameof(_moveInput) + ".y",
        //                "Already checked Y could not be 0!"),
        //        });
        //    localForce.y = _moveInput.y * (thrustY.Thrust + coreThrust);
        //}

        //if (_moveInput.x != 0)
        //{
        //    ThrustAtRotation thrustX = GetThrustAtRotation(
        //        _moveInput.x switch
        //        {
        //            > 0 => TileRotation.Right,
        //            < 0 => TileRotation.Left,
        //            _ => throw new ArgumentOutOfRangeException(
        //                nameof(_moveInput) + ".X",
        //                "Already checked X could not be 0!"),
        //        });
        //    localForce.x = _moveInput.x * (thrustX.Thrust + coreThrust);
        //}

        //_rb.AddRelativeForce(localForce, ForceMode2D.Force);

        bool thrustInput = _moveInput.sqrMagnitude > ThrustThresh;
        
        foreach (ThrusterTile thruster in _thrusters)
        {
            Vector2 direction = thruster.LocalThrustDirection;
            bool shouldFire = false;

            //Fire a thruster only if it has input and its direction needs it to fire
            if (thrustInput &&
                Vector2.Dot(direction, _moveInput) > ThrustThresh)
            {
                shouldFire = true;
            }

            if (shouldFire)
            {
                thruster.Move(ref _rb);
            }
        }
        
}

    #endregion

    // #region Thrust Calculations
    //
    // private void UpdateThrusters()
    // {
    //     int directions = new TileRotation().Length();
    //     _directedThrust = new ThrustAtRotation[directions];
    //
    //     for (int i = 0; i < directions; i++)
    //     {
    //         _directedThrust[i] = new ThrustAtRotation
    //         {
    //             Orientation = (TileRotation)i,
    //             Thrust = 0f,
    //         };
    //     }
    //     
    //     foreach (ThrusterTile thruster in _thrusters)
    //     {
    //         _directedThrust[thruster.Rotation.ToInt()].Thrust
    //             += thruster.Thrust;
    //     }
    // }
    //
    // private ThrustAtRotation
    //     GetThrustAtRotation(TileRotation orientation) =>
    //     _directedThrust[(int)orientation];
    //
    // #endregion
}

}