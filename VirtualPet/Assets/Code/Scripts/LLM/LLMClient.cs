using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;

namespace LLM
{
    /// <summary>Low-level POST helper for Ollama /api/generate.</summary>
    public static class LLMClient
    {
        [Serializable] class RequestBody { public string model; public string prompt; public bool stream; }
        [Serializable] class Chunk { public string response; }  // Ollama field when stream=true
        [Serializable] class FinalStats { public string done; public int total_duration; }

        /// <summary>
        /// Sends <paramref name="prompt"/> and returns the full text when streaming completes.
        /// </summary>
        public static System.Collections.IEnumerator Send(string prompt, LLMConfig cfg, Action<string> onComplete)
        {
            var body = JsonUtility.ToJson(
                new RequestBody { model = cfg.model, prompt = prompt, stream = cfg.stream });
            using var req = new UnityWebRequest($"{cfg.server}/api/generate", "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            // --- SendRequest (Unity-safe coroutine) ----------------------------------------
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"LLM HTTP error: {req.error}");
                onComplete?.Invoke(string.Empty);
                yield break;
            }

            Debug.Log($"RAW >>>\n{req.downloadHandler.text}");

            if (cfg.stream)
            {
                // Ollama returns multiple JSON lines; split on '\n'
                var chunks = req.downloadHandler.text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var sb = new StringBuilder();
                foreach (string json in chunks)
                {
                    if (json.Contains("\"done\":")) continue;        // skip final stats
                    var c = JsonUtility.FromJson<Chunk>(json);
                    sb.Append(c.response);
                }
                onComplete?.Invoke(sb.ToString());
            }
            else
            {
                var whole = JsonUtility.FromJson<Chunk>(req.downloadHandler.text);
                onComplete?.Invoke(whole.response);
            }
        }
    }
}
