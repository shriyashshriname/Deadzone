using UnityEngine;
using Deadzone.Player;
using Deadzone.Combat;
using Deadzone.Core;

namespace Deadzone.UI
{
    public static class MobileContextScanner
    {
        public static bool Poll(PlayerController player, out float progress, out string actionName)
        {
            progress = 0f;
            actionName = "USE";
            if (player == null) return false;

            if (PlayerRevive.Instance != null && PlayerRevive.Instance.IsDowned)
            {
                progress = PlayerRevive.Instance.ReviveProgressRatio;
                actionName = "REVIVE";
                return true;
            }

            var stations = Object.FindObjectsByType<PowerSubstation>();
            for (int i = 0; i < stations.Length; i++)
            {
                if (stations[i].IsPlayerInRange && !stations[i].IsOnline)
                {
                    progress = stations[i].RebootProgressRatio;
                    actionName = "REBOOT";
                    return true;
                }
            }

            var airdrops = Object.FindObjectsByType<SupplyAirdrop>();
            for (int i = 0; i < airdrops.Length; i++)
            {
                if (airdrops[i].IsPlayerInRange && !airdrops[i].IsClaimed)
                {
                    progress = airdrops[i].HoldProgressRatio;
                    actionName = "SUPPLY";
                    return true;
                }
            }

            if (ExtractionZone.Instance != null && ExtractionZone.Instance.IsActive && ExtractionZone.Instance.IsPlayerInside && !ExtractionZone.Instance.IsExtracted)
            {
                progress = Mathf.Clamp01(ExtractionZone.Instance.CurrentHoldTime / ExtractionZone.Instance.RequiredHoldTime);
                actionName = "EVAC";
                return true;
            }

            return false;
        }
    }
}
