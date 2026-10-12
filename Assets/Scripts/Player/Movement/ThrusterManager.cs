using System;
using System.Collections.Generic;
using UnityEngine;

public struct DirectionThrust
{
    public ThrusterOrientation orientation;
    public float thrust;
}

public class ThrusterManager : MonoBehaviour
{
    [SerializeField]
    private List<ThrusterBlock> thrusters;

    private DirectionThrust[] _directedThrust;

    private void Awake()
    {
        int directions = Enum.GetNames(typeof(ThrusterOrientation)).Length;
        _directedThrust = new DirectionThrust[directions];

        for (int i = 0; i < directions; i++)
        {
            _directedThrust[i] = new DirectionThrust
            {
                orientation = (ThrusterOrientation)i,
                thrust = 0f,
            };
        }

        CalculateThrusters();
    }

    private void CalculateThrusters()
    {
        foreach (ThrusterBlock thruster in thrusters)
        {
            _directedThrust[(int)thruster.GetOrientation()].thrust
                += thruster.GetThrust();
        }
    }

    public DirectionThrust
        GetDirectionThrust(ThrusterOrientation orientation) =>
        _directedThrust[(int)orientation];
}