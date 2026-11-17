using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Deadzone.Player;

namespace Deadzone.UI
{
    public class PerkSelectionUI : MonoBehaviour
    {
        [Header("Perk Window")]
        [SerializeField] private GameObject windowContainer;
        [SerializeField] private List<Button> cardButtons = new List<Button>();
        [SerializeField] private List<Text> cardTitles = new List<Text>();
        [SerializeField] private List<Text> cardDescriptions = new List<Text>();

        private List<PerkOption> currentOptions = new List<PerkOption>();

        private void Awake()
        {
            if (windowContainer != null) windowContainer.SetActive(false);
        }

        private void Start()
        {
            if (PlayerPerks.Instance != null)
            {
                PlayerPerks.Instance.OnPerkSelectionPrompt += DisplayPerks;
            }

            // Hook card buttons
            for (int i = 0; i < cardButtons.Count; i++)
            {
                int index = i;
                cardButtons[i].onClick.AddListener(() => OnCardClicked(index));
            }
        }

        private void OnDestroy()
        {
            if (PlayerPerks.Instance != null)
            {
                PlayerPerks.Instance.OnPerkSelectionPrompt -= DisplayPerks;
            }
        }

        public void DisplayPerks(List<PerkOption> options)
        {
            currentOptions = options;
            if (windowContainer != null) windowContainer.SetActive(true);

            for (int i = 0; i < cardButtons.Count; i++)
            {
                if (i < options.Count)
                {
                    cardButtons[i].gameObject.SetActive(true);
                    if (i < cardTitles.Count && cardTitles[i] != null) cardTitles[i].text = options[i].title;
                    if (i < cardDescriptions.Count && cardDescriptions[i] != null) cardDescriptions[i].text = options[i].description;
                }
                else
                {
                    cardButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnCardClicked(int index)
        {
            if (index >= 0 && index < currentOptions.Count)
            {
                if (PlayerPerks.Instance != null)
                {
                    PlayerPerks.Instance.SelectPerk(currentOptions[index]);
                }
            }

            if (windowContainer != null) windowContainer.SetActive(false);
        }
    }
}
