using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Deadzone.Core
{
    [Serializable]
    public class RunMetricEntry
    {
        public string runId;
        public string timestamp;
        public float runDurationSeconds;
        public float timeToFirstDamageSeconds;
        public float peakNoiseLevel;
        public int totalKills;
        public int creditsBanked;
        public string runResult; // "EXTRACTED", "KIA", "ABORTED"
        public List<string> objectiveHealthCheckpoints = new List<string>();
    }

    [Serializable]
    public class RunTelemetryData
    {
        public List<RunMetricEntry> runs = new List<RunMetricEntry>();
    }

    public class RunTelemetryLogger : MonoBehaviour
    {
        public static RunTelemetryLogger Instance { get; private set; }

        private RunMetricEntry currentRun;
        private float runStartTime;
        private bool hasTakenFirstDamage = false;
        private string logFilePath;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            logFilePath = Path.Combine(Application.persistentDataPath, "run_metrics.json");
            StartNewRunTracking();
        }

        public void StartNewRunTracking()
        {
            runStartTime = Time.time;
            hasTakenFirstDamage = false;

            currentRun = new RunMetricEntry
            {
                runId = Guid.NewGuid().ToString("N").Substring(0, 8),
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                runResult = "IN_PROGRESS"
            };
        }

        private void Update()
        {
            if (currentRun == null) return;

            // Track peak noise
            if (NoiseManager.Instance != null && NoiseManager.Instance.CurrentNoiseLevel > currentRun.peakNoiseLevel)
            {
                currentRun.peakNoiseLevel = NoiseManager.Instance.CurrentNoiseLevel;
            }
        }

        public void RecordDamageTaken(float currentHp, float maxHp)
        {
            if (currentRun == null) return;

            if (!hasTakenFirstDamage)
            {
                hasTakenFirstDamage = true;
                currentRun.timeToFirstDamageSeconds = Time.time - runStartTime;
            }
        }

        public void RecordObjectiveCheckpoint(string objectiveName, float currentHp)
        {
            if (currentRun == null) return;
            currentRun.objectiveHealthCheckpoints.Add($"{objectiveName} @ {currentHp:F0} HP ({Time.time - runStartTime:F1}s)");
        }

        public void EndRun(string result, int kills, int credits)
        {
            if (currentRun == null) return;

            currentRun.runDurationSeconds = Time.time - runStartTime;
            currentRun.totalKills = kills;
            currentRun.creditsBanked = credits;
            currentRun.runResult = result;

            SaveTelemetryToFile();
        }

        private void SaveTelemetryToFile()
        {
            try
            {
                RunTelemetryData data = new RunTelemetryData();
                if (File.Exists(logFilePath))
                {
                    string existingJson = File.ReadAllText(logFilePath);
                    data = JsonUtility.FromJson<RunTelemetryData>(existingJson) ?? new RunTelemetryData();
                }

                data.runs.Add(currentRun);
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(logFilePath, json);
                Debug.Log($"[TELEMETRY] Run logged to {logFilePath} | Result: {currentRun.runResult} | Duration: {currentRun.runDurationSeconds:F1}s | Peak Noise: {currentRun.peakNoiseLevel:F1}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TELEMETRY] Failed to write run metrics: {ex.Message}");
            }
        }
    }
}
