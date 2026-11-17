#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Deadzone.Editor
{
    public static class PipelineMetricsProbe
    {
        [MenuItem("Deadzone/QA/Probe Pipeline Metrics")]
        public static void MeasureSceneMetrics()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Scenes/Graybox_Sandbox.unity");
            
            RenderPipelineAsset currentPipeline = GraphicsSettings.currentRenderPipeline;
            string pipelineName = currentPipeline != null ? currentPipeline.GetType().Name : "Built-in Render Pipeline";
            
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
            
            int directionalLights = 0;
            int pointLights = 0;
            int spotLights = 0;
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional) directionalLights++;
                else if (l.type == LightType.Point) pointLights++;
                else if (l.type == LightType.Spot) spotLights++;
            }

            int standardShaderCount = 0;
            int urpLitShaderCount = 0;
            int otherShaderCount = 0;

            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.shader != null)
                {
                    string sName = r.sharedMaterial.shader.name;
                    if (sName.Contains("Standard")) standardShaderCount++;
                    else if (sName.Contains("Universal Render Pipeline") || sName.Contains("URP")) urpLitShaderCount++;
                    else otherShaderCount++;
                }
            }

            Debug.Log("==================================================");
            Debug.Log($"[PIPELINE METRICS PROBE]");
            Debug.Log($"Active Pipeline: {pipelineName}");
            Debug.Log($"Total Scene MeshRenderers: {renderers.Length}");
            Debug.Log($"Shaders in Scene -> Standard: {standardShaderCount} | URP Lit: {urpLitShaderCount} | Other: {otherShaderCount}");
            Debug.Log($"Scene Lights -> Total: {lights.Length} (Directional: {directionalLights}, Point: {pointLights}, Spot: {spotLights})");
            Debug.Log("==================================================");
        }
    }
}
#endif
