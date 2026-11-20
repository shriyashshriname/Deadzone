using System;
using UnityEngine;
using Deadzone.Core;
using Deadzone.Combat;
using Deadzone.UI;

namespace Deadzone.Player
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(PlayerController))]
    public class PlayerRevive : MonoBehaviour
    {
        [Header("DBNO Settings")]
        [SerializeField] private float bleedoutDuration = 15f;
        [SerializeField] private float reviveTimeRequired = 3.0f;
        [SerializeField] private float reviveInteractRadius = 3.2f;

        private Health health;
        private PlayerController playerController;
        private bool isDowned = false;
        private float currentBleedout;
        private float currentReviveProgress;
        private FriendlySquadmate activeSquadmate;

        public static PlayerRevive Instance { get; private set; }
        public bool IsDowned => isDowned;
        public float BleedoutTimeRemaining => currentBleedout;
        public float ReviveProgressRatio => Mathf.Clamp01(currentReviveProgress / reviveTimeRequired);

        private void Awake()
        {
            if (Instance == null) Instance = this;
            health = GetComponent<Health>();
            playerController = GetComponent<PlayerController>();
        }

        private void Start()
        {
            health.OnDeath += HandleFatalDamage;
            activeSquadmate = FindAnyObjectByType<FriendlySquadmate>();
        }

        private void OnDestroy()
        {
            if (health != null) health.OnDeath -= HandleFatalDamage;
        }

        private void HandleFatalDamage()
        {
            if (isDowned) return;

            // If a friendly teammate is alive and standing, enter DBNO instead of instant defeat!
            if (activeSquadmate != null && !activeSquadmate.IsDead && !activeSquadmate.IsDowned)
            {
                EnterDownedState();
            }
            else
            {
                // Full team wiped out
                if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver();
            }
        }

        public void EnterDownedState()
        {
            isDowned = true;
            currentBleedout = bleedoutDuration;
            currentReviveProgress = 0f;
            transform.localScale = new Vector3(1f, 0.45f, 1f); // Crawl pose
            KillFeedUI.PostMessage("⚠ OPERATOR DOWN! Teammate dispatched for revive!", new Color(1f, 0.8f, 0.2f));

            if (AudioManager.Instance != null) AudioManager.Instance.PlayHit();
        }

        public void TickBleedout(float delta)
        {
            if (!isDowned) return;
            currentBleedout -= delta;
            if (currentBleedout <= 0f)
            {
                isDowned = false;
                transform.localScale = Vector3.one;
                if (GameManager.Instance != null) GameManager.Instance.TriggerGameOver();
            }
        }

        private void Update()
        {
            if (activeSquadmate == null) activeSquadmate = FindAnyObjectByType<FriendlySquadmate>();

            if (isDowned)
            {
                TickBleedout(Time.deltaTime);
                if (!isDowned) return;

                // Slow crawl movement while downed
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                Vector3 crawlDir = new Vector3(h, 0f, v).normalized;
                transform.position += crawlDir * (2.2f * Time.deltaTime);

                bool isInteracting = Input.GetKey(KeyCode.E) || (PlayerController.Instance != null && PlayerController.Instance.CurrentCommand.interact);

                // Self-stabilize by holding interact
                if (isInteracting)
                {
                    currentReviveProgress += Time.deltaTime * 0.6f;
                    if (currentReviveProgress >= reviveTimeRequired) RevivePlayer(45f);
                }
            }
            else
            {
                // Player is standing, check if squadmate needs revive
                if (activeSquadmate != null && activeSquadmate.IsDowned && !activeSquadmate.IsDead)
                {
                    bool isInteracting = Input.GetKey(KeyCode.E) || (PlayerController.Instance != null && PlayerController.Instance.CurrentCommand.interact);
                    float dist = Vector3.Distance(transform.position, activeSquadmate.transform.position);
                    if (dist <= reviveInteractRadius && isInteracting)
                    {
                        currentReviveProgress += Time.deltaTime;
                        if (currentReviveProgress >= reviveTimeRequired)
                        {
                            activeSquadmate.ReviveSquadmate(50f);
                            currentReviveProgress = 0f;
                        }
                    }
                    else if (!isInteracting)
                    {
                        currentReviveProgress = Mathf.Max(0f, currentReviveProgress - Time.deltaTime);
                    }
                }
            }
        }

        public void RevivePlayer(float restoredHp)
        {
            if (!isDowned) return;
            if (health == null) health = GetComponent<Health>();
            isDowned = false;
            transform.localScale = Vector3.one;
            if (health != null)
            {
                health.ResetHealth();
                health.TakeDamage(health.MaxHealth - restoredHp);
            }
            currentReviveProgress = 0f;

            KillFeedUI.PostMessage("✔ REVIVED: Operator back on their feet!", new Color(0.2f, 1f, 0.4f));
            if (AudioManager.Instance != null) AudioManager.Instance.PlayReload(1.2f);
        }

        private void OnGUI()
        {
            if (!isDowned) return;

            float w = 360, h = 55;
            float x = (Screen.width - w) * 0.5f;
            float y = Screen.height * 0.72f;

            GUI.Box(new Rect(x, y, w, h), "");
            GUI.color = new Color(1f, 0.25f, 0.25f);
            GUI.Label(new Rect(x + 10, y + 6, w - 20, 22), $"DOWNED! BLEEDOUT: {currentBleedout:F1}s  [HOLD E TO STABILIZE]");

            // Bleedout bar
            float bleedPct = Mathf.Clamp01(currentBleedout / bleedoutDuration);
            GUI.color = Color.Lerp(Color.red, Color.green, bleedPct);
            GUI.Box(new Rect(x + 10, y + 30, (w - 20) * bleedPct, 16), "");
            GUI.color = Color.white;
        }
    }
}
