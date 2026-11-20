using UnityEngine;
using Deadzone.Core;
using Deadzone.Combat;
using Deadzone.Player;

namespace Deadzone.Enemies
{
    [RequireComponent(typeof(Health))]
    public class SpitterEnemy : MonoBehaviour
    {
        [Header("Spitter Stats")]
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float preferredDistance = 9f;
        [SerializeField] private float fleeDistance = 4.5f;

        [Header("Spit Combat")]
        [SerializeField] private GameObject acidPrefab;
        [SerializeField] private Transform spitPoint;
        [SerializeField] private float spitInterval = 2.0f;
        [SerializeField] private float spitDamage = 18f;
        [SerializeField] private float spitSpeed = 14f;

        private Health health;
        private Transform playerTarget;
        private float nextSpitTime;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Start()
        {
            health.OnDeath += HandleDeath;
            if (PlayerController.Instance != null) playerTarget = PlayerController.Instance.transform;
            nextSpitTime = Time.time + Random.Range(1f, spitInterval);
        }

        private void OnDestroy()
        {
            if (health != null) health.OnDeath -= HandleDeath;
        }

        private void Update()
        {
            if (health.IsDead) return;
            if (!playerTarget && PlayerController.Instance != null) playerTarget = PlayerController.Instance.transform;
            if (!playerTarget) return;

            float dist = Vector3.Distance(transform.position, playerTarget.position);
            Vector3 dirToPlayer = (playerTarget.position - transform.position).normalized;
            dirToPlayer.y = 0f;

            // Rotate towards player
            if (dirToPlayer.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirToPlayer), 8f * Time.deltaTime);
            }

            // Tactical positioning: retreat if too close, approach if too far
            if (dist < fleeDistance)
            {
                transform.position -= dirToPlayer * (moveSpeed * Time.deltaTime);
            }
            else if (dist > preferredDistance)
            {
                transform.position += dirToPlayer * (moveSpeed * Time.deltaTime);
            }

            // Spit attack
            if (dist <= preferredDistance + 2f && Time.time >= nextSpitTime)
            {
                SpitAcid();
            }
        }

        private void SpitAcid()
        {
            Deadzone.Core.CharacterAnimDriver.PlayAttack(this);
            nextSpitTime = Time.time + spitInterval;
            if (!acidPrefab) return;

            Vector3 spawnPos = spitPoint ? spitPoint.position : transform.position + transform.forward + Vector3.up * 0.8f;
            Quaternion rot = transform.rotation;

            GameObject acid = Instantiate(acidPrefab, spawnPos, rot);
            if (acid.TryGetComponent<AcidProjectile>(out var proj))
            {
                proj.Initialize(spitDamage, spitSpeed);
            }
        }

        public void SetSpeedMultiplier(float mult)
        {
            moveSpeed *= mult;
        }

        private void HandleDeath()
        {
            if (TryGetComponent<Collider>(out var col)) col.enabled = false;
            Destroy(gameObject, 0.4f);
        }
    }
}
