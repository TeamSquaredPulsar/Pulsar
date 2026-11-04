using UnityEngine;

namespace Pulsar.Weapons
{
public interface IWeapon
{
    Collider2D ParentCollider { get; }
    Transform[] FirePoints { get; }
    bool CanFire { get; }
    WeaponStrategy Strategy { get; }

    void Fire();
}
}