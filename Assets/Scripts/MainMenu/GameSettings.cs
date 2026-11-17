using System;
using UnityEngine;

namespace Pulsar.UI
{
[Serializable]
public class GameSettings
{
    public bool fullscreen = true;
    public Vector2Int resolution = SettingsScreen.Resolutions[0];
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float vfxVolume = 1f;

    public bool Windowed
    {
        get => !fullscreen;
        set => fullscreen = !value;
    }

    public void Normalize()
    {
        if (Array.IndexOf(SettingsScreen.Resolutions, resolution) < 0)
        {
            resolution = SettingsScreen.Resolutions[0];
        }

        masterVolume = NormalizeVolume(masterVolume);
        musicVolume = NormalizeVolume(musicVolume);
        vfxVolume = NormalizeVolume(vfxVolume);
    }

    private static float NormalizeVolume(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value)
            ? 1f
            : Math.Max(0f, Math.Min(1f, value));
    }
}
}
