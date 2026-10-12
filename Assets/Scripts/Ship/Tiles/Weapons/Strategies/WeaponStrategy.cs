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
    ///  <param name="weaponTile"></param>
    /// <param name="spawnProjectile"></param>
    public abstract void
        Fire(
            Transform firePoint,
            WeaponTile
                weaponTile,
            Func<Transform, GameObject> spawnProjectile);
}