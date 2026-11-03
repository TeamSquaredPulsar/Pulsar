using Pulsar.Interactions;
using UnityEngine;

namespace Pulsar.DataClasses
{
[CreateAssetMenu(fileName = "NewSprites", menuName = "Pulsar/Tile Sprites")]
public class TileSpritesSO : ScriptableObject
{
    public Sprite fullHealth;
    public Sprite seventyFivePercent;
    public Sprite fiftyPercent;
    public Sprite twentyFivePercent;
    public Sprite tenPercent;

    public Sprite Get(HealthState state) => state switch
    {
        HealthState.FullHealth             => fullHealth,
        HealthState.LessOrEqualTo75Percent => seventyFivePercent,
        HealthState.LessOrEqualTo50Percent => fiftyPercent,
        HealthState.LessOrEqualTo25Percent => twentyFivePercent,
        HealthState.LessOrEqualTo10Percent => tenPercent,
        _ => fullHealth
    };
}
}