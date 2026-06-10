// CloudSaveService.cs — Firestore-backed cloud save for PlayerData.
//
// Architecture (when enabled):
//   1. App start → Initialize() signs in anonymously and pulls the doc.
//   2. If the cloud doc is newer than this device, JSON-merge into SaveService.Data.
//   3. App pause → Upload() pushes the current PlayerData + a server timestamp.
//   Conflict resolution: last-write-wins by `updatedUnix` (good enough for an
//   idle game; refine later if multi-device sees real concurrency).
//
// ── How to enable ─────────────────────────────────────────────────────────
// This file COMPILES AS A NO-OP today. To turn it on:
//   1. Import the Firebase Unity SDK packages: FirebaseAuth + FirebaseFirestore
//   2. Drop the GoogleService-Info.plist / google-services.json into the project
//   3. Project Settings → Player → Scripting Define Symbols → add: SPARQ_CLOUDSAVE
// The real Firebase code below activates immediately. Editor or device works
// once Firebase is initialised.

using UnityEngine;
using System.Threading.Tasks;
#if SPARQ_CLOUDSAVE
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
#endif

namespace Sparq.Systems
{
    public static class CloudSaveService
    {
        // Firestore document layout:  users/{uid}/save/playerdata
        //   data:         string  (JsonUtility.ToJson of PlayerData)
        //   updatedUnix:  long    (server-side write time, used for last-write-wins)
        private const string COLLECTION_ROOT   = "users";
        private const string DOC_SUBPATH       = "save/playerdata";
        private const string PREF_LAST_UPLOAD  = "sparq.cloudsave.lastUploadUnix";

        public static bool   IsAvailable { get; private set; }
        public static string UserId      { get; private set; }

        private static bool _initStarted;
        private static MonoBehaviour _runner;

        /// <summary>
        /// Initialise Firebase, sign in anonymously, then pull the cloud save if
        /// it's newer than this device's last upload. Safe to call repeatedly —
        /// only the first call does work.
        /// </summary>
        public static void InitializeAsync()
        {
            if (_initStarted) return;
            _initStarted = true;
            EnsureAutoSyncRunner();

#if SPARQ_CLOUDSAVE
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(t =>
            {
                if (t.Result != DependencyStatus.Available)
                {
                    Debug.LogWarning($"[CloudSave] Firebase deps unavailable: {t.Result}");
                    return;
                }
                FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(authT =>
                {
                    if (authT.IsFaulted || authT.Result == null)
                    {
                        Debug.LogWarning("[CloudSave] anonymous sign-in failed.");
                        return;
                    }
                    UserId = authT.Result.User.UserId;
                    IsAvailable = true;
                    Debug.Log($"[CloudSave] signed in (uid={UserId}). Pulling save…");
                    _ = DownloadIfNewerAsync();
                });
            });
#else
            Debug.Log("[CloudSave] (no-op) Initialize — add Firebase SDK + SPARQ_CLOUDSAVE to enable.");
#endif
        }

        /// <summary>Upload current PlayerData to Firestore (last-write-wins).</summary>
        public static Task<bool> UploadAsync()
        {
#if SPARQ_CLOUDSAVE
            if (!IsAvailable || string.IsNullOrEmpty(UserId)) return Task.FromResult(false);
            var data = Sparq.Core.SaveService.Data;
            if (data == null) return Task.FromResult(false);

            string json = JsonUtility.ToJson(data);
            long nowUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var doc = FirebaseFirestore.DefaultInstance.Document($"{COLLECTION_ROOT}/{UserId}/{DOC_SUBPATH}");
            var payload = new System.Collections.Generic.Dictionary<string, object>
            {
                { "data",         json },
                { "updatedUnix",  nowUnix },
            };
            var tcs = new TaskCompletionSource<bool>();
            doc.SetAsync(payload).ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted) { Debug.LogWarning("[CloudSave] upload failed."); tcs.SetResult(false); return; }
                PlayerPrefs.SetString(PREF_LAST_UPLOAD, nowUnix.ToString());
                PlayerPrefs.Save();
                Debug.Log("[CloudSave] uploaded.");
                tcs.SetResult(true);
            });
            return tcs.Task;
#else
            Debug.Log("[CloudSave] (no-op) Upload — Firebase SDK + SPARQ_CLOUDSAVE not enabled.");
            return Task.FromResult(false);
#endif
        }

        /// <summary>Pull the cloud save and merge it into local IF the cloud is newer.</summary>
        public static Task<bool> DownloadIfNewerAsync()
        {
#if SPARQ_CLOUDSAVE
            if (!IsAvailable || string.IsNullOrEmpty(UserId)) return Task.FromResult(false);
            var doc = FirebaseFirestore.DefaultInstance.Document($"{COLLECTION_ROOT}/{UserId}/{DOC_SUBPATH}");
            var tcs = new TaskCompletionSource<bool>();
            doc.GetSnapshotAsync().ContinueWithOnMainThread(t =>
            {
                if (t.IsFaulted || t.Result == null || !t.Result.Exists)
                {
                    Debug.Log("[CloudSave] no cloud doc yet — uploading local as initial.");
                    _ = UploadAsync();
                    tcs.SetResult(false);
                    return;
                }
                long cloudUpdated = t.Result.TryGetValue<long>("updatedUnix", out var u) ? u : 0;
                long lastUpload   = 0; long.TryParse(PlayerPrefs.GetString(PREF_LAST_UPLOAD, "0"), out lastUpload);
                if (cloudUpdated <= lastUpload)
                {
                    Debug.Log("[CloudSave] local is newer — keeping local.");
                    tcs.SetResult(false);
                    return;
                }
                string json = t.Result.TryGetValue<string>("data", out var d) ? d : null;
                if (string.IsNullOrEmpty(json) || Sparq.Core.SaveService.Data == null)
                { tcs.SetResult(false); return; }
                try
                {
                    // Overwrite in-place so the existing reference + events stay valid.
                    JsonUtility.FromJsonOverwrite(json, Sparq.Core.SaveService.Data);
                    // The cloud blob may carry a legacy xpToNextLevel from before
                    // the curve unification. Force MigrateLegacyThreshold to re-run
                    // against the freshly-loaded data (the per-session latch would
                    // otherwise skip it because local data already migrated earlier).
                    Sparq.Systems.Progression.ResetMigrationLatch();
                    Sparq.Systems.Progression.MigrateLegacyThreshold(Sparq.Core.SaveService.Data);
                    Sparq.Core.SaveService.Save();   // mirror to PlayerPrefs
                    PlayerPrefs.SetString(PREF_LAST_UPLOAD, cloudUpdated.ToString());
                    PlayerPrefs.Save();
                    Debug.Log("[CloudSave] cloud save applied (newer than local).");
                    tcs.SetResult(true);
                }
                catch (System.Exception ex)
                { Debug.LogWarning($"[CloudSave] merge failed: {ex.Message}"); tcs.SetResult(false); }
            });
            return tcs.Task;
#else
            Debug.Log("[CloudSave] (no-op) DownloadIfNewer — Firebase SDK + SPARQ_CLOUDSAVE not enabled.");
            return Task.FromResult(false);
#endif
        }

        // ── Auto-sync on app pause ────────────────────────────────────────
        private static void EnsureAutoSyncRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("Sparq_CloudSaveAutoSync");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<AutoSync>();
        }

        // Fires Upload() when the OS backgrounds the app. Cheap and reliable —
        // a user who closes the app gets their latest progress saved cloud-side.
        private class AutoSync : MonoBehaviour
        {
            private void OnApplicationPause(bool paused)
            {
                if (!paused) return;
                _ = CloudSaveService.UploadAsync();
            }
            private void OnApplicationQuit()
            {
                _ = CloudSaveService.UploadAsync();
            }
        }
    }
}
