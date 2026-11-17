using UnityEngine;
using Deadzone.Core;
using Deadzone.Player;
using Deadzone.UI;

namespace Deadzone.Combat
{
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(TeamMember))]
    public class RivalOperator : MonoBehaviour
    {
        [Header("Operator Identity")]
        public string callsign = "Rival_Operator";
        [SerializeField] private float moveSpeed = 6.2f;
        [SerializeField] private float fireRate = 5.5f;
        [SerializeField] private float damagePerShot = 18f;
        [SerializeField] private float combatRange = 16f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private GameObject deathLootPrefab;

        private Health health;
        private TeamMember team;
        private Transform currentTarget;
        private float nextFireTime;
        private float nextStrafeSwitch;
        private float strafeDir = 1f;
        private UnityEngine.Camera mainCam;

        private static readonly string[] CallsignPool = { "Viper_07", "Ghost_X", "Raven_11", "Reaper_99", "Striker_4", "Apex_Hawk" };

        private void Awake()
        {
            health = GetComponent<Health>();
            team = GetComponent<TeamMember>();
            team.TeamId = 2; // Rival Team Alpha
            callsign = CallsignPool[Random.Range(0, CallsignPool.Length)];
        }

        private void Start()
        {
            health.OnDeath += HandleDeath;
            health.OnHealthChanged += HandleHitReaction;
            mainCam = UnityEngine.Camera.main;
            KillFeedUI.PostMessage($"⚠ INFILTRATION: [AI] [{callsign}] deployed into sector", new Color(1f, 0.5f, 0.2f));
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
                health.OnHealthChanged -= HandleHitReaction;
            }
        }

        private void Update()
        {
            if (health.IsDead) return;

            FindOptimalTarget();

            if (currentTarget != null)
            {
                Vector3 toTarget = currentTarget.position - transform.position;
                toTarget.y = 0f;
                float dist = toTarget.magnitude;

                if (toTarget.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), 10f * Time.deltaTime);
                }

                HandleTacticalMovement(toTarget, dist);

                if (dist <= combatRange && Time.time >= nextFireTime)
                {
                    FireAtTarget();
                }
            }
        }

        private void HandleTacticalMovement(Vector3 toTarget, float dist)
        {
            if (Time.time >= nextStrafeSwitch)
            {
                strafeDir = (Random.value > 0.5f) ? 1f : -1f;
                nextStrafeSwitch = Time.time + Random.Range(1.2f, 2.5f);
            }

            Vector3 moveDir = Vector3.zero;
            // Advance if far, retreat if too close, strafe sideways
            if (dist > 9f) moveDir += toTarget.normalized;
            else if (dist < 4.5f) moveDir -= toTarget.normalized;

            Vector3 strafe = Vector3.Cross(Vector3.up, toTarget.normalized) * strafeDir;
            moveDir += strafe * 0.8f;

            transform.position += moveDir.normalized * (moveSpeed * Time.deltaTime);
        }

        private void FindOptimalTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, combatRange * 1.5f);
            float closestDist = float.MaxValue;
            currentTarget = null;

            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                TeamMember otherTeam = hit.GetComponentInParent<TeamMember>() ?? hit.GetComponent<TeamMember>();
                if (otherTeam != null && otherTeam.TeamId == team.TeamId) continue;

                Health hp = hit.GetComponentInParent<Health>() ?? hit.GetComponent<Health>();
                if (hp != null && !hp.IsDead)
                {
                    float d = Vector3.Distance(transform.position, hit.transform.position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        currentTarget = hp.transform;
                    }
                }
            }
        }

        private void FireAtTarget()
        {
            nextFireTime = Time.time + (1f / fireRate);

            if (projectilePrefab != null)
            {
                Vector3 spawnPos = transform.position + Vector3.up * 0.8f + transform.forward * 0.6f;
                Vector3 spread = new Vector3(Random.Range(-0.04f, 0.04f), 0, Random.Range(-0.04f, 0.04f));
                Quaternion rot = Quaternion.LookRotation(transform.forward + spread);

                GameObject projObj = Instantiate(projectilePrefab, spawnPos, rot);
                if (projObj.TryGetComponent<Projectile>(out var proj))
                {
                    proj.Initialize(damagePerShot, 38f, 1, team.TeamId);
                }
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayShoot(0.4f);
        }

        private void HandleHitReaction(float cur, float max)
        {
            // Combat Roll / Dash impulse away from damage
            if (Random.value < 0.35f)
            {
                Vector3 dodgeDir = (transform.right * strafeDir) - (transform.forward * 0.5f);
                transform.position += dodgeDir.normalized * 2.2f;
            }
        }

        private void HandleDeath()
        {
            KillFeedUI.PostMessage($"☠ ELIMINATED: [AI] [{callsign}] neutralized", new Color(1f, 0.25f, 0.25f));
            if (GameManager.Instance != null) GameManager.Instance.AddCredits(25);

            if (deathLootPrefab != null)
            {
                Instantiate(deathLootPrefab, transform.position + Vector3.up * 0.4f, Quaternion.identity);
            }

            Destroy(gameObject, 0.5f);
        }

        private void OnGUI()
        {
            if (health.IsDead || !mainCam) return;

            Vector3 headPos = transform.position + Vector3.up * 1.8f;
            Vector3 screenPos = mainCam.WorldToScreenPoint(headPos);
            if (screenPos.z <= 0) return;

            float y = Screen.height - screenPos.y;
            float x = screenPos.x;

            GUI.color = new Color(1f, 0.3f, 0.3f);
            GUI.Label(new Rect(x - 60, y - 24, 120, 20), $"[AI] {callsign}");

            // Mini Health bar
            float hpPct = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
            GUI.Box(new Rect(x - 30, y - 4, 60, 6), "");
            GUI.color = hpPct > 0.35f ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.95f, 0.2f, 0.2f);
            GUI.Box(new Rect(x - 30, y - 4, 60 * hpPct, 6), "");
            GUI.color = Color.white;
        }
    }
}
