using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PetBehavior;

namespace PetBehavior
{
    [Serializable]
    public class CommandLogEntry
    {
        public string timestamp;
        public string command;
        public string action;
        public float duration;
        public string target;
        public string speed;
        public bool success;
        public string result;
        public string error;
        public float executionTimeMs;
        public string focusState;

        public CommandLogEntry()
        {
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
    }

    [Serializable]
    public class CommandLogData
    {
        public List<CommandLogEntry> entries = new List<CommandLogEntry>();
        public int totalCommands;
        public int successfulCommands;
        public int failedCommands;
        public string lastUpdated;

        public CommandLogData()
        {
            lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    public class LLMCommandLogger : MonoBehaviour
    {
        [Header("Logging Configuration")]
        [SerializeField] private bool enableLogging = true;
        [SerializeField] private bool logToFile = true;
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private int maxLogEntries = 1000;
        [SerializeField] private string logFileName = "LLMCommandLog.json";

        [Header("Log Display")]
        [SerializeField] private bool showInInspector = false;
        [SerializeField, TextArea(10, 20)] private string recentLogsDisplay;

        // Internal data
        private CommandLogData logData;
        private string logFilePath;
        private Queue<CommandLogEntry> recentEntries;

        // References
        private LLMCommandExecutor commandExecutor;

        public int TotalCommands => logData?.totalCommands ?? 0;
        public int SuccessfulCommands => logData?.successfulCommands ?? 0;
        public int FailedCommands => logData?.failedCommands ?? 0;
        public float SuccessRate => TotalCommands > 0 ? (float)SuccessfulCommands / TotalCommands * 100f : 0f;

        void Start()
        {
            InitializeLogger();
            FindAndConnectToCommandExecutor();
        }

        private void InitializeLogger()
        {
            if (!enableLogging) return;

            logData = new CommandLogData();
            recentEntries = new Queue<CommandLogEntry>();

            // Setup log file path
            string logsPath = Path.Combine(Application.dataPath, "Code", "Scripts", "Logs");
            Directory.CreateDirectory(logsPath);
            logFilePath = Path.Combine(logsPath, logFileName);

            // Load existing log data if available
            LoadExistingLogs();

            Debug.Log($"LLMCommandLogger initialized. Log file: {logFilePath}");
        }

        private void FindAndConnectToCommandExecutor()
        {
            commandExecutor = FindObjectOfType<LLMCommandExecutor>();
            if (commandExecutor != null)
            {
                // Subscribe to command events
                commandExecutor.OnCommandsParsed += OnCommandsParsed;
                commandExecutor.OnCommandExecuted += OnCommandExecuted;
                commandExecutor.OnFocusStateChanged += OnFocusStateChanged;

                Debug.Log("LLMCommandLogger connected to LLMCommandExecutor");
            }
            else
            {
                Debug.LogWarning("LLMCommandLogger: LLMCommandExecutor not found!");
            }
        }

        private void LoadExistingLogs()
        {
            if (!logToFile || !File.Exists(logFilePath)) return;

            try
            {
                string existingData = File.ReadAllText(logFilePath);
                if (!string.IsNullOrEmpty(existingData))
                {
                    logData = JsonUtility.FromJson<CommandLogData>(existingData);

                    // Load recent entries for display
                    if (logData.entries != null && logData.entries.Count > 0)
                    {
                        int startIndex = Mathf.Max(0, logData.entries.Count - 10);
                        for (int i = startIndex; i < logData.entries.Count; i++)
                        {
                            recentEntries.Enqueue(logData.entries[i]);
                        }
                        UpdateInspectorDisplay();
                    }

                    Debug.Log($"Loaded {logData.entries.Count} existing log entries");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load existing logs: {e.Message}");
                logData = new CommandLogData(); // Reset to new data
            }
        }

        private void OnCommandsParsed(List<LLMCommand> commands)
        {
            if (!enableLogging) return;

            foreach (var command in commands)
            {
                var logEntry = new CommandLogEntry
                {
                    command = $"ACTION: {command.action}" +
                             (command.HasDuration ? $", DURATION: {command.duration}" : "") +
                             (command.IsMovementCommand ? $", TARGET: {{{command.target}}}, TYPE: {{{command.speed}}}" : ""),
                    action = command.action,
                    duration = command.duration,
                    target = command.target,
                    speed = command.speed,
                    success = false, // Will be updated in OnCommandExecuted
                    result = "Parsed - Awaiting Execution",
                    focusState = commandExecutor?.CurrentFocusState.ToString() ?? "Unknown"
                };

                if (logToConsole)
                    Debug.Log($"[LLM Command Parsed] {logEntry.command}");
            }
        }

        private void OnCommandExecuted(LLMCommand command, CommandResult result)
        {
            if (!enableLogging) return;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var logEntry = new CommandLogEntry
            {
                command = $"ACTION: {command.action}" +
                         (command.HasDuration ? $", DURATION: {command.duration}" : "") +
                         (command.IsMovementCommand ? $", TARGET: {{{command.target}}}, TYPE: {{{command.speed}}}" : ""),
                action = command.action,
                duration = command.duration,
                target = command.target,
                speed = command.speed,
                success = result.success,
                result = result.success ? result.message : "Failed",
                error = result.success ? "" : result.error,
                executionTimeMs = stopwatch.ElapsedMilliseconds,
                focusState = commandExecutor?.CurrentFocusState.ToString() ?? "Unknown"
            };

            LogCommand(logEntry);

            if (logToConsole)
            {
                string logMessage = $"[LLM Command {(result.success ? "Success" : "Failed")}] {logEntry.command}";
                if (!result.success)
                    logMessage += $" - Error: {result.error}";

                if (result.success)
                    Debug.Log(logMessage);
                else
                    Debug.LogWarning(logMessage);
            }
        }

        private void OnFocusStateChanged(FocusState newState)
        {
            if (!enableLogging) return;

            var logEntry = new CommandLogEntry
            {
                command = "FOCUS_STATE_CHANGE",
                action = "FocusChange",
                success = true,
                result = $"Focus changed to: {newState}",
                focusState = newState.ToString()
            };

            LogCommand(logEntry);

            if (logToConsole)
                Debug.Log($"[Focus Change] {newState}");
        }

        private void LogCommand(CommandLogEntry entry)
        {
            // Add to in-memory log
            logData.entries.Add(entry);
            logData.totalCommands++;

            if (entry.success)
                logData.successfulCommands++;
            else
                logData.failedCommands++;

            logData.lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Maintain recent entries queue for display
            recentEntries.Enqueue(entry);
            if (recentEntries.Count > 10)
                recentEntries.Dequeue();

            // Trim log data if it exceeds max entries
            if (logData.entries.Count > maxLogEntries)
            {
                int entriesToRemove = logData.entries.Count - maxLogEntries;
                logData.entries.RemoveRange(0, entriesToRemove);
            }

            // Update inspector display
            if (showInInspector)
                UpdateInspectorDisplay();

            // Save to file
            if (logToFile)
                SaveLogToFile();
        }

        private void UpdateInspectorDisplay()
        {
            if (!showInInspector) return;

            var displayLines = new List<string>();
            displayLines.Add($"=== LLM Command Log ({TotalCommands} total, {SuccessRate:F1}% success) ===");
            displayLines.Add($"Last Updated: {logData.lastUpdated}");
            displayLines.Add("");

            displayLines.Add("Recent Commands:");
            foreach (var entry in recentEntries)
            {
                string status = entry.success ? "✓" : "✗";
                string line = $"{status} [{entry.timestamp}] {entry.command}";
                if (!entry.success && !string.IsNullOrEmpty(entry.error))
                    line += $" (Error: {entry.error})";
                displayLines.Add(line);
            }

            recentLogsDisplay = string.Join("\n", displayLines);
        }

        private void SaveLogToFile()
        {
            try
            {
                string jsonData = JsonUtility.ToJson(logData, true);
                File.WriteAllText(logFilePath, jsonData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save log file: {e.Message}");
            }
        }

        // Public API methods
        public List<CommandLogEntry> GetRecentEntries(int count = 10)
        {
            if (logData.entries == null) return new List<CommandLogEntry>();

            int startIndex = Mathf.Max(0, logData.entries.Count - count);
            return logData.entries.GetRange(startIndex, logData.entries.Count - startIndex);
        }

        public List<CommandLogEntry> GetEntriesInTimeRange(DateTime start, DateTime end)
        {
            var result = new List<CommandLogEntry>();
            if (logData.entries == null) return result;

            foreach (var entry in logData.entries)
            {
                if (DateTime.TryParse(entry.timestamp, out DateTime entryTime))
                {
                    if (entryTime >= start && entryTime <= end)
                        result.Add(entry);
                }
            }

            return result;
        }

        public void ExportLogToCSV(string filePath = null)
        {
            if (filePath == null)
            {
                string logsPath = Path.Combine(Application.dataPath, "Code", "Scripts", "Logs");
                filePath = Path.Combine(logsPath, $"LLMCommandLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }

            try
            {
                var csvLines = new List<string>();
                csvLines.Add("Timestamp,Action,Duration,Target,Speed,Success,Result,Error,ExecutionTime(ms),FocusState");

                foreach (var entry in logData.entries)
                {
                    string line = $"\"{entry.timestamp}\",\"{entry.action}\",{entry.duration},\"{entry.target}\",\"{entry.speed}\",{entry.success},\"{entry.result}\",\"{entry.error}\",{entry.executionTimeMs},\"{entry.focusState}\"";
                    csvLines.Add(line);
                }

                File.WriteAllLines(filePath, csvLines);
                Debug.Log($"Log exported to CSV: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to export CSV: {e.Message}");
            }
        }

        public void ClearLogs()
        {
            logData = new CommandLogData();
            recentEntries.Clear();

            if (logToFile && File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }

            UpdateInspectorDisplay();
            Debug.Log("Command logs cleared");
        }

        // Context menu methods for Inspector
        [ContextMenu("Show Log Summary")]
        public void ShowLogSummary()
        {
            Debug.Log($"=== LLM Command Log Summary ===");
            Debug.Log($"Total Commands: {TotalCommands}");
            Debug.Log($"Successful: {SuccessfulCommands}");
            Debug.Log($"Failed: {FailedCommands}");
            Debug.Log($"Success Rate: {SuccessRate:F2}%");
            Debug.Log($"Log File: {logFilePath}");
        }

        [ContextMenu("Export to CSV")]
        public void ExportToCSV()
        {
            ExportLogToCSV();
        }

        [ContextMenu("Clear All Logs")]
        public void ClearAllLogs()
        {
            ClearLogs();
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (commandExecutor != null)
            {
                commandExecutor.OnCommandsParsed -= OnCommandsParsed;
                commandExecutor.OnCommandExecuted -= OnCommandExecuted;
                commandExecutor.OnFocusStateChanged -= OnFocusStateChanged;
            }

            // Final save
            if (logToFile && logData != null)
                SaveLogToFile();
        }

        // Unity Inspector display
        void OnValidate()
        {
            if (Application.isPlaying && showInInspector)
                UpdateInspectorDisplay();
        }
    }
}
