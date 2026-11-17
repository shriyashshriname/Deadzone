using System;
using UnityEngine;
using Deadzone.Combat;
using Deadzone.Core;

namespace Deadzone.Player
{
    public class PlayerAbilities : MonoBehaviour
    {
        [Header("Grenade Ability")]
        [SerializeField] private GameObject grenadePrefab;
        [SerializeField] private float grenadeCooldown = 8.0f;
        [SerializeField] private float throwForce = 15f;

        [Header("Turret Ability")]
        [SerializeField] private GameObject turretPrefab;
        [SerializeField] private float turretCooldown = 25.0f;

        private float nextGrenadeTime;
        private float nextTurretTime;

        public event Action<float> OnGrenadeCooldownChanged;
        public event Action<float> OnTurretCooldownChanged;

        public float GrenadeCooldownRatio => Mathf.Clamp01((nextGrenadeTime - Time.time) / grenadeCooldown);
        public float TurretCooldownRatio => Mathf.Clamp01((nextTurretTime - Time.time) / turretCooldown);
        public float GrenadeRemainingTime => Mathf.Max(0f, nextGrenadeTime - Time.time);
        public float TurretRemainingTime => Mathf.Max(0f, nextTurretTime - Time.time);
        public bool IsGrenadeReady => Time.time >= nextGrenadeTime;
        public bool IsTurretReady => Time.time >= nextTurretTime;

        public static PlayerAbilities Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q) && Time.time >= nextGrenadeTime)
            {
                ThrowGrenade();
            }

            if (Input.GetKeyDown(KeyCode.F) && Time.time >= nextTurretTime)
            {
                DeployTurret();
            }

            float gRem = GrenadeCooldownRatio;
            OnGrenadeCooldownChanged?.Invoke(gRem);

            float tRem = TurretCooldownRatio;
            OnTurretCooldownChanged?.Invoke(tRem);
        }

        public void TryThrowGrenade()
        {
            if (Time.time >= nextGrenadeTime) ThrowGrenade();
        }

        public void TryDeployTurret()
        {
            if (Time.time >= nextTurretTime) DeployTurret();
        }

        private void ThrowGrenade()
        {
            nextGrenadeTime = Time.time + grenadeCooldown;

            Vector3 spawnPos = transform.position + transform.forward * 1.2f + Vector3.up * 1f;
            GameObject grenadeObj;
            if (grenadePrefab != null)
            {
                grenadeObj = Instantiate(grenadePrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                grenadeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                grenadeObj.name = "TossedGrenade";
                grenadeObj.transform.position = spawnPos;
                grenadeObj.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
                grenadeObj.AddComponent<Grenade>();
            }

            var rb = grenadeObj.GetComponent<Rigidbody>() ?? grenadeObj.AddComponent<Rigidbody>();
            rb.linearVelocity = (transform.forward * throwForce) + Vector3.up * 4f;

            if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot(0.5f);
        }

        private void DeployTurret()
        {
            nextTurretTime = Time.time + turretCooldown;

            if (turretPrefab != null)
            {
                Vector3 spawnPos = transform.position + transform.forward * 1.5f;
                Instantiate(turretPrefab, spawnPos, transform.rotation);
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayReload(0.9f);
        }
    }
}
