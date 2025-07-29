// Assets/Code/Scripts/PetLogic/PetBrain.cs
using UnityEngine;
using System.Text;

public class PetBrain : MonoBehaviour
{
    [TextArea(4, 10)]
    public string mockSceneJson =
@"{ ""objects"": [
      { ""label"": ""Table"", ""pos"": [1.2, 0.5, -0.3] },
      { ""label"": ""Floor"",  ""pos"": [0, 0, 0] }
  ] }";

    void Start()
    {
        // Build the same prompt template we discussed earlier
        var prompt = new StringBuilder()
            .AppendLine("System: You are a virtual pet brain inside a visionOS app.")
            .AppendLine("User:")
            .AppendLine(mockSceneJson)
            .AppendLine("Based on the scene, describe the pet's next behaviour")
            .AppendLine("as \"ACTION:{verb} TARGET:{object_label} POS:{x,y,z}\".")
            .AppendLine("Do not print anything else.")
            .ToString();

        // ask the LLM (non-blocking coroutine inside LLMService)
        LLMService.Instance.Ask(prompt);

        // subscribe to the answer
        LLMService.Instance.OnLLMResponse += HandleLLMReply;
    }

    void HandleLLMReply(string txt)
    {
        Debug.Log($"[PET ACTION] {txt}");
        // Later you’ll parse & drive animation here.
    }

    void OnDestroy()
    {
        // good hygiene
        if (LLMService.Instance != null)
            LLMService.Instance.OnLLMResponse -= HandleLLMReply;
    }
}
