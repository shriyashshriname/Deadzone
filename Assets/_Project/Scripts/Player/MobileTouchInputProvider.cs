using UnityEngine;
using Deadzone.Core;
using Deadzone.Enemies;

namespace Deadzone.Player
{
    public enum TouchShootingMode
    {
        ModeA_PushToFire, // Fires when aim stick deflection exceeds ~28%
        ModeB_AimAndFire  // Dedicated fire button required
    }

    public class MobileTouchInputProvider : IInputProvider
    {
        private readonly Transform playerTransform;

        public TouchShootingMode ShootingMode { get; set; } = TouchShootingMode.ModeA_PushToFire;
        public bool EnableAimAssist { get; set; } = true;
        public float AimAssistConeAngle { get; set; } = 12f; // Narrow cone (degrees)

        // Virtual joystick inputs
        public Vector2 MoveJoystickVector { get; set; }
        public Vector2 AimJoystickVector { get; set; }
        public bool IsAimPressed { get; set; }

        // Touch button states
        public bool FireButtonPressed { get; set; }
        public bool DashTriggered { get; set; }
        public bool GrenadeTriggered { get; set; }
        public bool TurretTriggered { get; set; }
        public bool ContextInteractHeld { get; set; }
        public bool ReloadTriggered { get; set; }
        public bool SwitchWeaponTriggered { get; set; }
        public int DirectSlotSelected { get; set; } = -1;

        public MobileTouchInputProvider(Transform player)
        {
            playerTransform = player;
        }

        public InputCommand GetCommand()
        {
            InputCommand cmd = new InputCommand
            {
                selectSlot = DirectSlotSelected
            };
            DirectSlotSelected = -1; // Consume one-shot

            // Movement
            cmd.moveVector = new Vector3(MoveJoystickVector.x, 0f, MoveJoystickVector.y).normalized;

            // Aiming
            Vector3 rawAim = new Vector3(AimJoystickVector.x, 0f, AimJoystickVector.y);
            float deflection = rawAim.magnitude;

            if (deflection > 0.05f)
            {
                Vector3 aimDir = rawAim.normalized;

                // Optional light aim assist (narrow cone guidance, never a hard lock)
                if (EnableAimAssist && playerTransform != null)
                {
                    aimDir = ApplyLightAimAssist(aimDir);
                }

                cmd.aimVector = aimDir;
            }

            // Shooting Modes (B4)
            if (ShootingMode == TouchShootingMode.ModeA_PushToFire)
            {
                // Mode A: fires automatically when aim deflection passes ~28%
                cmd.fire = deflection >= 0.28f;
            }
            else
            {
                // Mode B: requires explicit fire button tap/hold
                cmd.fire = FireButtonPressed;
            }

            // Button actions
            cmd.dash = DashTriggered;
            DashTriggered = false;

            cmd.grenade = GrenadeTriggered;
            GrenadeTriggered = false;

            cmd.turret = TurretTriggered;
            TurretTriggered = false;

            cmd.reload = ReloadTriggered;
            ReloadTriggered = false;

            cmd.switchWeapon = SwitchWeaponTriggered;
            SwitchWeaponTriggered = false;

            cmd.interact = ContextInteractHeld;

            return cmd;
        }

        private Vector3 ApplyLightAimAssist(Vector3 currentAim)
        {
            Vector3 origin = playerTransform.position;
            float closestDistSqr = 225f; // 15m radius squared
            Vector3 bestTargetDir = currentAim;
            bool foundTarget = false;

            // Check against nearby enemies
            Collider[] colliders = Physics.OverlapSphere(origin, 15f);
            for (int i = 0; i < colliders.Length; i++)
            {
                TeamMember tm = colliders[i].GetComponentInParent<TeamMember>() ?? colliders[i].GetComponent<TeamMember>();
                if (tm != null && tm.IsHostileTo(1))
                {
                    Vector3 toTarget = (colliders[i].transform.position - origin);
                    toTarget.y = 0f;
                    float angle = Vector3.Angle(currentAim, toTarget.normalized);
                    if (angle <= AimAssistConeAngle && toTarget.sqrMagnitude < closestDistSqr)
                    {
                        closestDistSqr = toTarget.sqrMagnitude;
                        bestTargetDir = toTarget.normalized;
                        foundTarget = true;
                    }
                }
            }

            if (foundTarget)
            {
                // Softly nudge aim vector by at most 30% toward target (smooth assist, no instant snap)
                return Vector3.Slerp(currentAim, bestTargetDir, 0.30f).normalized;
            }

            return currentAim;
        }
    }
}
