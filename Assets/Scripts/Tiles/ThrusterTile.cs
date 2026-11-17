using Audune.Utils.Dictionary;
using Pulsar.DataClasses;
using Pulsar.Interfaces;
using UnityEngine;

public class ThrusterTile : Tile, IThrustable
{
    [SerializeField]
    private ParticleSystem ps;

    protected override void Awake()
    {
        base.Awake();
        ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null && ps.isPlaying)
        {
            ps.Stop();
        }
    }

    [field: SerializeField]
    [field: SerializableDictionaryOptions(keyHeader = "Direction",
        valueHeader = "Force")]
    public SerializableDictionary<TileRotation, float> DirectionalThrust
    {
        get;
        private set;
    }

    [field: SerializeField]
    [Min(0f)]
    public float RotationForce { get; private set; }

    public void ActivateThrust(float amount, in Rigidbody2D targetRb)
    {
        if (ps != null)
        {
            ps.Play();
        }
    }

    public void DeactivateThrust()
    {
        if (ps != null)
        {
            ps.Stop();
        }
    }

    public void ActivateRotation(float amount, in Rigidbody2D targetRb)
    {
    }

    public void DeactivateRotation()
    {
    }

    // deactivate effect on detachment
    public override void Detatch(
        Vector2? velocity = null,
        float angularVelocity = 0f)
    {
        DeactivateThrust();
        base.Detatch(velocity, angularVelocity);
    }
}
