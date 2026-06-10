// RemindAlertRunner.cs — DontDestroyOnLoad ticker that polls
// RemindService.DueNow() and surfaces in-game reminder alerts via
// RemindAlertOverlay. Single-instance, idle-cheap (30s poll).
//
// Spawn via RemindAlertRunner.EnsureRunning() — safe to call on every
// lobby load; it noops if an instance already exists.

using System.Collections;
using UnityEngine;

namespace Sparq.Systems
{
    public class RemindAlertRunner : MonoBehaviour
    {
        // Poll cadence. 30s is plenty — reminders are wall-clock, not
        // sub-minute, so this never misses a fire.
        private const float TICK_SECONDS = 30f;

        // On launch, only surface reminders that came due within this many
        // minutes — anything older is treated as a passed backlog and
        // suppressed, so logging in at 3pm doesn't dump the morning's
        // reminders all at once.
        private const int LAUNCH_GRACE_MINUTES = 3;

        private static RemindAlertRunner _instance;

        public static void EnsureRunning()
        {
            if (_instance != null) return;
            var existing = FindAnyObjectByType<RemindAlertRunner>();
            if (existing != null) { _instance = existing; return; }
            var go = new GameObject("Sparq_RemindAlertRunner");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<RemindAlertRunner>();
            Debug.Log("[RemindAlertRunner] Started.");
        }

        private void Start()
        {
            // Suppress the backlog of reminders that already passed earlier
            // today BEFORE the first poll — so opening the app doesn't dump a
            // pile of missed reminders. Only ones due in the last few minutes
            // (or that arrive while the app is open) will alert.
            try { RemindService.SuppressOverdueBacklog(LAUNCH_GRACE_MINUTES); }
            catch (System.Exception ex)
            { Debug.LogWarning($"[RemindAlertRunner] Backlog suppress failed: {ex.Message}"); }

            // Immediate first check so a reminder that fired in the last few
            // minutes (e.g. opened the app at 9:02 with a 9:00 reminder) still
            // surfaces right away.
            StartCoroutine(Tick());
        }

        private IEnumerator Tick()
        {
            while (true)
            {
                CheckOnce();
                yield return new WaitForSeconds(TICK_SECONDS);
            }
        }

        private void CheckOnce()
        {
            try
            {
                var due = RemindService.DueNow();
                if (due == null || due.Count == 0) return;
                foreach (var r in due)
                {
                    // Mark fired first so a slow tick doesn't double-enqueue.
                    RemindService.Fire(r);
                    Sparq.UI.RemindAlertOverlay.Enqueue(r);
                }
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[RemindAlertRunner] Tick failed: {ex.Message}"); }
        }
    }
}
