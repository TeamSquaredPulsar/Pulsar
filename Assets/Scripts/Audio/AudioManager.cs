using Pulsar.DataClasses;
using FMODUnity;
using UnityEngine;

namespace Pulsar.Audio
{

public class AudioManager : Singleton<AudioManager>
{
    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }
}
}
