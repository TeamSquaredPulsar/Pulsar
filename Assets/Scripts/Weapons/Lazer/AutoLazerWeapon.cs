using UnityEngine;

namespace Pulsar.Weapons.Strategies
{
[RequireComponent(typeof(LazerBeam))]
public class AutoLazerWeapon : AutoWeapon
{
    [SerializeField]
    private LazerBeam lazerBeam;

    protected override bool SnapToTarget => true;

    protected override void Awake()
    {
        base.Awake();
        if (!lazerBeam)
        {
            Debug.LogWarning(
                $"Someone forgot to assign a LazerBeam to {gameObject.name}!");
            lazerBeam = GetComponent<LazerBeam>();
        }

        lazerBeam.Overheated += StartCooldown;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        lazerBeam.Overheated -= StartCooldown;
    }

    public override void Fire()
    {
        if (!CanFire)
        {
            return;
        }

        lazerBeam.Hold();
    }
}
}
