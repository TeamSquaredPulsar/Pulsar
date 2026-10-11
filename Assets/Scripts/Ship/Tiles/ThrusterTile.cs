using UnityEngine;

namespace Pulsar.Ship
{
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
    }
}
