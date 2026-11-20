using System.Collections;
using UnityEngine;

namespace Deadzone.Core
{
    /// <summary>
    /// Drives a character visual's Animator from how its gameplay root actually moves, plus
    /// attack / hit / death reactions. Works for any mover (CharacterController, NavMeshAgent,
    /// scripted AI) because it measures world-space position deltas. Lives on the "Visual" child
    /// built by CharacterArtBuilder / CreatureBuilder.
    ///
    /// Twin-stick aware: the body twists toward strafe direction and the run plays in reverse when
    /// backpedalling. Playback is matched to the measured ground speed of the locomotion cycle.
    /// Animator params: Speed (0 idle .. 1 run), AnimSpeed (signed playback), optional triggers
    /// Attack, Hit, Dead. Missing triggers fall back to procedural motion (lunge / flinch / topple).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimDriver : MonoBehaviour
    {
        [Tooltip("World speed (m/s) the locomotion cycle covers at 1x playback (measured at build time).")]
        [SerializeField] private float runReferenceSpeed = 4f;
        [SerializeField] private float moveEnter = 0.35f;
        [SerializeField] private float idleExit = 0.15f;
        [SerializeField] private float speedDamping = 0.08f;
        [SerializeField] private Vector2 playbackRange = new Vector2(0.6f, 2.3f);
        [SerializeField] private float maxTwist = 65f;
        [SerializeField] private float twistSmoothTime = 0.09f;
        [SerializeField] private float backpedalAngle = 115f;
        [SerializeField] private bool randomizePhase = true;
        [Tooltip("Seconds the corpse stays before sinking away.")]
        [SerializeField] private float corpseTime = 2.2f;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int AnimSpeedId = Animator.StringToHash("AnimSpeed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DeadId = Animator.StringToHash("Dead");

        private Animator animator;
        private Transform mover;
        private Health health;
        private Quaternion baseLocalRotation;
        private Vector3 lastPos;
        private Vector3 smoothedVelocity;
        private bool moving, dead, hasAttack, hasHit, hasDead, isFriendly;
        private float twist, twistVel, lastHp = -1f, nextHitTime;

        public float CurrentSpeed => smoothedVelocity.magnitude;
        public float RunReferenceSpeed => runReferenceSpeed;

        /// <summary>Called by enemy AI at the moment it attacks.</summary>
        public static void PlayAttack(Component owner)
        {
            if (owner == null) return;
            var d = owner.GetComponentInChildren<CharacterAnimDriver>();
            if (d != null) d.TriggerAttack();
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            mover = transform.parent != null ? transform.parent : transform;
            baseLocalRotation = transform.localRotation;
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == AttackId) hasAttack = true;
                else if (p.nameHash == HitId) hasHit = true;
                else if (p.nameHash == DeadId) hasDead = true;
            }
            health = mover.GetComponent<Health>();
            var team = mover.GetComponent<TeamMember>();
            isFriendly = team != null && team.TeamId == 1;
        }

        private void OnEnable()
        {
            lastPos = mover.position;
            smoothedVelocity = Vector3.zero;
            moving = false;
            if (randomizePhase && animator.runtimeAnimatorController != null)
                animator.Play(0, 0, Random.value);
            if (health != null)
            {
                health.OnDeath += HandleDeath;
                health.OnHealthChanged += HandleHealthChanged;
                lastHp = health.CurrentHealth;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDeath -= HandleDeath;
                health.OnHealthChanged -= HandleHealthChanged;
            }
        }

        private void LateUpdate()
        {
            if (dead) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 delta = mover.position - lastPos;
            lastPos = mover.position;
            delta.y = 0f;
            Vector3 v = delta / dt;
            if (v.sqrMagnitude > 2500f) v = Vector3.zero; // teleport / respawn guard
            smoothedVelocity = Vector3.Lerp(smoothedVelocity, v, 1f - Mathf.Exp(-dt / Mathf.Max(speedDamping, 0.001f)));

            float speed = smoothedVelocity.magnitude;
            moving = moving ? speed > idleExit : speed > moveEnter;

            float targetTwist = 0f, playback = 1f, speedN = 0f;
            if (moving)
            {
                Vector3 local = mover.InverseTransformDirection(smoothedVelocity);
                float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                bool back = Mathf.Abs(angle) > backpedalAngle;
                float rel = back ? Mathf.DeltaAngle(180f, angle) : angle;
                targetTwist = Mathf.Clamp(rel, -maxTwist, maxTwist);
                playback = Mathf.Clamp(speed / Mathf.Max(runReferenceSpeed, 0.1f), playbackRange.x, playbackRange.y);
                if (back) playback = -playback;
                speedN = Mathf.Clamp01(speed / Mathf.Max(runReferenceSpeed * 0.6f, 0.1f));
            }

            twist = Mathf.SmoothDampAngle(twist, targetTwist, ref twistVel, twistSmoothTime);
            transform.localRotation = Quaternion.Euler(0f, twist, 0f) * baseLocalRotation;

            animator.SetFloat(SpeedId, speedN, 0.06f, dt);
            animator.SetFloat(AnimSpeedId, playback);
        }

        public void TriggerAttack()
        {
            if (dead) return;
            if (hasAttack) animator.SetTrigger(AttackId);
            else StartCoroutine(Lunge());
        }

        private void HandleHealthChanged(float current, float max)
        {
            bool damaged = lastHp >= 0f && current < lastHp;
            lastHp = current;
            if (!damaged || dead || isFriendly || Time.time < nextHitTime) return;
            nextHitTime = Time.time + 0.45f;
            if (hasHit) animator.SetTrigger(HitId);
        }

        private void HandleDeath()
        {
            if (dead) return;
            dead = true;
            OnDisable();
            animator.SetFloat(SpeedId, 0f);
            animator.SetFloat(AnimSpeedId, 1f);
            if (isFriendly)
            {
                // Player / squadmate: downed handling belongs to gameplay; just topple in place.
                if (hasDead) animator.SetTrigger(DeadId); else StartCoroutine(Topple(false));
                return;
            }
            // Enemy: detach so the corpse outlives the gameplay object (which is destroyed quickly).
            transform.SetParent(null, true);
            if (hasDead) animator.SetTrigger(DeadId);
            StartCoroutine(Topple(hasDead));
        }

        private IEnumerator Lunge()
        {
            Vector3 start = transform.localPosition;
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / 0.25f * Mathf.PI) * 0.25f;
                transform.localPosition = start + transform.parent.InverseTransformDirection(mover.forward) * k;
                yield return null;
            }
            transform.localPosition = start;
        }

        private IEnumerator Topple(bool animatedDeath)
        {
            if (!animatedDeath)
            {
                // No death clip: fall over sideways with a small bounce.
                Quaternion from = transform.rotation;
                Quaternion to = Quaternion.AngleAxis(Random.value < 0.5f ? 85f : -85f, transform.forward) * from;
                for (float t = 0f; t < 0.35f; t += Time.deltaTime)
                {
                    float k = t / 0.35f; k = k * k;
                    transform.rotation = Quaternion.Slerp(from, to, k);
                    yield return null;
                }
                transform.rotation = to;
                animator.enabled = false;
            }
            if (isFriendly) yield break;
            yield return new WaitForSeconds(corpseTime);
            Vector3 p = transform.position;
            for (float t = 0f; t < 1.2f; t += Time.deltaTime)
            {
                transform.position = p + Vector3.down * (t / 1.2f) * 1.2f;
                yield return null;
            }
            Destroy(gameObject);
        }

        public void Configure(float referenceSpeed) => runReferenceSpeed = Mathf.Max(0.5f, referenceSpeed);
    }
}
