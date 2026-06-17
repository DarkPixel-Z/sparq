using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Surfaces when ContentModerator flags Category.ThreatViolence in
    /// something the user typed — direct threats against others, weapon
    /// possession claims, mass-harm patterns (school shootings, bombs).
    ///
    /// Tone is INTENTIONALLY different from CrisisResourcesPanel:
    ///   - CrisisResources: "You reached out. We hear you. Here's help."
    ///     (compassionate — they're hurting themselves)
    ///   - ThreatResponse:  "We hid this. If you're really planning to
    ///     hurt someone, you need help NOW. Here's how."
    ///     (firm but supportive — they're a risk to others, but they're
    ///      still a kid who likely needs intervention more than punishment)
    ///
    /// What happens around this panel (handled by ContentModerator + caller):
    ///   - Message is BLOCKED — never reaches chat / quest / journal storage
    ///   - ModerationQueue.AutoFlag records the attempt for review
    ///   - RateLimiter.RecordViolation strikes against the sender
    ///
    /// What this panel itself does:
    ///   - Acknowledges + de-escalates (anger is okay; acting isn't)
    ///   - Gives ONE primary path: Crisis Text Line + 988 + 911 + adult
    ///   - Notes that Sparq's safety team has been notified (transparency)
    ///   - Single dismiss — no "I'm safe just venting" option, because a
    ///     threat to others is qualitatively different from venting
    ///
    /// The cooldown mirrors CrisisResourcesPanel so repeated trigger
    /// attempts don't spam the user — but the cooldown is shorter (the
    /// firm tone is the point; if they triggered it twice we still want
    /// them to see it).
    /// </summary>
    public static class ThreatResponsePanel
    {
        private static GameObject _root;

        // 3-minute cooldown — shorter than CrisisResources' 10 min. We want
        // repeat offenders to keep seeing this; cooldown only stops UI spam
        // mid-conversation.
        private const string KEY_LAST_DISMISS = "sparq.safety.threat_dismissed_unix";
        private const long   COOLDOWN_SEC = 180;

        public static bool RecentlyDismissed()
        {
            long last = (long)System.Convert.ToDouble(PlayerPrefs.GetString(KEY_LAST_DISMISS, "0"));
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return (now - last) < COOLDOWN_SEC;
        }

        public static void Show()
        {
            if (_root != null) Object.Destroy(_root);
            EnsureEventSystem();

            _root = new GameObject("Sparq_ThreatResponsePanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 17000;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 60;   // above CrisisResourcesPanel
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Heavy dim — taps DON'T close it (must use the button).
            var dim = NewGO("Dim", _root.transform, typeof(Image));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0.05f, 0.02f, 0.04f, 0.94f);

            // Card — deep slate, not red (alarm fatigues; firm doesn't need red).
            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(960, 1500);
            card.GetComponent<Image>().color = new Color(0.13f, 0.14f, 0.20f, 1f);

            // Title bar — amber, not red. Signals "important", not "punish".
            var titleBar = NewGO("TitleBar", card.transform, typeof(Image));
            var tbRT = titleBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 1); tbRT.anchorMax = new Vector2(1, 1);
            tbRT.pivot = new Vector2(0.5f, 1);
            tbRT.anchoredPosition = Vector2.zero;
            tbRT.sizeDelta = new Vector2(0, 140);
            titleBar.GetComponent<Image>().color = new Color(0.95f, 0.62f, 0.20f, 1f);
            var titleTm = MakeText(titleBar.transform, "Title", "MESSAGE NOT SENT",
                52, FontStyles.Bold, new Color(0.10f, 0.08f, 0.18f));
            var titleRT = titleTm.rectTransform;
            titleRT.anchorMin = Vector2.zero; titleRT.anchorMax = Vector2.one;
            titleRT.offsetMin = new Vector2(20, 0); titleRT.offsetMax = new Vector2(-20, 0);
            titleTm.alignment = TextAlignmentOptions.Center;

            // Main body — firm but de-escalating.
            float y = -180;
            var bodyTxt = MakeText(card.transform, "Body",
                "Messages about hurting other people aren't shared in Sparq.\n\n" +
                "If you're really angry or upset, that's okay — but acting on it isn't. " +
                "If you have a plan to hurt someone, or you're in danger, please reach out NOW:",
                28, FontStyles.Normal, new Color(0.95f, 0.95f, 1f));
            var btRT = bodyTxt.rectTransform;
            btRT.anchorMin = new Vector2(0, 1); btRT.anchorMax = new Vector2(1, 1);
            btRT.pivot = new Vector2(0.5f, 1);
            btRT.anchoredPosition = new Vector2(0, y);
            btRT.sizeDelta = new Vector2(-80, 320);
            bodyTxt.alignment = TextAlignmentOptions.TopLeft;
            bodyTxt.textWrappingMode = TextWrappingModes.Normal;
            y -= 360;

            // 911 — most direct path for active danger.
            BuildResource(card.transform, y,
                "911 · Emergency",
                "If you or someone else is in immediate danger.",
                "Call 911", () => SafeOpen("tel:911"),
                null, null);
            y -= 240;

            // Crisis Text Line — trained for exactly this.
            BuildResource(card.transform, y,
                "Crisis Text Line",
                "Text HOME to 741741. Free, 24/7. Trained for this exact kind of moment.",
                "Text HOME", () => SafeOpen("sms:741741?body=HOME"),
                null, null);
            y -= 240;

            // Trusted adult — long-term resource.
            BuildResource(card.transform, y,
                "Talk to someone you trust",
                "A parent, teacher, school counselor, or doctor. " +
                "Telling someone is not snitching — it's how you stay safe.",
                null, null, null, null);
            y -= 200;

            // Single dismiss button — no "I was just venting" option. A
            // threat to others is qualitatively different from venting and
            // the panel shouldn't offer a path that minimizes it.
            var okBtn = MakeButton(card.transform, 0, 50, 600, 130,
                "Got it",
                new Color(0.30f, 0.45f, 0.65f, 1f), Color.white, 30,
                () => { Dismiss(); });

            // Footer — transparency about what just happened.
            var fTm = MakeText(card.transform, "Footer",
                "Sparq's safety team has been notified. " +
                "If this was a joke, please understand why we take it seriously.",
                20, FontStyles.Italic, new Color(0.75f, 0.78f, 0.92f));
            var fRT = fTm.rectTransform;
            fRT.anchorMin = new Vector2(0, 0); fRT.anchorMax = new Vector2(1, 0);
            fRT.pivot = new Vector2(0.5f, 0);
            fRT.anchoredPosition = new Vector2(0, 12);
            fRT.sizeDelta = new Vector2(-60, 60);
            fTm.alignment = TextAlignmentOptions.Center;
            fTm.textWrappingMode = TextWrappingModes.Normal;

            Debug.LogWarning("[ThreatResponsePanel] Opened — user typed a threat-of-violence pattern.");
        }

        public static void Hide()
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
        }

        private static void Dismiss()
        {
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            PlayerPrefs.SetString(KEY_LAST_DISMISS, now.ToString());
            PlayerPrefs.Save();
            Hide();
        }

        private static void SafeOpen(string url)
        {
            try { Application.OpenURL(url); }
            catch (System.Exception ex)
            { Debug.LogError($"[ThreatResponsePanel] OpenURL '{url}': {ex.Message}"); }
        }

        // ─────────────────────────────────────────────────────────────────
        // Shared layout helpers — same shape as CrisisResourcesPanel's so
        // the two cards look like siblings, not strangers. Duplicated
        // intentionally (rather than reaching into the other class's
        // privates) — these panels evolve independently.
        // ─────────────────────────────────────────────────────────────────

        private static void BuildResource(Transform parent, float y,
            string title, string desc,
            string btn1Text, System.Action btn1, string btn2Text, System.Action btn2)
        {
            var card = NewGO("Resource", parent, typeof(Image));
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(-80, 220);
            card.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.18f, 1f);

            var tt = MakeText(card.transform, "Title", title,
                32, FontStyles.Bold, new Color(1f, 0.85f, 0.40f));
            var ttRT = tt.rectTransform;
            ttRT.anchorMin = new Vector2(0, 1); ttRT.anchorMax = new Vector2(1, 1);
            ttRT.pivot = new Vector2(0.5f, 1);
            ttRT.anchoredPosition = new Vector2(0, -16);
            ttRT.sizeDelta = new Vector2(-40, 44);
            tt.alignment = TextAlignmentOptions.MidlineLeft;

            var dt = MakeText(card.transform, "Desc", desc,
                24, FontStyles.Normal, new Color(0.92f, 0.94f, 1f));
            var dtRT = dt.rectTransform;
            dtRT.anchorMin = new Vector2(0, 1); dtRT.anchorMax = new Vector2(1, 1);
            dtRT.pivot = new Vector2(0.5f, 1);
            dtRT.anchoredPosition = new Vector2(0, -64);
            dtRT.sizeDelta = new Vector2(-40, 70);
            dt.alignment = TextAlignmentOptions.TopLeft;
            dt.textWrappingMode = TextWrappingModes.Normal;

            if (!string.IsNullOrEmpty(btn1Text) && btn1 != null)
                MakeButton(card.transform, -180, 16, 320, 90, btn1Text,
                    new Color(0.40f, 0.85f, 0.55f, 1f), new Color(0.10f, 0.10f, 0.16f), 26, btn1);
            if (!string.IsNullOrEmpty(btn2Text) && btn2 != null)
                MakeButton(card.transform,  180, 16, 320, 90, btn2Text,
                    new Color(0.40f, 0.85f, 0.55f, 1f), new Color(0.10f, 0.10f, 0.16f), 26, btn2);
        }

        private static Button MakeButton(Transform parent,
            float xOffset, float yOffset, float w, float h,
            string label, Color bg, Color fg, float fontSize,
            System.Action onClick)
        {
            var btnGO = NewGO("Btn", parent, typeof(Image), typeof(Button));
            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(xOffset, yOffset);
            rt.sizeDelta = new Vector2(w, h);
            btnGO.GetComponent<Image>().color = bg;
            var btn = btnGO.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var lbl = MakeText(btnGO.transform, "Lbl", label,
                fontSize, FontStyles.Bold, fg);
            var lRT = lbl.rectTransform;
            lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
            lRT.offsetMin = Vector2.zero; lRT.offsetMax = Vector2.zero;
            lbl.alignment = TextAlignmentOptions.Center;
            return btn;
        }

        private static TMP_Text MakeText(Transform parent, string name, string text,
            float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text; tm.fontSize = size; tm.fontStyle = style; tm.color = color;
            tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }

        private static GameObject NewGO(string name, Transform parent, params System.Type[] comps)
        {
            var types = new System.Collections.Generic.List<System.Type> { typeof(RectTransform) };
            types.AddRange(comps);
            var go = new GameObject(name, types.ToArray());
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem",
                typeof(EventSystem), typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(go);
        }
    }
}
