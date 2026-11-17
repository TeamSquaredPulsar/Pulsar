using UnityEngine;

namespace Pulsar.Interfaces
{
public interface IDamageable
{
    void TakeDamageAt(Vector2 point, float damage);
}
}
