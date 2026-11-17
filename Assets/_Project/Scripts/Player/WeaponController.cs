using System;
using System.Collections.Generic;
using UnityEngine;
using Deadzone.Combat;
using Deadzone.Data;

namespace Deadzone.Player
{
    public class WeaponController : MonoBehaviour
    {
        [Header("Loadout Settings")]
        [SerializeField] private List<WeaponData> startingWeapons = new List<WeaponData>();
        [SerializeField] private Transform weaponHolder;
        [SerializeField] private GameObject weaponPrefab;

        private List<Weapon> equippedWeapons = new List<Weapon>();
        private int currentWeaponIndex = 0;

        public event Action<Weapon> OnWeaponSwitched;

        public Weapon CurrentWeapon => (equippedWeapons.Count > 0 && currentWeaponIndex < equippedWeapons.Count) 
            ? equippedWeapons[currentWeaponIndex] 
            : null;

        private void Start()
        {
            InitializeWeapons();
        }

        private void Update()
        {
            HandleWeaponSwitchInput();
        }

        private void InitializeWeapons()
        {
            // If weapon is already assigned in child
            var existingWeapon = GetComponentInChildren<Weapon>();
            if (existingWeapon != null)
            {
                equippedWeapons.Add(existingWeapon);
            }
        }

        private void HandleWeaponSwitchInput()
        {
            if (equippedWeapons.Count <= 1) return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchWeapon(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchWeapon(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchWeapon(2);

            // Scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
            {
                int next = (currentWeaponIndex + 1) % equippedWeapons.Count;
                SwitchWeapon(next);
            }
            else if (scroll < 0f)
            {
                int prev = (currentWeaponIndex - 1 + equippedWeapons.Count) % equippedWeapons.Count;
                SwitchWeapon(prev);
            }
        }

        public void SwitchWeapon(int index)
        {
            if (index < 0 || index >= equippedWeapons.Count || index == currentWeaponIndex) return;

            equippedWeapons[currentWeaponIndex].gameObject.SetActive(false);
            currentWeaponIndex = index;
            equippedWeapons[currentWeaponIndex].gameObject.SetActive(true);

            OnWeaponSwitched?.Invoke(equippedWeapons[currentWeaponIndex]);
        }
    }
}
