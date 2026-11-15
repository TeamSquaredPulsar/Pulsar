using System;
using NaughtyAttributes;
using UnityEngine;

namespace Pulsar.Weapons.Strategies
{
/// <summary>
/// Creates a weapon that fires from more than 1 point every time it fires.
/// @author Corey Buchan
/// @version 0.9
/// </summary>
[CreateAssetMenu(fileName = "SpreadShot",
    menuName = "Scriptable Objects/SpreadShot")]
[Serializable]
public class SpreadShot : WeaponStrategy
{
    [SerializeField]
    [Min(1)]
    protected int maxFirePoints = 1;

    public override void Fire(
        Transform[] firePoints,
        Weapon weapon,
        Func<Transform, GameObject> spawnProjectile)
    {
        VerifyFirePoints(firePoints, maxFirePoints);
        foreach (Transform firePoint in firePoints[..maxFirePoints])
        {
            spawnProjectile(firePoint);
        }
    }
}
}
