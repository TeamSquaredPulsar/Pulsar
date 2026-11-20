using System;
using UnityEngine;

namespace Pulsar.Settings
{
[Serializable]
public class GameSettings
{
    public static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(2560, 1440),
        new Vector2Int(1920, 1080),
        new Vector2Int(1600, 900),
        new Vector2Int(1280, 720),
    };

    public bool fullscreen = true;
    public Vector2Int resolution = Resolutions[0];
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
        if (Array.IndexOf(Resolutions, resolution) < 0)
        {
            resolution = Resolutions[0];
        }

        masterVolume = NormalizeVolume(masterVolume);
        musicVolume = NormalizeVolume(musicVolume);
        vfxVolume = NormalizeVolume(vfxVolume);
    }

    private static float NormalizeVolume(float value) =>
        float.IsNaN(value) || float.IsInfinity(value)
            ? 1f
            : Math.Max(0f, Math.Min(1f, value));
}
}
