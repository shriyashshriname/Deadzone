using System;
using UnityEngine;
using Deadzone.Core;
using Deadzone.Camera;

namespace Deadzone.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    public class PlayerDash : MonoBehaviour
    {
        [Header("Dash Settings")]
        [SerializeField] private float dashSpeed = 22f;
        [SerializeField] private float dashDuration = 0.18f;
        [SerializeField] private float dashCooldown = 1.4f;

        private CharacterController controller;
        private Health health;
        private bool isDashing = false;
        private float dashTimer = 0f;
        private float nextDashTime = 0f;
        private Vector3 dashDirection;

        public event Action OnDashStarted;
        public event Action OnDashEnded;
        public bool IsDashing => isDashing;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            health = GetComponent<Health>();
        }

        private void Update()
        {
            if (health.IsDead) return;

            // Trigger dash on Spacebar or Dash button
            if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextDashTime && !isDashing)
            {
                StartDash();
            }

            if (isDashing)
            {
                dashTimer -= Time.deltaTime;
                controller.Move(dashDirection * (dashSpeed * Time.deltaTime));

                if (dashTimer <= 0f)
                {
                    EndDash();
                }
            }
        }

        public void TryTriggerDash()
        {
            if (Time.time >= nextDashTime && !isDashing && !health.IsDead)
            {
                StartDash();
            }
        }

        private void StartDash()
        {
            isDashing = true;
            dashTimer = dashDuration;
            float cd = dashCooldown * (PlayerPerks.Instance != null ? PlayerPerks.Instance.DashCooldownMultiplier : 1.0f);
            nextDashTime = Time.time + cd;

            // Dash in input direction, or forward if stationary
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 inputDir = new Vector3(h, 0f, v).normalized;

            dashDirection = (inputDir.sqrMagnitude > 0.01f) ? inputDir : transform.forward;
            dashDirection.y = 0f;

            if (TopDownCamera.Instance != null)
            {
                TopDownCamera.Instance.TriggerShake(0.15f, 0.1f);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayShoot(0.4f);
            }

            OnDashStarted?.Invoke();
        }

        private void EndDash()
        {
            isDashing = false;
            OnDashEnded?.Invoke();
        }
    }
}
