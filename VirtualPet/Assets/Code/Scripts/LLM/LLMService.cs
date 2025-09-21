using UnityEngine;
using System;

[DefaultExecutionOrder(-100)]             // ensure it initializes early
public class LLMService : MonoBehaviour
{
    public LLMConfig config;               // assign in Inspector
    public static LLMService Instance { get; private set; }

    public event Action<string> OnLLMResponse;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Ask(string prompt)
    {
        StartCoroutine(LLM.LLMClient.Send(prompt, config, resp =>
        {
            OnLLMResponse?.Invoke(resp);
        }));
    }
}
