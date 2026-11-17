using UnityEngine;
using Deadzone.Enemies;

namespace Deadzone.UI
{
    [RequireComponent(typeof(LineRenderer))]
    public class TelegraphRenderer : MonoBehaviour
    {
        [Header("Telegraph Line")]
        [SerializeField] private BossBrute boss;
        [SerializeField] private float chargeLength = 18f;
        [SerializeField] private Color telegraphColor = new Color(1f, 0f, 0f, 0.45f);

        private LineRenderer line;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.enabled = false;
            line.positionCount = 2;
            line.startWidth = 2.2f;
            line.endWidth = 2.2f;
        }

        private void Start()
        {
            if (boss == null) boss = GetComponentInParent<BossBrute>();
            if (boss != null)
            {
                boss.OnBossStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (boss != null)
            {
                boss.OnBossStateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            if (line.enabled && boss != null)
            {
                Vector3 origin = boss.transform.position + Vector3.up * 0.1f;
                Vector3 target = origin + (boss.transform.forward * chargeLength);
                line.SetPosition(0, origin);
                line.SetPosition(1, target);
            }
        }

        private void HandleStateChanged(BossState state)
        {
            if (state == BossState.Telegraphing)
            {
                line.enabled = true;
            }
            else
            {
                line.enabled = false;
            }
        }
    }
}
