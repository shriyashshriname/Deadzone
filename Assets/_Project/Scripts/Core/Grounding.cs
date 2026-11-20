using UnityEngine;
using UnityEngine.AI;

namespace Deadzone.Core
{
    /// <summary>
    /// Puts a freshly spawned character's collider bottom on the floor. Transform-driven enemies
    /// (Runner, Spitter, Tank, bosses, rivals) keep whatever Y they spawn at, so spawning their
    /// capsule centre at y=0.5 left them half under the floor. NavMeshAgents place themselves.
    /// </summary>
    public static class Grounding
    {
        public static void SnapToGround(GameObject go, float groundY)
        {
            if (go == null) return;
            if (go.TryGetComponent<NavMeshAgent>(out var agent) && agent.enabled) return;

            // Explicit checks: Unity's fake-null objects make '??' unsafe on components.
            Collider col = go.GetComponent<CharacterController>();
            if (col == null) col = go.GetComponent<Collider>();
            if (col == null) return;

            Physics.SyncTransforms();
            float lift = groundY - col.bounds.min.y;
            if (Mathf.Abs(lift) > 0.001f) go.transform.position += Vector3.up * lift;
        }

        /// <summary>Floor height under the player (the arena is flat); 0 if no player yet.</summary>
        public static float PlayerFloorY(Transform player)
        {
            if (player != null && player.TryGetComponent<CharacterController>(out var cc))
                return cc.bounds.min.y;
            return 0f;
        }
    }
}
