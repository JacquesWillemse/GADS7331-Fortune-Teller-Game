using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Minimal Ollama HTTP client using POST /api/generate (non-streaming).
/// </summary>
public class OllamaClient : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://127.0.0.1:11434";
    [SerializeField] private string model = "llama3.2";
    [SerializeField] private int timeoutSeconds = 120;

    public string BaseUrl => baseUrl.TrimEnd('/');
    public string Model => model;

    public void SetEndpoint(string url, string modelName)
    {
        if (!string.IsNullOrEmpty(url))
            baseUrl = url;
        if (!string.IsNullOrEmpty(modelName))
            model = modelName;
    }

    public void Generate(string prompt, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(GenerateRoutine(prompt, onSuccess, onError));
    }

    /// <summary>
    /// Waits for a single generate call (for chaining steps in another component's coroutine).
    /// </summary>
    public IEnumerator GenerateWait(string prompt, Action<string> onSuccess, Action<string> onError)
    {
        bool done = false;
        string ok = null;
        string err = null;
        Generate(
            prompt,
            s =>
            {
                ok = s;
                done = true;
            },
            e =>
            {
                err = e;
                done = true;
            });
        while (!done)
            yield return null;
        if (!string.IsNullOrEmpty(err))
            onError?.Invoke(err);
        else
            onSuccess?.Invoke(ok);
    }

    private IEnumerator GenerateRoutine(string prompt, Action<string> onSuccess, Action<string> onError)
    {
        var payload = new OllamaGenerateRequest
        {
            model = model,
            prompt = prompt,
            stream = false
        };
        string json = JsonUtility.ToJson(payload);
        byte[] body = Encoding.UTF8.GetBytes(json);

        string url = BaseUrl + "/api/generate";
        using var request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = timeoutSeconds;

        yield return request.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        if (request.result != UnityWebRequest.Result.Success)
#else
        if (request.isNetworkError || request.isHttpError)
#endif
        {
            onError?.Invoke($"{request.error} ({request.responseCode})");
            yield break;
        }

        string raw = request.downloadHandler.text;
        try
        {
            var parsed = JsonUtility.FromJson<OllamaGenerateResponse>(raw);
            if (parsed == null || string.IsNullOrEmpty(parsed.response))
            {
                onError?.Invoke("Empty response from Ollama: " + raw);
                yield break;
            }
            onSuccess?.Invoke(parsed.response.Trim());
        }
        catch (Exception e)
        {
            onError?.Invoke("Parse error: " + e.Message + "\n" + raw);
        }
    }

    [Serializable]
    private class OllamaGenerateRequest
    {
        public string model;
        public string prompt;
        public bool stream;
    }

    [Serializable]
    private class OllamaGenerateResponse
    {
        public string model;
        public string response;
        public bool done;
    }
}
