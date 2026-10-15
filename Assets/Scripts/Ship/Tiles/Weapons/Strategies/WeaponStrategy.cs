using System;
using UnityEngine;

/// <summary>
/// Represents a WeaponStrategy
///
/// Reference: https://www.youtube.com/watch?v=2mThTAhD16M
///
/// @author Alfredo Luzardo
/// @version 1.1
/// </summary>
[CreateAssetMenu(fileName = "WeaponStrategy",
    menuName = "Scriptable Objects/WeaponStrategy")]
[Serializable]
public abstract class WeaponStrategy : ScriptableObject
{
    /// <summary>
    /// Abstract Fire function
    /// 
    /// </summary>
    /// <param name="firePoints"></param>
    ///  <param name="weapon"></param>
    /// <param name="spawnProjectile"></param>
    public abstract void
        Fire(
            Transform[] firePoints,
            WeaponTile weapon,
            Func<Transform, GameObject> spawnProjectile);

    protected void verifyFirePoints(
        Transform[] firePoints,
        int maxFirePoints)
    {
        if (firePoints == null || firePoints.Length < maxFirePoints)
        {
            throw new ArgumentOutOfRangeException(nameof(firePoints),
                "Fire points must be at least " + maxFirePoints);
        }
    }
}