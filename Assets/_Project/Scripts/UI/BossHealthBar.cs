using UnityEngine;
using UnityEngine.UI;
using Deadzone.Enemies;

namespace Deadzone.UI
{
    public class BossHealthBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject container;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Text bossNameText;
        [SerializeField] private Image fillImage;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.85f, 0.15f, 0.15f);
        [SerializeField] private Color enrageColor = new Color(1.0f, 0.45f, 0.0f);

        private BossBrute activeBoss;

        private void Awake()
        {
            if (container != null) container.SetActive(false);
        }

        private void Update()
        {
            // Auto-detect boss in scene if not assigned
            if (activeBoss == null)
            {
                var foundBoss = FindAnyObjectByType<BossBrute>();
                if (foundBoss != null)
                {
                    BindBoss(foundBoss);
                }
            }
        }

        public void BindBoss(BossBrute boss)
        {
            activeBoss = boss;
            if (activeBoss == null) return;

            if (container != null) container.SetActive(true);
            if (bossNameText != null) bossNameText.text = activeBoss.bossName;

            activeBoss.OnBossHealthChanged += UpdateHealth;
            activeBoss.GetComponent<Deadzone.Core.Health>().OnDeath += HideBar;
        }

        private void UpdateHealth(float current, float max)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = max;
                healthSlider.value = current;
            }

            // Color shift on enrage (<50% HP)
            if (fillImage != null)
            {
                fillImage.color = (current < max * 0.5f) ? enrageColor : normalColor;
            }
        }

        private void HideBar()
        {
            if (container != null) container.SetActive(false);
        }
    }
}
