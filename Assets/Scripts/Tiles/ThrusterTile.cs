using Audune.Utils.Dictionary;
using Pulsar.Audio;
using Pulsar.DataClasses;
using Pulsar.Interfaces;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using NaughtyAttributes;

[RequireComponent(typeof(StudioEventEmitter))]
public class ThrusterTile : Tile, IThrustable
{
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

    private StudioEventEmitter _emitter;

    [SerializeField]
    private ParticleSystem ps;

    protected override void Awake()
    {
        base.Awake();
        ps = GetComponentInChildren<ParticleSystem>();
        if (ps && ps.isPlaying)
        {
            ps.Stop();
        }
    }

    protected override void Start()
    {
        base.Start();
        _emitter = AudioManager.Instance.CreateEmitter(
            FmodEvents.Instance.Thrust, gameObject);
    }

    protected override void OnDestroy()
    {
        _emitter.Stop();
        base.OnDestroy();
    }

    public void ActivateThrust(float amount, in Rigidbody2D targetRb)
    {
        StartAudio();
        if (ps)
        {
            ps.Play();
        }
    }

    public void DeactivateThrust()
    {
        StopAudio();
        if (ps)
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

    private void StartAudio()
    {
        _emitter.EventInstance.getPlaybackState(out PLAYBACK_STATE state);
        if (state == PLAYBACK_STATE.STOPPED)
        {
            _emitter.Play();
            // _emitter.EventInstance.start();
        }
    }

    private void StopAudio()
    {
        _emitter.EventInstance.getPlaybackState(out PLAYBACK_STATE state);
        if (state is not (PLAYBACK_STATE.STOPPING
            or PLAYBACK_STATE.STOPPED))
        {
            _emitter.Stop();
            // _emitter.EventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }
}
