using System.Collections;
using Pulsar.Hazards;
using UnityEngine;

namespace Pulsar.Weapons.Strategies
{
public class LazerWeapon : ManualWeapon
{
    private const int k_MaxBeamHits = 16;
    private const int k_FirstFirePoint = 0;

    [SerializeField]
    [Min(0f)]
    private float range = 6f;

    [SerializeField]
    [Min(0f)]
    private float damagePerTick = 1f;

    [SerializeField]
    [Min(0.01f)]
    private float tickInterval = 0.1f;

    [SerializeField]
    [Min(0f)]
    private float releaseDelay = 0.1f;

    [SerializeField]
    private LineRenderer beam;

    // k_MaxBeamHits is the number of things the raycast can report back
    // before it starts ignoring things
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[k_MaxBeamHits];
    // Tells which colliders to count, gets set up in awake
    private ContactFilter2D _filter;

    private bool _isFiring;
    private float _lastFireTime;
    private float _tickTimer;

    private Transform FirePoint => FirePoints[k_FirstFirePoint];

    protected override void Awake()
    {
        base.Awake();

        // This is so that it can ignore triggers, like the circles that target
        // for the auto turrets
        _filter = new ContactFilter2D { useTriggers = false };

        beam.useWorldSpace = true;
        // This makes the line straight
        beam.positionCount = 2;
        beam.enabled = false;
    }

    public override void Fire()
    {
        if (!_isFiring)
        {
            _isFiring = true;
            _tickTimer = tickInterval;
        }

        _lastFireTime = Time.time;
    }

    private void StopBeam()
    {
        _isFiring = false;
        foreach (Transform firePoint in FirePoints)
        {
            LineRenderer line = GetBeamLine(firePoint);
            if (line)
            {
                line.enabled = false;
            }
        }
    }

    private bool IsTriggerHeld => Time.time - _lastFireTime <= releaseDelay;

    private void Update()
    {
        if (!_isFiring)
        {
            return;
        }

        if (!IsTriggerHeld || !IsAttached)
        {
            StopBeam();
            return;
        }

        UpdateBeam(AdvanceTick());
    }

    /// <summary>
    /// Returns true once every tickInterval seconds.
    /// </summary>
    private bool AdvanceTick()
    {
        _tickTimer += Time.deltaTime;
        if (_tickTimer < tickInterval)
        {
            return false;
        }

        _tickTimer -= tickInterval;
        return true;
    }

    private void UpdateBeam(bool isTick)
    {
        Vector2 origin = FirePoint.position;
        Vector2 direction = FirePoint.up;
        Vector2 end = origin + direction * range;

        if (TryCastBeam(origin, direction, out RaycastHit2D hit))
        {
            end = hit.point;
            if (isTick)
            {
                DamageHit(hit);
            }
        }

        DrawBeam(origin, end);
    }

    /// <summary>
    /// Casts the beam and returns the closest thing it hits
    /// It ignores the plauers ship and projectiles
    /// </summary>
    private bool TryCastBeam(
        Vector2 origin,
        Vector2 direction,
        out RaycastHit2D closest)
    {
        closest = default;
        bool found = false;

        int count
            = Physics2D.Raycast(origin, direction, _filter, _hits, range);

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = _hits[i];

            if (ShouldIgnore(hit.collider))
            {
                continue;
            }

            if (!found || hit.distance < closest.distance)
            {
                closest = hit;
                found = true;
            }
        }

        return found;
    }

    private bool ShouldIgnore(Collider2D other) =>
        other == ParentCollider
        || other == tileCollider
        || other.GetComponent<Projectile>();

    private void DamageHit(RaycastHit2D hit)
    {
        Tile target = FindClosestTile(hit);
        if (target)
        {
            target.TakeDamage(damagePerTick);
        }
    }

    /// <summary>
    /// Finds the tile nearest the hit point on whatever the beam hit.
    /// A ship's tiles are merged into one composite collider on the ship, so the
    /// ray hits the ship itself; we then pick the tile whose centre is closest.
    /// Also works for loose tiles floating on their own.
    /// </summary>
    private static Tile FindClosestTile(RaycastHit2D hit)
    {
        Vector2 point = hit.point;
        Transform root = hit.rigidbody
            ? hit.rigidbody.transform
            : hit.transform;

        Tile closestTile = null;
        float bestDistance = float.PositiveInfinity;
        foreach (Tile tile in root.GetComponentsInChildren<Tile>())
        {
            float distance =
                ((Vector2)tile.transform.position - point).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                closestTile = tile;
            }
        }

        return closestTile;
    }


    private void DrawBeam(Vector2 start, Vector2 end)
    {
        beam.SetPosition(0, start);
        beam.SetPosition(1, end);
        beam.enabled = true;
    }

    private LineRenderer GetBeamLine(Transform firePoint)
    {
        LineRenderer line = firePoint.GetComponent<LineRenderer>();
        return line ? line : beam;
    }

}
}
