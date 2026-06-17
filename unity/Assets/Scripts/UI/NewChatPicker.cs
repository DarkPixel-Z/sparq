using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Modal "start a new chat" picker. Sparq doesn't have a friends-list
    /// backend yet (the PlayerData comment literally says "friends list (when
    /// social lands)"), so this round we let the user type a username instead
    /// of picking from a list. Once a real friends graph exists, replace the
    /// input field with a scrolling friend list — the on-pick callback shape
    /// stays the same.
    ///
    /// Username is run through ContentModerator.InspectUsername so a tester
    /// can't open a DM titled with PII or a slur.
    /// </summary>
    public static class NewChatPicker
    {
        private static GameObject _root;

        // Caller supplies a callback that receives the chosen username.
        // ChatPanel passes a closure that creates the Convo + opens its thread.
        public static void Show(System.Action<string> onPicked)
        {
            if (_root != null) Object.Destroy(_root);
            EnsureEventSystem();

            _root = new GameObject("Sparq_NewChatPicker",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;

            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 16000;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 10;   // above ChatPanel
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim — tap outside to cancel.
            var dim = NewGO("Dim", _root.transform, typeof(Image), typeof(Button));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.78f);
            dim.GetComponent<Button>().onClick.AddListener(Hide);

            // Card
            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(880, 640);
            card.GetComponent<Image>().color = new Color(0.18f, 0.16f, 0.32f, 1f);

            // Title bar
            var title = MakeText(card.transform, "Title", "Start a new chat",
                42, FontStyles.Bold, new Color(1f, 0.97f, 0.85f));
            var tRT = title.rectTransform;
            tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.anchoredPosition = new Vector2(0, -28);
            tRT.sizeDelta = new Vector2(-40, 60);
            title.alignment = TextAlignmentOptions.Center;

            // Helper text
            var hint = MakeText(card.transform, "Hint",
                "Type the username of the person you want to chat with.",
                24, FontStyles.Italic, new Color(0.80f, 0.78f, 0.92f));
            var hRT = hint.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.anchoredPosition = new Vector2(0, -110);
            hRT.sizeDelta = new Vector2(-60, 60);
            hint.alignment = TextAlignmentOptions.Center;
            hint.textWrappingMode = TextWrappingModes.Normal;

            // Input field
            var fieldGO = NewGO("Field", card.transform, typeof(Image), typeof(TMP_InputField));
            var fRT = fieldGO.GetComponent<RectTransform>();
            fRT.anchorMin = new Vector2(0.5f, 0.5f); fRT.anchorMax = new Vector2(0.5f, 0.5f);
            fRT.pivot = new Vector2(0.5f, 0.5f);
            fRT.anchoredPosition = new Vector2(0, 20);
            fRT.sizeDelta = new Vector2(720, 110);
            fieldGO.GetComponent<Image>().color = new Color(1f, 0.96f, 0.85f, 1f);

            var textArea = NewGO("TextArea", fieldGO.transform, typeof(RectMask2D));
            var taRT = textArea.GetComponent<RectTransform>();
            Stretch(taRT); taRT.offsetMin = new Vector2(24, 6); taRT.offsetMax = new Vector2(-24, -6);

            var placeholder = MakeText(textArea.transform, "Placeholder", "username…",
                32, FontStyles.Italic, new Color(0.55f, 0.50f, 0.36f, 0.9f));
            Stretch(placeholder.rectTransform);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;

            var inputText = MakeText(textArea.transform, "Text", "",
                32, FontStyles.Bold, new Color(0.20f, 0.15f, 0.10f, 1f));
            Stretch(inputText.rectTransform);
            inputText.alignment = TextAlignmentOptions.MidlineLeft;

            var field = fieldGO.GetComponent<TMP_InputField>();
            field.textViewport       = taRT;
            field.textComponent      = inputText;
            field.placeholder        = placeholder;
            field.lineType           = TMP_InputField.LineType.SingleLine;
            field.characterLimit     = 24;
            field.restoreOriginalTextOnEscape = false;

            // Buttons
            void Submit()
            {
                string raw = (field.text ?? "").Trim();
                if (string.IsNullOrEmpty(raw)) return;
                // Run the same username moderation gate AuthPanel uses.
                var verdict = Sparq.Safety.ContentModerator.InspectUsername(raw);
                if (!verdict.Allowed)
                {
                    Debug.LogWarning($"[NewChatPicker] Blocked username: {verdict.UserFacingMessage}");
                    field.text = verdict.SanitizedText ?? "";
                    placeholder.text = string.IsNullOrEmpty(verdict.UserFacingMessage)
                        ? "Try a different name…"
                        : verdict.UserFacingMessage;
                    return;
                }
                string clean = verdict.SanitizedText ?? raw;
                Hide();
                onPicked?.Invoke(clean);
            }

            var cancel = MakeButton(card.transform, -180, 28, 320, 110,
                "Cancel", new Color(0.40f, 0.36f, 0.55f), Color.white, 28,
                Hide);
            var start  = MakeButton(card.transform,  180, 28, 320, 110,
                "Start", new Color(0.30f, 0.78f, 0.42f), Color.white, 32,
                Submit);

            field.onSubmit.AddListener(_ => Submit());

            // Auto-focus the field so the keyboard pops immediately.
            EventSystem.current?.SetSelectedGameObject(fieldGO);
            field.ActivateInputField();
        }

        public static void Hide()
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
        }

        // ── Tiny shared layout helpers ──

        private static Button MakeButton(Transform parent,
            float xOffset, float yOffset, float w, float h,
            string label, Color bg, Color fg, float fontSize,
            System.Action onClick)
        {
            var btnGO = NewGO("Btn_" + label, parent, typeof(Image), typeof(Button));
            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(xOffset, yOffset);
            rt.sizeDelta = new Vector2(w, h);
            btnGO.GetComponent<Image>().color = bg;
            var btn = btnGO.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var lbl = MakeText(btnGO.transform, "Lbl", label, fontSize, FontStyles.Bold, fg);
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
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
            var types = new List<System.Type> { typeof(RectTransform) };
            types.AddRange(comps);
            var go = new GameObject(name, types.ToArray());
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
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
