using UnityEngine;
using Deadzone.Core;
using Deadzone.Combat;
using Deadzone.UI;

namespace Deadzone.Player
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(TeamMember))]
    public class FriendlySquadmate : MonoBehaviour
    {
        [Header("Squadmate Identity")]
        public string callsign = "Echo_Bravo";
        [SerializeField] private float moveSpeed = 6.8f;
        [SerializeField] private float fireRate = 6.0f;
        [SerializeField] private float damagePerShot = 18f;
        [SerializeField] private float followDistance = 5.0f;
        [SerializeField] private GameObject projectilePrefab;

        private Health health;
        private TeamMember team;
        private Transform playerTransform;
        private Transform currentEnemyTarget;
        private bool isDowned = false;
        private bool isDead = false;
        private float bleedoutTimer = 20f;
        private float nextFireTime;
        private float reviveChannelTimer = 0f;
        private UnityEngine.Camera mainCam;

        public static FriendlySquadmate Instance { get; private set; }
        public bool IsDowned => isDowned;
        public bool IsDead => isDead;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            health = GetComponent<Health>();
            team = GetComponent<TeamMember>();
            team.TeamId = 1; // Friendly Team
        }

        private void Start()
        {
            health.OnDeath += HandleFatalDamage;
            mainCam = UnityEngine.Camera.main;
            if (PlayerController.Instance != null) playerTransform = PlayerController.Instance.transform;
            KillFeedUI.PostMessage($"★ DUO LINK: [{callsign}] deployed alongside you!", new Color(0.2f, 0.9f, 1f));
        }

        private void OnDestroy()
        {
            if (health != null) health.OnDeath -= HandleFatalDamage;
        }

        private void HandleFatalDamage()
        {
            if (isDowned || isDead) return;
            isDowned = true;
            bleedoutTimer = 20f;
            transform.localScale = new Vector3(1f, 0.45f, 1f);
            KillFeedUI.PostMessage($"⚠ SQUADMATE DOWN: [{callsign}] needs revive! [HOLD E]", new Color(1f, 0.75f, 0.2f));
        }

        public void ReviveSquadmate(float restoredHp)
        {
            if (!isDowned) return;
            isDowned = false;
            transform.localScale = Vector3.one;
            health.ResetHealth();
            health.TakeDamage(health.MaxHealth - restoredHp);
            KillFeedUI.PostMessage($"✔ SQUADMATE REVIVED: [{callsign}] back in action!", new Color(0.2f, 1f, 0.4f));
        }

        private void Update()
        {
            if (isDead) return;

            if (!playerTransform && PlayerController.Instance != null) playerTransform = PlayerController.Instance.transform;

            if (isDowned)
            {
                bleedoutTimer -= Time.deltaTime;
                if (bleedoutTimer <= 0f)
                {
                    isDead = true;
                    isDowned = false;
                    KillFeedUI.PostMessage($"☠ SQUADMATE KIA: [{callsign}] bled out", new Color(1f, 0.25f, 0.25f));
                    Destroy(gameObject, 1.0f);
                }
                return;
            }

            // If player is downed, rush to revive!
            if (PlayerRevive.Instance != null && PlayerRevive.Instance.IsDowned)
            {
                HandlePlayerReviveBehavior();
                return;
            }

            // Normal tactical combat & follow
            HandleFollowAndCombat();
        }

        private void HandlePlayerReviveBehavior()
        {
            if (!playerTransform) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            Vector3 dir = (playerTransform.position - transform.position).normalized;
            dir.y = 0f;

            if (dist > 2.2f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);
                transform.position += dir * (moveSpeed * 1.25f * Time.deltaTime); // Sprint to revive
                reviveChannelTimer = 0f;
            }
            else
            {
                // In revive range, channel
                reviveChannelTimer += Time.deltaTime;
                if (reviveChannelTimer >= 2.5f)
                {
                    PlayerRevive.Instance.RevivePlayer(50f);
                    reviveChannelTimer = 0f;
                }
            }
        }

        private void HandleFollowAndCombat()
        {
            FindEnemyTarget();

            if (currentEnemyTarget != null)
            {
                Vector3 toTarget = currentEnemyTarget.position - transform.position;
                toTarget.y = 0f;
                float dist = toTarget.magnitude;

                if (toTarget.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), 10f * Time.deltaTime);

                if (dist <= 18f && Time.time >= nextFireTime) FireAtEnemy();
            }

            // Formation movement relative to player
            if (playerTransform != null)
            {
                float pDist = Vector3.Distance(transform.position, playerTransform.position);
                if (pDist > followDistance)
                {
                    Vector3 moveDir = (playerTransform.position - transform.position).normalized;
                    moveDir.y = 0f;
                    transform.position += moveDir * (moveSpeed * Time.deltaTime);
                    if (currentEnemyTarget == null && moveDir.sqrMagnitude > 0.01f)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), 8f * Time.deltaTime);
                }
            }
        }

        private void FindEnemyTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 20f);
            float closestDist = float.MaxValue;
            currentEnemyTarget = null;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                TeamMember other = hit.GetComponentInParent<TeamMember>() ?? hit.GetComponent<TeamMember>();
                if (other != null && other.TeamId == 1) continue; // Skip player and friendlies

                Health hp = hit.GetComponentInParent<Health>() ?? hit.GetComponent<Health>();
                if (hp != null && !hp.IsDead)
                {
                    float d = Vector3.Distance(transform.position, hit.transform.position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        currentEnemyTarget = hp.transform;
                    }
                }
            }
        }

        private void FireAtEnemy()
        {
            nextFireTime = Time.time + (1f / fireRate);

            if (projectilePrefab != null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 0.8f + transform.forward * 0.6f;
                Vector3 spread = new Vector3(Random.Range(-0.03f, 0.03f), 0, Random.Range(-0.03f, 0.03f));
                Quaternion rot = Quaternion.LookRotation(transform.forward + spread);

                GameObject projObj = Instantiate(projectilePrefab, spawnPos, rot);
                if (projObj.TryGetComponent<Projectile>(out var proj))
                {
                    proj.Initialize(damagePerShot, 38f, 1, 1); // Team 1
                }
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot(0.35f);
        }

        private void OnGUI()
        {
            if (isDead || !mainCam) return;

            Vector3 headPos = transform.position + Vector3.up * (isDowned ? 1.0f : 1.8f);
            Vector3 screenPos = mainCam.WorldToScreenPoint(headPos);
            if (screenPos.z <= 0) return;

            float y = Screen.height - screenPos.y;
            float x = screenPos.x;

            GUI.color = isDowned ? new Color(1f, 0.7f, 0.2f) : new Color(0.2f, 0.9f, 1.0f);
            string label = isDowned ? $"[DUO DOWN] {callsign} (HOLD E)" : $"[DUO] {callsign}";
            GUI.Label(new Rect(x - 70, y - 24, 140, 20), label);

            // Mini Health bar
            float hpPct = isDowned ? Mathf.Clamp01(bleedoutTimer / 20f) : Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
            GUI.Box(new Rect(x - 30, y - 4, 60, 6), "");
            GUI.color = isDowned ? Color.yellow : (hpPct > 0.35f ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.95f, 0.2f, 0.2f));
            GUI.Box(new Rect(x - 30, y - 4, 60 * hpPct, 6), "");
            GUI.color = Color.white;
        }
    }
}
