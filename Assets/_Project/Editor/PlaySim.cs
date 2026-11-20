using UnityEditor;
using UnityEditor.SceneManagement;

namespace Deadzone.Editor
{
    /// <summary>
    /// Launches a headless play-mode simulation of the real game (see Deadzone.Core.PlaySimDriver).
    /// Run WITHOUT -quit and with the environment variable DZ_PLAYSIM=1, e.g. (bash):
    ///   DZ_PLAYSIM=1 Unity.exe -batchmode -nographics -projectPath . -executeMethod Deadzone.Editor.PlaySim.Run -logFile Logs/playsim.log
    /// The driver writes Logs/playsim_report.txt and exits the editor (code 0 = pass).
    /// </summary>
    public static class PlaySim
    {
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Graybox_Sandbox.unity", OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }
    }
}
