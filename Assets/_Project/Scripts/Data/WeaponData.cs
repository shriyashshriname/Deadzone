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

    public enum WeaponRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
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

        [Header("Presentation & Loot (M1)")]
        [Tooltip("Model shown in the player's hand and on the ground as a pickup")]
        public GameObject modelPrefab;
        public WeaponRarity rarity = WeaponRarity.Common;
        [Tooltip("Futuristic energy weapon (glowing tracers, drops from sci-fi enemies)")]
        public bool futuristic;
        [Tooltip("Tracer / projectile tint; alpha 0 = keep the default bullet look")]
        public Color tracerColor = new Color(0, 0, 0, 0);

        public static Color RarityColor(WeaponRarity r) => r switch
        {
            WeaponRarity.Rare => new Color(0.13f, 0.83f, 0.93f),
            WeaponRarity.Epic => new Color(0.66f, 0.33f, 0.97f),
            WeaponRarity.Legendary => new Color(0.96f, 0.65f, 0.14f),
            _ => new Color(0.85f, 0.85f, 0.8f),
        };

        [Header("Acoustics & Stealth (D-010)")]
        [Tooltip("Acoustic broadcast radius in meters that alerts nearby infected upon firing")]
        public float acousticNoiseRadius = 24f;
    }
}
