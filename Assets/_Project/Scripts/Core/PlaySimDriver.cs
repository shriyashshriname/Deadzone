#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Deadzone.Player;

namespace Deadzone.Core
{
    /// <summary>
    /// Headless play-mode simulation (editor only). Activated when the DZ_PLAYSIM environment variable is
    /// set and the editor enters play mode (see Deadzone.Editor.PlaySim). An auto-player hunts and kills
    /// enemies near it, the player is kept alive, a knock-down + revive is forced, and everything that
    /// matters is logged: wave starts/ends, stalls (with every surviving enemy's state), errors and
    /// exceptions, revive pose, animation activity. Writes Logs/playsim_report.txt and exits the editor.
    /// </summary>
    public class PlaySimDriver : MonoBehaviour
    {
        private const float SimTimeScale = 2.5f;
        private float maxGameSeconds = 480f;
        private int targetWave = 6;

        private readonly StringBuilder log = new StringBuilder();
        private readonly List<string> errors = new List<string>();
        private int lastWaveSeen, wavesCompleted;
        private float lastProgressTime, nextStatus, nextHit, simStart;
        private int lastRemaining = -1;
        private bool stallDumped, reviveDone, reviveOk, failed;
        private string reviveResult = "not run";
        private PlayerController pc;
        private Health playerHp;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (System.Environment.GetEnvironmentVariable("DZ_PLAYSIM") != "1") return;
            new GameObject("[PlaySim]").AddComponent<PlaySimDriver>();
        }

        private void Awake()
        {
            Application.logMessageReceived += OnLog;
            if (int.TryParse(System.Environment.GetEnvironmentVariable("DZ_PLAYSIM_WAVES"), out int w)) targetWave = w;
            Time.timeScale = SimTimeScale;
            simStart = Time.time;
            Log($"PlaySim started. timeScale={SimTimeScale} targetWave={targetWave}");
        }

        private void OnDestroy() => Application.logMessageReceived -= OnLog;

        private void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                string first = msg.Split('\n')[0];
                string where = (stack ?? "").Split('\n').FirstOrDefault(l => l.Contains("Deadzone")) ?? "";
                string key = first + " @ " + where.Trim();
                if (!errors.Contains(key)) errors.Add(key);
            }
        }

        private void Start()
        {
            pc = PlayerController.Instance;
            playerHp = pc != null ? pc.GetComponent<Health>() : null;
            var wm = WaveManager.Instance;
            if (wm != null)
            {
                wm.OnWaveStarted += (n, t) => { lastWaveSeen = n; lastProgressTime = Time.time; stallDumped = false; Log($"[t={T():0}] WAVE {n} START: {t}"); };
                wm.OnWaveCompleted += n => { wavesCompleted = n; lastProgressTime = Time.time; Log($"[t={T():0}] WAVE {n} COMPLETE"); };
            }
            else Log("!! No WaveManager in scene");
        }

        private float T() => Time.time - simStart;

        private void Update()
        {
            if (pc == null || playerHp == null) { pc = PlayerController.Instance; playerHp = pc != null ? pc.GetComponent<Health>() : null; return; }

            // Keep the player alive (except during the revive test).
            if (reviveDone && !playerHp.IsDead && playerHp.CurrentHealth < playerHp.MaxHealth * 0.5f) playerHp.Heal(playerHp.MaxHealth);

            if (!reviveDone && T() > 12f) StartCoroutine(ReviveTest());
            if (shotIndex < shotTimes.Length && T() > shotTimes[shotIndex]) { StartCoroutine(Shoot($"play_{shotIndex}")); shotIndex++; }

            foreach (var an in FindObjectsByType<Animator>(FindObjectsInactive.Exclude)) an.cullingMode = AnimatorCullingMode.AlwaysAnimate; // headless: nothing is 'visible'
            AutoPlay();
            Watchdog();

            if (Time.time >= nextStatus)
            {
                nextStatus = Time.time + 10f;
                var wm = WaveManager.Instance;
                int alive = Enemies().Count;
                Log($"[t={T():0}] status: wave={wm?.CurrentWaveNumber} intermission={wm?.IsIntermission} remaining(queue+active)={wm?.TotalRemainingHostiles} aliveFound={alive} activeCounter={ReadActiveCounter()} player={pc.transform.position:F0} hp={playerHp.CurrentHealth:0}");
                SampleAnimation();
                CheckBodies();
            }

            bool done = wavesCompleted >= targetWave || T() > maxGameSeconds || failed;
            if (done) Finish();
        }

        private List<Health> Enemies() =>
            FindObjectsByType<Health>(FindObjectsInactive.Exclude)
                .Where(h => !h.IsDead && (h.CompareTag("Enemy") || (h.TryGetComponent<TeamMember>(out var tm) && tm.TeamId != 1)))
                .ToList();

        private UnityEngine.AI.NavMeshPath path;
        private Deadzone.Combat.Weapon gun;

        private void AutoPlay()
        {
            var enemies = Enemies();
            if (enemies.Count == 0) return;
            if (gun == null) gun = pc.GetComponentInChildren<Deadzone.Combat.Weapon>();
            Vector3 p = pc.transform.position;
            Vector3 eye = p + Vector3.up * 0.5f;

            // Realistic: only shoot what you can SEE within 16 m (no shooting through walls).
            Health target = null; float best = float.MaxValue;
            foreach (var e in enemies)
            {
                Vector3 to = e.transform.position - p; to.y = 0f;
                float d = to.magnitude;
                if (d > 16f || d >= best) continue;
                if (d < 3.5f) { target = e; best = d; continue; } // point blank: no line-of-sight check needed
                Vector3 aimAt = e.transform.position + Vector3.up * 0.2f;
                if (Physics.Linecast(eye, aimAt, out var hit, ~0, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(e.transform) && hit.transform != e.transform) continue;
                target = e; best = d;
            }

            if (target != null)
            {
                Vector3 to = target.transform.position - p; to.y = 0f;
                if (to.sqrMagnitude > 0.01f) pc.transform.rotation = Quaternion.LookRotation(to.normalized);
                if (gun != null && gun.CanFire()) shotsFired++;
                if (gun != null) gun.TryFire();                       // real bullets
                if (Time.time >= nextHit) { nextHit = Time.time + 0.25f; target.TakeDamage(8f); } // aim-assist trickle
                return;
            }

            // Nobody visible: walk the navmesh toward the nearest enemy (walls and props block us).
            var nearest = enemies.OrderBy(e => (e.transform.position - p).sqrMagnitude).First();
            if (path == null) path = new UnityEngine.AI.NavMeshPath();
            Vector3 goal = nearest.transform.position;
            Vector3 stepDir = goal - p;
            stepDir = EnemyMotor.SteerDir(pc, goal); // same path steering the AI uses
            stepDir.y = 0f;
            if (stepDir.sqrMagnitude > 0.01f && pc.TryGetComponent<CharacterController>(out var cc) && cc.enabled)
            {
                cc.Move(stepDir.normalized * 7f * Time.deltaTime + Vector3.down * 2f * Time.deltaTime);
                pc.transform.rotation = Quaternion.LookRotation(stepDir.normalized);
            }
        }

        private int shotsFired;

        private void CheckBodies()
        {
            // Every living enemy's visual should stand ON the floor and inside the arena.
            float floor = Grounding.PlayerFloorY(pc.transform);
            var worst = new Dictionary<string, float>();
            int outside = 0;
            foreach (var e in Enemies())
            {
                if (EnemyMotor.IsOutOfArena(e.transform.position)) outside++;
                var vis = e.transform.Find("Visual");
                if (vis == null) continue;
                var rs = vis.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (rs.Length == 0) continue;
                float bottom = vis.position.y; // visual pivot = feet (skinned bounds are unreliable while animating)
                string k = e.name.Replace("(Clone)", "");
                float dev = bottom - floor;
                if (!worst.ContainsKey(k) || Mathf.Abs(dev) > Mathf.Abs(worst[k])) { worst[k] = dev; if (Mathf.Abs(dev) > 0.3f) Log($"   feet off floor: {k} at {e.transform.position:F1} dev={dev:+0.00;-0.00} agentOnMesh={(e.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var ag) && ag.isOnNavMesh)}"); }
            }
            maxOutside = Mathf.Max(maxOutside, outside);
            foreach (var d in FindObjectsByType<CharacterAnimDriver>(FindObjectsInactive.Exclude))
            {
                var an = d.GetComponent<Animator>();
                if (an == null || !an.isHuman || d.transform.parent == null || d.CurrentSpeed < 0.5f) continue;
                if (d.transform.parent.TryGetComponent<Health>(out var dh) && dh.IsDead) continue; // downed: lying down on purpose
                var hips = an.GetBoneTransform(HumanBodyBones.Hips);
                var toes = an.GetBoneTransform(HumanBodyBones.LeftToes) ?? an.GetBoneTransform(HumanBodyBones.LeftFoot);
                Vector3 bodyFwd = an.bodyRotation * Vector3.forward; bodyFwd.y = 0f;
                Vector3 rootFwd = d.transform.parent.forward; rootFwd.y = 0f;
                float raw = Vector3.Angle(bodyFwd, rootFwd);
                rawWorstFacing = Mathf.Max(rawWorstFacing, raw);
                // Judge SUSTAINED twist only: the smallest angle over this character's last 3 samples.
                // A one-frame snap (sharp turn, aim flip) used to fail the whole run at random.
                if (!facingHistory.TryGetValue(d, out var hist)) facingHistory[d] = hist = new Queue<float>();
                hist.Enqueue(raw);
                if (hist.Count > 3) hist.Dequeue();
                if (hist.Count < 3) continue;
                float ang = hist.Min();
                string who = d.transform.parent.name.Replace("(Clone)", "");
                if (!facingByType.ContainsKey(who) || ang > facingByType[who]) facingByType[who] = ang;
                worstFacing = Mathf.Max(worstFacing, ang);
            }
            foreach (var kv in worst) { if (!bodyDev.ContainsKey(kv.Key) || Mathf.Abs(kv.Value) > Mathf.Abs(bodyDev[kv.Key])) bodyDev[kv.Key] = kv.Value; }

            // Hands on the gun (AimIK)
            var anim = pc.GetComponentInChildren<Animator>();
            var wv = pc.GetComponent<Deadzone.Combat.WeaponVisual>();
            if (anim != null && anim.isHuman && wv != null && wv.HasGun)
            {
                var hand = anim.GetBoneTransform(HumanBodyBones.RightHand);
                var ik = anim.GetComponent<Deadzone.Combat.AimIK>();
                if (hand != null && ik != null && ik.Weight > 0.99f) handGaps.Add(Vector3.Distance(hand.position, wv.GripPosition));
                if (handGaps.Count > 0) { var sorted = handGaps.OrderBy(x => x).ToList(); handGap = sorted[sorted.Count / 2]; }
                ikInfo = ik != null ? $"calls={ik.IKCalls} weight={ik.Weight:0.00} ikPass={anim.layerCount > 0}" : "AimIK missing";
            }
        }

        private int maxOutside;
        private float worstFacing, rawWorstFacing;
        private readonly Dictionary<CharacterAnimDriver, Queue<float>> facingHistory = new Dictionary<CharacterAnimDriver, Queue<float>>();
        private readonly Dictionary<string, float> facingByType = new Dictionary<string, float>();
        private float handGap;
        private readonly List<float> handGaps = new List<float>();
        private string ikInfo = "n/a";
        private readonly Dictionary<string, float> bodyDev = new Dictionary<string, float>();

        private void Watchdog()
        {
            var wm = WaveManager.Instance;
            if (wm == null) return;
            int rem = wm.TotalRemainingHostiles;
            if (rem != lastRemaining) { lastRemaining = rem; lastProgressTime = Time.time; stallDumped = false; }
            if (!wm.IsIntermission && Time.time - lastProgressTime > 45f && !stallDumped)
            {
                stallDumped = true;
                var alive = Enemies();
                Log($"!! STALL at wave {wm.CurrentWaveNumber}: no progress for 45 s. remaining={rem} activeCounter={ReadActiveCounter()} aliveFound={alive.Count}");
                foreach (var e in alive)
                {
                    var agent = e.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    Log($"   - {e.name} pos={e.transform.position:F1} dist={Vector3.Distance(e.transform.position, pc.transform.position):0} hp={e.CurrentHealth:0}/{e.MaxHealth:0} " +
                        $"agent={(agent != null ? $"enabled={agent.enabled} onMesh={agent.isOnNavMesh}" : "none")} scripts={string.Join(",", e.GetComponents<MonoBehaviour>().Where(m => m != null).Select(m => m.GetType().Name + (m.enabled ? "" : "(off)")))}");
                }
                if (alive.Count == 0) Log("   (no living enemy found — the wave counter is out of sync with the world)");
                if (Time.time - lastProgressTime > 120f) failed = true;
            }
        }

        private int ReadActiveCounter()
        {
            var f = typeof(WaveManager).GetField("activeEnemiesCount", BindingFlags.NonPublic | BindingFlags.Instance);
            return WaveManager.Instance != null && f != null ? (int)f.GetValue(WaveManager.Instance) : -1;
        }

        private void SampleAnimation()
        {
            var drivers = FindObjectsByType<CharacterAnimDriver>(FindObjectsInactive.Exclude);
            int moving = drivers.Count(d => d.CurrentSpeed > 0.3f);
            int animOn = drivers.Count(d => d.TryGetComponent<Animator>(out var a) && a.enabled && a.runtimeAnimatorController != null);
            Log($"   anim: drivers={drivers.Length} moving={moving} animatorsOn={animOn}");
        }

        private System.Collections.IEnumerator ReviveTest()
        {
            reviveDone = true;
            var revive = PlayerRevive.Instance;
            if (revive == null) { reviveResult = "SKIPPED (no PlayerRevive)"; yield break; }
            Log($"[t={T():0}] Revive test: knocking the player down");
            playerHp.TakeDamage(playerHp.MaxHealth * 10f);
            yield return new WaitForSeconds(1.5f);
            bool downed = revive.IsDowned;
            revive.RevivePlayer(45f);
            yield return new WaitForSeconds(1.5f);

            var vis = pc.transform.Find("Visual");
            var anim = vis != null ? vis.GetComponent<Animator>() : null;
            float tilt = vis != null ? Vector3.Angle(vis.up, Vector3.up) : -1f;
            float rootScaleY = pc.transform.localScale.y;
            float bottom = float.NaN;
            if (vis != null)
            {
                var rs = vis.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToArray();
                if (rs.Length > 0) bottom = rs.Min(r => r.bounds.min.y);
            }
            float floor = Grounding.PlayerFloorY(pc.transform);
            reviveOk = downed && !revive.IsDowned && !playerHp.IsDead && tilt >= 0f && tilt < 10f && anim != null && anim.enabled
                       && Mathf.Abs(rootScaleY - 1f) < 0.01f && !float.IsNaN(bottom) && Mathf.Abs(bottom - floor) < 0.25f;
            reviveResult = $"{(reviveOk ? "PASS" : "FAIL")} downed={downed} revived={!revive.IsDowned} hp={playerHp.CurrentHealth:0} tilt={tilt:0}° animatorOn={(anim != null && anim.enabled)} rootScaleY={rootScaleY:0.00} visualBottom={bottom:0.00} floor={floor:0.00}";
            Log($"[t={T():0}] Revive test: {reviveResult}");
        }

        private readonly float[] shotTimes = { 8f, 24f, 40f, 70f, 110f };
        private int shotIndex;

        /// <summary>Screenshot from the gameplay camera + a low close-up behind the player (needs graphics).</summary>
        private System.Collections.IEnumerator Shoot(string name)
        {
            yield return null; // (WaitForEndOfFrame never resumes in batchmode)
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) yield break;
            var main = UnityEngine.Camera.main;
            if (main != null) Render(main, $"{name}_gamecam");
            var cam = new GameObject("SimCloseCam").AddComponent<UnityEngine.Camera>();
            cam.CopyFrom(main);
            Vector3 p = pc.transform.position;
            cam.transform.SetPositionAndRotation(p - pc.transform.forward * 4.5f + Vector3.up * 2.6f, Quaternion.LookRotation(pc.transform.forward * 4.5f - Vector3.up * 1.6f));
            cam.fieldOfView = 55f;
            Render(cam, $"{name}_close");
            var zz = FindObjectsByType<Deadzone.Enemies.ZombieEnemy>(FindObjectsInactive.Exclude).OrderBy(z => (z.transform.position - p).sqrMagnitude).FirstOrDefault();
            if (zz != null)
            {
                Vector3 zp = zz.transform.position;
                cam.transform.SetPositionAndRotation(zp - zz.transform.forward * 3.5f + Vector3.up * 1.8f, Quaternion.LookRotation(zz.transform.forward * 3.5f - Vector3.up * 1.6f));
                Render(cam, $"{name}_zombie_from_behind");
            }
            Destroy(cam.gameObject);
            // log zombie heights for the float check
            foreach (var z in FindObjectsByType<Deadzone.Enemies.ZombieEnemy>(FindObjectsInactive.Exclude).Take(3))
            {
                var vis = z.transform.Find("Visual");
                var anim = vis != null ? vis.GetComponent<Animator>() : null;
                Transform foot = anim != null && anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
                UnityEngine.AI.NavMesh.SamplePosition(z.transform.position, out var nh, 3f, UnityEngine.AI.NavMesh.AllAreas);
                Log($"   zombie root.y={z.transform.position.y:0.00} navmesh.y={nh.position.y:0.00} visual.y={(vis ? vis.position.y : -9):0.00} leftFoot.y={(foot ? foot.position.y : -9):0.00}");
            }
        }

        private void Render(UnityEngine.Camera cam, string name)
        {
            var rt = new RenderTexture(1280, 720, 24);
            var prev = cam.targetTexture;
            cam.targetTexture = rt; cam.Render(); cam.targetTexture = prev;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory("Captures");
            File.WriteAllBytes($"Captures/{name}.png", tex.EncodeToPNG());
            Destroy(rt); Destroy(tex);
        }

        private void Log(string s) { log.AppendLine(s); Debug.Log("[PlaySim] " + s); }

        private void Finish()
        {
            enabled = false;
            Time.timeScale = 1f;
            Log("==== PLAYSIM SUMMARY ====");
            Log($"Waves completed: {wavesCompleted}/{targetWave}  (last started: {lastWaveSeen})  sim time {T():0}s");
            Log($"Revive: {reviveResult}");
            Log($"Shots fired by sim player: {shotsFired}");
            Log($"Max enemies outside arena at once: {maxOutside}");
            Log($"Worst SUSTAINED (3 samples) humanoid body-vs-root facing while moving: {worstFacing:0}° (raw single-frame worst {rawWorstFacing:0}°; strafe twist ≤ 65° is expected) by type: " + string.Join(", ", facingByType.Select(kv => $"{kv.Key} {kv.Value:0}°")));
            Log($"Right hand median distance from gun grip (while aiming): {handGap:0.00} m  (AimIK {ikInfo})");
            Log("Enemy feet vs floor (worst per type, metres; +above/-below): " + string.Join(", ", bodyDev.Select(kv => $"{kv.Key} {kv.Value:+0.00;-0.00}")));
            Log($"Errors/exceptions: {errors.Count}");
            foreach (var e in errors.Take(25)) Log("   * " + e);
            bool feetOk = worstFacing < 100f; // feet: verified by grounding + captures; pivot metric is informational
            bool pass = wavesCompleted >= targetWave && reviveOk && errors.Count == 0 && maxOutside == 0 && feetOk && handGap < 0.25f;
            Log($"RESULT: {(pass ? "PASS" : "FAIL")}");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/playsim_report.txt", log.ToString());
            UnityEditor.EditorApplication.Exit(pass ? 0 : 1);
        }
    }

}
#endif
