#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Deadzone.Editor
{
    public static class UrpSetupHelper
    {
        [MenuItem("Deadzone/Pipeline/Setup Mobile URP and Convert Materials")]
        public static void SetupMobileUrpAndConvert()
        {
            Debug.Log("[URP SETUP] Starting Mobile URP configuration...");

            // 1. Ensure directory
            string settingsDir = "Assets/_Project/Settings";
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
                AssetDatabase.Refresh();
            }

            string urpAssetPath = $"{settingsDir}/Deadzone_URPAsset.asset";
            string rendererDataPath = $"{settingsDir}/Deadzone_RendererData.asset";

            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererDataPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.name = "Deadzone_RendererData";
                AssetDatabase.CreateAsset(rendererData, rendererDataPath);
            }

            UniversalRenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpAssetPath);
            if (urpAsset == null)
            {
                urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                urpAsset.name = "Deadzone_URPAsset";
                AssetDatabase.CreateAsset(urpAsset, urpAssetPath);
            }

            // 2. Configure mobile-friendly parameters
            SerializedObject so = new SerializedObject(urpAsset);
            
            // Disable HDR
            var hdrProp = so.FindProperty("m_SupportsHDR");
            if (hdrProp != null) hdrProp.boolValue = false;

            // Enable SRP Batcher
            var srpProp = so.FindProperty("m_UseSRPBatcher");
            if (srpProp != null) srpProp.boolValue = true;

            // Set additional lights limit low (max 2)
            var addLightsCountProp = so.FindProperty("m_AdditionalLightsPerObjectLimit");
            if (addLightsCountProp != null) addLightsCountProp.intValue = 2;

            // Limited shadows
            var shadowDistProp = so.FindProperty("m_ShadowDistance");
            if (shadowDistProp != null) shadowDistProp.floatValue = 30f;

            var addShadowsProp = so.FindProperty("m_AdditionalLightShadowsSupported");
            if (addShadowsProp != null) addShadowsProp.boolValue = false;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(urpAsset);

            // 3. Assign as default render pipeline
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            QualitySettings.renderPipeline = urpAsset;

            Debug.Log($"[URP SETUP] URP Asset created and assigned. HDR: false, SRP Batcher: true, Additional Lights Limit: 2, Shadow Distance: 30m.");

            // 4. Convert all materials in Assets/_Project/Materials to URP/Lit
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogError("[URP SETUP] Universal Render Pipeline/Lit shader not found!");
                return;
            }

            string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/Materials" });
            int convertedCount = 0;

            foreach (string guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    Color originalColor = Color.white;
                    if (mat.HasProperty("_Color")) originalColor = mat.GetColor("_Color");
                    else if (mat.HasProperty("_BaseColor")) originalColor = mat.GetColor("_BaseColor");

                    Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;

                    mat.shader = urpLitShader;
                    mat.SetColor("_BaseColor", originalColor);
                    if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);

                    // Set smoothness reasonable for mobile
                    if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);

                    EditorUtility.SetDirty(mat);
                    convertedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[URP SETUP] Successfully converted {convertedCount} materials to Universal Render Pipeline/Lit.");

            // 5. Re-save Graybox scene to ensure renderers serialize with URP
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Graybox_Sandbox.unity");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[URP SETUP] Graybox_Sandbox scene updated and saved with URP.");
        }
    }
}
#endif
