using System;
using System.Collections.Generic;
using UnityEngine;
using Deadzone.Data;
using Deadzone.Core;

namespace Deadzone.Player
{
    public class SkinManager : MonoBehaviour
    {
        public static SkinManager Instance { get; private set; }

        [Header("Available Skins")]
        [SerializeField] private List<SkinData> allSkins = new List<SkinData>();
        [SerializeField] private Renderer targetRenderer;

        private SkinData currentEquippedSkin;

        public event Action<SkinData> OnSkinEquipped;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        }

        public bool TryUnlockSkin(SkinData skin)
        {
            if (SaveManager.Instance == null || skin == null) return false;

            var save = SaveManager.Instance.CurrentSave;
            if (save.totalGems >= skin.gemCost && !save.unlockedWeapons.Contains(skin.skinId))
            {
                save.totalGems -= skin.gemCost;
                save.unlockedWeapons.Add(skin.skinId);
                SaveManager.Instance.SaveGame();
                return true;
            }

            return false;
        }

        public void EquipSkin(SkinData skin)
        {
            if (skin == null || targetRenderer == null) return;

            currentEquippedSkin = skin;

            if (skin.customMaterial != null)
            {
                targetRenderer.material = skin.customMaterial;
            }
            else if (targetRenderer.material.HasProperty("_BaseColor"))
            {
                targetRenderer.material.SetColor("_BaseColor", skin.primaryColor);
            }

            OnSkinEquipped?.Invoke(skin);
        }
    }
}
