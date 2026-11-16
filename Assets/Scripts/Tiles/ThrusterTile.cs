using Audune.Utils.Dictionary;
using Pulsar.Audio;
using Pulsar.DataClasses;
using Pulsar.Interfaces;
using UnityEngine;
using FMOD.Studio;

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

    private EventInstance thrusterAudio;

    protected override void Start()
    {
        base.Start();
        thrusterAudio
            = AudioManager.Instance.CreateInstance(FmodEvents.Instance.Thrust);
    }

    public void ActivateThrust(float amount, in Rigidbody2D targetRb)
    {
        thrusterAudio.getPlaybackState(out PLAYBACK_STATE state);
        if (state == PLAYBACK_STATE.STOPPED)
        {
            Debug.Log("Starting thruster audio");
            thrusterAudio.start();
        }

        spriteRenderer.color = Color.red;
    }

    public void DeactivateThrust()
    {
        thrusterAudio.getPlaybackState(out PLAYBACK_STATE state);
        Debug.Log($"Playback state: {state}");
        if (state is not (PLAYBACK_STATE.STOPPING or PLAYBACK_STATE.STOPPED))
        {
            Debug.Log("Stopping thruster audio");
            thrusterAudio.stop(STOP_MODE.ALLOWFADEOUT);
        }
        spriteRenderer.color = Color.white;
    }

    public void ActivateRotation(float amount, in Rigidbody2D targetRb)
    {
    }

    public void DeactivateRotation()
    {
    }
}
