using System.Linq;
using UnityEngine;
using Deadzone.Combat;
using Deadzone.Core;
using Deadzone.Data;
using Deadzone.Player;

namespace Deadzone.Editor
{
    /// <summary>Real-behaviour tests for M1 systems (weapon pool / drops / pickup swap). Run from RealBehaviorTestSuite.</summary>
    public static class M1BehaviorTests
    {
        /// <summary>Drop pool exists, every gun has a model, rarity bias shifts the roll as designed.</summary>
        public static bool Test_WeaponDropPoolAndRarityBias()
        {
            var pool = Resources.LoadAll<WeaponData>("Weapons");
            bool poolOk = pool.Length >= 4 && pool.All(w => w.modelPrefab != null && w.futuristic);
            int highLow = 0, highHigh = 0, commonLow = 0;
            Random.InitState(1234);
            for (int i = 0; i < 2000; i++)
            {
                var lo = WeaponPickup.RollWeapon(0.05f, false);
                var hi = WeaponPickup.RollWeapon(0.95f, true);
                if (lo != null && lo.rarity >= WeaponRarity.Epic) highLow++;
                if (lo != null && lo.rarity == WeaponRarity.Common) commonLow++;
                if (hi != null && hi.rarity >= WeaponRarity.Epic) highHigh++;
            }
            bool biasOk = highHigh > highLow * 1.5f && commonLow > 0;
            bool ok = poolOk && biasOk;
            if (ok) Debug.Log($"[REAL TEST 12 PASSED] Weapon Drop Pool: {pool.Length} futuristic guns with models; Epic+ rolls {highLow}/2000 at low bias vs {highHigh}/2000 at boss bias.");
            else Debug.LogError($"[REAL TEST 12 FAILED] Pool={pool.Length} PoolOk={poolOk} EpicLow={highLow} EpicHigh={highHigh} CommonLow={commonLow}");
            return ok;
        }

        /// <summary>Touching a gun with both slots full swaps the held gun and drops the old one as a pickup.</summary>
        public static bool Test_WeaponPickupSwapDropsOldGun()
        {
            var playerGo = new GameObject("Test_Player_Pickup");
            playerGo.AddComponent<CharacterController>();
            playerGo.AddComponent<Health>();
            playerGo.AddComponent<TeamMember>();
            var pc = playerGo.AddComponent<PlayerController>();
            var a = ScriptableObject.CreateInstance<WeaponData>(); a.weaponName = "Test_A";
            var b = ScriptableObject.CreateInstance<WeaponData>(); b.weaponName = "Test_B";
            var c = ScriptableObject.CreateInstance<WeaponData>(); c.weaponName = "Test_C"; c.rarity = WeaponRarity.Epic;
            pc.PickupWeapon(a);
            pc.PickupWeapon(b);              // fills slot 1, switches to it
            int activeBefore = pc.ActiveSlotIndex;

            var pickup = WeaponPickup.Spawn(c, playerGo.transform.position);
            pickup.GiveTo(pc);

            var held = activeBefore == 0 ? pc.PrimaryWeapon : pc.SecondaryWeapon;
            var dropped = Object.FindObjectsByType<WeaponPickup>(FindObjectsInactive.Include).FirstOrDefault(p => p.Data == b);
            bool ok = held == c && dropped != null && (pc.PrimaryWeapon == a);
            if (ok) Debug.Log("[REAL TEST 13 PASSED] Weapon Pickup Swap: full loadout swapped the held gun for the Epic pickup and dropped the old gun on the ground.");
            else Debug.LogError($"[REAL TEST 13 FAILED] Held={(held != null ? held.weaponName : "null")} Primary={(pc.PrimaryWeapon != null ? pc.PrimaryWeapon.weaponName : "null")} DroppedOld={dropped != null}");

            foreach (var p in Object.FindObjectsByType<WeaponPickup>(FindObjectsInactive.Include)) Object.DestroyImmediate(p.gameObject);
            Object.DestroyImmediate(playerGo);
            return ok;
        }
    }
}
