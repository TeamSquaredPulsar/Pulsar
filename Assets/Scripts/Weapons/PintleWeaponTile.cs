using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

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

    [SerializeField]
    private float searchInterval = 0.15f;

    private Transform _activeTarget;
    private bool _isLockedOn;
    
    private readonly HashSet<Transform> _targetsInRange = new HashSet<Transform>();
    private Coroutine _searchCoroutine;
    private WaitForSeconds _searchWait;
    
    private void Awake()
    {
        _isLockedOn = false;

        targetRadius.radius = range;
        
        _searchWait = new WaitForSeconds(searchInterval);
    }
    
    private void OnEnable()
    {
         _searchCoroutine = StartCoroutine(SearchForTargets());
    }

    private void OnDisable()
    {
        if (_searchCoroutine != null)
        {
            StopCoroutine(_searchCoroutine);
            _searchCoroutine = null;
        }
        ClearLock();
    }

     private void Update()
     {
         if (!IsAttached || !_isLockedOn || _activeTarget == null)
         {
             if (targetLock.gameObject.activeInHierarchy)
             {
                 targetLock.gameObject.SetActive(false);
             }
             return;
         }
         EngageTargetLock();
     }

    private IEnumerator SearchForTargets()
    {
        while (true)
        {
            if (IsAttached)
            {
                EvaluateTargets();
            }
            else
            {
                ClearLock();
            }

            yield return _searchWait;
        }
    }

    private void EvaluateTargets()
    {
        _targetsInRange.RemoveWhere(t => t == null ||
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
    
    private void EngageTargetLock()
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

    private void ClearLock()
    {
        _activeTarget = null;
        _isLockedOn = false;
        if (targetLock != null && targetLock.gameObject.activeSelf)
        {
            targetLock.gameObject.SetActive(false);
        }
    }

    public override void Fire()
    {
        if (!_isLockedOn)
        {
            return;
        }
        base.Fire();
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
        ClearLock();

        if (targetRadius != null)
        {
            //Flicker the collider so it detects anything already in range
            targetRadius.enabled = false;
            targetRadius.enabled = true;
        }
    }
}

 