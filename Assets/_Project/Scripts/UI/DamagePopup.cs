using UnityEngine;

namespace Deadzone.UI
{
    [RequireComponent(typeof(TextMesh))]
    public class DamagePopup : MonoBehaviour
    {
        [Header("Popup Motion")]
        [SerializeField] private float moveYSpeed = 2.5f;
        [SerializeField] private float disappearTimer = 0.55f;

        private TextMesh textMesh;
        private Color textColor;
        private float maxTimer;

        private void Awake()
        {
            textMesh = GetComponent<TextMesh>();
            textColor = textMesh.color;
            maxTimer = disappearTimer;
        }

        public void Setup(float damageAmount, bool isCritical = false)
        {
            textMesh.text = Mathf.RoundToInt(damageAmount).ToString();
            if (isCritical)
            {
                textMesh.fontSize = 32;
                textMesh.color = Color.yellow;
            }
            else
            {
                textMesh.fontSize = 24;
                textMesh.color = Color.white;
            }
            textColor = textMesh.color;
        }

        private void Update()
        {
            // Float upward and billboard towards camera
            transform.position += Vector3.up * (moveYSpeed * Time.deltaTime);

            if (UnityEngine.Camera.main != null)
            {
                transform.rotation = UnityEngine.Camera.main.transform.rotation;
            }

            disappearTimer -= Time.deltaTime;
            if (disappearTimer < 0f)
            {
                // Fade alpha
                textColor.a -= 4f * Time.deltaTime;
                textMesh.color = textColor;

                if (textColor.a <= 0f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
