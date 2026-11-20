using UnityEngine;
using UnityEngine.AI;

namespace Deadzone.Core
{
    /// <summary>
    /// The one place AI characters move. Replaces raw "transform.position += ..." which let enemies,
    /// rivals and squadmates walk through walls and props (and let Spitters back out of the arena,
    /// stalling the wave). With a NavMeshAgent on the navmesh it uses agent.Move (stays on walkable
    /// ground, slides along walls); otherwise a sphere-cast slide plus an arena clamp.
    /// </summary>
    public static class EnemyMotor
    {
        public const float ArenaHalfSize = 57.5f; // inner wall faces are at ±59.5

        public static void Step(Component self, Vector3 delta)
        {
            if (self == null) return;
            delta.y = 0f;
            var t = self.transform;
            if (delta.sqrMagnitude < 1e-10f) return;

            if (self.TryGetComponent<NavMeshAgent>(out var agent) && agent.enabled && agent.isOnNavMesh)
            {
                agent.Move(delta);
                return;
            }

            float d = delta.magnitude;
            Vector3 dir = delta / d;
            Vector3 origin = t.position + Vector3.up * 0.2f;
            if (Physics.SphereCast(origin, 0.35f, dir, out var hit, d + 0.25f, ~0, QueryTriggerInteraction.Ignore)
                && !hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Enemy") && !hit.transform.IsChildOf(t))
            {
                Vector3 slide = Vector3.ProjectOnPlane(delta, hit.normal);
                slide.y = 0f;
                t.position += slide * 0.9f;
            }
            else t.position += delta;
            ClampToArena(t);
        }

        private class PathCache { public NavMeshPath path = new NavMeshPath(); public float nextRepath; public Vector3 lastTarget; }
        private static readonly System.Collections.Generic.Dictionary<Component, PathCache> paths = new System.Collections.Generic.Dictionary<Component, PathCache>();

        /// <summary>
        /// Direction to walk to reach target AROUND obstacles (navmesh path, re-planned ~4x/second),
        /// instead of a straight line that gets stuck behind barricades. Falls back to a straight line.
        /// </summary>
        public static Vector3 SteerDir(Component self, Vector3 target)
        {
            Vector3 pos = self.transform.position;
            Vector3 straight = target - pos; straight.y = 0f;
            if (straight.sqrMagnitude < 0.0001f) return Vector3.zero;
            if (!paths.TryGetValue(self, out var c))
            {
                if (paths.Count > 300) paths.Clear(); // dead enemies' entries; cheap to rebuild
                c = new PathCache(); paths[self] = c;
            }
            if (Time.time >= c.nextRepath || (c.lastTarget - target).sqrMagnitude > 4f)
            {
                c.nextRepath = Time.time + 0.25f + Random.value * 0.1f;
                c.lastTarget = target;
                bool ok = NavMesh.SamplePosition(pos, out var a, 2.5f, NavMesh.AllAreas)
                          && NavMesh.SamplePosition(target, out var b, 3f, NavMesh.AllAreas)
                          && NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, c.path);
                if (!ok) c.path.ClearCorners();
            }
            var corners = c.path.corners;
            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 d = corners[i] - pos; d.y = 0f;
                if (d.sqrMagnitude > 0.36f || i == corners.Length - 1) return d.sqrMagnitude > 0.0001f ? d.normalized : straight.normalized;
            }
            return straight.normalized;
        }

        public static void ClampToArena(Transform t)
        {
            var p = t.position;
            p.x = Mathf.Clamp(p.x, -ArenaHalfSize, ArenaHalfSize);
            p.z = Mathf.Clamp(p.z, -ArenaHalfSize, ArenaHalfSize);
            t.position = p;
        }

        public static bool IsOutOfArena(Vector3 p) =>
            Mathf.Abs(p.x) > ArenaHalfSize + 1.5f || Mathf.Abs(p.z) > ArenaHalfSize + 1.5f || p.y < -3f;
    }
}
