using UnityEngine;

public class ThrusterTile : Tile
{
    public override void SetAttached(ShipManager manager)
    {
        base.SetAttached(manager);
        // register with ship thruster groups
    }

    public override void SetDetached(ShipManager manager)
    {
        base.SetDetached(manager);
        // unregister from ship thruster groups
    }
        
    public void Move(float amount, Rigidbody2D rb)
    {
        Debug.Log($"I want to move {amount}");
        //TODO: Add movement logic here
        // Apply force to rigidbody at the point i exist at
    }
}