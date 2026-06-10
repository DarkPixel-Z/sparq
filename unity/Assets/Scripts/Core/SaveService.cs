using UnityEngine;

namespace Sparq.Core
{
    /// <summary>
    /// Handles saving/loading PlayerData to local storage (PlayerPrefs) and later to
    /// Firestore (added in Week 2). All game systems go through this — never touch
    /// PlayerPrefs or Firestore directly elsewhere.
    /// </summary>
    public static class SaveService
    {
        private const string SAVE_KEY = "sparq_player_data_v1";
        private const string KEY_VERSION = "sparq_save_version";
        private const int    CURRENT_VERSION = 1;

        /// <summary>The one in-memory PlayerData instance. All code reads/writes this.</summary>
        public static PlayerData Data { get; private set; }

        /// <summary>Raised after a successful load (or after creating a fresh save).</summary>
        public static event System.Action OnLoaded;

        /// <summary>Raised after a successful save.</summary>
        public static event System.Action OnSaved;

        /// <summary>
        /// Load from disk (PlayerPrefs). Call once on app start.
        /// Creates a fresh PlayerData if none exists.
        /// </summary>
        public static PlayerData Load()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            if (string.IsNullOrEmpty(json))
            {
                Data = new PlayerData();
                Debug.Log("[SaveService] Fresh save created.");
            }
            else
            {
                try
                {
                    Data = JsonUtility.FromJson<PlayerData>(json);
                    if (Data == null) Data = new PlayerData();
                    Debug.Log($"[SaveService] Loaded save — Level {Data.level}, {Data.totalXP} XP");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[SaveService] Corrupt save — starting fresh. {e.Message}");
                    Data = new PlayerData();
                }
            }
            PlayerPrefs.SetInt(KEY_VERSION, CURRENT_VERSION);
            OnLoaded?.Invoke();
            return Data;
        }

        /// <summary>Save immediately (blocking). Call after important state changes.</summary>
        public static void Save()
        {
            if (Data == null) return;
            string json = JsonUtility.ToJson(Data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
            OnSaved?.Invoke();
        }

        /// <summary>Nuke save (for debug / account deletion).</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.Save();
            Data = new PlayerData();
            OnLoaded?.Invoke();
            Debug.Log("[SaveService] Save cleared.");
        }

        // ── Debounced save (prevents thrashing PlayerPrefs on rapid changes) ──
        private static float _debounceTimer = 0f;
        private const float DEBOUNCE_SECONDS = 1.5f;

        /// <summary>
        /// Schedule a save ~1.5s from now. Call this after any state mutation.
        /// Multiple calls within the window collapse into a single save.
        /// The SaveTicker MonoBehaviour drives the timer.
        /// </summary>
        public static void ScheduleSave()
        {
            _debounceTimer = DEBOUNCE_SECONDS;
        }

        internal static void TickDebounce(float deltaTime)
        {
            if (_debounceTimer <= 0f) return;
            _debounceTimer -= deltaTime;
            if (_debounceTimer <= 0f) Save();
        }
    }
}
