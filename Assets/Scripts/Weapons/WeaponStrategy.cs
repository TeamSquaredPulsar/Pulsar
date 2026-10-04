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
public abstract class WeaponStrategy : ScriptableObject
{
    [SerializeField]
    protected float radius = 1f;

    [SerializeField]
    protected float cooldown = 1f;

    [SerializeField]
    protected float projectileSpeed = 10f;

    [SerializeField]
    protected GameObject projectilePrefab;

    [field: SerializeField]
    public int damage { get; protected set; } = 10;

    [field: SerializeField]
    public float fireRate { get; protected set; } = 0.5f;

    /// <summary>
    /// Abstract Fire function
    ///
    /// </summary>
    /// <param name="firePoint"></param>
    public abstract void Fire(Transform firePoint, Weapon weapon);
}