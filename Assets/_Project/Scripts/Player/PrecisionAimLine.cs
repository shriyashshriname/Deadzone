using UnityEngine;
using Deadzone.Core;

namespace Deadzone.Player
{
    /// <summary>
    /// Sniper range finder / aim assist line (founder request, Build #8). While a precision weapon is held
    /// at aim (PlayerController.IsPrecisionAiming) a straight line runs from the gun along the aim direction
    /// to the first thing it would hit, capped at the bullet's real range. Colours:
    ///   white = clear shot, red = an enemy is on the line, grey = not ready (reloading / cooling down).
    /// Releasing fires exactly along this line.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PrecisionAimLine : MonoBehaviour
    {
        private const string MatPath = "FX/M_AimLine";
        private const float GunAboveRoot = 0.15f;     // gun ~1.15 m above the floor
        private const float ProjectileLifetime = 3f;  // Projectile.lifetime default
        private const float MaxRange = 45f;

        private static readonly Color Clear = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Color OnTarget = new Color(1f, 0.22f, 0.18f, 1f);
        private static readonly Color NotReady = new Color(0.6f, 0.6f, 0.6f, 0.45f);

        private PlayerController pc;
        private LineRenderer line;
        private Transform endDot;
        private readonly RaycastHit[] hits = new RaycastHit[16];
        private MaterialPropertyBlock block;
        private Renderer dotRenderer;

        /// <summary>True when the current aim line ends on a hostile (exposed for tests/HUD).</summary>
        public bool OnEnemy { get; private set; }
        public float CurrentLength { get; private set; }
        public bool IsVisible => line != null && line.enabled;

        private void Awake()
        {
            pc = GetComponent<PlayerController>();
            var go = new GameObject("PrecisionAimLine");
            go.transform.SetParent(transform, false);
            line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 0.09f), new Keyframe(1f, 0.045f));
            var mat = Resources.Load<Material>(MatPath);
            if (mat != null) line.sharedMaterial = mat;
            line.enabled = false;

            var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(dot.GetComponent<Collider>());
            dot.name = "AimEnd";
            dot.transform.SetParent(go.transform, false);
            dot.transform.localScale = Vector3.one * 0.28f;
            var dr = dot.GetComponent<Renderer>();
            if (mat != null) dr.sharedMaterial = mat;
            dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            endDot = dot.transform;
            endDot.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            bool show = pc != null && pc.enabled && pc.IsPrecisionAiming;
            if (line.enabled != show) { line.enabled = show; endDot.gameObject.SetActive(show); }
            if (!show) { OnEnemy = false; return; }

            var weapon = pc.EquippedWeapon;
            float range = MaxRange;
            if (weapon != null && weapon.Data != null) range = Mathf.Min(MaxRange, weapon.Data.bulletSpeed * ProjectileLifetime);

            Vector3 dir = pc.PrecisionAimDir; dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
            dir.Normalize();
            // The player's root (CharacterController centre) sits ~1 m above the floor; the gun is a bit higher.
            Vector3 start = transform.position + Vector3.up * GunAboveRoot + dir * 0.6f;

            float dist = range;
            OnEnemy = false;
            int n = Physics.SphereCastNonAlloc(start, 0.15f, dir, hits, range, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (h.collider.transform.IsChildOf(transform)) continue;          // ourselves
                if (h.distance <= 0f) continue;                                    // started inside something
                var tm = h.collider.GetComponentInParent<TeamMember>();
                if (tm != null && !tm.IsHostileTo(1)) continue;                    // squadmate: shot passes
                if (h.distance < dist)
                {
                    dist = h.distance;
                    OnEnemy = tm != null && tm.IsHostileTo(1);
                }
            }
            CurrentLength = dist;
            Vector3 end = start + dir * dist;

            Color c = weapon != null && !weapon.CanFire() ? NotReady : OnEnemy ? OnTarget : Clear;
            line.startColor = c;
            line.endColor = new Color(c.r, c.g, c.b, c.a * 0.75f);
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            endDot.position = end;
            if (block == null) { block = new MaterialPropertyBlock(); dotRenderer = endDot.GetComponent<Renderer>(); }
            block.SetColor("_BaseColor", c);
            dotRenderer.SetPropertyBlock(block);
        }
    }
}
