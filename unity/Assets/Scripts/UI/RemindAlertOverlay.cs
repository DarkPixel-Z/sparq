// RemindAlertOverlay.cs — top-of-screen banner for in-game reminder
// alerts. Fired by RemindService when a scheduled reminder's time
// arrives (via RemindAlertRunner). Queues overlapping alerts so two
// reminders firing in the same tick stack neatly instead of clobbering
// each other.
//
// Visual: a bright gold banner that slides in from the top, shows the
// reminder title + time, with a "Done" (mark complete) and an "X"
// (dismiss without affecting future fires today). Auto-dismisses after
// AUTO_DISMISS_SECONDS so it doesn't block the lobby forever.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class RemindAlertOverlay
    {
        private const float AUTO_DISMISS_SECONDS = 8f;

        // Bright palette so the banner reads instantly over the lobby.
        private static readonly Color BANNER_BG = new Color(0.96f, 0.66f, 0.10f, 1f);  // gold
        private static readonly Color BANNER_FG = new Color(0.13f, 0.10f, 0.20f, 1f);  // dark ink
        private static readonly Color DONE_BG   = new Color(0.40f, 0.85f, 0.55f, 1f);  // green
        private static readonly Color CLOSE_BG  = new Color(0.82f, 0.26f, 0.26f, 1f);  // red

        private static GameObject _root;
        private static readonly Queue<Sparq.Systems.RemindService.Reminder> _queue = new Queue<Sparq.Systems.RemindService.Reminder>();
        private static bool _showing;
        private static MonoBehaviour _runner;

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC
        // ─────────────────────────────────────────────────────────────────

        public static void Enqueue(Sparq.Systems.RemindService.Reminder r)
        {
            if (r == null) return;
            _queue.Enqueue(r);
            TryShowNext();
        }

        public static void DismissAll()
        {
            _queue.Clear();
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
            _showing = false;
        }

        // ─────────────────────────────────────────────────────────────────
        // INTERNALS
        // ─────────────────────────────────────────────────────────────────

        private static void TryShowNext()
        {
            if (_showing || _queue.Count == 0) return;
            var r = _queue.Dequeue();
            _showing = true;
            BuildBanner(r);
        }

        private static void BuildBanner(Sparq.Systems.RemindService.Reminder r)
        {
            EnsureEventSystem();

            _root = new GameObject("Sparq_RemindAlert",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>(); Stretch(rrt);
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            // Highest sort — toast must sit above journal/quests/spoon panels.
            int maxSort = 16000;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 30;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Banner — top of screen.
            var banner = NewGO("Banner", _root.transform, typeof(Image));
            var bRT = banner.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.5f, 1); bRT.anchorMax = new Vector2(0.5f, 1);
            bRT.pivot = new Vector2(0.5f, 1);
            bRT.anchoredPosition = new Vector2(0, -36);
            bRT.sizeDelta = new Vector2(1000, 188);
            banner.GetComponent<Image>().color = BANNER_BG;

            // "REMINDER" label (small bold)
            var tag = MakeText(banner.transform, "Tag", "REMINDER", 24, FontStyles.Bold,
                new Color(0.13f, 0.10f, 0.20f, 0.75f));
            var tagRT = tag.rectTransform;
            tagRT.anchorMin = new Vector2(0, 1); tagRT.anchorMax = new Vector2(1, 1);
            tagRT.pivot = new Vector2(0.5f, 1);
            tagRT.offsetMin = new Vector2(28, -38); tagRT.offsetMax = new Vector2(-28, -10);
            tag.alignment = TextAlignmentOptions.MidlineLeft;

            // Title — the reminder itself
            var title = MakeText(banner.transform, "Title", r.title ?? "Reminder",
                42, FontStyles.Bold, BANNER_FG);
            var ttRT = title.rectTransform;
            ttRT.anchorMin = new Vector2(0, 1); ttRT.anchorMax = new Vector2(1, 1);
            ttRT.pivot = new Vector2(0.5f, 1);
            ttRT.offsetMin = new Vector2(28, -90); ttRT.offsetMax = new Vector2(-220, -40);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.textWrappingMode = TextWrappingModes.Normal;

            // Time line
            var time = MakeText(banner.transform, "Time",
                Sparq.Systems.RemindService.FormatTime(r.hour, r.minute),
                28, FontStyles.Italic, new Color(0.13f, 0.10f, 0.20f, 0.85f));
            var tmRT = time.rectTransform;
            tmRT.anchorMin = new Vector2(0, 0); tmRT.anchorMax = new Vector2(1, 0);
            tmRT.pivot = new Vector2(0.5f, 0);
            tmRT.offsetMin = new Vector2(28, 16); tmRT.offsetMax = new Vector2(-220, 50);
            time.alignment = TextAlignmentOptions.MidlineLeft;

            // "Done" — green pill, marks complete.
            var done = NewGO("Done", banner.transform, typeof(Image), typeof(Button));
            var doneRT = done.GetComponent<RectTransform>();
            doneRT.anchorMin = new Vector2(1, 0.5f); doneRT.anchorMax = new Vector2(1, 0.5f);
            doneRT.pivot = new Vector2(1, 0.5f);
            doneRT.anchoredPosition = new Vector2(-110, 0);
            doneRT.sizeDelta = new Vector2(180, 96);
            var doneImg = done.GetComponent<Image>();
            doneImg.color = DONE_BG; doneImg.raycastTarget = true;
            var doneBtn = done.GetComponent<Button>();
            doneBtn.targetGraphic = doneImg; doneBtn.interactable = true;
            var doneLbl = MakeText(done.transform, "L", "Done", 32, FontStyles.Bold, BANNER_FG);
            Stretch(doneLbl.rectTransform); doneLbl.alignment = TextAlignmentOptions.Center;
            doneBtn.onClick.AddListener(() => Dismiss(true));

            // "X" — closes without marking complete (will not re-fire today).
            var close = NewGO("Close", banner.transform, typeof(Image), typeof(Button));
            var closeRT = close.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1, 0.5f); closeRT.anchorMax = new Vector2(1, 0.5f);
            closeRT.pivot = new Vector2(1, 0.5f);
            closeRT.anchoredPosition = new Vector2(-16, 0);
            closeRT.sizeDelta = new Vector2(80, 80);
            var closeImg = close.GetComponent<Image>();
            closeImg.color = CLOSE_BG; closeImg.raycastTarget = true;
            var closeBtn = close.GetComponent<Button>();
            closeBtn.targetGraphic = closeImg; closeBtn.interactable = true;
            var closeLbl = MakeText(close.transform, "X", "X", 38, FontStyles.Bold, Color.white);
            Stretch(closeLbl.rectTransform); closeLbl.alignment = TextAlignmentOptions.Center;
            closeBtn.onClick.AddListener(() => Dismiss(false));

            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}

            // Auto-dismiss timer.
            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(AutoDismiss());
        }

        private static void Dismiss(bool wasActioned)
        {
            // Reminder was already MarkFiredToday'd by Fire() when it
            // originally fired, so neither button changes the fired state
            // — that prevents re-fires today regardless of which button
            // the user picked. (wasActioned is logged for analytics.)
            Debug.Log($"[RemindAlert] Dismissed (actioned={wasActioned}).");
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
            _showing = false;
            TryShowNext();
        }

        private static IEnumerator AutoDismiss()
        {
            float t = 0f;
            while (t < AUTO_DISMISS_SECONDS && _root != null)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_root != null) Dismiss(false);
        }

        private static void EnsureRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("Sparq_RemindAlertCoroutines");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<DummyRunner>();
        }

        private class DummyRunner : MonoBehaviour {}

        // ─────────────────────────────────────────────────────────────────
        // PRIMITIVES
        // ─────────────────────────────────────────────────────────────────

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static GameObject NewGO(string name, Transform parent, params System.Type[] comps)
        {
            var go = new GameObject(name, new System.Type[] { typeof(RectTransform) });
            go.transform.SetParent(parent, false);
            foreach (var c in comps) go.AddComponent(c);
            return go;
        }

        private static TMP_Text MakeText(Transform parent, string name, string text,
            float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text; tm.fontSize = size; tm.fontStyle = style; tm.color = color;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }

        private static void EnsureEventSystem()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (existing != null && existing.isActiveAndEnabled) return;
            var go = existing != null ? existing.gameObject : new GameObject("EventSystem");
            if (existing == null)
            {
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();   // Old Input Manager only — Input System package not installed.
            }
            go.SetActive(true);
            var es = go.GetComponent<EventSystem>();
            if (es != null) es.enabled = true;
        }
    }
}
