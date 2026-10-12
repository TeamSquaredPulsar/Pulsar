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

    private DirectionThrust[] directedThrust;

    private void Awake()
    {
        directedThrust = new DirectionThrust[4];
        
        for (int i = 0; i < 4; i++)
        {
            directedThrust[i] = new DirectionThrust 
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
            directedThrust[(int)thruster.GetOrientation()].thrust
                += thruster.GetThrust();
        }
    }

    public DirectionThrust GetDirectionThrust(ThrusterOrientation orientation)
    {
        return directedThrust[(int)orientation];
    }
    
}