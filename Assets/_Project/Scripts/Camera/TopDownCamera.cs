using UnityEngine;

namespace Deadzone.Camera
{
    public class TopDownCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Camera Offset & Angle")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 14f, -10f);
        [SerializeField] private float smoothSpeed = 6f;
        [SerializeField] private float pitchAngle = 55f;

        [Header("Portrait framing (phone held upright; default since Build #8)")]
        [Tooltip("Steeper than landscape so the tall screen shows more ground ahead and behind, less sky-ward depth")]
        [SerializeField] private float portraitPitch = 58f;
        [SerializeField] private float portraitDistance = 24f;
        [Tooltip("Vertical FOV. With a ~9:20 screen this shows ~15 m of ground across, so the hero reads about twice as big as in landscape")]
        [SerializeField] private float portraitFov = 70f;

        /// <summary>Tests/sim: frame as portrait even when the render target is not (e.g. batchmode).</summary>
        public static bool ForcePortrait { get; set; }

        private UnityEngine.Camera cam;
        private float landscapeFov;
        private Vector3 landscapeOffset;
        private float landscapePitch;

        public bool IsPortrait => ForcePortrait || Screen.height > Screen.width;

        [Header("Screen Shake")]
        private float shakeDuration = 0f;
        private float shakeMagnitude = 0f;
        private Vector3 shakeOffset = Vector3.zero;

        public static TopDownCamera Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            cam = GetComponent<UnityEngine.Camera>();
            landscapeFov = cam != null ? cam.fieldOfView : 60f;
            landscapeOffset = offset;
            landscapePitch = pitchAngle;
            ApplyFraming();
        }

        /// <summary>Switches between the landscape values from the scene and the portrait framing.</summary>
        private void ApplyFraming()
        {
            if (IsPortrait)
            {
                float p = portraitPitch * Mathf.Deg2Rad;
                offset = new Vector3(0f, Mathf.Sin(p) * portraitDistance, -Mathf.Cos(p) * portraitDistance);
                pitchAngle = portraitPitch;
                if (cam != null) cam.fieldOfView = portraitFov;
            }
            else
            {
                offset = landscapeOffset;
                pitchAngle = landscapePitch;
                if (cam != null) cam.fieldOfView = landscapeFov;
            }
            transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (!target) return;
            if (cam != null && (pitchAngle == portraitPitch) != IsPortrait) ApplyFraming();

            // Handle Screen Shake
            if (shakeDuration > 0)
            {
                shakeOffset = Random.insideUnitSphere * shakeMagnitude;
                shakeOffset.y = 0; // keep shake primarily on horizontal plane
                shakeDuration -= Time.deltaTime;
            }
            else
            {
                shakeOffset = Vector3.zero;
            }

            Vector3 desiredPosition = target.position + offset + shakeOffset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void TriggerShake(float magnitude = 0.2f, float duration = 0.15f)
        {
            shakeMagnitude = magnitude;
            shakeDuration = duration;
        }
    }
}
