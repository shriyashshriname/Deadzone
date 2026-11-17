using System.Collections.Generic;
using UnityEngine;

namespace Deadzone.UI
{
    public struct KillFeedMessage
    {
        public string text;
        public Color color;
        public float expireTime;
    }

    public class KillFeedUI : MonoBehaviour
    {
        public static KillFeedUI Instance { get; private set; }

        private readonly List<KillFeedMessage> messages = new List<KillFeedMessage>();
        private const float MessageDuration = 4.5f;
        private const int MaxMessages = 4;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public static void PostMessage(string text, Color? color = null)
        {
            if (Instance != null)
            {
                Instance.AddMessage(text, color ?? Color.white);
            }
            else
            {
                Debug.Log($"[KILLFEED] {text}");
            }
        }

        public void AddMessage(string text, Color color)
        {
            if (messages.Count >= MaxMessages)
            {
                messages.RemoveAt(0);
            }

            messages.Add(new KillFeedMessage
            {
                text = text,
                color = color,
                expireTime = Time.time + MessageDuration
            });
        }

        private void Update()
        {
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (Time.time >= messages[i].expireTime)
                {
                    messages.RemoveAt(i);
                }
            }
        }

        private void OnGUI()
        {
            if (messages.Count == 0) return;

            float startX = Screen.width - 320;
            float startY = 110;
            float itemHeight = 24;

            for (int i = 0; i < messages.Count; i++)
            {
                var msg = messages[i];
                float timeLeft = msg.expireTime - Time.time;
                float alpha = Mathf.Clamp01(timeLeft);

                Color c = msg.color;
                c.a = alpha;
                GUI.color = c;

                GUI.Box(new Rect(startX, startY + (i * itemHeight), 300, 22), "");
                GUI.Label(new Rect(startX + 8, startY + (i * itemHeight) + 1, 284, 20), msg.text);
            }

            GUI.color = Color.white;
        }
    }
}
