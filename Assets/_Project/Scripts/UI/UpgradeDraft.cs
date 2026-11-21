using System.Collections.Generic;
using UnityEngine;
using Deadzone.Core;
using Deadzone.Player;

namespace Deadzone.UI
{
    /// <summary>
    /// Round-end upgrade popup (founder, Build #9). Replaces the always-open upgrade shop.
    ///
    /// When: after round 2, then after EVERY round from 4 to 7, then every 2nd round (9, 11, ...).
    ///       This is the same schedule planned for online play, where the window is a fixed 15 s that
    ///       does not pause anyone; offline uses the same 15 s so the two modes feel the same.
    /// What: 3 random upgrades the player can afford RIGHT NOW with gold earned in the field. Pick one
    ///       (or skip). If nothing is affordable there is no popup, only a "need N gold" note.
    /// Cost: grows with the round and with how many times that upgrade was already bought.
    /// The next round waits for the window to close (WaveManager checks IsOpenNow).
    /// </summary>
    public class UpgradeDraft : MonoBehaviour
    {
        public const float WindowSeconds = 15f;
        public const int MaxChoices = 3;

        public static UpgradeDraft Instance { get; private set; }
        public static bool IsOpenNow => Instance != null && Instance.IsOpen;

        public bool IsOpen { get; private set; }
        public float TimeLeft { get; private set; }
        public IReadOnlyList<Offer> Offers => offers;
        public int LastRound { get; private set; }
        public string LastResult { get; private set; } = "";

        public enum Kind { Perk, Backpack }

        public class Offer
        {
            public Kind kind;
            public PerkType perk;
            public string title, effect;
            public int cost, level;
        }

        private struct PerkDef { public PerkType type; public string title, effect; public int baseCost; }
        private static readonly PerkDef[] Perks =
        {
            new PerkDef { type = PerkType.DamageBoost,       title = "HOLLOW POINTS",   effect = "+20% bullet damage",       baseCost = 45 },
            new PerkDef { type = PerkType.FireRateBoost,     title = "HAIR TRIGGER",    effect = "+25% fire rate",           baseCost = 50 },
            new PerkDef { type = PerkType.HealthBoost,       title = "BIO-PLATING",     effect = "+35 max HP, full heal",    baseCost = 55 },
            new PerkDef { type = PerkType.SpeedBoost,        title = "COMBAT STIMS",    effect = "+15% move speed",          baseCost = 35 },
            new PerkDef { type = PerkType.DashCooldownBoost, title = "KINETIC THRUSTERS", effect = "-20% dash cooldown",     baseCost = 40 },
        };

        private readonly Dictionary<PerkType, int> levels = new Dictionary<PerkType, int>();
        private readonly List<Offer> offers = new List<Offer>();
        private GUIStyle titleStyle, bodyStyle, costStyle, headStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Instance != null || WaveManager.Instance == null) return;
            new GameObject("UpgradeDraft").AddComponent<UpgradeDraft>();
        }

        public static bool IsUpgradeRound(int round) =>
            round == 2 || (round >= 4 && round <= 7) || (round > 7 && (round - 7) % 2 == 0);

        /// <summary>
        /// Price of the next level: +15% per round after 2, +35% per level already owned (rounded to 5).
        /// Tuned to field income: a gold gem is 10 gold and ~28% of kills drop one, so a player who collects
        /// most drops has ~60 gold by round 2 and ~130 by round 4.
        /// </summary>
        public static int CostFor(int baseCost, int round, int level) =>
            Mathf.RoundToInt(baseCost * (1f + 0.15f * Mathf.Max(0, round - 2)) * (1f + 0.35f * level) / 5f) * 5;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (WaveManager.Instance != null) WaveManager.Instance.OnWaveCompleted += OnRoundCompleted;
        }

        private void OnDestroy()
        {
            if (WaveManager.Instance != null) WaveManager.Instance.OnWaveCompleted -= OnRoundCompleted;
            if (Instance == this) Instance = null;
        }

        private static int Gold => GameManager.Instance != null ? GameManager.Instance.CreditsEarned : 0;

        private void OnRoundCompleted(int round)
        {
            if (!IsUpgradeRound(round)) return;
            LastRound = round;
            var affordable = new List<Offer>();
            int cheapest = int.MaxValue;
            foreach (var d in Perks)
            {
                levels.TryGetValue(d.type, out int lv);
                int cost = CostFor(d.baseCost, round, lv);
                cheapest = Mathf.Min(cheapest, cost);
                if (cost <= Gold) affordable.Add(new Offer { kind = Kind.Perk, perk = d.type, title = d.title, effect = d.effect, cost = cost, level = lv + 1 });
            }
            var bag = Backpack.Instance;
            if (bag != null && !bag.IsMaxTier)
            {
                int cost = bag.NextUpgradeCost;
                cheapest = Mathf.Min(cheapest, cost);
                if (cost <= Gold) affordable.Add(new Offer { kind = Kind.Backpack, title = "BIGGER BACKPACK", effect = $"{bag.Capacity} -> {Backpack.TierSlots[bag.Tier + 1]} slots (permanent)", cost = cost, level = bag.Tier + 1 });
            }

            if (affordable.Count == 0)
            {
                LastResult = $"no popup: gold {Gold} < cheapest {cheapest}";
                KillFeedUI.PostMessage($"UPGRADE WINDOW: need {cheapest} gold (you have {Gold}). Collect gold drops!", new Color(1f, 0.8f, 0.3f));
                return;
            }

            offers.Clear();
            while (offers.Count < MaxChoices && affordable.Count > 0)
            {
                int i = Random.Range(0, affordable.Count);
                offers.Add(affordable[i]);
                affordable.RemoveAt(i);
            }
            TimeLeft = WindowSeconds;
            IsOpen = true;
            LastResult = $"opened with {offers.Count} offers at gold {Gold}";
        }

        /// <summary>Tests/screenshots: run the round-end check for this round now.</summary>
        public void DebugOpen(int round) => OnRoundCompleted(round);

        private void Update()
        {
            if (!IsOpen) return;
            TimeLeft -= Time.deltaTime;
            if (TimeLeft <= 0f) Close("timed out");
        }

        /// <summary>Buys offer i (if still affordable) and closes the window. Returns true on success.</summary>
        public bool Choose(int i)
        {
            if (!IsOpen || i < 0 || i >= offers.Count) return false;
            var o = offers[i];
            if (o.cost > Gold) return false;
            bool ok;
            if (o.kind == Kind.Backpack) ok = Backpack.Instance != null && Backpack.Instance.TryUpgrade();
            else
            {
                ok = PlayerPerks.Instance != null && PlayerPerks.Instance.TryPurchaseUpgrade(o.perk, o.cost);
                if (ok) { levels.TryGetValue(o.perk, out int lv); levels[o.perk] = lv + 1; }
            }
            if (!ok) return false;
            KillFeedUI.PostMessage($"UPGRADED: {o.title} (Lv {o.level})  -{o.cost} gold", BarDrawer.Green);
            Close($"bought {o.title} for {o.cost}");
            return true;
        }

        public void Skip() => Close("skipped");

        private void Close(string why)
        {
            IsOpen = false;
            offers.Clear();
            LastResult = why;
        }

        // ---------------- UI ----------------
        private void OnGUI()
        {
            if (!IsOpen) return;
            GUI.depth = -100; // above every other HUD script (lower depth draws on top)
            UIScale.Begin();
            EnsureStyles();
            float W = UIScale.W, H = UIScale.H;

            // dim the world a little so the choice reads
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
            GUI.color = Color.white;

            int n = offers.Count;
            float cardW = 190f, cardH = 150f, gap = 14f;
            float totalW = n * cardW + (n - 1) * gap;
            float panelW = Mathf.Max(totalW + 40f, 360f), panelH = cardH + 118f;
            float px = (W - panelW) * 0.5f, py = (H - panelH) * 0.5f - 20f;

            Fill(new Rect(px, py, panelW, panelH), new Color(0.04f, 0.06f, 0.09f, 0.94f));
            Fill(new Rect(px, py, panelW, 3f), new Color(1f, 0.75f, 0.25f, 1f));
            GUI.Label(new Rect(px, py + 10, panelW, 26), $"ROUND {LastRound} CLEARED - CHOOSE AN UPGRADE", headStyle);
            GUI.color = new Color(1f, 0.82f, 0.35f);
            GUI.Label(new Rect(px, py + 36, panelW, 20), $"GOLD {Gold}     {Mathf.CeilToInt(TimeLeft)}s", bodyStyle);
            GUI.color = Color.white;
            // countdown bar
            BarDrawer.Draw(new Rect(px + 20, py + 58, panelW - 40, 6), TimeLeft / WindowSeconds, new Color(1f, 0.75f, 0.25f), 1f);

            float cx = px + (panelW - totalW) * 0.5f, cy = py + 74f;
            for (int i = 0; i < n; i++)
            {
                var o = offers[i];
                var r = new Rect(cx + i * (cardW + gap), cy, cardW, cardH);
                bool can = o.cost <= Gold;
                Fill(r, can ? new Color(0.10f, 0.16f, 0.20f, 1f) : new Color(0.10f, 0.10f, 0.10f, 1f));
                Fill(new Rect(r.x, r.y, r.width, 3f), o.kind == Kind.Backpack ? new Color(0.4f, 0.8f, 1f) : BarDrawer.Green);
                GUI.Label(new Rect(r.x + 8, r.y + 12, r.width - 16, 24), o.title, titleStyle);
                GUI.Label(new Rect(r.x + 8, r.y + 38, r.width - 16, 20), $"Level {o.level}", bodyStyle);
                GUI.Label(new Rect(r.x + 8, r.y + 60, r.width - 16, 36), o.effect, bodyStyle);
                GUI.color = can ? Color.white : new Color(0.6f, 0.6f, 0.6f);
                if (GUI.Button(new Rect(r.x + 14, r.y + cardH - 44, r.width - 28, 34), $"BUY  {o.cost} GOLD", costStyle) && can) Choose(i);
                GUI.color = Color.white;
            }
            if (GUI.Button(new Rect(px + panelW * 0.5f - 60, py + panelH - 36, 120, 28), "SKIP")) Skip();
        }

        private static void Fill(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (headStyle != null) return;
            headStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 17 };
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 15, wordWrap = true };
            bodyStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = 13, wordWrap = true };
            costStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 14 };
        }
    }
}
