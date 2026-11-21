using UnityEngine;
using Deadzone.Core;
using Deadzone.Player;

namespace Deadzone.UI
{
    /// <summary>
    /// Green health bar floating over the player's head (founder, Build #9), same style as the squadmate's.
    /// Installed by HUDController. Hidden while downed (PlayerRevive draws its own bleed-out UI).
    /// </summary>
    public class OverheadHealthBars : MonoBehaviour
    {
        private UnityEngine.Camera cam;
        private Health hp;

        private void OnGUI()
        {
            GUI.matrix = Matrix4x4.identity;
            if (HUDController.IsMenuOpen) return;
            var pc = PlayerController.Instance;
            if (pc == null) return;
            if (hp == null) hp = pc.GetComponent<Health>();
            if (cam == null) cam = UnityEngine.Camera.main;
            if (hp == null || hp.IsDead) return;
            if (PlayerRevive.Instance != null && PlayerRevive.Instance.IsDowned) return;
            BarDrawer.DrawOverhead(cam, pc.transform.position + Vector3.up * 1.35f, hp.CurrentHealth / hp.MaxHealth, BarDrawer.Green, out _);
        }
    }
}
