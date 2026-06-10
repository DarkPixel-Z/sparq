using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Crisis resources panel. Opens when ContentModerator detects a
    /// SelfHarmIdeation signal in something the user typed.
    ///
    /// Design principles:
    ///   - COMPASSIONATE, not clinical or punitive
    ///   - No "you violated a rule" framing — they reached out, not attacked
    ///   - Concrete, actionable resources (988, Crisis Text Line)
    ///   - Easy escape ("I'm safe — just venting") so users who used a
    ///     phrase casually aren't trapped
    ///   - Original message is NOT blocked — their friend might be the help
    ///
    /// Sources: 988 Suicide & Crisis Lifeline (US/Canada, call or text 988),
    ///          Crisis Text Line (text HOME to 741741 in US/CA/UK/IE)
    ///
    /// On mobile, "Call/Text" buttons use Application.OpenURL with tel:/sms:
    /// schemes — the OS handles routing to the phone app.
    /// </summary>
    public static class CrisisResourcesPanel
    {
        private static GameObject _root;

        // Cooldown — if user dismissed in the last 10 min, don't re-show on
        // every message. Reaching out repeatedly shouldn't feel like nagging.
        private const string KEY_LAST_DISMISS = "sparq.safety.crisis_dismissed_unix";
        private const long   COOLDOWN_SEC = 600;

        /// <summary>Returns true if we recently showed + dismissed this panel.</summary>
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

            // Top-sort canvas (above everything)
            _root = new GameObject("Sparq_CrisisResourcesPanel",
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
            canv.sortingOrder = maxSort + 50;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim backdrop — does NOT close on tap (intentional, this is important)
            var dim = NewGO("Dim", _root.transform, typeof(Image));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.10f, 0.92f);

            // Card — soft blue/teal palette (calming, not red/alarm)
            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(960, 1500);
            card.GetComponent<Image>().color = new Color(0.10f, 0.18f, 0.32f, 1f);

            // Title bar — warm gold (hope, not red)
            var titleBar = NewGO("TitleBar", card.transform, typeof(Image));
            var tbRT = titleBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 1); tbRT.anchorMax = new Vector2(1, 1);
            tbRT.pivot = new Vector2(0.5f, 1);
            tbRT.anchoredPosition = Vector2.zero;
            tbRT.sizeDelta = new Vector2(0, 140);
            titleBar.GetComponent<Image>().color = new Color(0.95f, 0.78f, 0.35f, 1f);
            var titleTm = MakeText(titleBar.transform, "Title", "WE'RE HERE FOR YOU",
                52, FontStyles.Bold, new Color(0.10f, 0.08f, 0.18f));
            var titleRT = titleTm.rectTransform;
            titleRT.anchorMin = Vector2.zero; titleRT.anchorMax = Vector2.one;
            titleRT.offsetMin = new Vector2(20, 0); titleRT.offsetMax = new Vector2(-20, 0);
            titleTm.alignment = TextAlignmentOptions.Center;

            // Main message — compassionate framing
            float y = -180;
            var bodyTxt = MakeText(card.transform, "Body",
                "It sounds like you're going through something heavy right now. " +
                "You're not alone — and you reached out, which takes real courage.\n\n" +
                "If you need to talk to someone who can help, the options below are " +
                "free, confidential, and available 24/7.",
                30, FontStyles.Normal, new Color(0.95f, 0.95f, 1f));
            var btRT = bodyTxt.rectTransform;
            btRT.anchorMin = new Vector2(0, 1); btRT.anchorMax = new Vector2(1, 1);
            btRT.pivot = new Vector2(0.5f, 1);
            btRT.anchoredPosition = new Vector2(0, y);
            btRT.sizeDelta = new Vector2(-80, 360);
            bodyTxt.alignment = TextAlignmentOptions.TopLeft;
            bodyTxt.textWrappingMode = TextWrappingModes.Normal;
            y -= 390;

            // ── Resources ──────────────────────────────────────────────
            // 988 — Call or text
            BuildResource(card.transform, y,
                "988 · Suicide & Crisis Lifeline",
                "Free, 24/7. Call or text 988 (US & Canada).",
                "Call 988", () => SafeOpen("tel:988"),
                "Text 988", () => SafeOpen("sms:988"));
            y -= 240;

            // Crisis Text Line
            BuildResource(card.transform, y,
                "Crisis Text Line",
                "Text HOME to 741741. Free, 24/7. US · CA · UK · IE.",
                "Text HOME", () => SafeOpen("sms:741741?body=HOME"),
                null, null);
            y -= 240;

            // Tell a trusted adult
            BuildResource(card.transform, y,
                "Talk to someone you trust",
                "A parent, teacher, school counselor, coach, or doctor. " +
                "You don't have to figure this out alone.",
                null, null, null, null);
            y -= 200;

            // ── Bottom action buttons ─────────────────────────────────
            // "I'm safe, just venting" — gentle close, sets cooldown
            var ventBtn = MakeButton(card.transform, -240, 50, 440, 130,
                "I'm safe — just venting",
                new Color(0.30f, 0.45f, 0.65f, 1f), Color.white, 26,
                () => { Dismiss(); });

            // "I need help now" — opens 988 directly
            var helpBtn = MakeButton(card.transform, 240, 50, 440, 130,
                "I need help now",
                new Color(0.95f, 0.55f, 0.30f, 1f), Color.white, 26,
                () => { SafeOpen("tel:988"); Dismiss(); });

            // Footer disclaimer
            var fTm = MakeText(card.transform, "Footer",
                "If you are in immediate danger, call your local emergency number (911 / 999 / 112).",
                20, FontStyles.Italic, new Color(0.75f, 0.78f, 0.92f));
            var fRT = fTm.rectTransform;
            fRT.anchorMin = new Vector2(0, 0); fRT.anchorMax = new Vector2(1, 0);
            fRT.pivot = new Vector2(0.5f, 0);
            fRT.anchoredPosition = new Vector2(0, 12);
            fRT.sizeDelta = new Vector2(-60, 36);
            fTm.alignment = TextAlignmentOptions.Center;
            fTm.textWrappingMode = TextWrappingModes.Normal;

            Debug.Log("[CrisisResourcesPanel] Opened.");
        }

        public static void Hide()
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
        }

        // ─────────────────────────────────────────────────────────────────
        // INTERNAL
        // ─────────────────────────────────────────────────────────────────

        private static void Dismiss()
        {
            // Record dismiss time so we don't re-show on every keystroke
            long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            PlayerPrefs.SetString(KEY_LAST_DISMISS, now.ToString());
            PlayerPrefs.Save();
            Hide();
        }

        private static void SafeOpen(string url)
        {
            try { Application.OpenURL(url); }
            catch (System.Exception ex)
            { Debug.LogError($"[CrisisResourcesPanel] OpenURL '{url}': {ex.Message}"); }
        }

        // Resource block: title + description + up to 2 action buttons
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
            card.GetComponent<Image>().color = new Color(0.06f, 0.12f, 0.22f, 1f);

            // Title
            var tt = MakeText(card.transform, "Title", title,
                32, FontStyles.Bold, new Color(1f, 0.85f, 0.40f));
            var ttRT = tt.rectTransform;
            ttRT.anchorMin = new Vector2(0, 1); ttRT.anchorMax = new Vector2(1, 1);
            ttRT.pivot = new Vector2(0.5f, 1);
            ttRT.anchoredPosition = new Vector2(0, -16);
            ttRT.sizeDelta = new Vector2(-40, 44);
            tt.alignment = TextAlignmentOptions.MidlineLeft;

            // Desc
            var dt = MakeText(card.transform, "Desc", desc,
                24, FontStyles.Normal, new Color(0.92f, 0.94f, 1f));
            var dtRT = dt.rectTransform;
            dtRT.anchorMin = new Vector2(0, 1); dtRT.anchorMax = new Vector2(1, 1);
            dtRT.pivot = new Vector2(0.5f, 1);
            dtRT.anchoredPosition = new Vector2(0, -68);
            dtRT.sizeDelta = new Vector2(-40, 70);
            dt.alignment = TextAlignmentOptions.TopLeft;
            dt.textWrappingMode = TextWrappingModes.Normal;

            // Buttons (1 or 2)
            if (!string.IsNullOrEmpty(btn1Text) && btn1 != null)
            {
                if (!string.IsNullOrEmpty(btn2Text) && btn2 != null)
                {
                    var b1 = MakeButton(card.transform, -120, -180, 340, 84,
                        btn1Text, new Color(0.30f, 0.65f, 0.45f), Color.white, 28, btn1);
                    var b1RT = b1.GetComponent<RectTransform>();
                    b1RT.anchorMin = new Vector2(0, 1); b1RT.anchorMax = new Vector2(0, 1);
                    b1RT.pivot = new Vector2(0, 1);
                    b1RT.anchoredPosition = new Vector2(20, -130);

                    var b2 = MakeButton(card.transform, 120, -180, 340, 84,
                        btn2Text, new Color(0.30f, 0.55f, 0.85f), Color.white, 28, btn2);
                    var b2RT = b2.GetComponent<RectTransform>();
                    b2RT.anchorMin = new Vector2(1, 1); b2RT.anchorMax = new Vector2(1, 1);
                    b2RT.pivot = new Vector2(1, 1);
                    b2RT.anchoredPosition = new Vector2(-20, -130);
                }
                else
                {
                    var b1 = MakeButton(card.transform, 0, -180, 700, 84,
                        btn1Text, new Color(0.30f, 0.65f, 0.45f), Color.white, 28, btn1);
                    var b1RT = b1.GetComponent<RectTransform>();
                    b1RT.anchorMin = new Vector2(0.5f, 1); b1RT.anchorMax = new Vector2(0.5f, 1);
                    b1RT.pivot = new Vector2(0.5f, 1);
                    b1RT.anchoredPosition = new Vector2(0, -130);
                }
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static GameObject MakeButton(Transform parent, float xPos, float yPos,
            float w, float h, string text, Color bg, Color fg, float fontSize, System.Action onTap)
        {
            var btn = NewGO("Btn_" + text, parent, typeof(Image), typeof(Button));
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(xPos, yPos);
            rt.sizeDelta = new Vector2(w, h);
            btn.GetComponent<Image>().color = bg;
            var lbl = MakeText(btn.transform, "Lbl", text, fontSize, FontStyles.Bold, fg);
            var lrt = lbl.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.textWrappingMode = TextWrappingModes.Normal;
            btn.GetComponent<Button>().onClick.AddListener(() => {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                onTap();
            });
            return btn;
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
            tm.text = text;
            tm.fontSize = size;
            tm.fontStyle = style;
            tm.color = color;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }

        private static void EnsureEventSystem()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
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
