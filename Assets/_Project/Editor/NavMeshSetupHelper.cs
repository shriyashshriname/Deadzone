#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace Deadzone.Editor
{
    public static class NavMeshSetupHelper
    {
        [MenuItem("Deadzone/AI/Bake NavMesh and Wire Agents")]
        public static void BakeNavMeshAndWireAgents()
        {
            Debug.Log("[NAVMESH SETUP] Starting NavMesh bake for Graybox_Sandbox...");

            // 1. Open Scene
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Graybox_Sandbox.unity");

            // 2. Locate or create NavMeshSurface GameObject
            GameObject surfaceObj = GameObject.Find("NavMeshSurface");
            if (surfaceObj == null)
            {
                surfaceObj = new GameObject("NavMeshSurface");
            }

            NavMeshSurface surface = surfaceObj.GetComponent<NavMeshSurface>();
            if (surface == null)
            {
                surface = surfaceObj.AddComponent<NavMeshSurface>();
            }

            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.defaultArea = 0; // Walkable

            // Bake surface
            surface.BuildNavMesh();
            Debug.Log("[NAVMESH SETUP] NavMeshSurface built successfully for Graybox_Sandbox.");

            // 3. Ensure Zombie_Prefab has NavMeshAgent
            string prefabPath = "Assets/_Project/Prefabs/Zombie_Prefab.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                NavMeshAgent agent = prefab.GetComponent<NavMeshAgent>();
                if (agent == null)
                {
                    agent = prefab.AddComponent<NavMeshAgent>();
                }
                agent.speed = 3.5f;
                agent.stoppingDistance = 1.2f;
                agent.radius = 0.5f;
                agent.height = 2.0f;
                agent.acceleration = 12f;
                agent.angularSpeed = 360f;

                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                Debug.Log("[NAVMESH SETUP] Zombie_Prefab wired with NavMeshAgent.");
            }

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[NAVMESH SETUP] Graybox_Sandbox saved with baked NavMesh.");
        }
    }
}
#endif
