using System;
using FMODUnity;
using UnityEngine;

namespace Pulsar.Audio
{
[CreateAssetMenu(fileName = "AudioManager",
    menuName = "Pulsar/Audio Manager")]
[Serializable]
public class AudioManager : SoSingleton<AudioManager>
{
    public void PlayOneShot(EventReference sound, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(sound, worldPos);
    }
}
}
