using System;
using System.Collections.Generic;
using UnityEngine;

public struct DirectionThrust
{
    public Rotation Orientation;
    public float Thrust;
}

public class ThrusterManager : MonoBehaviour
{
    [SerializeField]
    private List<ThrusterBlock> thrusters;

    private DirectionThrust[] _directedThrust;

    private void Awake()
    {
        int directions = Enum.GetNames(typeof(Rotation)).Length;
        _directedThrust = new DirectionThrust[directions];

        for (int i = 0; i < directions; i++)
        {
            _directedThrust[i] = new DirectionThrust
            {
                Orientation = (Rotation)i,
                Thrust = 0f,
            };
        }

        CalculateThrusters();
    }

    private void CalculateThrusters()
    {
        foreach (ThrusterBlock thruster in thrusters)
        {
            _directedThrust[(int)thruster.GetOrientation()].Thrust
                += thruster.GetThrust();
        }
    }

    public DirectionThrust
        GetDirectionThrust(Rotation orientation) =>
        _directedThrust[(int)orientation];
}