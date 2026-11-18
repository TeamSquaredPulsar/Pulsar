namespace Pulsar.Weapons.Strategies
{
public class AutoLazerWeapon : AutoWeapon
{
    private LazerBeam _beam;

    protected override bool SnapToTarget => true;

    protected override void Awake()
    {
        base.Awake();
        _beam = GetComponent<LazerBeam>();
        _beam.Overheated += StartCooldown;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _beam.Overheated -= StartCooldown;
    }

    public override void Fire()
    {
        if (!CanFire)
        {
            return;
        }

        _beam.Hold();
    }
}
}
