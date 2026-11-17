using UnityEngine;
using Deadzone.Core;

namespace Deadzone.Player
{
    public class DesktopInputProvider : IInputProvider
    {
        private readonly Transform playerTransform;
        private UnityEngine.Camera mainCamera;

        public DesktopInputProvider(Transform player)
        {
            playerTransform = player;
            mainCamera = UnityEngine.Camera.main;
        }

        public InputCommand GetCommand()
        {
            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;

            InputCommand cmd = new InputCommand
            {
                selectSlot = -1
            };

            // Movement
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            cmd.moveVector = new Vector3(h, 0f, v).normalized;

            // Aiming
            if (mainCamera != null && playerTransform != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, playerTransform.position);
                if (groundPlane.Raycast(ray, out float enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    Vector3 lookDir = (hitPoint - playerTransform.position);
                    lookDir.y = 0f;
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        cmd.aimVector = lookDir.normalized;
                    }
                }
            }

            // Actions
            cmd.fire = Input.GetMouseButton(0);
            cmd.dash = Input.GetKeyDown(KeyCode.Space);
            cmd.grenade = Input.GetKeyDown(KeyCode.G);
            cmd.turret = Input.GetKeyDown(KeyCode.T);
            cmd.interact = Input.GetKey(KeyCode.E);
            cmd.reload = Input.GetKeyDown(KeyCode.R);

            // Weapon switching
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            cmd.switchWeapon = Input.GetKeyDown(KeyCode.Q) || Mathf.Abs(scroll) > 0.05f;

            if (Input.GetKeyDown(KeyCode.Alpha1)) cmd.selectSlot = 0;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) cmd.selectSlot = 1;

            return cmd;
        }
    }
}
