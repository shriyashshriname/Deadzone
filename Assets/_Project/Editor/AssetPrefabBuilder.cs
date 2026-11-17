using UnityEngine;
using UnityEditor;
using System.IO;

namespace Deadzone.Editor
{
    [InitializeOnLoad]
    public static class AssetPrefabBuilder
    {
        private const string ModelsRoot = "Assets/_Project/Art/Models";
        private const string MaterialsRoot = "Assets/_Project/Art/Materials";
        private const string PrefabsRoot = "Assets/_Project/Prefabs/Props";

        static AssetPrefabBuilder()
        {
            EditorApplication.delayCall += BuildAssets;
        }

        [MenuItem("Deadzone/Art/Build 3D Asset Prefabs & Materials")]
        public static void BuildAssets()
        {
            EnsureDirectories();

            // 1. Create or load materials (URP Lit / Standard fallback)
            Material matCrate = GetOrCreateMaterial("M_MilitaryCrate", new Color(0.24f, 0.30f, 0.20f), 0.8f);
            Material matBarrel = GetOrCreateMaterial("M_HazardBarrel", new Color(0.85f, 0.65f, 0.10f), 0.5f);
            Material matBarrier = GetOrCreateMaterial("M_ConcreteBarrier", new Color(0.60f, 0.60f, 0.58f), 0.9f);
            Material matWall = GetOrCreateMaterial("M_ModularBlastWall", new Color(0.42f, 0.44f, 0.45f), 0.85f);
            Material matOperator = GetOrCreateMaterial("M_TacticalOperator", new Color(0.18f, 0.32f, 0.40f), 0.6f);
            Material matWalker = GetOrCreateMaterial("M_InfectedWalker", new Color(0.35f, 0.42f, 0.30f), 0.9f);

            // 2. Generate prop prefabs
            CreatePropPrefab("MilitaryCrate", $"{ModelsRoot}/Props/MilitaryCrate.obj", matCrate, new Vector3(1.24f, 0.78f, 0.84f), new Vector3(0, 0.39f, 0));
            CreatePropPrefab("HazardBarrel", $"{ModelsRoot}/Props/HazardBarrel.obj", matBarrel, new Vector3(0.62f, 0.96f, 0.62f), new Vector3(0, 0.48f, 0));
            CreatePropPrefab("ConcreteBarrier", $"{ModelsRoot}/Props/ConcreteBarrier.obj", matBarrier, new Vector3(2.4f, 1.0f, 0.70f), new Vector3(0, 0.50f, 0));
            CreatePropPrefab("ModularBlastWall", $"{ModelsRoot}/Props/ModularBlastWall.obj", matWall, new Vector3(4.1f, 3.0f, 0.65f), new Vector3(0, 1.50f, 0));

            // 3. Generate character visual prefabs
            CreateCharacterVisualPrefab("OperatorModel", $"{ModelsRoot}/Characters/TacticalOperator.obj", matOperator);
            CreateCharacterVisualPrefab("InfectedWalkerModel", $"{ModelsRoot}/Enemies/InfectedWalker.obj", matWalker);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AssetPrefabBuilder] Successfully generated 3D materials and prefabs!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(MaterialsRoot)) Directory.CreateDirectory(MaterialsRoot);
            if (!Directory.Exists(PrefabsRoot)) Directory.CreateDirectory(PrefabsRoot);
            if (!Directory.Exists("Assets/_Project/Prefabs/Characters")) Directory.CreateDirectory("Assets/_Project/Prefabs/Characters");
        }

        private static Material GetOrCreateMaterial(string name, Color baseColor, float smoothness)
        {
            string path = $"{MaterialsRoot}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader) { name = name };
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void CreatePropPrefab(string prefabName, string objPath, Material mat, Vector3 colSize, Vector3 colCenter)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(objPath);
            if (mesh == null)
            {
                // Try loading through model importer
                GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
                if (imported != null)
                {
                    MeshFilter mf = imported.GetComponentInChildren<MeshFilter>();
                    if (mf != null) mesh = mf.sharedMesh;
                }
            }

            GameObject go = new GameObject(prefabName);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            if (mesh != null) filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;

            BoxCollider col = go.AddComponent<BoxCollider>();
            col.size = colSize;
            col.center = colCenter;

            string outPath = $"{PrefabsRoot}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, outPath);
            Object.DestroyImmediate(go);
            Debug.Log($"Created Prop Prefab: {outPath}");
        }

        private static void CreateCharacterVisualPrefab(string prefabName, string objPath, Material mat)
        {
            GameObject imported = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
            Mesh mesh = (imported != null) ? imported.GetComponentInChildren<MeshFilter>()?.sharedMesh : AssetDatabase.LoadAssetAtPath<Mesh>(objPath);

            GameObject go = new GameObject(prefabName);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            if (mesh != null) filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;

            string outPath = $"Assets/_Project/Prefabs/Characters/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, outPath);
            Object.DestroyImmediate(go);
            Debug.Log($"Created Character Model Prefab: {outPath}");
        }
    }
}
