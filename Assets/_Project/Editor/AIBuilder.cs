using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Deadzone.Editor
{
    /// <summary>
    /// Gives every AI prefab (and the scene squadmate) a NavMeshAgent so Deadzone.Core.EnemyMotor can
    /// keep them on walkable ground (no walking through walls/props, no leaving the arena).
    /// Zombie_Prefab keeps its pathfinding agent. Idempotent. -executeMethod Deadzone.Editor.AIBuilder.BuildAll
    /// </summary>
    public static class AIBuilder
    {
        static readonly string[] Prefabs =
        {
            "Runner_Prefab", "Spitter_Prefab", "Tank_Prefab", "Boss_Brute_Prefab", "Crawler_King_Prefab",
            "Rival_Operator_Prefab", "Friendly_Squadmate_Prefab",
        };

        [MenuItem("Deadzone/AI/Add Nav Agents To AI")]
        public static void BuildAll()
        {
            foreach (var name in Prefabs)
            {
                string path = $"Assets/_Project/Prefabs/{name}.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                Configure(root, name.Contains("Boss") || name.Contains("King"));
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Graybox_Sandbox.unity", OpenSceneMode.Single);
            foreach (var sq in Object.FindObjectsByType<Deadzone.Player.FriendlySquadmate>(FindObjectsInactive.Include))
                if (!PrefabUtility.IsPartOfPrefabInstance(sq)) Configure(sq.gameObject, false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[AI] Nav agents configured");
        }

        static void Configure(GameObject root, bool big)
        {
            var a = root.GetComponent<NavMeshAgent>();
            if (a == null) a = root.AddComponent<NavMeshAgent>(); // explicit: Unity fake-null breaks '??'
            // Root pivot is the capsule centre (height 2, centre 0): 1 local unit above the feet.
            a.baseOffset = 1f;
            a.radius = big ? 0.6f : 0.45f;
            a.height = 2f;
            a.speed = 4f;               // EnemyMotor uses agent.Move; speed only matters for SetDestination users
            a.acceleration = 40f;
            a.angularSpeed = 0f;
            a.updateRotation = false;   // AI scripts rotate themselves
            a.autoBraking = false;
            a.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            a.avoidancePriority = big ? 30 : 50;
            EditorUtility.SetDirty(a);
        }
    }
}
