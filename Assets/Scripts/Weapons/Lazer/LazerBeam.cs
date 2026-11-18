using System;
using Pulsar.Hazards;
using Pulsar.Interfaces;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Pulsar.Weapons
{
[RequireComponent(typeof(Weapon))]
public class LazerBeam : MonoBehaviour
{
    private const int k_MaxBeamHits = 16;
    private const int k_FirstFirePoint = 0;
    private const float k_MinHeat = 0f;
    private const float k_MaxHeat = 1f;
    private const int firstBend = 1;

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
    [Min(0.1f)]
    private float timeToOverheat = 3f;

    [SerializeField]
    [Min(0.1f)]
    private float timeToCool = 2f;

    [SerializeField]
    [Min(0f)]
    private float overheatLockTime = 2f;

    [SerializeField]
    private Color coolColor = Color.cyan;

    [SerializeField]
    private Color hotColor = Color.red;

    [SerializeField]
    private LineRenderer beam;

    [SerializeField]
    [Min(1)]
    private int segments = 1;

    [SerializeField]
    [Min(0f)]
    private float jitter = 0.15f;

    [SerializeField]
    [Min(0f)]
    private float crackleInterval = 0.05f;

    // k_MaxBeamHits is the number of things the raycast can report back
    // before it starts ignoring things
    private readonly RaycastHit2D[] _hits = new RaycastHit2D[k_MaxBeamHits];

    // Tells which colliders to count, gets set up in awake
    private ContactFilter2D _filter;

    // 0 -> 1 is overheated
    private bool _isFiring;
    private float _lastHoldTime;
    private float _nextCrackleTime;

    private float[] _offsets;
    private float _tickTimer;

    private Weapon _weapon;
    private float Heat { get; set; }

    private Transform FirePoint => _weapon.FirePoints[k_FirstFirePoint];
    private bool IsTriggerHeld => Time.time - _lastHoldTime <= releaseDelay;
    private int PointCount => segments + 1;

    private void Awake()
    {
        _weapon = GetComponent<Weapon>();

        // This is so that it can ignore triggers, like the circles that target
        // for the auto turrets
        _filter = new ContactFilter2D { useTriggers = false, };

        beam.useWorldSpace = true;
        // One more point than segments: start, the bends, and end
        beam.positionCount = PointCount;
        _offsets = new float[PointCount];
        beam.enabled = false;
    }

    private void Update()
    {
        if (!_isFiring)
        {
            Cool();
            return;
        }

        if (!IsTriggerHeld || !_weapon.IsAttached)
        {
            StopBeam();
            return;
        }

        Heat += Time.deltaTime / timeToOverheat;
        if (Heat >= k_MaxHeat)
        {
            Overheat();
            return;
        }

        UpdateBeam(AdvanceTick());
    }

    public event Action<float> Overheated;

    private void StopBeam()
    {
        _isFiring = false;
        beam.enabled = false;
    }

    private void Cool()
    {
        Heat = Mathf.Max(k_MinHeat, Heat - Time.deltaTime / timeToCool);
    }

    private void Overheat()
    {
        StopBeam();
        Heat = k_MinHeat;
        Overheated?.Invoke(overheatLockTime);
    }

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

    private bool TryCastBeam(
        Vector2 origin,
        Vector2 direction,
        out RaycastHit2D closest)
    {
        closest = default(RaycastHit2D);
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

            if (found && !(hit.distance < closest.distance))
            {
                continue;
            }

            closest = hit;
            found = true;
        }

        return found;
    }

    private bool ShouldIgnore(Collider2D other) =>
        other == _weapon.ParentCollider
        || other == _weapon.Coll2D
        || other.GetComponent<Projectile>();

    private void DamageHit(RaycastHit2D hit)
    {
        IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
        target?.TakeDamageAt(hit.point, damagePerTick);
    }

    private void DrawBeam(Vector2 start, Vector2 end)
    {
        ApplyHeatColor();
        UpdateCrackle();
        PlaceBeamPoints(start, end);
        beam.enabled = true;
    }

    private void ApplyHeatColor()
    {
        Color color = Color.Lerp(coolColor, hotColor, Heat);
        beam.startColor = color;
        beam.endColor = color;
    }

    private void UpdateCrackle()
    {
        if (Time.time < _nextCrackleTime)
        {
            return;
        }

        RandomizeBends();
        _nextCrackleTime = Time.time + crackleInterval;
    }

    private void PlaceBeamPoints(Vector2 start, Vector2 end)
    {
        Vector2 sideways = Vector2.Perpendicular(end - start).normalized;

        for (int i = 0; i < PointCount; i++)
        {
            float alongBeam = (float)i / segments;
            Vector2 point = Vector2.Lerp(start, end, alongBeam) +
                            sideways * _offsets[i];
            beam.SetPosition(i, point);
        }
    }

    private void RandomizeBends()
    {
        int lastBend = PointCount - 2;

        for (int i = firstBend; i <= lastBend; i++)
        {
            _offsets[i] = Random.Range(-jitter, jitter);
        }
    }

    private void Crackle()
    {
        for (int i = 1; i < segments; i++)
        {
            _offsets[i] = Random.Range(-jitter, jitter);
        }
    }

    public void Hold()
    {
        if (!_isFiring)
        {
            _isFiring = true;
            _tickTimer = tickInterval;
        }

        _lastHoldTime = Time.time;
    }
}
}
