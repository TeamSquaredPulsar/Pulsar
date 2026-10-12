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
    /// <param name="firePoint"></param>
    ///  <param name="weapon"></param>
    /// <param name="spawnProjectile"></param>
    public abstract void
        Fire(
            Transform firePoint,
            Weapon
                weapon,
            Func<Transform, GameObject> spawnProjectile);
}