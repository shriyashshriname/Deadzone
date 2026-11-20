using UnityEngine;

namespace Deadzone.Core
{
    /// <summary>
    /// Drives a character visual's Animator from how fast its gameplay root actually moves.
    /// Works for any mover (CharacterController, NavMeshAgent, scripted AI) because it measures
    /// world-space position deltas instead of reading a specific component.
    /// Lives on the visual child created by Deadzone.Editor.CharacterArtBuilder.
    /// Animator params: Speed (0 idle .. 1 full run), AnimSpeed (playback multiplier).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimDriver : MonoBehaviour
    {
        [Tooltip("World speed (m/s) at which the run cycle plays at 1x.")]
        [SerializeField] private float runReferenceSpeed = 6f;
        [Tooltip("Below this speed (m/s) the character idles.")]
        [SerializeField] private float idleThreshold = 0.15f;
        [SerializeField] private float speedDamping = 0.12f;
        [Tooltip("Clamp on run-cycle playback rate so slow shamblers and sprinters stay readable.")]
        [SerializeField] private Vector2 playbackRange = new Vector2(0.55f, 1.6f);
        [Tooltip("Idle/locomotion playback offset so a horde doesn't move in lockstep.")]
        [SerializeField] private bool randomizePhase = true;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int AnimSpeedId = Animator.StringToHash("AnimSpeed");

        private Animator animator;
        private Transform mover;
        private Vector3 lastPos;
        private float smoothedSpeed;

        public float CurrentSpeed => smoothedSpeed;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
            mover = transform.parent != null ? transform.parent : transform;
        }

        private void OnEnable()
        {
            lastPos = mover.position;
            smoothedSpeed = 0f;
            if (randomizePhase && animator.runtimeAnimatorController != null)
                animator.Play(0, 0, Random.value);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 delta = mover.position - lastPos;
            lastPos = mover.position;
            delta.y = 0f;
            float raw = delta.magnitude / dt;
            if (raw > 50f) raw = 0f; // teleport / respawn guard

            smoothedSpeed = Mathf.Lerp(smoothedSpeed, raw, 1f - Mathf.Exp(-dt / Mathf.Max(speedDamping, 0.001f)));

            float speedN = smoothedSpeed < idleThreshold ? 0f : Mathf.Clamp01(smoothedSpeed / runReferenceSpeed * 1.5f);
            float playback = speedN > 0f
                ? Mathf.Clamp(smoothedSpeed / runReferenceSpeed, playbackRange.x, playbackRange.y)
                : 1f;

            animator.SetFloat(SpeedId, speedN);
            animator.SetFloat(AnimSpeedId, playback);
        }

        public void Configure(float referenceSpeed) => runReferenceSpeed = Mathf.Max(0.5f, referenceSpeed);
    }
}
