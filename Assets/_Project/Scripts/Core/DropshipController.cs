using System;
using System.Collections;
using UnityEngine;
using Deadzone.UI;

namespace Deadzone.Core
{
    /// <summary>
    /// The dropship that serves the helipad (on the ExtractionZone object):
    ///  - Insertion: the run starts with the ship on the pad; it lifts off and leaves.
    ///  - Evac: when the zone activates it flies in, hovers and lands.
    ///  - Departure: when the hold completes the player boards, the ship takes off and flies away,
    ///    and only then is the extraction finalised (debrief / next level).
    /// The model's forward is +Z (nose). Engine glow lights pulse while flying.
    /// </summary>
    public class DropshipController : MonoBehaviour
    {
        public static DropshipController Instance { get; private set; }

        [SerializeField] private GameObject shipPrefab;
        [SerializeField] private float cruiseHeight = 26f;
        [SerializeField] private float approachDistance = 70f;
        [SerializeField] private bool playInsertion = true;

        private Transform ship;
        private Light[] engineLights;
        private ExtractionZone zone;
        private Vector3 padPos;

        private void Awake()
        {
            Instance = this;
            zone = GetComponent<ExtractionZone>();
            padPos = new Vector3(transform.position.x, 0f, transform.position.z);
        }

        private void OnEnable()
        {
            if (zone != null) zone.OnZoneActivated += HandleEvacCalled;
        }

        private void OnDisable()
        {
            if (zone != null) zone.OnZoneActivated -= HandleEvacCalled;
        }

        private void Start()
        {
            if (shipPrefab == null) return;
            ship = Instantiate(shipPrefab, padPos, Quaternion.Euler(0f, 30f, 0f)).transform;
            ship.name = "Dropship";
            foreach (var c in ship.GetComponentsInChildren<Collider>()) Destroy(c);
            engineLights = new Light[2];
            for (int i = 0; i < 2; i++)
            {
                var l = new GameObject("EngineLight").AddComponent<Light>();
                l.transform.SetParent(ship, false);
                l.transform.localPosition = new Vector3(i == 0 ? -4f : 4f, 0.6f, -0.2f);
                l.type = LightType.Point; l.color = new Color(0.5f, 0.9f, 1f); l.range = 7f; l.intensity = 0f;
                l.shadows = LightShadows.None;
                engineLights[i] = l;
            }
            if (playInsertion) StartCoroutine(Insertion());
            else ship.gameObject.SetActive(false);
        }

        private void SetEngines(float k)
        {
            if (engineLights == null) return;
            foreach (var l in engineLights) if (l != null) l.intensity = k * (2.5f + Mathf.Sin(Time.time * 18f) * 0.6f);
        }

        private IEnumerator Insertion()
        {
            yield return new WaitForSeconds(1.2f);
            KillFeedUI.PostMessage("✈ DROPSHIP: Operator deployed. Clear the zone and hold out for evac.", Palette.Objective);
            yield return Fly(padPos, padPos + Vector3.up * 8f, 2.2f, 0f, ease: true);
            Vector3 away = padPos + Quaternion.Euler(0f, 30f, 0f) * Vector3.forward * approachDistance + Vector3.up * cruiseHeight;
            yield return Fly(ship.position, away, 3.5f, 12f, ease: false);
            ship.gameObject.SetActive(false);
        }

        private void HandleEvacCalled()
        {
            if (ship == null) return;
            StopAllCoroutines();
            StartCoroutine(Arrive());
        }

        private IEnumerator Arrive()
        {
            ship.gameObject.SetActive(true);
            Vector3 from = padPos + new Vector3(-approachDistance, cruiseHeight, -approachDistance * 0.6f);
            Vector3 hover = padPos + Vector3.up * 9f;
            ship.position = from;
            yield return Fly(from, hover, 4.5f, 10f, ease: true);
            yield return Fly(hover, padPos, 2.4f, 0f, ease: true);
            SetEngines(0.35f);
            KillFeedUI.PostMessage("✈ DROPSHIP DOWN on the helipad — get aboard! Hold position to extract.", Palette.Objective);
        }

        /// <summary>Boards the player, flies away, then calls onDeparted (finalises the extraction).</summary>
        public void Depart(Action onDeparted)
        {
            if (ship == null || !ship.gameObject.activeInHierarchy) { onDeparted?.Invoke(); return; }
            StopAllCoroutines();
            StartCoroutine(DepartRoutine(onDeparted));
        }

        private IEnumerator DepartRoutine(Action onDeparted)
        {
            BoardPlayer();
            KillFeedUI.PostMessage("✈ WHEELS UP! Extraction successful.", Palette.Health);
            yield return Fly(ship.position, padPos + Vector3.up * 10f, 2.0f, 0f, ease: true);
            Vector3 away = padPos + ship.forward * approachDistance + Vector3.up * cruiseHeight;
            yield return Fly(ship.position, away, 2.6f, 14f, ease: false);
            onDeparted?.Invoke();
        }

        private void BoardPlayer()
        {
            var pc = Deadzone.Player.PlayerController.Instance;
            if (pc == null) return;
            if (pc.TryGetComponent<CharacterController>(out var cc)) cc.enabled = false; // out of reach of enemies
            pc.enabled = false;
            pc.transform.SetParent(ship, true);
            pc.transform.localPosition = new Vector3(0f, 1.2f, -1.5f);
            var vis = pc.transform.Find("Visual");
            if (vis != null) vis.gameObject.SetActive(false);
            foreach (var wv in pc.GetComponentsInChildren<Deadzone.Combat.WeaponVisual>()) wv.enabled = false;
            var held = pc.transform.Find("HeldWeapon");
            if (held != null) held.gameObject.SetActive(false);
        }

        private IEnumerator Fly(Vector3 from, Vector3 to, float duration, float bankDeg, bool ease)
        {
            Vector3 flat = to - from; flat.y = 0f;
            Quaternion startRot = ship.rotation;
            Quaternion faceRot = flat.sqrMagnitude > 1f ? Quaternion.LookRotation(flat.normalized) : startRot;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float e = ease ? k * k * (3f - 2f * k) : k;
                ship.position = Vector3.LerpUnclamped(from, to, e);
                Quaternion yaw = Quaternion.Slerp(startRot, faceRot, Mathf.Clamp01(k * 2.5f));
                ship.rotation = yaw * Quaternion.Euler(Mathf.Sin(k * Mathf.PI) * bankDeg * 0.4f, 0f, Mathf.Sin(k * Mathf.PI) * -bankDeg);
                SetEngines(1f);
                yield return null;
            }
            ship.position = to;
            ship.rotation = faceRot;
        }
    }
}
