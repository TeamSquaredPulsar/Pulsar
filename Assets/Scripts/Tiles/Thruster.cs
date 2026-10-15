using UnityEngine;

public class Thruster : Tile
{
    [SerializeField]
    private float thrust = 0.1f;

    [SerializeField]
    private Rotation orientation = Rotation.Up;
    
    public float GetThrust() => thrust;

    public Rotation GetOrientation() => orientation;
    
}