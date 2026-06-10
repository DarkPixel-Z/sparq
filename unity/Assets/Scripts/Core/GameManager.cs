using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sparq.Core
{
    /// <summary>
    /// Top-level app-wide singleton. Persists across scene loads.
    /// - Loads save data on boot
    /// - Drives the debounced auto-save timer
    /// - Exposes app-lifetime events
    /// Created automatically in Boot scene; attach it to an empty GameObject named "Game".
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        /// <summary>Convenience shortcut — null until SaveService.Load() finishes.</summary>
        public static PlayerData Data => SaveService.Data;

        [Header("Debug")]
        [Tooltip("If true, wipes save on app start — use only for testing.")]
        public bool resetSaveOnStart = false;

        private void Awake()
        {
            // Enforce singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load save immediately
            if (resetSaveOnStart) SaveService.Clear();
            SaveService.Load();

            // Target 60fps on mobile
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }

        private void Start()
        {
            // Boot → Home transition. In the Editor we usually hit Play with
            // Home.unity already open so this never fires; in a built APK,
            // Boot.unity is scene 0 and nothing else moves us off it without
            // this call. Guard on scene name so reloading Home (e.g. via the
            // debug menu) doesn't trigger a redundant load.
            if (SceneManager.GetActiveScene().name == "Boot")
            {
                Debug.Log("[GameManager] Boot → Home");
                SceneManager.LoadScene("Home");
            }
        }

        private void Update()
        {
            // Drive debounced save timer
            SaveService.TickDebounce(Time.unscaledDeltaTime);
        }

        private void OnApplicationPause(bool paused)
        {
            // On Android background: flush any pending save immediately
            if (paused) SaveService.Save();
        }

        private void OnApplicationQuit()
        {
            SaveService.Save();
        }
    }
}
