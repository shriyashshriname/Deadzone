using UnityEngine;
using Deadzone.Combat;
using Deadzone.Core;
using Deadzone.Data;

namespace Deadzone.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(TeamMember))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float rotationSpeed = 15f;
        [SerializeField] private float gravity = -20f;

        [Header("Hybrid 2-Slot Loadout")]
        [SerializeField] private Weapon equippedWeapon;
        [SerializeField] private WeaponData starterWeaponData;
        private readonly WeaponData[] carriedSlots = new WeaponData[2];
        private int activeSlotIndex = 0;

        private CharacterController controller;
        private Health health;
        private PlayerDash playerDash;
        private PlayerAbilities playerAbilities;
        private float verticalVelocity;
        private IInputProvider inputProvider;

        public static PlayerController Instance { get; private set; }
        public InputCommand CurrentCommand { get; private set; }
        public WeaponData PrimaryWeapon => carriedSlots[0];
        public WeaponData SecondaryWeapon => carriedSlots[1];
        public int ActiveSlotIndex => activeSlotIndex;
        public int CarriedWeaponCount => (carriedSlots[0] != null ? 1 : 0) + (carriedSlots[1] != null ? 1 : 0);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            playerDash = GetComponent<PlayerDash>();
            playerAbilities = GetComponent<PlayerAbilities>();

            TeamMember tm = GetComponent<TeamMember>();
            if (tm != null) tm.TeamId = 1;

            InitializeInput();
            InitializeHybridLoadout();
        }

        private void InitializeInput()
        {
            if (Application.isMobilePlatform || Deadzone.UI.MobileControlsManager.ForceMobileInEditor)
            {
                var mgr = FindFirstObjectByType<Deadzone.UI.MobileControlsManager>();
                if (mgr == null)
                {
                    GameObject go = new GameObject("MobileControlsManager");
                    mgr = go.AddComponent<Deadzone.UI.MobileControlsManager>();
                }
                inputProvider = mgr.GetOrCreateProvider(transform);
            }
            else
            {
                inputProvider = new DesktopInputProvider(transform);
            }
        }

        public void SetInputProvider(IInputProvider provider) => inputProvider = provider;

        private void InitializeHybridLoadout()
        {
            if (!equippedWeapon) equippedWeapon = GetComponentInChildren<Weapon>();
            if (starterWeaponData == null && equippedWeapon != null) starterWeaponData = equippedWeapon.Data;

            // Slot 0: Starter basic weapon; Slot 1: Initially empty (pickup only)
            carriedSlots[0] = starterWeaponData;
            carriedSlots[1] = null;
            activeSlotIndex = 0;

            if (equippedWeapon != null && starterWeaponData != null)
            {
                equippedWeapon.SetWeaponData(starterWeaponData);
            }
        }

        private void Start() => health.OnDeath += HandleDeath;
        private void OnDestroy() { if (health != null) health.OnDeath -= HandleDeath; }

        private void Update()
        {
            if (health != null && health.IsDead) return;
            if (PlayerRevive.Instance != null && PlayerRevive.Instance.IsDowned) return;
            if (Deadzone.UI.HUDController.IsMenuOpen) return;

            if (transform.position.y < -3.0f)
            {
                TriggerFallDeath();
                return;
            }

            InputCommand cmd = inputProvider != null ? inputProvider.GetCommand() : default;
            CurrentCommand = cmd;

            HandleMovement(cmd.moveVector);
            HandleAiming(cmd.aimVector);
            HandleActions(cmd);
        }

        private void TriggerFallDeath()
        {
            if (health != null && !health.IsDead) health.TakeDamage(9999f);
            HandleDeath();
        }

        private void HandleMovement(Vector3 moveVector)
        {
            verticalVelocity = controller.isGrounded ? -0.5f : verticalVelocity + gravity * Time.deltaTime;
            float curSpeed = moveSpeed * (PlayerPerks.Instance != null ? PlayerPerks.Instance.SpeedMultiplier : 1.0f);
            Vector3 movement = (moveVector * curSpeed) + (Vector3.up * verticalVelocity);
            controller.Move(movement * Time.deltaTime);
        }

        private void HandleAiming(Vector3 aimVector)
        {
            if (aimVector.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(aimVector);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
        }

        private void HandleActions(InputCommand cmd)
        {
            if (!equippedWeapon) equippedWeapon = GetComponentInChildren<Weapon>();

            // Primary fire & reload
            if (cmd.fire && equippedWeapon != null) equippedWeapon.TryFire();
            if (cmd.reload && equippedWeapon != null) equippedWeapon.StartReload();

            // Abilities
            if (cmd.dash && playerDash != null) playerDash.TryTriggerDash();
            if (cmd.grenade && playerAbilities != null) playerAbilities.TryThrowGrenade();
            if (cmd.turret && playerAbilities != null) playerAbilities.TryDeployTurret();

            // Hybrid Loadout Switching (B1)
            if (cmd.switchWeapon) SwitchCarriedWeapon();
            if (cmd.selectSlot == 0) SelectSlot(0);
            else if (cmd.selectSlot == 1) SelectSlot(1);
        }

        public void PickupWeapon(WeaponData newWeapon)
        {
            if (newWeapon == null) return;

            if (carriedSlots[0] == null)
            {
                // Empty primary slot
                carriedSlots[0] = newWeapon;
                SelectSlot(0);
            }
            else if (carriedSlots[1] == null)
            {
                // Fill empty secondary slot & switch to it
                carriedSlots[1] = newWeapon;
                SelectSlot(1);
            }
            else
            {
                // Replace currently held carried slot
                carriedSlots[activeSlotIndex] = newWeapon;
                if (equippedWeapon != null) equippedWeapon.SetWeaponData(newWeapon);
            }
        }

        public void SwitchCarriedWeapon()
        {
            if (carriedSlots[1] == null) return; // Only 1 weapon carried; cannot switch
            int next = (activeSlotIndex == 0) ? 1 : 0;
            SelectSlot(next);
        }

        public void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 1) return;
            if (carriedSlots[slotIndex] == null || slotIndex == activeSlotIndex) return;

            activeSlotIndex = slotIndex;
            if (equippedWeapon != null && carriedSlots[activeSlotIndex] != null)
            {
                equippedWeapon.SetWeaponData(carriedSlots[activeSlotIndex]);
            }
        }

        private void HandleDeath()
        {
            if (PlayerRevive.Instance != null && FriendlySquadmate.Instance != null && !FriendlySquadmate.Instance.IsDead && !FriendlySquadmate.Instance.IsDowned)
            {
                return;
            }
            enabled = false;
            if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver();
        }

        public void EquipWeapon(Weapon newWeapon) => equippedWeapon = newWeapon;
    }
}
