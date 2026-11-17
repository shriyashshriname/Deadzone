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
        private Coroutine flashRoutine;

        private void Awake()
        {
            health = GetComponent<Health>();

            // Collect all renderers on this entity
            GetComponentsInChildren(true, renderers);
            foreach (var r in renderers)
            {
                if (r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Color"))
                {
                    originalColors.Add(r.sharedMaterial.color);
                }
                else
                {
                    originalColors.Add(Color.white);
                }
            }
        }

        private void Start()
        {
            health.OnHealthChanged += HandleHealthChanged;
        }

        private void OnDestroy()
        {
            if (health != null) health.OnHealthChanged -= HandleHealthChanged;
        }

        private MaterialPropertyBlock propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void HandleHealthChanged(float current, float max)
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(DoFlash());
        }

        private IEnumerator DoFlash()
        {
            if (propBlock == null) propBlock = new MaterialPropertyBlock();

            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].GetPropertyBlock(propBlock);
                    propBlock.SetColor(BaseColorId, flashColor);
                    propBlock.SetColor(ColorId, flashColor);
                    renderers[i].SetPropertyBlock(propBlock);
                }
            }

            yield return new WaitForSeconds(flashDuration);

            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].GetPropertyBlock(propBlock);
                    propBlock.SetColor(BaseColorId, originalColors[i]);
                    propBlock.SetColor(ColorId, originalColors[i]);
                    renderers[i].SetPropertyBlock(propBlock);
                }
            }

            flashRoutine = null;
        }
    }
}
