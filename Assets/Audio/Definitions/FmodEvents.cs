using FMODUnity;
using NaughtyAttributes;
using UnityEngine;

namespace Pulsar.Audio
{
[CreateAssetMenu(fileName = "FmodEvents",
    menuName = "Pulsar/Audio/FmodEvents")]
public class FmodEvents : SoSingleton<FmodEvents>
{
    [field: BoxGroup("Damage")]
    [field: SerializeField]
    public EventReference ShipHit { get; private set; }

    [field: BoxGroup("Damage")]
    [field: SerializeField]
    public EventReference ShipReduceState { get; private set; }

    [field: BoxGroup("Damage")]
    [field: SerializeField]
    public EventReference ShipDestroy { get; private set; }

    [field: BoxGroup("Attack")]
    [field: SerializeField]
    public EventReference ShootLight { get; private set; }

    [field: BoxGroup("Attack")]
    [field: SerializeField]
    public EventReference ShootHeavy { get; private set; }

    [field: BoxGroup("Rocket")]
    [field: SerializeField]
    public EventReference RocketExplode { get; private set; }

    [field: BoxGroup("Rocket")]
    [field: SerializeField]
    public EventReference RocketMove { get; private set; }

    [field: BoxGroup("UI")]
    [field: SerializeField]
    public EventReference ButtonClick { get; private set; }

    [field: BoxGroup("Inputs")]
    [field: SerializeField]
    public EventReference Thrust { get; private set; }

    [field: BoxGroup("Inputs")]
    [field: SerializeField]
    public EventReference Attach { get; private set; }

    [field: BoxGroup("Inputs")]
    [field: SerializeField]
    public EventReference Detach { get; private set; }
}
}
