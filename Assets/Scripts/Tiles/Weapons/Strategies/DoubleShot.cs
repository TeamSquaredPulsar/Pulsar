using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DoubleShot",
    menuName = "Scriptable Objects/DoubleShot")]
public class DoubleShot : WeaponStrategy
{
    private const char k_left = 'L';
    private const char k_right = 'R';
    private const int k_max_firePoints = 2;
    private readonly Stack<char> firePointStack = new();

    public override void Fire(
        Transform[] firePoints,
        Weapon weapon,
        Func<Transform, GameObject> spawnProjectiles)
    {
        if (firePoints == null || firePoints.Length < k_max_firePoints)
        {
            return;
        }

        if (firePointStack.Count == 0)
        {
            firePointStack.Push(k_right);
            firePointStack.Push(k_left);
        }

        char side = firePointStack.Pop();

        Transform selectedFirePoint =
            side == k_left ? firePoints[0] : firePoints[1];

        spawnProjectiles(selectedFirePoint);
    }
}