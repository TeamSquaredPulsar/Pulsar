using System;
using NaughtyAttributes;
using UnityEngine;

namespace Pulsar
{
[CreateAssetMenu(fileName = "MultiShot", menuName = "Scriptable Objects/MultiShot"),
 Serializable,
]
public class MultiShot : WeaponStrategy
{
    [SerializeField, Min(1),OnValueChanged("SetupFirePoints"),]
    protected int maxFirePoints = 1;

    public override void Fire(Transform[] firePoints, Weapon weapon, Func<Transform, GameObject> spawnProjectile)
    {
        VerifyFirePoints(firePoints, maxFirePoints);
        foreach (Transform firePoint in firePoints[..maxFirePoints])
        {
            spawnProjectile(firePoint);
        }
    }
}
}
