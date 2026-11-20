using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Deadzone.Editor
{
    /// <summary>
    /// Fits a character/creature visual under a gameplay root: cancels non-uniform root scale,
    /// sizes the (baked, bind-pose) mesh to a target height or footprint in world metres, and
    /// puts its lowest point on the bottom of the root's collider.
    /// </summary>
    public static class VisualFit
    {
        public enum Mode { Height, Width }

        public static float ColliderBottomWorldY(GameObject root, out float colliderWorldHeight)
        {
            float h = 2f, cy = 0f;
            if (root.TryGetComponent<CharacterController>(out var cc)) { h = cc.height; cy = cc.center.y; }
            else if (root.TryGetComponent<CapsuleCollider>(out var cap)) { h = cap.height; cy = cap.center.y; }
            colliderWorldHeight = h * root.transform.lossyScale.y;
            return root.transform.TransformPoint(new Vector3(0f, cy - h * 0.5f, 0f)).y;
        }

        public static void Fit(GameObject root, GameObject vis, Mode mode, float targetMeters, float yaw)
        {
            Vector3 rs = root.transform.lossyScale;
            Vector3 inv = new Vector3(1f / rs.x, 1f / rs.y, 1f / rs.z);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            vis.transform.localScale = inv; // world scale 1

            var b = WorldBounds(vis);
            float measured = mode == Mode.Height ? b.size.y : Mathf.Max(b.size.x, b.size.z);
            float s = targetMeters / Mathf.Max(measured, 1e-4f);
            vis.transform.localScale = inv * s;

            b = WorldBounds(vis);
            float bottom = ColliderBottomWorldY(root, out _);
            vis.transform.position += Vector3.up * (bottom - b.min.y);
        }

        /// <summary>World bounds of all renderers, using baked skinned meshes (bind pose).</summary>
        public static Bounds WorldBounds(GameObject go)
        {
            bool any = false;
            var bounds = new Bounds();
            var baked = new Mesh();
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                IEnumerable<Vector3> pts;
                Matrix4x4 m = r.transform.localToWorldMatrix;
                if (r is SkinnedMeshRenderer smr)
                {
                    smr.BakeMesh(baked, true);
                    pts = baked.vertices;
                }
                else if (r.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
                    pts = mf.sharedMesh.vertices;
                else continue;
                foreach (var v in pts)
                {
                    var w = m.MultiplyPoint3x4(v);
                    if (!any) { bounds = new Bounds(w, Vector3.zero); any = true; }
                    else bounds.Encapsulate(w);
                }
            }
            Object.DestroyImmediate(baked);
            return bounds;
        }

        /// <summary>
        /// Ground speed of a locomotion clip at 1x: how fast the planted foot slides backwards.
        /// Feet are found by name (contains "foot", not "_end"/"ik"/"ctrl"/"roll"). 0 if unmeasurable.
        /// </summary>
        public static float MeasureGroundSpeed(GameObject vis, AnimationClip clip, Transform[] feet = null)
        {
            if (clip == null) return 0f;
            if (feet == null || feet.Length == 0)
            {
                feet = vis.GetComponentsInChildren<Transform>()
                    .Where(t =>
                    {
                        string n = t.name.ToLower();
                        return n.Contains("foot") && !n.Contains("_end") && !n.Contains("ik") && !n.Contains("ctrl") && !n.Contains("roll");
                    }).ToArray();
            }
            if (feet.Length == 0) return 0f;

            var saved = vis.GetComponentsInChildren<Transform>().Select(x => (x, x.localPosition, x.localRotation, x.localScale)).ToArray();
            const int N = 48;
            float dt = clip.length / N;
            var pos = new Vector3[feet.Length, N + 1];
            for (int i = 0; i <= N; i++)
            {
                clip.SampleAnimation(vis, i * dt);
                for (int f = 0; f < feet.Length; f++) pos[f, i] = feet[f].position;
            }
            foreach (var (x, p, r, s) in saved) { x.localPosition = p; x.localRotation = r; x.localScale = s; }

            var speeds = new List<float>();
            for (int f = 0; f < feet.Length; f++)
            {
                var ys = Enumerable.Range(0, N + 1).Select(i => pos[f, i].y).OrderBy(y => y).ToArray();
                float stanceY = ys[(int)(N * 0.35f)];
                for (int i = 0; i < N; i++)
                {
                    if (pos[f, i].y > stanceY || pos[f, i + 1].y > stanceY) continue;
                    var d = pos[f, i + 1] - pos[f, i]; d.y = 0f;
                    speeds.Add(d.magnitude / dt);
                }
            }
            if (speeds.Count == 0) return 0f;
            speeds.Sort();
            float median = speeds[speeds.Count / 2];
            return (median > 0.3f && median < 20f) ? median : 0f;
        }
    }
}
