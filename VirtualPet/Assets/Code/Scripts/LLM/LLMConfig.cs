using UnityEngine;

[CreateAssetMenu(menuName = "LLM/Config", fileName = "LLMConfig")]
public class LLMConfig : ScriptableObject
{
    public string server = "http://localhost:11434";
    public string model = "llama3:8b-instruct-q4_K_M";
    public bool stream = true;
}
