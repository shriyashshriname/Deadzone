using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deadzone.Core;

namespace Deadzone.Combat
{
    [RequireComponent(typeof(Health))]
    public class HitFlash : MonoBehaviour
    {
        [Header("Flash Config")]
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.08f;

        private Health health;
        private List<Renderer> renderers = new List<Renderer>();
        private List<Color> originalColors = new List<Color>();
        private List<Color> originalEmission = new List<Color>();
        private Coroutine flashRoutine;

        private MaterialPropertyBlock propBlock;
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public static readonly int ColorId = Shader.PropertyToID("_Color");
        // Textured materials (base colour white) need an emissive flash to read; materials opt in via _EMISSION.
        public static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            health = GetComponent<Health>();
            CacheRenderersAndColors();
        }

        public void CacheRenderersAndColors()
        {
            renderers.Clear();
            originalColors.Clear();
            originalEmission.Clear();
            GetComponentsInChildren(true, renderers);
            foreach (var r in renderers)
            {
                originalEmission.Add(r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty(EmissionId) && r.sharedMaterial.IsKeywordEnabled("_EMISSION")
                    ? r.sharedMaterial.GetColor(EmissionId) : Color.black);
                if (r != null && r.sharedMaterial != null)
                {
                    if (r.sharedMaterial.HasProperty(BaseColorId))
                    {
                        originalColors.Add(r.sharedMaterial.GetColor(BaseColorId));
                    }
                    else if (r.sharedMaterial.HasProperty(ColorId))
                    {
                        originalColors.Add(r.sharedMaterial.GetColor(ColorId));
                    }
                    else
                    {
                        originalColors.Add(Color.white);
                    }
                }
                else
                {
                    originalColors.Add(Color.white);
                }
            }
        }

        private void Start()
        {
            if (health != null) health.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDestroy()
        {
            if (health != null) health.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(DoFlash());
        }

        public void ApplyFlashColors(Color c)
        {
            if (propBlock == null) propBlock = new MaterialPropertyBlock();

            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].GetPropertyBlock(propBlock);
                    propBlock.SetColor(BaseColorId, c);
                    propBlock.SetColor(ColorId, c);
                    propBlock.SetColor(EmissionId, c * 0.6f);
                    renderers[i].SetPropertyBlock(propBlock);
                }
            }
        }

        public void FlashImmediateForTest()
        {
            ApplyFlashColors(flashColor);
        }

        public Color GetCurrentPropertyBlockColor(Renderer rend, int propertyId)
        {
            if (rend == null) return Color.clear;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            rend.GetPropertyBlock(block);
            return block.GetColor(propertyId);
        }

        private IEnumerator DoFlash()
        {
            ApplyFlashColors(flashColor);

            yield return new WaitForSeconds(flashDuration);

            if (propBlock == null) propBlock = new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null && i < originalColors.Count)
                {
                    renderers[i].GetPropertyBlock(propBlock);
                    propBlock.SetColor(BaseColorId, originalColors[i]);
                    propBlock.SetColor(ColorId, originalColors[i]);
                    propBlock.SetColor(EmissionId, i < originalEmission.Count ? originalEmission[i] : Color.black); // keep built-in glow (robots)
                    renderers[i].SetPropertyBlock(propBlock);
                }
            }

            flashRoutine = null;
        }
    }
}
