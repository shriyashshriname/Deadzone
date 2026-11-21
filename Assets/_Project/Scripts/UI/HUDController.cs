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

        public static bool IsMenuOpen { get; private set; }

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
            IsMenuOpen = isShopOpen || isPaused;
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
        }

        private void OnGUI()
        {
            UIScale.Begin();
            if (playerHealth == null || currentWeapon == null || playerAbilities == null) FindPlayerReferences();
            Color oldColor = GUI.color;
            bool isMobile = MobileControlsManager.IsMobileActive;

            // 1. Health Bar & Shop Toggle (Top Left)
            if (playerHealth != null)
            {
                float hpPct = Mathf.Clamp01(playerHealth.CurrentHealth / playerHealth.MaxHealth);
                GUI.Box(new Rect(16, 16, 210, 28), "");
                GUI.color = hpPct > 0.35f ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.95f, 0.2f, 0.2f);
                GUI.Box(new Rect(18, 18, 206 * hpPct, 24), "");
                GUI.color = Color.white;
                GUI.Label(new Rect(24, 20, 200, 22), $"♥ {Mathf.CeilToInt(playerHealth.CurrentHealth)} / {playerHealth.MaxHealth}");

                string shopText = isShopOpen ? (isMobile ? "CLOSE SHOP" : "CLOSE [B]") : (isMobile ? "UPGRADE SHOP" : "UPGRADE SHOP [B]");
                if (GUI.Button(new Rect(16, 48, 130, 26), shopText)) isShopOpen = !isShopOpen;
            }

            // 2. Sector Mission Objectives (Top Left)
            int onlineStations = PowerSubstation.TotalSubstationsOnline;
            int totalStations = Mathf.Max(2, PowerSubstation.TotalSubstationsCount);
            GUI.Box(new Rect(16, 78, 210, 56), "");
            GUI.color = new Color(0.2f, 0.9f, 1f);
            GUI.Label(new Rect(22, 80, 200, 18), "=== SECTOR OBJECTIVES ===");
            GUI.color = onlineStations >= totalStations ? new Color(0.2f, 1f, 0.4f) : Color.white;
            GUI.Label(new Rect(22, 98, 200, 18), $"GRID: {onlineStations}/{totalStations} ONLINE");
            bool evacReady = ExtractionZone.Instance != null && ExtractionZone.Instance.IsActive;
            GUI.color = evacReady ? new Color(0.2f, 1f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(22, 114, 200, 18), evacReady ? "EVAC: READY AT CENTER" : "EVAC: STANDBY");
            GUI.color = Color.white;

            // 3. Tactical Abilities (Desktop Only - Mobile buttons handle this)
            if (!isMobile && playerAbilities != null)
            {
                GUI.Box(new Rect(20, UIScale.H - 90, 220, 68), "");
                bool gReady = playerAbilities.IsGrenadeReady;
                GUI.color = gReady ? new Color(1f, 0.7f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
                GUI.Label(new Rect(30, UIScale.H - 84, 200, 24), gReady ? "[Q] GRENADE: READY" : $"[Q] GRENADE: {playerAbilities.GrenadeRemainingTime:F1}s");
                bool tReady = playerAbilities.IsTurretReady;
                GUI.color = tReady ? new Color(0.2f, 0.9f, 1f) : new Color(0.6f, 0.6f, 0.6f);
                GUI.Label(new Rect(30, UIScale.H - 58, 200, 24), tReady ? "[F] TURRET: READY" : $"[F] TURRET: {playerAbilities.TurretRemainingTime:F1}s");
                GUI.color = Color.white;
            }

            // 4. Ammo & Active Weapon
            if (currentWeapon != null && currentWeapon.Data != null)
            {
                string weaponName = currentWeapon.Data.weaponName.ToUpper();
                string ammoStr = currentWeapon.IsReloading ? "RELOADING..." : $"{currentWeapon.CurrentAmmo} / {currentWeapon.Data.magazineCapacity}";
                if (!isMobile)
                {
                    GUI.Box(new Rect(UIScale.W - 240, UIScale.H - 85, 220, 65), "");
                    GUI.Label(new Rect(UIScale.W - 230, UIScale.H - 80, 200, 25), $"WEAPON: {weaponName}");
                    GUI.Label(new Rect(UIScale.W - 230, UIScale.H - 55, 200, 25), $"AMMO: {ammoStr}  [R]");
                }
                else
                {
                    GUI.Box(new Rect(UIScale.W - 240, 72, 185, 38), "");
                    GUI.Label(new Rect(UIScale.W - 232, 74, 175, 18), $"GUN: {weaponName}");
                    GUI.Label(new Rect(UIScale.W - 232, 90, 175, 18), $"AMMO: {ammoStr}");
                }
            }

            // 5. Mobile Pause Button (Top Right)
            if (isMobile)
            {
                if (GUI.Button(new Rect(UIScale.W - 48, 16, 36, 36), "II")) TogglePause();
            }

            // 6. Wave, Kills & Credits (Top Right)
            int kills = GameManager.Instance != null ? GameManager.Instance.KillCount : 0;
            int credits = GameManager.Instance != null ? GameManager.Instance.CreditsEarned : 0;
            int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWaveNumber : 1;
            int hostiles = WaveManager.Instance != null ? WaveManager.Instance.TotalRemainingHostiles : 0;
            bool isIntermission = WaveManager.Instance != null && WaveManager.Instance.IsIntermission;

            float waveX = isMobile ? UIScale.W - 240 : UIScale.W - 220;
            GUI.Box(new Rect(waveX, 16, isMobile ? 185 : 200, 52), "");
            GUI.Label(new Rect(waveX + 8, 18, 180, 18), $"WAVE {wave}  |  CR {credits}");
            GUI.color = hostiles > 0 ? new Color(1f, 0.4f, 0.4f) : Color.white;
            GUI.Label(new Rect(waveX + 8, 36, 180, 18), isIntermission ? "CLEARED! REGROUP" : $"KILLS: {kills} | HOSTILES: {hostiles}");
            GUI.color = Color.white;

            // 7. Loadout Bar (Bottom Center)
            var pc = PlayerController.Instance;
            string slot1Name = pc != null && pc.PrimaryWeapon != null ? pc.PrimaryWeapon.weaponName : "BASIC RIFLE";
            string slot2Name = pc != null && pc.SecondaryWeapon != null ? pc.SecondaryWeapon.weaponName : "EMPTY";
            int activeSlot = pc != null ? pc.ActiveSlotIndex : 0;

            if (!isMobile)
            {
                string s1 = activeSlot == 0 ? $"★ [1] {slot1Name}" : $"[1] {slot1Name}";
                string s2 = activeSlot == 1 ? $"★ [2] {slot2Name}" : $"[2] {slot2Name}";
                GUI.Box(new Rect(UIScale.W / 2 - 220, UIScale.H - 45, 440, 32), "");
                GUI.Label(new Rect(UIScale.W / 2 - 210, UIScale.H - 40, 420, 25), $"{s1}   |   {s2}  [Q TO SWAP]");
            }
            else
            {
                float barW = 280, barH = 34, barX = (UIScale.W - barW) * 0.5f, barY = UIScale.H - (UIScale.Portrait ? 56 : 42);
                GUI.Box(new Rect(barX, barY, barW, barH), "");
                GUI.color = activeSlot == 0 ? new Color(0.2f, 1f, 0.5f) : new Color(0.7f, 0.7f, 0.7f);
                if (GUI.Button(new Rect(barX + 4, barY + 3, (barW - 12) * 0.5f, barH - 6), $"★ S1: {slot1Name}") && pc != null) pc.SelectSlot(0);
                GUI.color = activeSlot == 1 ? new Color(0.2f, 1f, 0.5f) : new Color(0.7f, 0.7f, 0.7f);
                if (GUI.Button(new Rect(barX + (barW - 12) * 0.5f + 8, barY + 3, (barW - 12) * 0.5f, barH - 6), $"★ S2: {slot2Name}") && pc != null) pc.SelectSlot(1);
                GUI.color = Color.white;
            }

            // 8. Evac / Wave Banner (Top Center)
            if (ExtractionZone.Instance != null && ExtractionZone.Instance.IsActive && !ExtractionZone.Instance.IsExtracted)
            {
                float by = UIScale.Portrait ? 142 : 34; // portrait: below the top-left/top-right panels
                GUI.Box(new Rect(UIScale.W / 2 - 210, by, 420, 34), "");
                GUI.color = ExtractionZone.Instance.IsPlayerInside ? new Color(0.2f, 1f, 0.4f) : new Color(0.2f, 0.9f, 1f);
                string evacText = ExtractionZone.Instance.IsPlayerInside ? $"EXTRACTING: {ExtractionZone.Instance.CurrentHoldTime:F1}s / {ExtractionZone.Instance.RequiredHoldTime:F1}s (HOLD!)" : "EVAC SHIP AT CENTER! HOLD 5s TO EXTRACT";
                GUI.Label(new Rect(UIScale.W / 2 - 200, by + 6, 400, 24), evacText);
                GUI.color = Color.white;
            }
            else if (WaveManager.Instance != null && isIntermission)
            {
                float by = UIScale.Portrait ? 142 : 34;
                GUI.Box(new Rect(UIScale.W / 2 - 180, by, 360, 36), "");
                GUI.color = new Color(0.2f, 1f, 0.4f);
                GUI.Label(new Rect(UIScale.W / 2 - 170, by + 6, 340, 25), $"WAVE CLEARED! NEXT IN: {WaveManager.Instance.IntermissionTimer:F1}s");
                GUI.color = Color.white;
            }

            if (isShopOpen) DrawUpgradeShop(credits, isMobile);
            if (isPaused) DrawPauseMenu(isMobile);

            bool isGameOver = (playerHealth != null && playerHealth.IsDead) || (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver);
            bool isVictory = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Victory;

            if (isGameOver) DrawEndModal("=== MISSION FAILED ===", "OPERATOR KIA / OUT OF BOUNDS", new Color(1f, 0.25f, 0.25f), wave, kills, credits);
            else if (isVictory) DrawEndModal("=== EXTRACTION SUCCESSFUL ===", "SECTOR PURGED | OPERATOR EVACUATED", new Color(0.2f, 1f, 0.4f), wave, kills, credits);

            GUI.color = oldColor;
        }

        private void DrawPauseMenu(bool isMobile)
        {
            float pW = 380, pH = 220, pX = (UIScale.W - pW) * 0.5f, pY = (UIScale.H - pH) * 0.5f;
            GUI.Box(new Rect(pX, pY, pW, pH), "");
            GUI.Box(new Rect(pX + 4, pY + 4, pW - 8, pH - 8), "");
            GUI.color = new Color(0.2f, 0.9f, 1f);
            GUI.Label(new Rect(pX + 20, pY + 16, pW - 40, 25), "=== TACTICAL PAUSE ===");
            GUI.color = Color.white;

            string resumeText = isMobile ? "RESUME MISSION" : "RESUME MISSION  [ESC]";
            string restartText = isMobile ? "RESTART RUN" : "RESTART RUN  [R]";
            if (GUI.Button(new Rect(pX + 30, pY + 55, pW - 60, 36), resumeText)) TogglePause();
            if (GUI.Button(new Rect(pX + 30, pY + 105, pW - 60, 36), restartText)) { TogglePause(); OnRestartButtonClicked(); }
            if (GUI.Button(new Rect(pX + 30, pY + 155, pW - 60, 36), isMobile ? "QUIT GAME" : "QUIT TO DESKTOP")) Application.Quit();
        }

        private void DrawUpgradeShop(int credits, bool isMobile)
        {
            float pW = 460, pH = 334, pX = (UIScale.W - pW) * 0.5f, pY = (UIScale.H - pH) * 0.5f;
            GUI.Box(new Rect(pX, pY, pW, pH), "");
            GUI.Box(new Rect(pX + 4, pY + 4, pW - 8, pH - 8), "");
            GUI.color = new Color(0.2f, 0.9f, 1.0f);
            GUI.Label(new Rect(pX + 20, pY + 16, pW - 40, 25), $"=== FIELD UPGRADES ===   [CREDITS: {credits}]");
            GUI.color = Color.white;

            DrawPerkButton(pX + 20, pY + 48, pW - 40, PerkType.DamageBoost, "+20% Bullet Damage", 40, credits);
            DrawPerkButton(pX + 20, pY + 92, pW - 40, PerkType.SpeedBoost, "+15% Movement Speed", 30, credits);
            DrawPerkButton(pX + 20, pY + 136, pW - 40, PerkType.HealthBoost, "+35 Max HP & Full Heal", 50, credits);
            DrawPerkButton(pX + 20, pY + 180, pW - 40, PerkType.FireRateBoost, "+25% Fire Rate", 45, credits);
            DrawPerkButton(pX + 20, pY + 224, pW - 40, PerkType.DashCooldownBoost, "-20% Dash Cooldown", 35, credits);
            DrawBackpackUpgrade(pX + 20, pY + 268, pW - 40, credits);

            if (GUI.Button(new Rect(pX + pW - 100, pY + 14, 85, 24), "CLOSE ✕")) isShopOpen = false;
        }

        private void DrawBackpackUpgrade(float x, float y, float w, int credits)
        {
            var bag = Backpack.Instance;
            if (bag == null) return;
            if (bag.IsMaxTier)
            {
                GUI.color = new Color(0.6f, 0.9f, 1f);
                GUI.Box(new Rect(x, y, w, 38), $"BACKPACK MAXED ({bag.Capacity} slots)");
                GUI.color = Color.white;
                return;
            }
            int cost = bag.NextUpgradeCost;
            bool canAfford = credits >= cost;
            GUI.color = canAfford ? new Color(0.6f, 0.95f, 1f) : new Color(0.6f, 0.6f, 0.6f, 0.8f);
            int next = Backpack.TierSlots[bag.Tier + 1];
            if (GUI.Button(new Rect(x, y, w, 38), $"BACKPACK {bag.Capacity} → {next} SLOTS — permanent  ({cost} Credits)") && canAfford)
                bag.TryUpgrade();
            GUI.color = Color.white;
        }

        private void DrawPerkButton(float x, float y, float w, PerkType type, string name, int cost, int currentCredits)
        {
            bool canAfford = currentCredits >= cost;
            GUI.color = canAfford ? Color.white : new Color(0.6f, 0.6f, 0.6f, 0.8f);
            if (GUI.Button(new Rect(x, y, w, 38), $"{name}  ({cost} Credits)") && canAfford && PlayerPerks.Instance != null)
            {
                PlayerPerks.Instance.TryPurchaseUpgrade(type, cost);
            }
            GUI.color = Color.white;
        }

        private void DrawEndModal(string title, string subtitle, Color headerColor, int wave, int kills, int credits)
        {
            float mW = 440, mH = 240, mX = (UIScale.W - mW) * 0.5f, mY = (UIScale.H - mH) * 0.5f;
            GUI.Box(new Rect(mX, mY, mW, mH), "");
            GUI.Box(new Rect(mX + 4, mY + 4, mW - 8, mH - 8), "");
            GUI.color = headerColor;
            GUI.Label(new Rect(mX + 20, mY + 20, mW - 40, 25), title);
            GUI.color = Color.white;
            GUI.Label(new Rect(mX + 20, mY + 48, mW - 40, 22), subtitle);
            GUI.Label(new Rect(mX + 20, mY + 75, mW - 40, 22), $"WAVES SURVIVED: {wave}   |   KILLS: {kills}");
            GUI.Label(new Rect(mX + 20, mY + 100, mW - 40, 22), $"TOTAL CREDITS EARNED: {credits}");

            if (GUI.Button(new Rect(mX + 30, mY + 140, mW - 60, 38), "DEPLOY AGAIN")) OnRestartButtonClicked();
            if (GUI.Button(new Rect(mX + 30, mY + 185, mW - 60, 36), "EXIT")) Application.Quit();
        }

        private void OnRestartButtonClicked()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
    }
}
