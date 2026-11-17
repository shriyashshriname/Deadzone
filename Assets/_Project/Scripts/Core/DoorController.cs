using UnityEngine;

namespace Deadzone.Core
{
    public class DoorController : MonoBehaviour
    {
        [Header("Door Movement")]
        [SerializeField] private Vector3 openOffset = new Vector3(0f, 4.5f, 0f);
        [SerializeField] private float slideSpeed = 4f;

        private Vector3 closedPos;
        private Vector3 openPos;
        private bool isOpen = false;

        private void Start()
        {
            closedPos = transform.position;
            openPos = closedPos + openOffset;
        }

        private void Update()
        {
            Vector3 target = isOpen ? openPos : closedPos;
            transform.position = Vector3.MoveTowards(transform.position, target, slideSpeed * Time.deltaTime);
        }

        public void OpenDoor()
        {
            if (isOpen) return;
            isOpen = true;
            if (AudioManager.Instance != null) AudioManager.Instance.PlayReload(0.8f);
        }

        public void CloseDoor()
        {
            if (!isOpen) return;
            isOpen = false;
        }
    }
}
