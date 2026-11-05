using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pulsar.Weapons.Weapons
{
    public class PintleWeaponTile : WeaponTile
{
    [Header("Pintle Information")]
    [SerializeField]
    private Transform targetLock;

    [SerializeField]
    private CircleCollider2D targetRadius;

    [SerializeField]
    private Transform mountingPoint;

    [SerializeField]
    private float rotSpeed;

    [SerializeField]
    [Tooltip("The radius for the targeting collider")]
    private float range;
    
    [Header("Debug")]
    [SerializeField]
    private Transform activeTarget;
    [SerializeField]
    private bool isLockedOn;

    private void Awake()
    {
        isLockedOn = false;
        
        targetRadius.radius = range;
    }

    private void Update()
    {
        if (isLockedOn && IsAttached)
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
        if (activeTarget != null)
        {
            targetLock.position = activeTarget.position;
        }
        targetLock.gameObject.SetActive(true);
        
        Vector2 targetLockPos = targetLock.position;
        Vector2 mountingPointPos = mountingPoint.position;
        
        float angle = Mathf.Atan2(targetLockPos.y - mountingPointPos.y,
            targetLockPos.x - mountingPointPos.x) * Mathf.Rad2Deg;
        
        Quaternion targetRotation = Quaternion.Euler(
            new Vector3(0f, 0f, angle - 90));
        
        mountingPoint.rotation = Quaternion.RotateTowards(
            mountingPoint.rotation, targetRotation, rotSpeed);

        Fire();
    }

    public override void Fire()
    {
        if (!isLockedOn)
        {
            return;
        }
        base.Fire();
    }

    private void SearchingForTarget()
    {
        targetLock.gameObject.SetActive(false);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.tag == "Enemy")
        {
            if (!isLockedOn)
            {
                activeTarget = other.transform;
                isLockedOn = true;
            }
            // if (activeTarget != null)
            // {
            //     float mountX = mountingPoint.position.x;
            //     float mountY = mountingPoint.position.y;
            //     
            //     float newTargetDifX = mountX -
            //                           other.transform.position.x;
            //     float newTargetDifY = mountY -
            //                           other.transform.position.y;
            //
            //     float newTargetDif =
            //         (Mathf.Abs(newTargetDifX) < Mathf.Abs(newTargetDifY))
            //             ? newTargetDifX
            //             : newTargetDifY;
            //     
            //     float oldTargetDifX = mountX -
            //                           activeTarget.position.x;
            //     float oldTargetDifY = mountY -
            //                           activeTarget.position.y;
            //
            //     float oldTargetDif =
            //         (Mathf.Abs(oldTargetDifX) < Mathf.Abs(oldTargetDifY))
            //             ? oldTargetDifX
            //             : oldTargetDifY;
            //     
            //     Debug.Log(newTargetDif);
            //     Debug.Log(oldTargetDifX);
            //     if (oldTargetDif - newTargetDif > -1)
            //     {
            //         activeTarget = other.transform;
            //         Debug.Log("Swapping Targets");
            //     }
            // }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform == activeTarget)
        {
            isLockedOn = false;
        }
    }
}
}
