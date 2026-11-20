using UnityEngine;
using UnityEngine.AI;
using Deadzone.Core;
using Deadzone.Player;

namespace Deadzone.Enemies
{
    [RequireComponent(typeof(Health))]
    public class ZombieEnemy : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 8f;
        [SerializeField] private float stoppingDistance = 1.2f;

        [Header("Combat")]
        [SerializeField] private float damage = 15f;
        [SerializeField] private float attackInterval = 1.0f;
        [SerializeField] private float attackRange = 1.5f;

        [Header("AI Pathing")]
        [SerializeField] private float pathUpdateInterval = 0.25f;

        private Health health;
        private NavMeshAgent navAgent;
        private Transform playerTarget;
        private float nextAttackTime;
        private float nextPathUpdateTime;
        private bool isDead;

        private void Awake()
        {
            health = GetComponent<Health>();
            navAgent = GetComponent<NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.speed = moveSpeed;
                navAgent.stoppingDistance = stoppingDistance;
                navAgent.updateRotation = false; // Custom smooth rotation
            }
            // Stagger initial path update so all enemies don't tick on the same frame
            nextPathUpdateTime = Time.time + Random.Range(0f, pathUpdateInterval);
        }

        private void Start()
        {
            health.OnDeath += HandleDeath;

            if (PlayerController.Instance != null)
            {
                playerTarget = PlayerController.Instance.transform;
            }

            if (InfectedSwarmDirector.Instance != null)
            {
                InfectedSwarmDirector.Instance.RegisterEnemy(this);
            }
        }

        private void OnDestroy()
        {
            if (health != null) health.OnDeath -= HandleDeath;
            if (InfectedSwarmDirector.Instance != null)
            {
                InfectedSwarmDirector.Instance.UnregisterEnemy(this);
            }
        }

        private void Update()
        {
            if (isDead) return;

            if (!playerTarget && PlayerController.Instance != null)
            {
                playerTarget = PlayerController.Instance.transform;
            }

            if (!playerTarget) return;

            float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

            // 1. Pathing Execution (NavMeshAgent or Obstacle-Aware Fallback)
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                ExecuteNavMeshMovement(distanceToPlayer);
            }
            else
            {
                ExecuteObstacleAwareMovement(distanceToPlayer);
            }

            // 2. Combat Execution
            if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
            {
                AttackPlayer();
            }
        }

        private void ExecuteNavMeshMovement(float distanceToPlayer)
        {
            if (Time.time >= nextPathUpdateTime)
            {
                nextPathUpdateTime = Time.time + pathUpdateInterval + Random.Range(-0.03f, 0.03f);
                if (distanceToPlayer > stoppingDistance)
                {
                    navAgent.isStopped = false;
                    navAgent.SetDestination(playerTarget.position);
                }
                else
                {
                    navAgent.isStopped = true;
                }
            }

            // Rotate towards movement direction or target
            Vector3 desiredDir = navAgent.desiredVelocity;
            desiredDir.y = 0f;
            if (desiredDir.sqrMagnitude > 0.05f)
            {
                Quaternion targetRot = Quaternion.LookRotation(desiredDir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }
            else
            {
                Vector3 toPlayer = (playerTarget.position - transform.position);
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.05f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
                }
            }
        }

        private void ExecuteObstacleAwareMovement(float distanceToPlayer)
        {
            if (distanceToPlayer <= stoppingDistance) return;

            Vector3 direction = (playerTarget.position - transform.position).normalized;
            direction.y = 0f;

            // Obstacle probe: raycast forward to avoid containers/walls
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, out RaycastHit hit, 1.2f))
            {
                if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Enemy"))
                {
                    Vector3 tangent = Vector3.Cross(hit.normal, Vector3.up);
                    if (Vector3.Dot(tangent, direction) < 0f) tangent = -tangent;
                    direction = (direction + tangent * 1.6f).normalized;
                }
            }

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            Deadzone.Core.EnemyMotor.Step(this, direction * (moveSpeed * Time.deltaTime));
        }

        private void AttackPlayer()
        {
            Deadzone.Core.CharacterAnimDriver.PlayAttack(this);
            nextAttackTime = Time.time + attackInterval;

            if (playerTarget.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(damage);
            }
        }

        public void SetSpeedMultiplier(float mult)
        {
            moveSpeed *= mult;
            if (navAgent != null) navAgent.speed = moveSpeed;
        }

        public bool IsAlerted { get; private set; }
        public Vector3 LastSoundOrigin { get; private set; }

        public virtual void OnAcousticNoiseAlert(Vector3 soundOrigin)
        {
            if (isDead) return;
            IsAlerted = true;
            LastSoundOrigin = soundOrigin;

            if (PlayerController.Instance != null)
            {
                playerTarget = PlayerController.Instance.transform;
            }
        }

        private void HandleDeath()
        {
            isDead = true;
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
                navAgent.enabled = false;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayHit();

            if (TryGetComponent<Collider>(out var col)) col.enabled = false;

            Destroy(gameObject, 0.25f);
        }
    }
}
