using System.Collections;
using System.Collections.Generic;
using Pulsar.DataClasses;
using Pulsar.Utilities;
using Pulsar.Weapons;
using UnityEngine;

public class PintleWeaponTile : AutoWeapon
{
    [Header("Pintle Information")]
    [SerializeField]
    private Transform targetReticle;

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

    
    private string TargetTag => gameObject.CompareTag("Player") ? "Enemy" : "Player";
    
    private readonly HashSet<Transform> _targetsInRange = new HashSet<Transform>();
    private Coroutine _searchCoroutine;
    private WaitForFixedUpdate _searchWait;
    
    private void Awake()
    {
        targetRadius.radius = range;

        targetRadius.enabled = false;
        targetReticle.gameObject.SetActive(false);

        _searchWait = new WaitForFixedUpdate();
    }
     
    #region Targetting
    private IEnumerator SearchForTargets()
    {
        Result<Transform> activeTarget = Result<Transform>.Fail();
        
        // _targetsInRange.Clear();
        yield return new WaitForEndOfFrame();
        while (_searchCoroutine != null) 
        {
            activeTarget = EvaluateTargets(activeTarget.Value);
            targetReticle.gameObject.SetActive(activeTarget.IsSuccess);
            if (activeTarget.IsSuccess)
            {
                MoveReticle(activeTarget.Value);
                EngageTarget();
            }

            yield return _searchWait;
        }
        
        targetReticle.gameObject.SetActive(false);
    }

    private Result<Transform> EvaluateTargets(Transform activeTarget)
    {
        _targetsInRange.RemoveWhere(t => !TargetTag.Equals(t.tag));

        if (_targetsInRange.Count == 0)
        {
            return Result<Transform>.Fail();
        }

        Transform bestTarget = null;

        float targetScore = float.MaxValue;

        Vector2 mountPos = mountingPoint.position;
        Vector2 forwardDir = mountingPoint.up;

        foreach (Transform target in _targetsInRange)
        {
            Vector2 dirToTarget = (Vector2)target.position - mountPos;

            float distance = dirToTarget.magnitude;
            float angleToTarget = Vector2.Angle(forwardDir, dirToTarget);

            float score = distance + (angleToTarget * angleWeight);

            if (target == activeTarget)
            {
                score -= currentTargetBias;
            }

            if (score < targetScore)
            {
                targetScore = score;
                bestTarget = target;
            }
        }

        return (bestTarget) ? Result<Transform>.Pass(bestTarget)
            : Result<Transform>.Fail();
    }
    
    private void EngageTarget()
    {
        Vector2 targetDir = targetReticle.position  - mountingPoint.position;
        
        float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

        Quaternion targetRotation = Quaternion.Euler(
            new Vector3(0f, 0f, angle - 90f));
        
        mountingPoint.rotation = Quaternion.RotateTowards(
            mountingPoint.rotation, targetRotation, rotSpeed 
                                                    * Time.deltaTime);
        
        Fire();
    }

    private void MoveReticle(Transform target)
    {
        targetReticle.position = target.position;
    }
    
    #endregion

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger)
        {
            return;
        }
        if (other.CompareTag(TargetTag))
        {
            _targetsInRange.Add(other.transform);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        _targetsInRange.Remove(other.transform);
    }

    public override void Attach(Vector2Int cell, TileRotation rotation)
    {
        base.Attach(cell, rotation);
        
        _targetsInRange.Clear();

        targetRadius.enabled = true;
        
        _searchCoroutine = StartCoroutine(SearchForTargets());
    }

    public override void Detatch(Vector2? velocity = null, 
                                    float angularVelocity = 0)
    {
        _searchCoroutine = null;
        
        targetRadius.enabled = false;
        
        base.Detatch(velocity, angularVelocity);
    }
}

 