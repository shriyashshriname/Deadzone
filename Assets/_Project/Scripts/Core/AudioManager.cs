using UnityEngine;

namespace Deadzone.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        [Header("Optional Assigned Clips")]
        [SerializeField] private AudioClip shootClip;
        [SerializeField] private AudioClip reloadClip;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip pickupClip;
        [SerializeField] private AudioClip bossRoarClip;

        // Procedural synth clips for $0 zero-asset guarantee
        private AudioClip synthShoot;
        private AudioClip synthHit;
        private AudioClip synthPickup;
        private AudioClip synthReload;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                EnsureAudioSources();
                GenerateProceduralSounds();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void EnsureAudioSources()
        {
            if (!sfxSource)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (!musicSource)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
        }

        private void GenerateProceduralSounds()
        {
            // Procedural synthesized SFX so game has sound even without downloaded assets
            synthShoot = CreateToneClip("SynthShoot", 480f, 0.06f, true);
            synthHit = CreateToneClip("SynthHit", 120f, 0.08f, false);
            synthPickup = CreateToneClip("SynthPickup", 880f, 0.12f, false);
            synthReload = CreateToneClip("SynthReload", 300f, 0.15f, false);
        }

        private AudioClip CreateToneClip(string name, float frequency, float duration, bool isNoiseDecay)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - ((float)i / samples); // linear decay

                if (isNoiseDecay)
                {
                    data[i] = (Random.Range(-1f, 1f) * 0.4f + Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.6f) * envelope;
                }
                else
                {
                    data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
                }
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void PlayShoot(float volume = 0.7f)
        {
            PlaySFX(shootClip ? shootClip : synthShoot, volume, Random.Range(0.95f, 1.05f));
        }

        public void PlayHit(float volume = 0.8f)
        {
            PlaySFX(hitClip ? hitClip : synthHit, volume, Random.Range(0.9f, 1.1f));
        }

        public void PlayPickup(float volume = 0.9f)
        {
            PlaySFX(pickupClip ? pickupClip : synthPickup, volume, 1.2f);
        }

        public void PlayReload(float volume = 0.6f)
        {
            PlaySFX(reloadClip ? reloadClip : synthReload, volume, 1.0f);
        }

        public void PlayBossRoar(float volume = 1f)
        {
            PlaySFX(bossRoarClip ? bossRoarClip : synthHit, volume, 0.6f);
        }

        private void PlaySFX(AudioClip clip, float volume, float pitch)
        {
            if (sfxSource == null || clip == null) return;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
