/// <summary>
/// Represents a single player weapon
/// - each of the players weapon tiles will likely have this
///
/// Reference: https://www.youtube.com/watch?v=2mThTAhD16M
///
/// @author Alfredo Luzardo
/// @version 1.1
/// </summary>
public class PlayerWeapon : Weapon
{
    public override void Fire()
    {
        if (!isActiveAndEnabled || !CanFire)
        {
            return;
        }

        base.Fire();
    }
}