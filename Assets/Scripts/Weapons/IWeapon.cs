using FMODUnity;
using Pulsar.Weapons.Strategies;
using UnityEngine;

namespace Pulsar.Weapons
{
/// <summary>
/// Common interface for all weapons to have.
/// @author Corey Buchan
/// @version 0.9
/// </summary>
public interface IWeapon
{
    Collider2D ParentCollider { get; }
    Transform[] FirePoints { get; }
    bool CanFire { get; }
    WeaponStrategy Strategy { get; }

    void Fire();
}
}
