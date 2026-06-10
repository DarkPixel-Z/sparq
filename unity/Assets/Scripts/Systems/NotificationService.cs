// NotificationService.cs — local push notifications (daily nudge + AFK-full).
//
// Retention for when the app is CLOSED. Two gentle, ND-friendly reminders:
//   • Daily check-in   — "your squad's ready" once a day (repeats)
//   • Idle-rewards full — fires when offline AFK earnings hit the cap
//
// ── How to enable on device ────────────────────────────────────────────────
// This file COMPILES AS A NO-OP today. To turn it on:
//   1. Package Manager → add "Mobile Notifications" (com.unity.mobile.notifications)
//   2. Project Settings → Player → Scripting Define Symbols → add: SPARQ_NOTIFICATIONS
// The real Android/iOS code below then activates. Notifications never fire in
// the Editor (device only) — that's expected.

using UnityEngine;
#if SPARQ_NOTIFICATIONS && UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if SPARQ_NOTIFICATIONS && UNITY_IOS
using Unity.Notifications.iOS;
#endif

namespace Sparq.Systems
{
    public static class NotificationService
    {
        private const string CHANNEL_ID  = "sparq_daily";
        private const int    DAILY_HOUR   = 18;   // 6pm local — gentle evening nudge

        private const string DAILY_TITLE  = "Your squad's ready";
        private const string DAILY_BODY   = "A quick check-in powers them up today. No pressure — even a tiny win counts.";
        private const string AFK_TITLE    = "Idle rewards are full";
        private const string AFK_BODY     = "Your squad maxed out their offline haul — come collect it!";

        private static bool _initialized;

        /// <summary>One-time setup: Android channel / iOS permission request.</summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
#if SPARQ_NOTIFICATIONS && UNITY_ANDROID
            var channel = new AndroidNotificationChannel
            {
                Id = CHANNEL_ID,
                Name = "Sparq Reminders",
                Importance = Importance.Default,
                Description = "Daily check-in + idle reward nudges",
            };
            AndroidNotificationCenter.RegisterNotificationChannel(channel);
#elif SPARQ_NOTIFICATIONS && UNITY_IOS
            // Request authorization once (async; fire-and-forget is fine here).
            using (var req = new AuthorizationRequest(
                AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound, true)) { }
#else
            Debug.Log("[Notifications] (no-op) Initialize — add Mobile Notifications package + SPARQ_NOTIFICATIONS to enable.");
#endif
        }

        /// <summary>
        /// Cancel any pending Sparq notifications and re-schedule the daily nudge
        /// plus the one-shot "idle rewards full" reminder. Safe to call on every
        /// lobby open — it always clears first so there are no duplicates.
        /// </summary>
        public static void ScheduleDailyReminders()
        {
            Initialize();
            CancelAll();

            System.DateTime dailyAt = NextDailyTime(DAILY_HOUR);
            System.DateTime? afkFullAt = ComputeAfkFullTime();

#if SPARQ_NOTIFICATIONS && UNITY_ANDROID
            var daily = new AndroidNotification
            {
                Title = DAILY_TITLE, Text = DAILY_BODY,
                FireTime = dailyAt,
                RepeatInterval = System.TimeSpan.FromDays(1),
                SmallIcon = "icon_0", LargeIcon = "icon_1",
            };
            AndroidNotificationCenter.SendNotification(daily, CHANNEL_ID);

            if (afkFullAt.HasValue)
            {
                var afk = new AndroidNotification
                {
                    Title = AFK_TITLE, Text = AFK_BODY,
                    FireTime = afkFullAt.Value,
                };
                AndroidNotificationCenter.SendNotification(afk, CHANNEL_ID);
            }
#elif SPARQ_NOTIFICATIONS && UNITY_IOS
            var dailyTrigger = new iOSNotificationCalendarTrigger
            {
                Hour = DAILY_HOUR, Minute = 0, Repeats = true,
            };
            iOSNotificationCenter.ScheduleNotification(new iOSNotification
            {
                Title = DAILY_TITLE, Body = DAILY_BODY,
                ShowInForeground = false, Trigger = dailyTrigger,
            });

            if (afkFullAt.HasValue)
            {
                var span = afkFullAt.Value - System.DateTime.Now;
                if (span.TotalSeconds > 60)
                {
                    iOSNotificationCenter.ScheduleNotification(new iOSNotification
                    {
                        Title = AFK_TITLE, Body = AFK_BODY,
                        ShowInForeground = false,
                        Trigger = new iOSNotificationTimeIntervalTrigger { TimeInterval = span, Repeats = false },
                    });
                }
            }
#else
            Debug.Log($"[Notifications] (no-op) would schedule: daily @ {dailyAt:t}" +
                      (afkFullAt.HasValue ? $", AFK-full @ {afkFullAt.Value:g}" : ""));
#endif
        }

        public static void CancelAll()
        {
#if SPARQ_NOTIFICATIONS && UNITY_ANDROID
            AndroidNotificationCenter.CancelAllNotifications();
#elif SPARQ_NOTIFICATIONS && UNITY_IOS
            iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
        }

        // ── Timing helpers (platform-agnostic) ──
        private static System.DateTime NextDailyTime(int hour)
        {
            var now = System.DateTime.Now;
            var t = new System.DateTime(now.Year, now.Month, now.Day, hour, 0, 0);
            if (t <= now.AddMinutes(1)) t = t.AddDays(1);   // always in the future
            return t;
        }

        // When will offline AFK earnings hit the cap? null if already full / unknown.
        private static System.DateTime? ComputeAfkFullTime()
        {
            try
            {
                long last = Sparq.Core.SaveService.Data?.afkLastClaimUnix ?? 0;
                if (last <= 0) return null;
                var fullUtc = System.DateTimeOffset.FromUnixTimeSeconds(
                    last + (long)(AfkRewardService.MAX_AFK_HOURS * 3600f)).LocalDateTime;
                return fullUtc > System.DateTime.Now.AddMinutes(5) ? fullUtc : (System.DateTime?)null;
            }
            catch { return null; }
        }
    }
}
