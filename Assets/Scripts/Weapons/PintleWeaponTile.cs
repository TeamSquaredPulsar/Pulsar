using UnityEngine;

namespace Pulsar.Weapons.Weapons
{
    public class PintleWeaponTile : WeaponTile
{
    [SerializeField]
    private Transform targetLock;
    
    private bool _isLockedOn;

    private void Awake()
    {
        _isLockedOn = false;
    }
}
}
