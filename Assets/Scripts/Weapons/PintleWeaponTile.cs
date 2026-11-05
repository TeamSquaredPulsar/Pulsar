using UnityEngine;

namespace Pulsar.Weapons.Weapons
{
    public class PintleWeaponTile : WeaponTile
{
    [SerializeField]
    private Transform targetLock;

    [SerializeField]
    private CircleCollider2D targetRadius;
    
    private bool _isLockedOn;

    private void Awake()
    {
        _isLockedOn = false;
    }
    
    
}
}
