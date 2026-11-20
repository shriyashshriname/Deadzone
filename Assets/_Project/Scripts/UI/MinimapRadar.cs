using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Deadzone.Player;
using Deadzone.Core;

namespace Deadzone.UI
{
    public class MinimapRadar : MonoBehaviour
    {
        [Header("Radar Settings")]
        [SerializeField] private RectTransform radarCompass;
        [SerializeField] private RectTransform playerBlip;
        [SerializeField] private GameObject enemyBlipPrefab;
        [SerializeField] private RectTransform extractionBlip;
        [SerializeField] private float radarRadius = 65f;
        [SerializeField] private float worldScanDistance = 30f;

        private List<RectTransform> activeBlips = new List<RectTransform>();

        private void Update()
        {
            if (PlayerController.Instance == null) return;
            Vector3 playerPos = PlayerController.Instance.transform.position;

            // Clear old blips
            foreach (var b in activeBlips)
            {
                if (b != null) Destroy(b.gameObject);
            }
            activeBlips.Clear();

            // Scan for enemies
            Collider[] hits = Physics.OverlapSphere(playerPos, worldScanDistance);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    Vector3 diff = hit.transform.position - playerPos;
                    Vector2 radarOffset = new Vector2(diff.x, diff.z) * (radarRadius / worldScanDistance);

                    if (radarOffset.magnitude <= radarRadius && enemyBlipPrefab != null)
                    {
                        GameObject blip = Instantiate(enemyBlipPrefab, radarCompass);
                        RectTransform rt = blip.GetComponent<RectTransform>();
                        rt.anchoredPosition = radarOffset;
                        activeBlips.Add(rt);
                    }
                }
            }

            // Update extraction beacon if active
            var extraction = FindAnyObjectByType<ExtractionZone>();
            if (extraction != null && extractionBlip != null)
            {
                extractionBlip.gameObject.SetActive(extraction.IsActive);
                if (extraction.IsActive)
                {
                    Vector3 diff = extraction.transform.position - playerPos;
                    Vector2 offset = new Vector2(diff.x, diff.z).normalized * (radarRadius * 0.9f);
                    extractionBlip.anchoredPosition = offset;
                }
            }
        }
    }
}
