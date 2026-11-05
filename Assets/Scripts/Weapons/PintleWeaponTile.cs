using System;
using UnityEngine;

namespace Pulsar.Weapons.Weapons
{
    public class PintleWeaponTile : WeaponTile
{
    [SerializeField]
    private Transform targetLock;

    [SerializeField]
    private CircleCollider2D targetRadius;

    [SerializeField]
    private Transform mountingPoint;

    [SerializeField]
    private float rotSpeed;

    private Transform _activeTarget;
    private bool _isLockedOn;

    private void Awake()
    {
        _isLockedOn = false;
    }

    private void Update()
    {
        if (_isLockedOn && IsAttached)
        {
            TargetLock();
        }
        else
        {
            SearchingForTarget();
        }
    }
    private void TargetLock()
    {
        targetLock.position = _activeTarget.position;
        targetLock.gameObject.SetActive(true);
        
        Vector2 targetLockPos = targetLock.position;
        Vector2 mountingPointPos = mountingPoint.position;
        
        float angle = Mathf.Atan2(targetLockPos.y - mountingPointPos.y,
            targetLockPos.x - mountingPointPos.x) * Mathf.Rad2Deg;
        
        Quaternion targetRotation = Quaternion.Euler(
            new Vector3(0f, 0f, angle + 90));
        
        mountingPoint.rotation = Quaternion.RotateTowards(
            mountingPoint.rotation, targetRotation, rotSpeed);
    }

    private void SearchingForTarget()
    {
        targetLock.gameObject.SetActive(false);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.tag == "Enemy")
        {
            _activeTarget = other.transform;
            _isLockedOn = true;
        }
        else
        {
            _isLockedOn = false;
        }
    }
}
}
