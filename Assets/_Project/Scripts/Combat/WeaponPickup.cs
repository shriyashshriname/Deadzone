using UnityEngine;
using Deadzone.Core;
using Deadzone.Data;
using Deadzone.Player;
using Deadzone.UI;

namespace Deadzone.Combat
{
    /// <summary>
    /// A gun lying on the ground (enemy drop or swapped-out weapon). Floats and spins above a
    /// rarity-coloured glow ring with a light beam so it reads from the top-down camera.
    /// Picking up is MANUAL (founder, Build #8): the player presses PICK UP (mobile) / E (desktop)
    /// while standing next to it -> PlayerController.PickupWeapon (fills the empty slot first,
    /// otherwise swaps the held weapon and drops the old one here). Walking over a gun the player
    /// already carries still tops up its ammo automatically. Despawns after a while.
    /// </summary>
    public class WeaponPickup : MonoBehaviour
    {
        private const string PoolPath = "Weapons"; // Resources/Weapons/*.asset

        [SerializeField] private WeaponData data;
        [SerializeField] private float lifetime = 45f;
        [SerializeField] private float pickupDelay = 0.6f;

        private Transform model;
        private float spawnTime;
        private Light glow;

        /// <summary>How close the player must stand to pick a gun up.</summary>
        public const float PickupRange = 2.0f;
        private static readonly System.Collections.Generic.List<WeaponPickup> active = new System.Collections.Generic.List<WeaponPickup>();

        public WeaponData Data => data;
        public bool IsReady => data != null && Time.time - spawnTime >= pickupDelay;

        /// <summary>Closest ready pickup within PickupRange of pos, or null.</summary>
        public static WeaponPickup NearestReady(Vector3 pos)
        {
            WeaponPickup best = null;
            float bestSqr = PickupRange * PickupRange;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                if (p == null) { active.RemoveAt(i); continue; }
                if (!p.IsReady) continue;
                Vector3 d = p.transform.position - pos; d.y = 0f;
                if (d.sqrMagnitude <= bestSqr) { bestSqr = d.sqrMagnitude; best = p; }
            }
            return best;
        }

        private void OnEnable() { if (!active.Contains(this)) active.Add(this); spawnTime = Time.time; }
        private void OnDisable() => active.Remove(this);

        /// <summary>Spawns a pickup at pos. Weighted towards common, rarer guns from tougher enemies.</summary>
        public static WeaponPickup Spawn(WeaponData weapon, Vector3 pos)
        {
            if (weapon == null) return null;
            var go = new GameObject($"Pickup_{weapon.weaponName}");
            go.transform.position = new Vector3(pos.x, 0f, pos.z);
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true; col.radius = 1.1f; col.center = Vector3.up * 0.6f;
            var rb = go.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
            var p = go.AddComponent<WeaponPickup>();
            p.data = weapon;
            return p;
        }

        /// <summary>Random weapon from Resources/Weapons; rarityBias 0 = mostly common, 1 = mostly epic/legendary.</summary>
        public static WeaponData RollWeapon(float rarityBias, bool futuristicOnly)
        {
            var pool = Resources.LoadAll<WeaponData>(PoolPath);
            if (pool == null || pool.Length == 0) return null;
            float total = 0f;
            var weights = new float[pool.Length];
            for (int i = 0; i < pool.Length; i++)
            {
                if (futuristicOnly && !pool[i].futuristic) continue;
                float r = (int)pool[i].rarity / 3f;                   // 0..1
                // low bias: commons dominate (~80% common/rare); high bias: epic/legendary dominate
                weights[i] = Mathf.Lerp((1f - r) * (1f - r) + 0.05f, 0.2f + r * r * 1.5f, Mathf.Clamp01(rarityBias));
                total += weights[i];
            }
            if (total <= 0f) return null;
            float roll = Random.value * total;
            for (int i = 0; i < pool.Length; i++)
            {
                roll -= weights[i];
                if (roll <= 0f && weights[i] > 0f) return pool[i];
            }
            return pool[pool.Length - 1];
        }

        private void Start()
        {
            Color rc = WeaponData.RarityColor(data != null ? data.rarity : WeaponRarity.Common);

            if (data != null && data.modelPrefab != null)
            {
                var m = Instantiate(data.modelPrefab, transform);
                foreach (var c in m.GetComponentsInChildren<Collider>()) Destroy(c);
                m.transform.localScale *= 1.5f; // readable from the gameplay camera
                model = m.transform;
            }

            // Glow ring + soft beam
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = Vector3.up * 0.02f;
            ring.transform.localScale = new Vector3(1.4f, 0.01f, 1.4f);
            // Material comes from Resources so the shader is guaranteed to be in the build.
            var baseMat = Resources.Load<Material>("Materials/M_PickupRing");
            var rr = ring.GetComponent<Renderer>();
            if (baseMat != null)
            {
                rr.sharedMaterial = baseMat;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", new Color(rc.r, rc.g, rc.b, 1f));
                block.SetColor("_EmissionColor", rc * 2f);
                rr.SetPropertyBlock(block);
            }
            ring.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            glow = new GameObject("Glow").AddComponent<Light>();
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = Vector3.up * 1.2f;
            glow.type = LightType.Point; glow.color = rc; glow.range = 4f; glow.intensity = 2f;
            glow.shadows = LightShadows.None;
        }

        private void Update()
        {
            if (model != null)
            {
                model.localPosition = Vector3.up * (0.8f + Mathf.Sin(Time.time * 2.5f) * 0.12f);
                model.Rotate(Vector3.up, 90f * Time.deltaTime, Space.World);
            }
            if (glow != null) glow.intensity = 1.6f + Mathf.Sin(Time.time * 4f) * 0.5f;
            float left = lifetime - (Time.time - spawnTime);
            if (left < 5f && model != null) model.gameObject.SetActive(Mathf.Repeat(Time.time * 4f, 1f) > 0.3f); // blink before vanishing
            if (left <= 0f) Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other) => TryAmmoTopUp(other);
        private void OnTriggerStay(Collider other) => TryAmmoTopUp(other);

        /// <summary>Only automatic action: a gun you already carry is absorbed as ammo. New guns need PICK UP.</summary>
        private void TryAmmoTopUp(Collider other)
        {
            if (!IsReady || !other.CompareTag("Player")) return;
            var pc = other.GetComponent<PlayerController>();
            if (pc == null || (pc.PrimaryWeapon != data && pc.SecondaryWeapon != data)) return;
            GiveTo(pc);
        }

        /// <summary>Gives this weapon to the player (ammo top-up if already carried; swaps and drops the old gun when both slots are full).</summary>
        public void GiveTo(PlayerController pc)
        {
            if (pc == null || data == null) return;
            var other = pc;

            // Already carrying this exact gun: top up ammo instead.
            if (pc.PrimaryWeapon == data || pc.SecondaryWeapon == data)
            {
                var w = other.GetComponentInChildren<Weapon>();
                if (w != null && w.Data == data) w.RefillInstant();
                if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();
                Despawn();
                return;
            }

            WeaponData dropped = (pc.PrimaryWeapon != null && pc.SecondaryWeapon != null)
                ? (pc.ActiveSlotIndex == 0 ? pc.PrimaryWeapon : pc.SecondaryWeapon) : null;
            pc.PickupWeapon(data);
            KillFeedUI.PostMessage($"✦ PICKED UP {data.weaponName.ToUpper()} [{data.rarity.ToString().ToUpper()}]", WeaponData.RarityColor(data.rarity));
            if (AudioManager.Instance != null) AudioManager.Instance.PlayPickup();
            if (dropped != null)
            {
                var back = Spawn(dropped, transform.position + pc.transform.right * 1.2f);
                if (back != null) back.pickupDelay = 2.5f; // don't instantly re-grab what you dropped
            }
            Despawn();
        }

        private void Despawn()
        {
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }
    }
}
