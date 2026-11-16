using System;
using FMODUnity;
using FMOD.Studio;
using UnityEngine;

namespace Pulsar.Audio
{
[CreateAssetMenu(fileName = "AudioManager",
    menuName = "Pulsar/Audio/Audio Manager")]
[Serializable]
public class AudioManager : SoSingleton<AudioManager>
{
    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }

    public EventInstance CreateInstance(EventReference eventReference)
    {
        EventInstance eventInstance
            = RuntimeManager.CreateInstance(eventReference);
        return eventInstance;
    }
}
}
