using UnityEngine;

public class Thruster : Tile
{
    [SerializeField]
    private float thrust = 0.1f;
    
    // [SerializeField]
    private TileRotationEnum orientation => GetRotationInt(Rotation);
    
    public float GetThrust() => thrust;

    public TileRotationEnum GetOrientation() => orientation;
    
}