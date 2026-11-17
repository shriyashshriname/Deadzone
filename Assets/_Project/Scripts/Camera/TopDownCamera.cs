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
            transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (!target) return;

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
