using UnityEngine;

namespace Deadzone.Data
{
    public enum WeaponType
    {
        AssaultRifle,
        Shotgun,
        SubmachineGun,
        Pistol,
        SniperRifle
    }

    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Deadzone/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("General")]
        public string weaponName = "Standard AR";
        public WeaponType weaponType = WeaponType.AssaultRifle;

        [Header("Firing Parameters")]
        [Tooltip("Damage delivered per bullet")]
        public float damagePerBullet = 25f;

        [Tooltip("Number of enemies bullet pierces before expiring (1 = standard, 3 = sniper)")]
        public int penetration = 1;

        [Tooltip("Rounds fired per second")]
        public float fireRate = 8f;

        [Tooltip("Speed of the projectile")]
        public float bulletSpeed = 40f;

        [Tooltip("Spread angle in degrees")]
        public float spreadAngle = 2f;

        [Tooltip("Number of bullets spawned per shot (e.g. 6 for shotgun)")]
        public int bulletsPerShot = 1;

        [Header("Magazine & Ammo")]
        public int magazineCapacity = 30;
        public float reloadDuration = 1.8f;

        [Header("Juice & Feel")]
        public float cameraShakeMagnitude = 0.15f;
        public float cameraShakeDuration = 0.08f;

        [Header("Acoustics & Stealth (D-010)")]
        [Tooltip("Acoustic broadcast radius in meters that alerts nearby infected upon firing")]
        public float acousticNoiseRadius = 24f;
    }
}
