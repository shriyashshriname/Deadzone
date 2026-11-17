using System;
using System.Collections.Generic;
using UnityEngine;
using Deadzone.Data;
using Deadzone.Combat;

namespace Deadzone.Player
{
    public class AmmoInventory : MonoBehaviour
    {
        public static AmmoInventory Instance { get; private set; }

        [Header("Starting Reserves")]
        [SerializeField] private int rifleAmmo = 120;
        [SerializeField] private int shotgunAmmo = 32;
        [SerializeField] private int smgAmmo = 200;

        public event Action<WeaponType, int> OnReserveAmmoChanged;

        private Dictionary<WeaponType, int> reserves = new Dictionary<WeaponType, int>();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            reserves[WeaponType.AssaultRifle] = rifleAmmo;
            reserves[WeaponType.Shotgun] = shotgunAmmo;
            reserves[WeaponType.SubmachineGun] = smgAmmo;
        }

        public int GetReserve(WeaponType type)
        {
            return reserves.ContainsKey(type) ? reserves[type] : 0;
        }

        public void AddAmmo(WeaponType type, int amount)
        {
            if (!reserves.ContainsKey(type)) reserves[type] = 0;
            reserves[type] += amount;
            OnReserveAmmoChanged?.Invoke(type, reserves[type]);
        }

        public int ConsumeAmmo(WeaponType type, int needed)
        {
            if (!reserves.ContainsKey(type)) return 0;

            int available = reserves[type];
            int taken = Mathf.Min(available, needed);
            reserves[type] -= taken;
            OnReserveAmmoChanged?.Invoke(type, reserves[type]);
            return taken;
        }
    }
}
