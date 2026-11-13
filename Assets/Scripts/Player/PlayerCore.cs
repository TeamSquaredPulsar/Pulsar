using System;
using Pulsar.Interfaces;
using Pulsar.Weapons;
using UnityEngine;

namespace Pulsar
{
    public class PlayerCore : PintleWeaponTile, IThrustable, IShipCore
    {
        public event Action OnCoreDestroyed;
        
        [field: SerializeField]
        public float Thrust { get; private set; }

        private void Awake()
        {
            DestroyEvent += (_) => OnCoreDestroyed?.Invoke();
        }
        
        public void Activate(float amount, in Rigidbody2D targetRb)
        {
            throw new NotImplementedException();
        }

        public void Deactivate()
        {
            throw new NotImplementedException();
        }

        
    }
}
