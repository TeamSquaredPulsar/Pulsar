using UnityEngine;

public class Thruster : Tile
{
    public void Move(float amount, Rigidbody2D rb)
    {
        Debug.Log($"I want to move {amount}");
        //TODO: Add movement logic here
        // Apply force to rigidbody at the point i exist at
    }
}