using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using PetBehavior;

namespace PetBehavior
{
    public enum FocusState
    {
        LLMControlled,
        UserControlled,
        Transitioning
    }

    [Serializable]
    public class LLMCommand
    {
        public string action;
        public float duration;
        public string target;
        public string speed;

        public bool IsMovementCommand => !string.IsNullOrEmpty(target);
        public bool HasDuration => duration > 0;
    }

    [Serializable]
    public class CommandResult
    {
        public bool success;
        public string message;
        public string error;
    }

    public class LLMCommandExecutor : MonoBehaviour
    {
        [Header("File Configuration")]
        [SerializeField] private string commandFileName = "LLMCommands.json";
        [SerializeField] private float fileCheckInterval = 0.5f;

        [Header("Focus Configuration")]
        [SerializeField] private float userTimeoutSeconds = 10f;
        [SerializeField] private Text focusIndicatorText;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        // File-based processing
        private string commandFilePath;
        private string lastFileContent = "";
        private bool isProcessingCommands = false;

        // Focus management
        private FocusState currentFocusState = FocusState.LLMControlled;
        private float lastUserInputTime;
        private bool hasDetectedUserInput = false;

        // References
        private PetMoveVR petMoveVR;
        // private VoiceCommandProcessor voiceCommandProcessor;

        // Events
        public System.Action<List<LLMCommand>> OnCommandsParsed;
        public System.Action<LLMCommand, CommandResult> OnCommandExecuted;
        public System.Action<FocusState> OnFocusStateChanged;
        public System.Action OnUserTakesControl;
        public System.Action OnLLMTakesControl;

        // Public properties
        public FocusState CurrentFocusState => currentFocusState;
        public bool IsUserControlled => currentFocusState == FocusState.UserControlled;
        public bool IsLLMControlled => currentFocusState == FocusState.LLMControlled;
        public string CommandFilePath => commandFilePath;

        void Start()
        {
            InitializeFilePath();
            InitializePetReference();
            InitializeFocus();

        }

        void Update()
        {
            DetectUserInput();
            HandleFocusTimeout();
            CheckFileForCommands();
        }

        private void InitializeFilePath()
        {
            string scriptsPath = Path.Combine(Application.dataPath, "Code", "Scripts");
            commandFilePath = Path.Combine(scriptsPath, commandFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(commandFilePath));

            if (!File.Exists(commandFilePath))
            {
                File.WriteAllText(commandFilePath, "");
            }
        }

        private void InitializePetReference()
        {
            petMoveVR = FindObjectOfType<PetMoveVR>();
            if (petMoveVR == null)
            {
                Debug.LogError("LLMCommandExecutor: PetMoveVR not found!");
            }

            // Initialize voice command processor
            // voiceCommandProcessor = FindObjectOfType<VoiceCommandProcessor>();
            // if (voiceCommandProcessor != null)
            // {
            //     // Subscribe to voice command events for user input detection
            //     voiceCommandProcessor.OnStateCommandRecognized += OnVoiceStateCommand;
            //     voiceCommandProcessor.OnFollowCommandRecognized += OnVoiceFollowCommand;
            //     voiceCommandProcessor.OnPetNameRecognized += OnVoicePetName;
            //     
            //     if (showDebugLogs)
            //         Debug.Log("[LLM] Voice command processor found and connected");
            // }
            // else if (showDebugLogs)
            // {
            //     Debug.Log("[LLM] Voice command processor not found - voice input disabled");
            // }
        }

        private void InitializeFocus()
        {
            lastUserInputTime = Time.time;
            UpdateFocusVisualIndicator();
        }

        // ============= FOCUS MANAGEMENT =============

        private void DetectUserInput()
        {
            bool userInputDetected = false;

            // Check keyboard inputs (1-6 keys for pet behaviors, C key for following)
            if (Keyboard.current.digit1Key.wasPressedThisFrame ||
                Keyboard.current.digit2Key.wasPressedThisFrame ||
                Keyboard.current.digit3Key.wasPressedThisFrame ||
                Keyboard.current.digit4Key.wasPressedThisFrame ||
                Keyboard.current.digit5Key.wasPressedThisFrame ||
                Keyboard.current.digit6Key.wasPressedThisFrame ||
                Keyboard.current.cKey.wasPressedThisFrame)
            {
                userInputDetected = true;
            }

            // Check mouse click
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                userInputDetected = true;
            }

            // Note: Voice input is handled separately through voice command events
            // to avoid triggering user input detection on every speech recognition

            if (userInputDetected)
            {
                OnUserInputDetected();
            }
        }

        private void OnUserInputDetected()
        {
            lastUserInputTime = Time.time;
            hasDetectedUserInput = true;

            if (currentFocusState != FocusState.UserControlled)
            {
                SetFocusState(FocusState.UserControlled);
                OnUserTakesControl?.Invoke();

            }
        }

        private void HandleFocusTimeout()
        {
            if (currentFocusState == FocusState.UserControlled && hasDetectedUserInput)
            {
                float timeSinceLastInput = Time.time - lastUserInputTime;

                if (timeSinceLastInput >= userTimeoutSeconds)
                {
                    SetFocusState(FocusState.LLMControlled);
                    OnLLMTakesControl?.Invoke();

                }
            }
        }

        private void SetFocusState(FocusState newState)
        {
            if (currentFocusState != newState)
            {
                FocusState previousState = currentFocusState;
                currentFocusState = newState;

                UpdateFocusVisualIndicator();
                OnFocusStateChanged?.Invoke(newState);

            }
        }

        private void UpdateFocusVisualIndicator()
        {
            if (focusIndicatorText != null)
            {
                string indicatorText = currentFocusState switch
                {
                    FocusState.UserControlled => "🎮 User Control",
                    FocusState.LLMControlled => "🤖 LLM Control",
                    FocusState.Transitioning => "⏳ Transitioning",
                    _ => "❓ Unknown"
                };

                focusIndicatorText.text = indicatorText;

                Color textColor = currentFocusState switch
                {
                    FocusState.UserControlled => Color.green,
                    FocusState.LLMControlled => Color.cyan,
                    FocusState.Transitioning => Color.yellow,
                    _ => Color.white
                };

                focusIndicatorText.color = textColor;
            }
        }

        // ============= FILE-BASED COMMAND PROCESSING =============

        private void CheckFileForCommands()
        {
            if (isProcessingCommands || !IsLLMControlled)
                return;

            try
            {
                if (!File.Exists(commandFilePath))
                    return;

                string currentContent = File.ReadAllText(commandFilePath);

                if (string.IsNullOrWhiteSpace(currentContent) || currentContent == lastFileContent)
                    return;

                lastFileContent = currentContent;
                StartCoroutine(ProcessFileCommands(currentContent));
            }
            catch (Exception e)
            {
            }
        }

        private IEnumerator ProcessFileCommands(string fileContent)
        {
            isProcessingCommands = true;

            yield return new WaitForSeconds(0.1f); // Ensure file write is complete

            // Execute the command processing logic in a separate coroutine to avoid try-catch yield issues
            yield return StartCoroutine(ExecuteCommandSequence(fileContent));

            isProcessingCommands = false;
        }

        private IEnumerator ExecuteCommandSequence(string fileContent)
        {
            List<LLMCommand> commands = null;
            bool parseSuccess = false;

            // Parse commands outside try-catch to avoid yield issues
            try
            {
                if (showDebugLogs)
                    Debug.Log($"[LLM] Processing commands: {fileContent.Replace("\n", " | ")}");

                commands = ParseCommands(fileContent);
                parseSuccess = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing commands: {e.Message}");
                yield break;
            }

            if (!parseSuccess || commands == null || commands.Count == 0)
                yield break;

            // Execute commands with yields outside try-catch
            OnCommandsParsed?.Invoke(commands);

            for (int i = 0; i < commands.Count; i++)
            {
                var command = commands[i];
                CommandResult result = null;

                // Execute individual command in try-catch (no yields here)
                try
                {
                    if (showDebugLogs)
                        Debug.Log($"[LLM] Executing command {i + 1}/{commands.Count}: '{command.action}'");

                    result = ExecuteCommandInternal(command);
                    OnCommandExecuted?.Invoke(command, result);

                    if (showDebugLogs)
                        Debug.Log($"[LLM] Command '{command.action}' execution returned: {result.success} - {result.message}");

                    if (!result.success && showDebugLogs)
                        Debug.LogWarning($"[LLM] Command failed: {result.error}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error executing command {command.action}: {e.Message}");
                    continue; // Skip to next command
                }

                // Wait for ALL commands to complete before proceeding to next command (yields are now outside try-catch)
                if (result != null && result.success)
                {
                    if (showDebugLogs)
                        Debug.Log($"[LLM] Waiting for command '{command.action}' to complete...");

                    yield return StartCoroutine(WaitForCommandCompletion());

                    if (showDebugLogs)
                        Debug.Log($"[LLM] Command '{command.action}' completed, proceeding to next command");
                }

                // Add small delay between commands for stability
                if (i < commands.Count - 1)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            // Clean up after successful processing
            try
            {
                File.WriteAllText(commandFilePath, "");
                lastFileContent = "";

                if (showDebugLogs)
                    Debug.Log($"[LLM] Processed {commands.Count} commands successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error clearing command file: {e.Message}");
            }
        }

        // ============= COMMAND COMPLETION TRACKING =============

        private IEnumerator WaitForCommandCompletion()
        {
            if (petMoveVR == null) yield break;

            // Wait for any queued commands to start executing
            yield return new WaitForSeconds(0.5f);

            if (showDebugLogs)
                Debug.Log("[LLM] Starting comprehensive command completion check...");

            // Wait for PetMoveVR's command queue to be completely processed
            float timeout = 45f; // Increased timeout for state transitions + movement
            float elapsed = 0f;

            NavMeshAgent agent = petMoveVR.GetComponent<NavMeshAgent>();

            while (elapsed < timeout)
            {
                // Primary check: is PetMoveVR still processing any commands?
                bool isExecutingCommands = IsFieldTrue(petMoveVR, "isExecutingCommand");

                // Secondary check: any commands still in queue?
                bool hasQueuedCommands = HasQueuedCommands();
                int queueCount = GetQueueCount();

                // Tertiary check: is state machine still transitioning?
                bool isStateTransitioning = IsStateTransitioning();

                // Quaternary check: if agent exists, is it still moving?
                bool isMoving = false;
                if (agent != null)
                {
                    bool isPathPending = agent.pathPending;
                    bool isNearDestination = agent.remainingDistance <= 0.5f;
                    bool isStopped = agent.velocity.magnitude <= 0.1f;
                    isMoving = isPathPending || !isNearDestination || !isStopped;
                }

                // Debug logging every 2 seconds to track progress
                if (elapsed % 2f < 0.1f && showDebugLogs)
                {
                    Debug.Log($"[LLM] Status check @ {elapsed:F1}s: Executing={isExecutingCommands}, Queue={queueCount}, Transitioning={isStateTransitioning}, Moving={isMoving}");
                }

                // All systems must be idle before we proceed to next command
                if (!isExecutingCommands && !hasQueuedCommands && !isStateTransitioning && !isMoving)
                {
                    // Extra wait to ensure everything is truly complete and stable
                    yield return new WaitForSeconds(0.5f);

                    // Double-check all conditions after the wait
                    bool stillProcessing = IsFieldTrue(petMoveVR, "isExecutingCommand") ||
                                         HasQueuedCommands() ||
                                         IsStateTransitioning();

                    if (!stillProcessing)
                    {
                        if (showDebugLogs)
                            Debug.Log($"[LLM] All command processing completed after {elapsed:F1}s");
                        break;
                    }
                    else if (showDebugLogs)
                    {
                        Debug.Log($"[LLM] False positive - still processing detected during double-check");
                    }
                }

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (elapsed >= timeout && showDebugLogs)
            {
                Debug.LogWarning($"[LLM] Command completion timeout reached after {timeout}s");
            }
        }

        private bool IsStateTransitioning()
        {
            try
            {
                // Check if state machine is transitioning
                var stateMachineField = petMoveVR.GetType().GetField("stateMachine", BindingFlags.NonPublic | BindingFlags.Instance);
                if (stateMachineField != null)
                {
                    var stateMachine = stateMachineField.GetValue(petMoveVR);
                    if (stateMachine != null)
                    {
                        var isTransitioningProperty = stateMachine.GetType().GetProperty("IsTransitioning");
                        if (isTransitioningProperty != null)
                        {
                            return (bool)isTransitioningProperty.GetValue(stateMachine);
                        }
                    }
                }

                // Also check animation controller for sequential transitions
                var animControllerField = petMoveVR.GetType().GetField("animationController", BindingFlags.NonPublic | BindingFlags.Instance);
                if (animControllerField != null)
                {
                    var animController = animControllerField.GetValue(petMoveVR);
                    if (animController != null)
                    {
                        var isExecutingMethod = animController.GetType().GetMethod("IsExecutingSequentialTransition");
                        if (isExecutingMethod != null)
                        {
                            return (bool)isExecutingMethod.Invoke(animController, null);
                        }
                    }
                }
            }
            catch
            {
                // If reflection fails, assume not transitioning
            }
            return false;
        }

        private bool IsFieldTrue(object obj, string fieldName)
        {
            try
            {
                var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(bool))
                {
                    return (bool)field.GetValue(obj);
                }
            }
            catch
            {
                // If reflection fails, assume false
            }
            return false;
        }

        private bool HasQueuedCommands()
        {
            return GetQueueCount() > 0;
        }

        private int GetQueueCount()
        {
            try
            {
                var field = petMoveVR.GetType().GetField("commandQueue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var queue = field.GetValue(petMoveVR) as System.Collections.ICollection;
                    return queue?.Count ?? 0;
                }
            }
            catch
            {
                // If reflection fails, assume no commands
            }
            return 0;
        }

        // ============= DIRECT API METHODS =============

        public CommandResult ExecuteCommand(LLMCommand command)
        {
            if (!IsLLMControlled)
            {
                return new CommandResult
                {
                    success = false,
                    error = "User is currently in control"
                };
            }

            return ExecuteCommandInternal(command);
        }

        public CommandResult ExecuteCommand(string action, float duration = 0, string target = "", string speed = "")
        {
            var command = new LLMCommand
            {
                action = action,
                duration = duration,
                target = target,
                speed = speed
            };

            return ExecuteCommand(command);
        }

        private CommandResult ExecuteCommandInternal(LLMCommand command)
        {
            try
            {
                if (petMoveVR == null)
                {
                    return new CommandResult
                    {
                        success = false,
                        error = "PetMoveVR reference not found"
                    };
                }

                // Execute command directly
                CommandResult result = ExecuteLLMCommandDirect(command);

                if (showDebugLogs && result.success)
                    Debug.Log($"[LLM] Executed: {command.action}");

                return result;
            }
            catch (Exception e)
            {
                return new CommandResult
                {
                    success = false,
                    error = e.Message
                };
            }
        }

        private CommandResult ExecuteLLMCommandDirect(LLMCommand command)
        {
            try
            {
                string actionLower = command.action.ToLower();

                if (command.IsMovementCommand)
                {
                    // Handle movement commands
                    if (command.target.ToLower() == "floor")
                    {
                        float speed = command.speed.ToLower() == "run" ? petMoveVR.RunSpeed : petMoveVR.WalkSpeed;

                        if (showDebugLogs)
                            Debug.Log($"[LLM] Speed calculation: command.speed='{command.speed}' → {(command.speed.ToLower() == "run" ? "RUN" : "WALK")} → speed={speed}");

                        Vector3? randomPos = petMoveVR.GetRandomNavMeshPosition(petMoveVR.MaxRandomMovementDistance);

                        if (randomPos.HasValue)
                        {
                            petMoveVR.QueueLLMMovementCommand(randomPos.Value, speed);
                            return new CommandResult { success = true, message = $"Movement command '{command.action}' queued" };
                        }
                        else
                        {
                            return new CommandResult { success = false, error = "Could not find valid movement position" };
                        }
                    }
                    else
                    {
                        return new CommandResult { success = false, error = $"Unknown movement target: {command.target}" };
                    }
                }
                else
                {
                    // Handle behavior state commands
                    PetActionState? targetState = actionLower switch
                    {
                        "idle" => PetActionState.Idle,
                        "sit" => PetActionState.Sit,
                        "lying" => PetActionState.Lying,
                        "flat" => PetActionState.Flat,
                        "sleep" => PetActionState.Sleep,
                        _ => null
                    };

                    if (targetState.HasValue)
                    {
                        if (command.HasDuration)
                        {
                            petMoveVR.QueueDurationBasedStateCommand(targetState.Value, command.duration);
                        }
                        else
                        {
                            petMoveVR.QueueStateCommand(targetState.Value);
                        }

                        return new CommandResult { success = true, message = $"State command '{command.action}' queued" };
                    }
                    else
                    {
                        return new CommandResult { success = false, error = $"Unknown action: {command.action}" };
                    }
                }
            }
            catch (Exception e)
            {
                return new CommandResult { success = false, error = $"Command execution failed: {e.Message}" };
            }
        }

        // ============= COMMAND PARSING =============

        private List<LLMCommand> ParseCommands(string content)
        {
            var commands = new List<LLMCommand>();
            string[] lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                LLMCommand command = ParseSingleCommand(trimmedLine);
                if (command != null)
                {
                    commands.Add(command);

                    if (showDebugLogs)
                        Debug.Log($"[LLM] Parsed: {command.action} (target:{command.target}, speed:{command.speed}, duration:{command.duration})");
                }
            }

            return commands;
        }

        private LLMCommand ParseSingleCommand(string commandLine)
        {
            // Try JSON format first
            if (commandLine.TrimStart().StartsWith("{"))
            {
                return ParseJSONCommand(commandLine);
            }

            // Fall back to legacy regex format for backwards compatibility
            return ParseLegacyCommand(commandLine);
        }

        private LLMCommand ParseJSONCommand(string jsonLine)
        {
            try
            {
                LLMCommand command = JsonUtility.FromJson<LLMCommand>(jsonLine);

                if (string.IsNullOrEmpty(command.action))
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[LLM] JSON command missing action: {jsonLine}");
                    return null;
                }

                return command;
            }
            catch (Exception e)
            {
                if (showDebugLogs)
                    Debug.LogError($"[LLM] Error parsing JSON: {e.Message}");
                return null;
            }
        }

        private LLMCommand ParseLegacyCommand(string commandLine)
        {
            // Legacy regex parsing for backwards compatibility
            try
            {
                var command = new LLMCommand();

                // Parse ACTION
                Match actionMatch = Regex.Match(commandLine, @"ACTION:\s*([^,]+)", RegexOptions.IgnoreCase);
                if (actionMatch.Success)
                {
                    command.action = actionMatch.Groups[1].Value.Trim();
                }
                else
                {
                    if (showDebugLogs)
                        Debug.LogWarning($"[LLM] No ACTION found in legacy command: {commandLine}");
                    return null;
                }

                // Parse DURATION
                Match durationMatch = Regex.Match(commandLine, @"DURATION:\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                if (durationMatch.Success && float.TryParse(durationMatch.Groups[1].Value, out float duration))
                {
                    command.duration = duration;
                }

                // Parse TARGET
                Match targetMatch = Regex.Match(commandLine, @"TARGET:\s*\{([^}]+)\}", RegexOptions.IgnoreCase);
                if (targetMatch.Success)
                {
                    command.target = targetMatch.Groups[1].Value.Trim();
                }

                // Parse TYPE (speed)
                Match typeMatch = Regex.Match(commandLine, @"TYPE:\s*\{([^}]+)\}", RegexOptions.IgnoreCase);
                if (typeMatch.Success)
                {
                    command.speed = typeMatch.Groups[1].Value.Trim();
                }

                return command;
            }
            catch (Exception e)
            {
                if (showDebugLogs)
                    Debug.LogError($"[LLM] Error parsing legacy command: {e.Message}");
                return null;
            }
        }

        // ============= PUBLIC UTILITY METHODS =============

        public void ForceUserControl()
        {
            lastUserInputTime = Time.time;
            hasDetectedUserInput = true;
            SetFocusState(FocusState.UserControlled);
            OnUserTakesControl?.Invoke();
        }

        public void ForceLLMControl()
        {
            SetFocusState(FocusState.LLMControlled);
            OnLLMTakesControl?.Invoke();
        }

        public void ResetTimeout()
        {
            if (currentFocusState == FocusState.UserControlled)
            {
                lastUserInputTime = Time.time;
            }
        }

        // ============= VOICE COMMAND EVENT HANDLERS =============

        private void OnVoiceStateCommand(string recognizedText, PetActionState targetState)
        {
            // Voice commands automatically trigger user input detection
            OnUserInputDetected();

            if (showDebugLogs)
                Debug.Log($"[LLM] Voice state command detected: '{recognizedText}' → {targetState}");
        }

        private void OnVoiceFollowCommand(string recognizedText)
        {
            // Voice commands automatically trigger user input detection
            OnUserInputDetected();

            if (showDebugLogs)
                Debug.Log($"[LLM] Voice follow command detected: '{recognizedText}'");
        }

        private void OnVoicePetName(string recognizedText)
        {
            // Pet name recognition triggers user input detection
            OnUserInputDetected();

            if (showDebugLogs)
                Debug.Log($"[LLM] Pet name recognized: '{recognizedText}'");
        }

        // Testing method for Inspector
        [ContextMenu("Test JSON Command")]
        public void TestJSONCommand()
        {
            string testCommand = "{\"action\":\"sit\",\"duration\":5.0}";
            LLMCommand cmd = ParseJSONCommand(testCommand);
            if (cmd != null)
            {
                Debug.Log($"Test JSON parsed: action={cmd.action}, duration={cmd.duration}");
                CommandResult result = ExecuteCommand(cmd);
                Debug.Log($"Test result: success={result.success}, message={result.message}");
            }
        }

        [ContextMenu("Test Legacy Command")]
        public void TestLegacyCommand()
        {
            string testCommand = "ACTION: sit, DURATION: 3.0";
            LLMCommand cmd = ParseLegacyCommand(testCommand);
            if (cmd != null)
            {
                Debug.Log($"Test legacy parsed: action={cmd.action}, duration={cmd.duration}");
            }
        }

        void OnDestroy()
        {
            // Unsubscribe from voice command events
            // if (voiceCommandProcessor != null)
            // {
            //     voiceCommandProcessor.OnStateCommandRecognized -= OnVoiceStateCommand;
            //     voiceCommandProcessor.OnFollowCommandRecognized -= OnVoiceFollowCommand;
            //     voiceCommandProcessor.OnPetNameRecognized -= OnVoicePetName;
            // }
        }
    }
}
