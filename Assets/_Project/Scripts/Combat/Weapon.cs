using System;
using System.Collections;
using UnityEngine;
using Deadzone.Data;
using Deadzone.Camera;
using Deadzone.Core;
using Deadzone.Enemies;
using Deadzone.Player;

namespace Deadzone.Combat
{
    public class Weapon : MonoBehaviour
    {
        [Header("Weapon Config")]
        [SerializeField] private WeaponData data;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject projectilePrefab;

        private float nextFireTime;
        private int currentAmmo;
        private bool isReloading;
        private float reloadFinishTime;
        private Light muzzleLight;

        public event Action<int, int> OnAmmoChanged;
        public event Action OnReloadStarted;
        public event Action OnReloadCompleted;
        public event Action<WeaponData> OnWeaponDataChanged;
        public event Action OnFired;
        private MaterialPropertyBlock tracerBlock;

        public WeaponData Data => data;
        public int CurrentAmmo => currentAmmo;
        public bool IsReloading => isReloading;

        public void SetWeaponData(WeaponData newData)
        {
            if (newData == null) return;
            data = newData;
            currentAmmo = data.magazineCapacity;
            isReloading = false;
            OnAmmoChanged?.Invoke(currentAmmo, data.magazineCapacity);
            OnWeaponDataChanged?.Invoke(data);
        }

        private void Awake()
        {
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<WeaponData>();
                data.weaponName = "Assault Rifle"; data.damagePerBullet = 25f; data.fireRate = 8f;
                data.bulletSpeed = 40f; data.spreadAngle = 2f; data.bulletsPerShot = 1;
                data.magazineCapacity = 30; data.reloadDuration = 1.6f;
            }
            if (projectilePrefab == null)
            {
                projectilePrefab = Resources.Load<GameObject>("Prefabs/Bullet_Prefab");
            }
            currentAmmo = data.magazineCapacity;
        }

        private void Start()
        {
            if (currentAmmo <= 0 && data != null) currentAmmo = data.magazineCapacity;
        }

        private void Update()
        {
            if (isReloading && Time.time >= reloadFinishTime) CompleteReload();
        }

        public bool CanFire() => !isReloading && currentAmmo > 0 && Time.time >= nextFireTime;

        public void TryFire()
        {
            if (!CanFire())
            {
                if (currentAmmo <= 0 && !isReloading) StartReload();
                return;
            }

            float fireRate = data.fireRate * (PlayerPerks.Instance != null ? PlayerPerks.Instance.FireRateMultiplier : 1.0f);
            nextFireTime = Time.time + (1f / fireRate);
            currentAmmo--;
            OnAmmoChanged?.Invoke(currentAmmo, data.magazineCapacity);

            FireBullets();
            OnFired?.Invoke();

            if (data.acousticNoiseRadius > 0f && InfectedSwarmDirector.Instance != null && firePoint != null)
            {
                InfectedSwarmDirector.Instance.AlertEnemiesInRadius(firePoint.position, data.acousticNoiseRadius);
            }

            if (TopDownCamera.Instance != null && data.cameraShakeMagnitude > 0f)
            {
                TopDownCamera.Instance.TriggerShake(data.cameraShakeMagnitude, data.cameraShakeDuration);
            }
        }

        private void FireBullets()
        {
            if (!firePoint) firePoint = transform;
            TriggerMuzzleFlash();

            for (int i = 0; i < data.bulletsPerShot; i++)
            {
                float spread = UnityEngine.Random.Range(-data.spreadAngle, data.spreadAngle);
                Quaternion rotation = firePoint.rotation * Quaternion.Euler(0f, spread, 0f);

                GameObject projObj = projectilePrefab != null ? Instantiate(projectilePrefab, firePoint.position, rotation)
                                                             : GameObject.CreatePrimitive(PrimitiveType.Sphere);
                if (projectilePrefab == null)
                {
                    projObj.transform.position = firePoint.position; projObj.transform.rotation = rotation;
                    projObj.transform.localScale = Vector3.one * 0.3f;
                }

                TeamMember ownerTeam = GetComponentInParent<TeamMember>();
                int team = ownerTeam != null ? ownerTeam.TeamId : 1;
                float perkMult = (team == 1 && PlayerPerks.Instance != null) ? PlayerPerks.Instance.DamageMultiplier : 1.0f;
                float dmg = data.damagePerBullet * perkMult;
                if (projObj.TryGetComponent<Projectile>(out var proj)) proj.Initialize(dmg, data.bulletSpeed, data.penetration, team);
                else projObj.AddComponent<Projectile>().Initialize(dmg, data.bulletSpeed, data.penetration, team);
                TintTracer(projObj);
            }
        }

        private void TintTracer(GameObject projObj)
        {
            if (data.tracerColor.a <= 0f) return;
            if (tracerBlock == null) tracerBlock = new MaterialPropertyBlock();
            foreach (var r in projObj.GetComponentsInChildren<Renderer>())
            {
                if (r is TrailRenderer tr) { tr.startColor = data.tracerColor; tr.endColor = new Color(data.tracerColor.r, data.tracerColor.g, data.tracerColor.b, 0f); continue; }
                r.GetPropertyBlock(tracerBlock);
                tracerBlock.SetColor("_BaseColor", data.tracerColor);
                tracerBlock.SetColor("_EmissionColor", data.tracerColor * 3f);
                r.SetPropertyBlock(tracerBlock);
            }
        }

        private void TriggerMuzzleFlash()
        {
            float intensity = data.weaponType == WeaponType.SniperRifle ? 4.0f : 2.5f;
            float range = data.weaponType == WeaponType.SniperRifle ? 12f : 8f;
            Vector3 flashPos = transform.position + transform.forward * 0.8f + Vector3.up * 0.3f;

            if (MuzzleFlashPool.Instance != null)
            {
                MuzzleFlashPool.Instance.TriggerFlash(flashPos, transform.forward, intensity, range);
            }
        }

        public void StartReload()
        {
            if (isReloading || currentAmmo == data.magazineCapacity) return;
            isReloading = true;
            reloadFinishTime = Time.time + data.reloadDuration;
            OnReloadStarted?.Invoke();
        }

        public void RefillInstant()
        {
            if (data == null) return;
            isReloading = false;
            currentAmmo = data.magazineCapacity;
            OnReloadCompleted?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, data.magazineCapacity);
        }

        private void CompleteReload()
        {
            isReloading = false;
            currentAmmo = data.magazineCapacity;
            OnReloadCompleted?.Invoke();
            OnAmmoChanged?.Invoke(currentAmmo, data.magazineCapacity);
        }
    }
}