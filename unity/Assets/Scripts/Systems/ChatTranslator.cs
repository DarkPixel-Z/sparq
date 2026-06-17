using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Sparq.Systems
{
    /// <summary>
    /// Client wrapper for the AI Auto-Translate feature on chat bubbles.
    /// Calls Sparq's Firebase Function proxy (`translateChatMessage`) which
    /// in turn talks to the Anthropic Messages API server-side, so the
    /// Claude API key never ships inside the APK.
    ///
    /// Until the proxy is deployed, set `USE_LIVE_BACKEND = false` to fall
    /// back to a clearly-fake stub that just prefixes the text with "🌐 " —
    /// the UI wiring stays the same, so flipping the flag is the only
    /// change once Anthropic key + Function deploy are done.
    ///
    /// API contract for the Function (cloud-functions/translateChatMessage.js):
    ///   POST  { "text": "...", "targetLang": "en" }
    ///   200   { "translatedText": "...", "sourceLang": "es" }
    ///   4xx   { "error": "..." }
    /// </summary>
    public static class ChatTranslator
    {
        // Flip to true once cloud-functions/translateChatMessage is live.
        private const bool USE_LIVE_BACKEND = false;

        // Default target language — pulled from PlayerData when that field
        // lands. For the closed-test round, assume English-speaking testers.
        private const string DEFAULT_TARGET_LANG = "en";

        // Replace with the deployed Function URL (firebase deploy emits this).
        private const string PROXY_URL = "https://us-central1-sparq-PLACEHOLDER.cloudfunctions.net/translateChatMessage";

        private static MonoBehaviour _runner;

        /// <summary>
        /// Translate `text` to the user's target language. Calls `onResult`
        /// on the main thread with the translated string. Failures fall back
        /// to the original text so the UI never goes blank.
        /// </summary>
        public static void Translate(string text, System.Action<string> onResult)
        {
            if (string.IsNullOrWhiteSpace(text)) { onResult?.Invoke(text); return; }

            if (!USE_LIVE_BACKEND)
            {
                // Stub: clearly fake so the demo shows the feature is wired
                // without making false promises about real translation.
                onResult?.Invoke("🌐 " + text);
                return;
            }

            EnsureRunner();
            _runner.StartCoroutine(TranslateRoutine(text, DEFAULT_TARGET_LANG, onResult));
        }

        private static IEnumerator TranslateRoutine(string text, string targetLang,
                                                    System.Action<string> onResult)
        {
            string body = JsonUtility.ToJson(new TranslateReq { text = text, targetLang = targetLang });
            using (var req = new UnityWebRequest(PROXY_URL, "POST"))
            {
                req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 6;   // a chat-bubble translation isn't worth holding the UI on
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"[ChatTranslator] Translate failed: {req.error}");
                    onResult?.Invoke(text);    // never leave the bubble blank
                    yield break;
                }
                TranslateRes res = null;
                try { res = JsonUtility.FromJson<TranslateRes>(req.downloadHandler.text); }
                catch (System.Exception ex)
                { Debug.LogWarning($"[ChatTranslator] Parse failed: {ex.Message}"); }
                if (res == null || string.IsNullOrEmpty(res.translatedText))
                {
                    onResult?.Invoke(text);
                    yield break;
                }
                onResult?.Invoke(res.translatedText);
            }
        }

        [System.Serializable] private class TranslateReq { public string text; public string targetLang; }
        [System.Serializable] private class TranslateRes { public string translatedText; public string sourceLang; }

        // Lightweight runner so the static class can launch coroutines.
        private static void EnsureRunner()
        {
            if (_runner != null && _runner.gameObject != null) return;
            var go = new GameObject("Sparq_ChatTranslatorRunner");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<RunnerStub>();
        }
        private class RunnerStub : MonoBehaviour { }
    }
}
