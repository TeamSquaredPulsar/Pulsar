using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private float fireTimer;

    // TODO: Refactor into coroutine/async func
    //   - In order to not need to do this every frame
    //   - Can set bool, run coroutine for n seconds,
    //     then reset the bool
    /// <summary>
    /// Trigger the weapon fire if the button is clicked
    /// </summary>
    private void Update()
    {
        fireTimer += Time.deltaTime;
    }

    public void Fire()
    {
        if (!isActiveAndEnabled ||
            fireTimer < weaponStrategy.fireRate)
        {
            return;
        }

        weaponStrategy.Fire(firePoint, this);
        fireTimer = 0f;
    }
}