using UnityEngine;
using PetBehavior;

public class LLMCommandDebugger : MonoBehaviour
{
    [Header("Debug Commands")]
    [SerializeField] private bool testMoveCommand = false;
    [SerializeField] private bool testSitCommand = false;
    
    private LLMCommandExecutor commandExecutor;
    
    void Start()
    {
        commandExecutor = FindObjectOfType<LLMCommandExecutor>();
        if (commandExecutor == null)
        {
            Debug.LogError("LLMCommandDebugger: LLMCommandExecutor not found!");
        }
    }
    
    void Update()
    {
        if (testMoveCommand)
        {
            testMoveCommand = false;
            TestMoveCommand();
        }
        
        if (testSitCommand)
        {
            testSitCommand = false;
            TestSitCommand();
        }
    }
    
    void TestMoveCommand()
    {
        if (commandExecutor == null) return;
        
        Debug.Log("=== Testing Move Command ===");
        
        var moveCommand = new LLMCommand
        {
            action = "move",
            target = "floor", 
            speed = "walk"
        };
        
        Debug.Log($"Command: action={moveCommand.action}, target={moveCommand.target}, speed={moveCommand.speed}");
        Debug.Log($"IsMovementCommand: {moveCommand.IsMovementCommand}");
        
        CommandResult result = commandExecutor.ExecuteCommand(moveCommand);
        
        Debug.Log($"Result: success={result.success}");
        if (!result.success)
        {
            Debug.LogError($"Move command failed: {result.error}");
        }
        else
        {
            Debug.Log($"Move command succeeded: {result.message}");
        }
    }
    
    void TestSitCommand()
    {
        if (commandExecutor == null) return;
        
        Debug.Log("=== Testing Sit Command ===");
        
        var sitCommand = new LLMCommand
        {
            action = "sit",
            duration = 3.0f
        };
        
        Debug.Log($"Command: action={sitCommand.action}, duration={sitCommand.duration}");
        Debug.Log($"IsMovementCommand: {sitCommand.IsMovementCommand}");
        
        CommandResult result = commandExecutor.ExecuteCommand(sitCommand);
        
        Debug.Log($"Result: success={result.success}");
        if (!result.success)
        {
            Debug.LogError($"Sit command failed: {result.error}");
        }
        else
        {
            Debug.Log($"Sit command succeeded: {result.message}");
        }
    }
    
    [ContextMenu("Debug Command File Parsing")]
    void TestFileParsingDirectly()
    {
        string testJSON = "{\"action\": \"move\", \"target\": \"floor\", \"speed\": \"walk\"}";
        
        try
        {
            LLMCommand cmd = JsonUtility.FromJson<LLMCommand>(testJSON);
            Debug.Log($"Parsed JSON: action={cmd.action}, target={cmd.target}, speed={cmd.speed}");
            Debug.Log($"IsMovementCommand: {cmd.IsMovementCommand}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"JSON parsing failed: {e.Message}");
        }
    }
    
    [ContextMenu("Check NavMesh Agent")]
    void CheckNavMeshStatus()
    {
        var petMoveVR = FindObjectOfType<PetMoveVR>();
        if (petMoveVR != null)
        {
            var agent = petMoveVR.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                Debug.Log($"NavMeshAgent found: enabled={agent.enabled}, isOnNavMesh={agent.isOnNavMesh}");
                Debug.Log($"Agent position: {agent.transform.position}");
            }
            else
            {
                Debug.LogError("NavMeshAgent not found on PetMoveVR!");
            }
        }
        else
        {
            Debug.LogError("PetMoveVR not found!");
        }
    }
}