using UnityEngine;

namespace Deadzone.Core
{
    /// <summary>
    /// Drives a character visual's Animator from how its gameplay root actually moves.
    /// Works for any mover (CharacterController, NavMeshAgent, scripted AI) because it measures
    /// world-space position deltas. Lives on the "Visual" child built by CharacterArtBuilder.
    ///
    /// Twin-stick aware: the root faces the aim direction, so when the character strafes the body
    /// twists toward the movement (up to maxTwist), and when it backpedals the run plays in reverse.
    /// Playback rate is matched to the measured ground speed of the run cycle so feet don't slide.
    /// Animator params: Speed (0 idle .. 1 run), AnimSpeed (signed playback multiplier).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimDriver : MonoBehaviour
    {
        [Tooltip("World speed (m/s) the run cycle covers at 1x playback (measured at build time).")]
        [SerializeField] private float runReferenceSpeed = 4f;
        [Tooltip("Start moving above this speed, stop below idleExit (hysteresis avoids idle/run flicker).")]
        [SerializeField] private float moveEnter = 0.35f;
        [SerializeField] private float idleExit = 0.15f;
        [SerializeField] private float speedDamping = 0.08f;
        [SerializeField] private Vector2 playbackRange = new Vector2(0.6f, 2.3f);
        [Tooltip("Max body twist toward the movement direction when strafing (degrees).")]
        [SerializeField] private float maxTwist = 65f;
        [SerializeField] private float twistSmoothTime = 0.09f;
        [Tooltip("Movement more than this many degrees from facing counts as backpedalling.")]
        [SerializeField] private float backpedalAngle = 115f;
        [SerializeField] private bool randomizePhase = true;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int AnimSpeedId = Animator.StringToHash("AnimSpeed");

        private Animator animator;
        private Transform mover;
        private Quaternion baseLocalRotation;
        private Vector3 lastPos;
        private Vector3 smoothedVelocity;
        private bool moving;
        private float twist, twistVel;

        public float CurrentSpeed => smoothedVelocity.magnitude;
        public float RunReferenceSpeed => runReferenceSpeed;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            mover = transform.parent != null ? transform.parent : transform;
            baseLocalRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            lastPos = mover.position;
            smoothedVelocity = Vector3.zero;
            moving = false;
            if (randomizePhase && animator.runtimeAnimatorController != null)
                animator.Play(0, 0, Random.value);
        }

        private void LateUpdate()
        {
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

            float targetTwist = 0f;
            float playback = 1f;
            float speedN = 0f;
            if (moving)
            {
                Vector3 local = mover.InverseTransformDirection(smoothedVelocity);
                float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg; // 0 = forward
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

        public void Configure(float referenceSpeed) => runReferenceSpeed = Mathf.Max(0.5f, referenceSpeed);
    }
}
