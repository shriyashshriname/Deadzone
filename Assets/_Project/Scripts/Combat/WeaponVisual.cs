using UnityEngine;
using Deadzone.Data;

namespace Deadzone.Combat
{
    /// <summary>
    /// Shows the current weapon's model in the character's right hand. The gun follows the hand's
    /// position (so it moves with the run cycle) but always points along the aim direction (root
    /// forward), which keeps it readable from the top-down camera. Swaps model when the Weapon's data
    /// changes (pickups / slot switching). Model prefabs are normalised: barrel along +Z, grip at origin.
    /// </summary>
    public class WeaponVisual : MonoBehaviour
    {
        [SerializeField] private Weapon weapon;
        [SerializeField] private Vector3 handOffset = new Vector3(0f, 0.02f, 0.12f); // in aim space
        [SerializeField] private float fallbackHeight = 1.05f;

        private Transform hand;
        private GameObject current;
        private WeaponData shownData;

        private void Awake()
        {
            if (weapon == null) weapon = GetComponentInChildren<Weapon>();
        }

        private void OnEnable()
        {
            if (weapon != null) weapon.OnWeaponDataChanged += Show;
        }

        private void OnDisable()
        {
            if (weapon != null) weapon.OnWeaponDataChanged -= Show;
        }

        private void Start()
        {
            var anim = GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman) hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
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
            foreach (var r in current.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void LateUpdate()
        {
            if (current == null) return;
            Quaternion aim = Quaternion.LookRotation(transform.forward, Vector3.up);
            Vector3 anchor = hand != null && hand.gameObject.activeInHierarchy
                ? hand.position
                : transform.position + Vector3.up * (fallbackHeight - 1f) + transform.right * 0.3f;
            current.transform.SetPositionAndRotation(anchor + aim * handOffset, aim);
        }
    }
}
