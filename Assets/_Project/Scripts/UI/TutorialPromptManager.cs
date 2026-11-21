using UnityEngine;
using Deadzone.Combat;
using Deadzone.Core;

namespace Deadzone.UI
{
    /// <summary>
    /// Contextual onboarding tutorial banner for Gate 4 Vertical Slice.
    /// Displays progressive touch & tactical instructions to new operators.
    /// </summary>
    public class TutorialPromptManager : MonoBehaviour
    {
        public static TutorialPromptManager Instance { get; private set; }

        private const string PrefKey = "Deadzone_Tutorial_Shown";
        private int currentStep = 0;
        private float stepTimer = 0f;
        private string currentMessage = "";
        private bool isVisible = false;

        private readonly string[] tutorialMessages = new string[]
        {
            "MOVE WITH LEFT STICK • AIM WITH RIGHT STICK",
            "GUNFIRE NOISE ATTRACTS INFECTED • WATCH RADAR",
            "SCAVENGE AMMO & RESTORE POWER SUBSTATIONS",
            "HOLD INTERACT ON GOLDEN AIRDROPS FOR CRITICAL GEAR",
            "HOLD HELIPAD EXTRACTION TO EVAC WITH 1.5X BOUNTY"
        };

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // Show tutorial for first-time runs
            ShowNextStep(0);
        }

        private void Update()
        {
            if (!isVisible) return;

            stepTimer += Time.deltaTime;

            // Automatically transition steps based on play time or player actions
            if (currentStep == 0 && stepTimer > 7f)
            {
                ShowNextStep(1);
            }
            else if (currentStep == 1 && stepTimer > 8f)
            {
                ShowNextStep(2);
            }
            else if (currentStep == 2 && stepTimer > 9f)
            {
                ShowNextStep(3);
            }
            else if (currentStep == 3 && stepTimer > 10f)
            {
                ShowNextStep(4);
            }
            else if (currentStep == 4 && stepTimer > 10f)
            {
                isVisible = false;
                PlayerPrefs.SetInt(PrefKey, 1);
                PlayerPrefs.Save();
            }
        }

        public void ShowNextStep(int stepIndex)
        {
            if (stepIndex >= tutorialMessages.Length)
            {
                isVisible = false;
                return;
            }
            currentStep = stepIndex;
            currentMessage = tutorialMessages[stepIndex];
            stepTimer = 0f;
            isVisible = true;
        }

        public void Dismiss()
        {
            isVisible = false;
        }

        private void OnGUI()
        {
            if (!isVisible || string.IsNullOrEmpty(currentMessage)) return;
            UIScale.Begin();

            float screenW = UIScale.W;
            float screenH = UIScale.H;

            // Compact centered banner below wave tracker
            float bannerW = Mathf.Min(screenW * 0.85f, 540f);
            float bannerH = 38f;
            float bannerX = (screenW - bannerW) * 0.5f;
            float bannerY = UIScale.Portrait ? 256f : 56f;

            GUI.color = new Color(0.05f, 0.10f, 0.15f, 0.88f);
            GUI.DrawTexture(new Rect(bannerX, bannerY, bannerW, bannerH), Texture2D.whiteTexture);

            // Glowing cyan accent line at top
            GUI.color = new Color(0.2f, 0.85f, 1.0f, 0.95f);
            GUI.DrawTexture(new Rect(bannerX, bannerY, bannerW, 2f), Texture2D.whiteTexture);

            // Tutorial instruction text
            GUIStyle textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp((int)(screenH / 45f), 12, 15),
                fontStyle = FontStyle.Bold
            };
            textStyle.normal.textColor = Color.white;

            GUI.color = Color.white;
            GUI.Label(new Rect(bannerX + 10f, bannerY + 2f, bannerW - 20f, bannerH - 4f), $"ⓘ  {currentMessage}", textStyle);
        }
    }
}
