using System.Collections.Generic;
using System.Linq;
using FMODUnity;
using Pulsar.Audio;
using UnityEngine;

namespace Pulsar.Hazards
{
/// <summary>
///     Represents a Projectile
///     Reference: https://www.youtube.com/watch?v=2mThTAhD16M
///     @author Alfredo Luzardo
///     @version 1.1
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(StudioEventEmitter))]
public class ExplosiveProjectile : Projectile
{
    [field: SerializeField]
    [field: Min(0f)]
    [field: Tooltip("Tiles within this distance of the hit point take damage.")]
    public float ExplosionRadius { get; private set; } = 1.5f;

    [SerializeField]
    private ParticleSystem ps;

    [field: SerializeField]
    [field: Range(0f, 1f)]
    // Fraction of dmg dealt at the edge of the radius
    // Basically, this controls how quickly damage drops off with distance
    // Damage goes from 'FullDamage -> EdgeDamageFraction'
    // if its at 1, every tile within the radius takes max damage
    public float EdgeDamageFraction { get; private set; } = 0.3f;

    [SerializeField]
    [Min(0f)]
    private float acceleration = 5f;

    [SerializeField]
    [Min(0f)]
    private float maxSpeed = 10f;

    private StudioEventEmitter _emitter;

    private void Awake()
    {
        ps = GetComponentInChildren<ParticleSystem>();
        _emitter = AudioManager.Instance.CreateEmitter(
            FmodEvents.Instance.RocketMove,
            gameObject);
    }

    protected override void Start()
    {
        base.Start();
        ps.Play();
        _emitter.Play();
    }

    private void FixedUpdate()
    {
        Vector2 forward = transform.up;

        float forwardSpeed = Vector2.Dot(rb.linearVelocity, forward);

        if (forwardSpeed >= maxSpeed)
        {
            return;
        }

        float boost = Mathf.Min(acceleration * Time.fixedDeltaTime,
            maxSpeed - forwardSpeed);

        rb.linearVelocity += forward * boost;
    }

    protected override void PlayAudioEffect()
    {
        _emitter.Stop();
        AudioManager.Instance.PlayOneShot(
            FmodEvents.Instance.RocketExplode,
            transform.position);
    }

    public override IEnumerable<(string label, string value)> GetStats() =>
        base.GetStats().Append(("Blast radius", $"{ExplosionRadius:0.#}"));
}
}
