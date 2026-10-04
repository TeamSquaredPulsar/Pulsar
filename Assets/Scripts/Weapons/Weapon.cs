using UnityEngine;

/// <summary>
/// Represents a weapon
///
/// Reference: https://www.youtube.com/watch?v=2mThTAhD16M
///
/// @author Alfredo Luzardo
/// @version 1.1
/// </summary>
public class Weapon : MonoBehaviour
{
    [SerializeField]
    protected WeaponStrategy weaponStrategy;

    [SerializeField]
    protected Transform firePoint;

    /// <summary>
    /// Setter for weapon strategy
    /// </summary>
    /// <param name="strat"></param>
    private void SetWeaponStrategy(WeaponStrategy strat)
    {
        weaponStrategy = strat;
    }
}