// PetCareRunner.cs — DontDestroyOnLoad ticker that periodically calls
// PetService.TickNeeds(). Catches up on real-time decay (so a player
// who closes the app for 6h returns to a hungry pet, not a frozen one)
// and triggers the death timer when any need sits at 0.
//
// Spawn once via PetCareRunner.EnsureRunning() — safe to call from
// HomeLobbyPanel.Show(); noops if an instance already exists.

using System.Collections;
using UnityEngine;

namespace Sparq.Systems
{
    public class PetCareRunner : MonoBehaviour
    {
        // 60s poll is plenty — needs decay at fractions of a point per
        // minute and inter-session decay is computed from real timestamps.
        private const float TICK_SECONDS = 60f;

        private static PetCareRunner _instance;

        public static void EnsureRunning()
        {
            if (_instance != null) return;
            var existing = FindAnyObjectByType<PetCareRunner>();
            if (existing != null) { _instance = existing; return; }
            var go = new GameObject("Sparq_PetCareRunner");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<PetCareRunner>();
            Debug.Log("[PetCareRunner] Started.");
        }

        private void Start()
        {
            // Immediate first tick so an app re-open instantly catches up
            // on hours of accumulated decay.
            PetService.TickNeeds();
            // And evaluate the care streak — handles the case where the
            // player closed the app for days and now a new calendar day
            // has rolled over while it wasn't running.
            try { PetService.CheckDailyCareStreak(); } catch {}
            StartCoroutine(Tick());
        }

        private IEnumerator Tick()
        {
            while (true)
            {
                yield return new WaitForSeconds(TICK_SECONDS);
                try { PetService.TickNeeds(); }
                catch (System.Exception ex)
                { Debug.LogWarning($"[PetCareRunner] TickNeeds failed: {ex.Message}"); }
                // Daily check is cheap — guarded by a date-string compare —
                // so running it every tick is fine and catches midnight
                // rollover within a minute.
                try { PetService.CheckDailyCareStreak(); }
                catch (System.Exception ex)
                { Debug.LogWarning($"[PetCareRunner] CheckDailyCareStreak failed: {ex.Message}"); }
            }
        }
    }
}
