using UnityEngine;

namespace Deadzone.Combat
{
    public class MuzzleFlashPool : MonoBehaviour
    {
        public static MuzzleFlashPool Instance { get; private set; }

        private Light pooledLight;
        private float turnOffTime = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializePooledLight();
        }

        private void InitializePooledLight()
        {
            if (pooledLight != null) return;

            GameObject lightObj = new GameObject("PooledMuzzleLight");
            lightObj.transform.SetParent(transform);
            pooledLight = lightObj.AddComponent<Light>();
            pooledLight.type = LightType.Point;
            pooledLight.color = new Color(1.0f, 0.85f, 0.45f);
            pooledLight.shadows = LightShadows.None;
            pooledLight.intensity = 0f;
            pooledLight.enabled = false;
        }

        public void TriggerFlash(Vector3 position, Vector3 forward, float intensity = 2.5f, float range = 8f)
        {
            if (pooledLight == null) InitializePooledLight();

            pooledLight.transform.position = position + (forward * 0.2f);
            pooledLight.range = range;
            pooledLight.intensity = intensity;
            pooledLight.enabled = true;

            turnOffTime = Time.time + 0.045f;
        }

        private void Update()
        {
            if (pooledLight != null && pooledLight.enabled && Time.time >= turnOffTime)
            {
                pooledLight.intensity = 0f;
                pooledLight.enabled = false;
            }
        }
    }
}
