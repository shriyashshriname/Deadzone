using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deadzone.Editor
{
    /// <summary>Writes a plain-text summary of the gameplay scene (roots, bounds, components) for tooling.</summary>
    public static class SceneReport
    {
        [MenuItem("Deadzone/Tools/Write Scene Report")]
        public static void Write()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Graybox_Sandbox.unity", OpenSceneMode.Single);
            var sb = new StringBuilder();
            foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).Select(t => t.gameObject).OrderBy(g => g.name))
            {
                if (go.transform.parent != null && go.transform.parent.parent != null) continue; // roots + 1 level
                var r = go.GetComponent<Renderer>();
                var c = go.GetComponent<Collider>();
                string comps = string.Join(",", go.GetComponents<Component>().Where(x => x != null && !(x is Transform)).Select(x => x.GetType().Name));
                string b = r != null ? $" rb={Fmt(r.bounds.center)}/{Fmt(r.bounds.size)}" : c != null ? $" cb={Fmt(c.bounds.center)}/{Fmt(c.bounds.size)}" : "";
                string mat = r != null && r.sharedMaterial != null ? $" mat={r.sharedMaterial.name}" : "";
                sb.AppendLine($"{(go.transform.parent ? "  " : "")}{go.name} pos={Fmt(go.transform.position)} rot={Fmt(go.transform.eulerAngles)} scl={Fmt(go.transform.lossyScale)}{b}{mat} static={go.isStatic} active={go.activeInHierarchy} [{comps}]");
            }
            sb.AppendLine($"Fog={RenderSettings.fog} fogColor={RenderSettings.fogColor} density={RenderSettings.fogDensity} ambient={RenderSettings.ambientLight} sun={(RenderSettings.sun ? RenderSettings.sun.name : "none")}");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/scene_report.txt", sb.ToString());
            Debug.Log("[SceneReport] Logs/scene_report.txt");
        }

        static string Fmt(Vector3 v) => $"({v.x:0.##},{v.y:0.##},{v.z:0.##})";
    }
}
