using UnityEngine;
using Deadzone.Core;
using Deadzone.Combat;
using Deadzone.Player;

namespace Deadzone.UI
{
    public class HUDController : MonoBehaviour
    {
        private Health playerHealth;
        private Weapon currentWeapon;
        private PlayerAbilities playerAbilities;
        private bool isShopOpen = false;
        private bool isPaused = false;

        private void Start() => FindPlayerReferences();

        private void FindPlayerReferences()
        {
            if (PlayerController.Instance != null)
            {
                playerHealth = PlayerController.Instance.GetComponent<Health>();
                currentWeapon = PlayerController.Instance.GetComponentInChildren<Weapon>();
                playerAbilities = PlayerController.Instance.GetComponent<PlayerAbilities>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Tab)) isShopOpen = !isShopOpen;
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) TogglePause();
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
        }

        private void OnGUI()
        {
            if (playerHealth == null || currentWeapon == null || playerAbilities == null) FindPlayerReferences();
            Color oldColor = GUI.color;

            // 1. Health Bar & Shop Toggle (Top Left)
            if (playerHealth != null)
            {
                float hpPct = Mathf.Clamp01(playerHealth.CurrentHealth / playerHealth.MaxHealth);
                GUI.Box(new Rect(20, 20, 220, 32), "");
                GUI.color = hpPct > 0.35f ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.95f, 0.2f, 0.2f);
                GUI.Box(new Rect(22, 22, 216 * hpPct, 28), "");
                GUI.color = Color.white;
                GUI.Label(new Rect(30, 26, 200, 25), $"HEALTH: {Mathf.CeilToInt(playerHealth.CurrentHealth)} / {playerHealth.MaxHealth}");

                if (GUI.Button(new Rect(20, 56, 140, 26), isShopOpen ? "CLOSE SHOP [B]" : "UPGRADE SHOP [B]")) isShopOpen = !isShopOpen;
            }

            // 2. Sector Mission Objectives (Top Left)
            int onlineStations = PowerSubstation.TotalSubstationsOnline;
            int totalStations = Mathf.Max(2, PowerSubstation.TotalSubstationsCount);
            GUI.Box(new Rect(20, 86, 220, 64), "");
            GUI.color = new Color(0.2f, 0.9f, 1f);
            GUI.Label(new Rect(28, 89, 204, 18), "=== SECTOR OBJECTIVES ===");
            GUI.color = onlineStations >= totalStations ? new Color(0.2f, 1f, 0.4f) : Color.white;
            GUI.Label(new Rect(28, 108, 204, 18), $"GRID POWER: {onlineStations}/{totalStations} ONLINE");
            bool evacReady = ExtractionZone.Instance != null && ExtractionZone.Instance.IsActive;
            GUI.color = evacReady ? new Color(0.2f, 1f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(28, 127, 204, 18), evacReady ? "EVAC CHOPPER: LANDED" : "EVAC CHOPPER: STANDBY");
            GUI.color = Color.white;

            // 3. Tactical Abilities
            if (playerAbilities != null)
            {
                bool isMobile = MobileControlsManager.IsMobileActive;
                float abY = isMobile ? 156f : Screen.height - 90f;
                GUI.Box(new Rect(20, abY, 220, 68), "");
                bool gReady = playerAbilities.IsGrenadeReady;
                GUI.color = gReady ? new Color(1f, 0.7f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
                string gText = gReady ? "[Q] GRENADE: READY" : $"[Q] GRENADE: {playerAbilities.GrenadeRemainingTime:F1}s";
                GUI.Label(new Rect(30, abY + 6, 200, 24), gText);

                bool tReady = playerAbilities.IsTurretReady;
                GUI.color = tReady ? new Color(0.2f, 0.9f, 1f) : new Color(0.6f, 0.6f, 0.6f);
                string tText = tReady ? "[F] TURRET: READY" : $"[F] TURRET: {playerAbilities.TurretRemainingTime:F1}s";
                GUI.Label(new Rect(30, abY + 32, 200, 24), tText);
                GUI.color = Color.white;
            }

            // 4. Ammo & Active Weapon
            if (currentWeapon != null && currentWeapon.Data != null)
            {
                bool isMobile = MobileControlsManager.IsMobileActive;
                float ammoY = isMobile ? 56f : Screen.height - 85f;
                string weaponName = currentWeapon.Data.weaponName.ToUpper();
                string ammoStr = currentWeapon.IsReloading ? "RELOADING..." : $"{currentWeapon.CurrentAmmo} / {currentWeapon.Data.magazineCapacity}";
                GUI.Box(new Rect(Screen.width - 240, ammoY, 220, 65), "");
                GUI.Label(new Rect(Screen.width - 230, ammoY + 5, 200, 25), $"WEAPON: {weaponName}");
                GUI.Label(new Rect(Screen.width - 230, ammoY + 30, 200, 25), $"AMMO: {ammoStr}  [R]");
            }

            // 5. Hybrid Loadout Bar (Bottom Center - B1)
            var pc = PlayerController.Instance;
            string slot1Name = pc != null && pc.PrimaryWeapon != null ? pc.PrimaryWeapon.weaponName : "BASIC RIFLE";
            string slot2Name = pc != null && pc.SecondaryWeapon != null ? pc.SecondaryWeapon.weaponName : "EMPTY (PICKUP)";
            int activeSlot = pc != null ? pc.ActiveSlotIndex : 0;
            string slot1Display = activeSlot == 0 ? $"★ [1] {slot1Name}" : $"[1] {slot1Name}";
            string slot2Display = activeSlot == 1 ? $"★ [2] {slot2Name}" : $"[2] {slot2Name}";

            GUI.Box(new Rect(Screen.width / 2 - 220, Screen.height - 45, 440, 32), "");
            GUI.Label(new Rect(Screen.width / 2 - 210, Screen.height - 40, 420, 25), $"{slot1Display}   |   {slot2Display}  [Q TO SWAP]");

            // 6. Wave, Kills & Credits (Top Right - B2)
            int kills = GameManager.Instance != null ? GameManager.Instance.KillCount : 0;
            int credits = GameManager.Instance != null ? GameManager.Instance.CreditsEarned : 0;
            int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveNumber : 1;
            int hostiles = WaveManager.Instance != null ? WaveManager.Instance.TotalRemainingHostiles : 0;
            bool isIntermission = WaveManager.Instance != null && WaveManager.Instance.IsIntermission;

            GUI.Box(new Rect(Screen.width - 220, 20, 200, 78), "");
            GUI.Label(new Rect(Screen.width - 210, 24, 180, 20), $"WAVE: {wave}   |   CREDITS: {credits}");
            GUI.Label(new Rect(Screen.width - 210, 46, 180, 20), $"KILLS: {kills}");
            GUI.color = hostiles > 0 ? new Color(1f, 0.4f, 0.4f) : Color.white;
            GUI.Label(new Rect(Screen.width - 210, 68, 180, 20), isIntermission ? "CLEARED! REGROUP" : $"HOSTILES: {hostiles}");
            GUI.color = Color.white;

            // 7. Evac / Wave Banner (Top Center)
            if (ExtractionZone.Instance != null && ExtractionZone.Instance.IsActive && !ExtractionZone.Instance.IsExtracted)
            {
                GUI.Box(new Rect(Screen.width / 2 - 210, 20, 420, 34), "");
                GUI.color = ExtractionZone.Instance.IsPlayerInside ? new Color(0.2f, 1f, 0.4f) : new Color(0.2f, 0.9f, 1f);
                string evacText = ExtractionZone.Instance.IsPlayerInside ? $"EXTRACTING: {ExtractionZone.Instance.CurrentHoldTime:F1}s / {ExtractionZone.Instance.RequiredHoldTime:F1}s (HOLD!)" : "EVAC SHIP AT CENTER! HOLD 5s TO EXTRACT";
                GUI.Label(new Rect(Screen.width / 2 - 200, 26, 400, 24), evacText);
                GUI.color = Color.white;
            }
            else if (WaveManager.Instance != null && isIntermission)
            {
                GUI.Box(new Rect(Screen.width / 2 - 180, 20, 360, 36), "");
                GUI.color = new Color(0.2f, 1f, 0.4f);
                GUI.Label(new Rect(Screen.width / 2 - 170, 26, 340, 25), $"WAVE CLEARED! NEXT HORDE IN: {WaveManager.Instance.IntermissionTimer:F1}s");
                GUI.color = Color.white;
            }

            // 8. Modals
            if (isShopOpen) DrawUpgradeShop(credits);
            if (isPaused) DrawPauseMenu();

            bool isGameOver = (playerHealth != null && playerHealth.IsDead) || (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver);
            bool isVictory = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Victory;

            if (isGameOver) DrawEndModal("=== MISSION FAILED ===", "OPERATOR KIA / OUT OF BOUNDS", new Color(1f, 0.25f, 0.25f), wave, kills, credits);
            else if (isVictory) DrawEndModal("=== EXTRACTION SUCCESSFUL ===", "SECTOR PURGED | OPERATOR EVACUATED", new Color(0.2f, 1f, 0.4f), wave, kills, credits);

            GUI.color = oldColor;
        }

        private void DrawPauseMenu()
        {
            float pW = 380, pH = 220, pX = (Screen.width - pW) * 0.5f, pY = (Screen.height - pH) * 0.5f;
            GUI.Box(new Rect(pX, pY, pW, pH), "");
            GUI.Box(new Rect(pX + 4, pY + 4, pW - 8, pH - 8), "");
            GUI.color = new Color(0.2f, 0.9f, 1f);
            GUI.Label(new Rect(pX + 20, pY + 16, pW - 40, 25), "=== TACTICAL PAUSE ===");
            GUI.color = Color.white;

            if (GUI.Button(new Rect(pX + 30, pY + 55, pW - 60, 36), "RESUME MISSION  [ESC]")) TogglePause();
            if (GUI.Button(new Rect(pX + 30, pY + 105, pW - 60, 36), "RESTART RUN  [R]")) { TogglePause(); OnRestartButtonClicked(); }
            if (GUI.Button(new Rect(pX + 30, pY + 155, pW - 60, 36), "QUIT TO DESKTOP")) Application.Quit();
        }

        private void DrawUpgradeShop(int credits)
        {
            float pW = 460, pH = 290, pX = (Screen.width - pW) * 0.5f, pY = (Screen.height - pH) * 0.5f;
            GUI.Box(new Rect(pX, pY, pW, pH), "");
            GUI.Box(new Rect(pX + 4, pY + 4, pW - 8, pH - 8), "");
            GUI.color = new Color(0.2f, 0.9f, 1.0f);
            GUI.Label(new Rect(pX + 20, pY + 16, pW - 40, 25), $"=== FIELD UPGRADE TERMINAL ===   [CREDITS: {credits}]");
            GUI.color = Color.white;

            DrawPerkButton(pX + 20, pY + 48, pW - 40, PerkType.DamageBoost, "+20% Bullet Damage", 40, credits);
            DrawPerkButton(pX + 20, pY + 92, pW - 40, PerkType.SpeedBoost, "+15% Movement Speed", 30, credits);
            DrawPerkButton(pX + 20, pY + 136, pW - 40, PerkType.HealthBoost, "+35 Max HP & Instant Heal", 35, credits);
            DrawPerkButton(pX + 20, pY + 180, pW - 40, PerkType.FireRateBoost, "+25% Weapon Fire Rate", 45, credits);
            DrawPerkButton(pX + 20, pY + 224, pW - 40, PerkType.DashCooldownBoost, "-20% Dash Cooldown", 30, credits);
        }

        private void DrawPerkButton(float x, float y, float w, PerkType type, string desc, int cost, int credits)
        {
            bool canAfford = credits >= cost;
            GUI.color = canAfford ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            string label = $"{desc}  ({cost} Credits)";
            if (GUI.Button(new Rect(x, y, w, 38), label) && canAfford && PlayerPerks.Instance != null)
                PlayerPerks.Instance.TryPurchaseUpgrade(type, cost);
            GUI.color = Color.white;
        }

        private void DrawEndModal(string title, string subtitle, Color headerColor, int wave, int kills, int credits)
        {
            float pW = 460, pH = 260, pX = (Screen.width - pW) * 0.5f, pY = (Screen.height - pH) * 0.5f;
            GUI.Box(new Rect(pX, pY, pW, pH), "");
            GUI.Box(new Rect(pX + 4, pY + 4, pW - 8, pH - 8), "");
            GUI.color = headerColor;
            GUI.Label(new Rect(pX + 20, pY + 20, pW - 40, 30), title);
            GUI.color = Color.white;
            GUI.Label(new Rect(pX + 20, pY + 55, pW - 40, 22), subtitle);
            GUI.Label(new Rect(pX + 20, pY + 80, pW - 40, 22), $"WAVES: {wave}   |   KILLS: {kills}   |   RUN CREDITS: {credits}");

            int banked = OutpostStashManager.Instance != null ? OutpostStashManager.Instance.BankedCredits : credits;
            int totalEvacs = OutpostStashManager.Instance != null ? OutpostStashManager.Instance.SuccessfulExtractions : 1;
            GUI.color = new Color(0.2f, 1f, 0.5f);
            GUI.Label(new Rect(pX + 20, pY + 105, pW - 40, 22), $"OUTPOST STASH: {banked} CREDITS   |   LIFETIME EVACS: {totalEvacs}");

            GUI.color = new Color(0.2f, 0.95f, 0.35f);
            if (GUI.Button(new Rect(pX + 50, pY + 145, pW - 100, 50), "NEW DEPLOYMENT  [SPACE / CLICK]")) OnRestartButtonClicked();
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.R)) OnRestartButtonClicked();
        }

        public void OnRestartButtonClicked() => GameManager.Instance?.RestartGame();
    }
}
