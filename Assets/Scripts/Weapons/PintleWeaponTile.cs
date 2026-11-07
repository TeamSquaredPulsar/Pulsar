using System.Collections.Generic;
using UnityEngine;

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
    
    [SerializeField]
    [Tooltip("Angle dif penalizes a targets score. " +
             "Higher = prefers enemies in front of the barrel.")]
    private float angleWeight = 0.05f;
    
    [SerializeField]
    [Tooltip("Min dif for a new target to get priority")]
    private float currentTargetBias = 1.5f;

    private Transform _activeTarget;
    private bool _isLockedOn;

    private List<Transform> _targetsInRange = new List<Transform>();

    private void Awake()
    {
        _isLockedOn = false;

        targetRadius.radius = range;
    }

    private void Update()
    {
        EvaluateTargets();
        
        if (_isLockedOn && IsAttached)
        {
            TargetLock();
        }
        else
        {
            SearchingForTarget();
        }
    }

    private void EvaluateTargets()
    {
        _targetsInRange.RemoveAll(t => t == null ||
                                       !t.gameObject.activeInHierarchy);

        if (_targetsInRange.Count == 0)
        {
            _activeTarget = null;
            _isLockedOn = false;
        }

        Transform bestTarget = null;

        float targetScore = float.MaxValue;

        Vector2 mountPos = mountingPoint.position;
        Vector2 forwardDir = mountingPoint.up;

        foreach (Transform target in _targetsInRange)
        {
            Vector2 dirToTarget = (Vector2)target.position - mountPos;
            
            float distance =  dirToTarget.magnitude;
            float angleToTarget = Vector2.Angle(forwardDir, dirToTarget);

            float score = distance + (angleToTarget * angleWeight);

            if (target == _activeTarget)
            {
                score -= currentTargetBias;
            }

            if (score < targetScore)
            {
                targetScore = score;
                bestTarget = target;
            }
        }
        
        _activeTarget = bestTarget;
        _isLockedOn = _activeTarget != null;
}
    
    private void TargetLock()
    {
        if (_activeTarget != null)
        {
            targetLock.position = _activeTarget.position;
        }
        targetLock.gameObject.SetActive(true);
        
        Vector2 targetLockPos = targetLock.position;
        Vector2 mountingPointPos = mountingPoint.position;
        
        float angle = Mathf.Atan2(targetLockPos.y - mountingPointPos.y,
            targetLockPos.x - mountingPointPos.x) * Mathf.Rad2Deg;
        
        Quaternion targetRotation = Quaternion.Euler(
            new Vector3(0f, 0f, angle - 90f));
        
        mountingPoint.rotation = Quaternion.RotateTowards(
            mountingPoint.rotation, targetRotation, rotSpeed 
                                                    * Time.deltaTime);

        Fire();
    }

    public override void Fire()
    {
        if (!_isLockedOn)
        {
            return;
        }
        base.Fire();
    }

    private void SearchingForTarget()
    {
        targetLock.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(TargetTag))
        {
            if (!_targetsInRange.Contains(other.transform))
            {
                _targetsInRange.Add(other.transform);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(TargetTag))
        {
            _targetsInRange.Remove(other.transform);
        }
    }

    public override void SetTargetTag(bool isPlayer)
    {
        base.SetTargetTag(isPlayer);
        
        //Clear enemy target list
        _targetsInRange.Clear();
        _activeTarget = null;
        _isLockedOn = false;

        if (targetRadius != null)
        {
            //Flicker the collider so it detects anything already in range
            targetRadius.enabled = false;
            targetRadius.enabled = true;
        }
    }
}

 