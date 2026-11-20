using UnityEngine;
using Deadzone.Data;

namespace Deadzone.Combat
{
    /// <summary>
    /// Shows the current weapon held in a firing position: the gun sits at chest height in front of
    /// the body, pointing along the aim direction (root forward), and kicks back + climbs on every shot.
    /// AimIK (added to the character's Animator) puts the right hand on the grip, the left hand on the
    /// fore-grip and turns head/upper body toward the target, so the character visibly aims and shoots.
    /// Model prefabs are normalised: barrel along +Z, grip at the origin (see WeaponBuilder).
    /// </summary>
    public class WeaponVisual : MonoBehaviour
    {
        [SerializeField] private Weapon weapon;
        [Tooltip("Gun grip position relative to the chest, in aim space (x right, y up, z forward).")]
        [SerializeField] private Vector3 socketOffset = new Vector3(0.14f, -0.12f, 0.30f);
        [SerializeField] private float fallbackChestHeight = 0.45f; // above the root pivot if no chest bone

        private Animator anim;
        private Transform chest;
        private GameObject current;
        private WeaponData shownData;
        private float gunLength = 0.6f;
        private float recoil, climb;

        public bool HasGun => current != null && current.activeInHierarchy;
        public Vector3 GripPosition => current != null ? current.transform.position : transform.position;
        public Vector3 ForeGripPosition => current != null ? current.transform.position + current.transform.forward * gunLength * 0.55f + current.transform.up * 0.02f : transform.position;
        public Quaternion GunRotation => current != null ? current.transform.rotation : transform.rotation;
        public Vector3 AimPoint => transform.position + transform.forward * 12f + Vector3.up * 0.4f;
        public bool IsPistol => shownData != null && shownData.weaponType == WeaponType.Pistol;

        private void Awake()
        {
            if (weapon == null) weapon = GetComponentInChildren<Weapon>();
        }

        private void OnEnable()
        {
            if (weapon != null) { weapon.OnWeaponDataChanged += Show; weapon.OnFired += Kick; }
        }

        private void OnDisable()
        {
            if (weapon != null) { weapon.OnWeaponDataChanged -= Show; weapon.OnFired -= Kick; }
            if (current != null) current.SetActive(false);
        }

        private void Start()
        {
            anim = GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman)
            {
                chest = anim.GetBoneTransform(HumanBodyBones.UpperChest);
                if (chest == null) chest = anim.GetBoneTransform(HumanBodyBones.Chest);
                if (anim.GetComponent<AimIK>() == null) anim.gameObject.AddComponent<AimIK>().Bind(this);
            }
            if (weapon != null) Show(weapon.Data);
        }

        private void Show(WeaponData data)
        {
            if (data == shownData && current != null) return;
            shownData = data;
            if (current != null) Destroy(current);
            if (data == null || data.modelPrefab == null) return;
            current = Instantiate(data.modelPrefab, transform);
            current.name = "HeldWeapon";
            foreach (var c in current.GetComponentsInChildren<Collider>()) Destroy(c);
            var b = new Bounds(current.transform.position, Vector3.zero);
            foreach (var r in current.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                b.Encapsulate(r.bounds);
            }
            gunLength = Mathf.Max(0.3f, b.size.z);
        }

        private void Kick()
        {
            float k = shownData != null ? Mathf.Clamp(shownData.cameraShakeMagnitude, 0.05f, 0.4f) : 0.12f;
            recoil = Mathf.Min(recoil + 0.05f + k * 0.25f, 0.16f);
            climb = Mathf.Min(climb + 3f + k * 18f, 14f);
        }

        private Deadzone.Core.Health ownerHealth;

        private void LateUpdate()
        {
            if (current == null) return;
            if (ownerHealth == null) ownerHealth = GetComponent<Deadzone.Core.Health>();
            bool down = ownerHealth != null && ownerHealth.IsDead;          // knocked down: holster the gun
            if (current.activeSelf == down) current.SetActive(!down);
            if (down) return;
            float dt = Time.deltaTime;
            recoil = Mathf.MoveTowards(recoil, 0f, dt * 0.9f) * Mathf.Exp(-dt * 14f);
            climb = Mathf.MoveTowards(climb, 0f, dt * 60f) * Mathf.Exp(-dt * 12f);

            Quaternion aim = Quaternion.LookRotation(transform.forward, Vector3.up);
            float chestY = chest != null ? chest.position.y : transform.position.y + fallbackChestHeight;
            Vector3 chestPos = new Vector3(transform.position.x, chestY, transform.position.z);
            Vector3 offset = socketOffset;
            if (IsPistol) offset += new Vector3(-0.08f, 0.04f, 0.12f); // pistols held further out, centred
            Vector3 pos = chestPos + aim * offset - aim * Vector3.forward * recoil;
            current.transform.SetPositionAndRotation(pos, aim * Quaternion.Euler(-climb, 0f, 0f));
        }
    }
}
