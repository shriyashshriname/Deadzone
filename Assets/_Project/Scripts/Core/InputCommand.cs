using UnityEngine;

namespace Deadzone.Core
{
    public struct InputCommand
    {
        public Vector3 moveVector;     // Normalized movement vector on XZ plane
        public Vector3 aimVector;      // Normalized aim direction on XZ plane
        public bool fire;             // Primary fire held / active
        public bool dash;             // Dash triggered
        public bool grenade;          // Throw tactical grenade
        public bool turret;           // Deploy sentry turret
        public bool interact;         // Hold interaction (revive, airdrop, substation, extraction)
        public bool reload;           // Reload triggered
        public bool switchWeapon;     // Toggle carried weapon slot
        public int selectSlot;        // Direct slot selection: 0 = primary, 1 = secondary, -1 = no direct selection
    }

    public interface IInputProvider
    {
        InputCommand GetCommand();
    }
}
