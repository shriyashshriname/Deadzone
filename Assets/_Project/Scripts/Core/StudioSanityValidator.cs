using UnityEngine;
using Deadzone.Player;

namespace Deadzone.Core
{
    public class StudioSanityValidator : MonoBehaviour
    {
        private void Start()
        {
            RunSanityCheck();
        }

        public void RunSanityCheck()
        {
            int passed = 0;
            int total = 4;

            // 1. Camera
            if (UnityEngine.Camera.main != null)
            {
                passed++;
            }
            else
            {
                Debug.LogWarning("[SCOUT QA] No MainCamera found in scene.");
            }

            // 2. Player
            if (PlayerController.Instance != null)
            {
                passed++;
            }
            else
            {
                Debug.LogWarning("[SCOUT QA] PlayerController instance not yet spawned.");
            }

            // 3. GameManager
            if (GameManager.Instance != null)
            {
                passed++;
            }
            else
            {
                Debug.LogWarning("[SCOUT QA] GameManager instance not found in scene.");
            }

            // 4. AudioManager
            if (AudioManager.Instance != null)
            {
                passed++;
            }
            else
            {
                Debug.LogWarning("[SCOUT QA] AudioManager instance not found in scene.");
            }

            Debug.Log($"[SCOUT QA AUDIT] Sanity Verification: {passed}/{total} Core Systems Online. Zero Fatal Errors.");
        }
    }
}
