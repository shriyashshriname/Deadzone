using System.Collections;
using System.IO;
using UnityEngine;
using Deadzone.Combat;
using Deadzone.Player;

namespace Deadzone.Core
{
    /// <summary>
    /// Dev-only screenshot run for checking the real screen (IMGUI HUD + touch controls are not in camera
    /// renders). Inactive unless the player is launched with "-dzshots &lt;folder&gt;", e.g.
    ///   Deadzone.exe -screen-width 540 -screen-height 1200 -screen-fullscreen 0 -dzshots Captures/ui
    /// Takes: plain HUD, PICK UP button next to a gun, sniper aim line + "release" ring. Then quits.
    /// </summary>
    public class DevCaptureRunner : MonoBehaviour
    {
        private string dir;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != "-dzshots") continue;
                var go = new GameObject("[DevCapture]");
                go.AddComponent<DevCaptureRunner>().dir = args[i + 1];
                return;
            }
        }

        private class Scripted : IInputProvider
        {
            public InputCommand Next;
            public InputCommand GetCommand() { var c = Next; c.selectSlot = -1; Next.pickup = false; return c; } // -1 = no slot change (0 would force slot 1 every frame)
        }

        private IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            yield return new WaitForSeconds(4f);
            Shot("1_hud");

            var pc = PlayerController.Instance;
            if (pc == null) { Application.Quit(); yield break; }
            var rail = System.Linq.Enumerable.FirstOrDefault(Resources.LoadAll<Deadzone.Data.WeaponData>("Weapons"), PlayerController.IsPrecision);
            WeaponPickup.Spawn(rail, pc.transform.position + pc.transform.right * 0.8f);
            yield return new WaitForSeconds(1.5f);
            Shot("2_pickup_button");

            var input = new Scripted();
            pc.SetInputProvider(input);
            input.Next.pickup = true;
            yield return new WaitForSeconds(1.5f);
            input.Next.fire = true; input.Next.aimVector = new Vector3(0.3f, 0f, 1f).normalized;
            yield return new WaitForSeconds(1.0f);
            Shot("3_sniper_aim");
            input.Next.fire = false;
            yield return new WaitForSeconds(1.0f);
            Shot("4_after_shot");

            var draft = Deadzone.UI.UpgradeDraft.Instance;
            if (draft != null && GameManager.Instance != null)
            {
                GameManager.Instance.AddCredits(600);
                draft.DebugOpen(4);
                yield return new WaitForSeconds(1.0f);
                Shot("5_upgrade_popup");
                yield return null; yield return null; // the capture happens at END of frame: don't close it first
                draft.Skip();
            }
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        private void Shot(string name)
        {
            string path = Path.Combine(dir, $"{name}_{Screen.width}x{Screen.height}.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[DevCapture] {path}");
        }
    }
}
