using UnityEngine;

namespace Deadzone.Combat
{
    [RequireComponent(typeof(Weapon))]
    public class WeaponRecoil : MonoBehaviour
    {
        [Header("Recoil & Bloom Settings")]
        [SerializeField] private float maxSpreadAngle = 14f;
        [SerializeField] private float spreadIncreasePerShot = 1.2f;
        [SerializeField] private float recoverySpeed = 18f;

        private Weapon weapon;
        private float currentSpreadOffset = 0f;
        private float baseSpread = 2f;

        public float CurrentSpread => baseSpread + currentSpreadOffset;

        private void Awake()
        {
            weapon = GetComponent<Weapon>();
        }

        private void Start()
        {
            if (weapon != null && weapon.Data != null)
            {
                baseSpread = weapon.Data.spreadAngle;
            }
        }

        private void Update()
        {
            // Recover spread when not firing
            if (currentSpreadOffset > 0f)
            {
                currentSpreadOffset = Mathf.Max(0f, currentSpreadOffset - (recoverySpeed * Time.deltaTime));
            }
        }

        public void AddRecoilBloom()
        {
            currentSpreadOffset = Mathf.Min(maxSpreadAngle - baseSpread, currentSpreadOffset + spreadIncreasePerShot);
        }
    }
}
