using UnityEngine;
using Deadzone.Core;
using Deadzone.Camera;

namespace Deadzone.Enemies
{
    public class BossStompAttack : MonoBehaviour
    {
        [Header("Stomp Parameters")]
        [SerializeField] private float stompRadius = 6.5f;
        [SerializeField] private float stompDamage = 40f;
        [SerializeField] private float stompInterval = 8.0f;

        private BossBrute boss;
        private float nextStompTime;

        private void Awake()
        {
            boss = GetComponent<BossBrute>();
        }

        private void Start()
        {
            nextStompTime = Time.time + stompInterval;
        }

        private void Update()
        {
            if (Time.time >= nextStompTime)
            {
                ExecuteStomp();
                nextStompTime = Time.time + stompInterval;
            }
        }

        public void ExecuteStomp()
        {
            if (TopDownCamera.Instance != null)
            {
                TopDownCamera.Instance.TriggerShake(0.5f, 0.35f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBossRoar(1.3f);
            }

            Collider[] hits = Physics.OverlapSphere(transform.position, stompRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player") && hit.TryGetComponent<IDamageable>(out var dmg))
                {
                    dmg.TakeDamage(stompDamage);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, stompRadius);
        }
    }
}
