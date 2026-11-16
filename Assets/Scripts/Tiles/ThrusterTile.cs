using Audune.Utils.Dictionary;
using FMOD.Studio;
using Pulsar.Audio;
using Pulsar.DataClasses;
using Pulsar.Interfaces;
using UnityEngine;
using FMODUnity;

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

    protected override void Start()
    {
        base.Start();
        _emitter = AudioManager.Instance.CreateEmitter(
            FmodEvents.Instance.Thrust, gameObject);
    }

    public void ActivateThrust(float amount, in Rigidbody2D targetRb)
    {
        StartAudio();
        spriteRenderer.color = Color.red;
    }

    public void DeactivateThrust()
    {
        StopAudio();

        spriteRenderer.color = Color.white;
    }

    public void ActivateRotation(float amount, in Rigidbody2D targetRb)
    {
    }

    public void DeactivateRotation()
    {
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
        Debug.Log(state);
        if (state is not (PLAYBACK_STATE.STOPPING
            or PLAYBACK_STATE.STOPPED))
        {
            _emitter.Stop();
            // _emitter.EventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }
    }

    protected override void OnDestroy()
    {
        _emitter.Stop();
        base.OnDestroy();
    }
}
