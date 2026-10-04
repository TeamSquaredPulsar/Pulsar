using UnityEngine;

namespace Pulsar.Ship
{
    public class ThrusterTile : Tile
    {
        public override void SetAttached(ShipGrid grid)
        {
            base.SetAttached(grid);
            // register with ship thruster groups
        }

        public override void SetDetached(ShipGrid grid)
        {
            base.SetDetached(grid);
            // unregister from ship thruster groups
        }
    }
}
