using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Codex password modal: pick a sequence of arcane glyphs.
    /// Used in two modes:
    ///   • SETUP — first-time setup, asks the user to pick 4 glyphs
    ///   • VERIFY — re-enter to unlock a locked entry; calls onSuccess
    /// </summary>
    public static class JournalCodexLock
    {
        private static readonly Color GOLD       = new Color(1f, 0.82f, 0.30f);
        private static readonly Color CREAM      = new Color(1f, 0.97f, 0.85f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.13f, 0.28f);
        private static readonly Color CARD_BG    = new Color(0.22f, 0.20f, 0.40f, 1f);

        private const int CODE_LEN = 4;

        private static GameObject _root;
        private static System.Text.StringBuilder _entered;
        private static TMP_Text[] _slots;
        private static TMP_Text _hint;
        private static System.Action<bool> _onClose;
        private static bool _setupMode;

        public static void ShowSetup(System.Action<bool> onClose = null)
            => Open(onClose, setupMode: true);

        public static void ShowVerify(System.Action<bool> onSuccess)
            => Open(onSuccess, setupMode: false);

        private static void Open(System.Action<bool> onClose, bool setupMode)
        {
            if (_root != null) Close(false);
            _entered = new System.Text.StringBuilder();
            _onClose = onClose;
            _setupMode = setupMode;

            _root = new GameObject("JournalCodexLock",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 14900;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim
            var dim = MakeImage(_root.transform, "Dim", new Color(0, 0, 0, 0.95f));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(() => Close(false));

            // Card
            var card = MakeRounded(_root.transform, "Card", CARD_BG, 28);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(900, 1300);

            // Title
            MakeText(card.transform, "T",
                setupMode ? "✦  SET YOUR CODEX  ✦" : "✦  ENTER CODEX  ✦",
                40, FontStyles.Bold, GOLD,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, 70))
                .alignment = TextAlignmentOptions.Center;

            // Hint
            _hint = MakeText(card.transform, "Hint",
                setupMode ? "Pick 4 glyphs in order. Memorize the sequence."
                          : "Enter the 4 glyphs to unlock.",
                22, FontStyles.Italic, new Color(1, 1, 1, 0.7f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -130), new Vector2(0, 36));
            _hint.alignment = TextAlignmentOptions.Center;

            // 4 slot boxes for the entered sequence
            _slots = new TMP_Text[CODE_LEN];
            float slotW = 110, gap = 16;
            float totalW = CODE_LEN * slotW + (CODE_LEN - 1) * gap;
            float startX = -totalW * 0.5f + slotW * 0.5f;
            for (int i = 0; i < CODE_LEN; i++)
            {
                var slot = MakeRounded(card.transform, $"Slot_{i}",
                    new Color(0.16f, 0.13f, 0.30f, 1f), 16);
                var srt = slot.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.5f, 1); srt.anchorMax = new Vector2(0.5f, 1);
                srt.pivot = new Vector2(0.5f, 1);
                srt.anchoredPosition = new Vector2(startX + i * (slotW + gap), -210);
                srt.sizeDelta = new Vector2(slotW, slotW);
                // Gold edge ring
                var ring = MakeRounded(slot.transform, "Ring", GOLD, 18);
                var rrt2 = ring.GetComponent<RectTransform>();
                rrt2.anchorMin = Vector2.zero; rrt2.anchorMax = Vector2.one;
                rrt2.offsetMin = new Vector2(-3, -3); rrt2.offsetMax = new Vector2(3, 3);
                ring.transform.SetAsFirstSibling();
                _slots[i] = MakeText(slot.transform, "G", "·",
                    72, FontStyles.Bold, GOLD,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                _slots[i].alignment = TextAlignmentOptions.Center;
                _slots[i].outlineWidth = 0.30f;
                _slots[i].outlineColor = new Color(0.10f, 0.05f, 0);
            }

            // Glyph picker grid (3 cols × 4 rows)
            var grid = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(card.transform, false);
            var gRT = grid.GetComponent<RectTransform>();
            gRT.anchorMin = new Vector2(0, 1); gRT.anchorMax = new Vector2(1, 1);
            gRT.pivot = new Vector2(0.5f, 1);
            gRT.anchoredPosition = new Vector2(0, -360);
            gRT.sizeDelta = new Vector2(-80, 720);
            var glg = grid.GetComponent<GridLayoutGroup>();
            glg.padding = new RectOffset(20, 20, 0, 0);
            glg.spacing = new Vector2(16, 16);
            glg.cellSize = new Vector2(240, 160);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.childAlignment = TextAnchor.UpperCenter;

            foreach (var glyph in Sparq.Systems.JournalCodex.GLYPHS)
            {
                BuildGlyphButton(grid.transform, glyph);
            }

            // Submit / Reset / Cancel
            var submit = MakeBtn(card.transform, "Submit",
                setupMode ? "✓  CONFIRM" : "✓  UNLOCK",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-200, 80), new Vector2(360, 110),
                new Color(0.30f, 0.80f, 0.42f), Color.white, 30);
            ApplyRound(submit);
            var sLbl = submit.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (sLbl != null) { sLbl.color = DEEP_NAVY; sLbl.outlineWidth = 0.22f; sLbl.outlineColor = new Color(0.85f, 1f, 0.85f); }
            submit.onClick.AddListener(OnSubmit);

            var cancel = MakeBtn(card.transform, "Cancel", "Cancel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(200, 80), new Vector2(360, 110),
                new Color(0.92f, 0.35f, 0.42f), Color.white, 28);
            ApplyRound(cancel);
            var cLbl = cancel.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (cLbl != null) { cLbl.outlineWidth = 0.22f; cLbl.outlineColor = new Color(0.10f, 0.05f, 0.20f); }
            cancel.onClick.AddListener(() => Close(false));
        }

        private static void BuildGlyphButton(Transform parent, string glyph)
        {
            var btn = MakeRounded(parent, $"G_{glyph}", new Color(0.36f, 0.32f, 0.60f), 16);
            var b = btn.AddComponent<Button>();
            var tm = MakeText(btn.transform, "T", glyph,
                72, FontStyles.Bold, GOLD,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            tm.alignment = TextAlignmentOptions.Center;
            tm.outlineWidth = 0.32f;
            tm.outlineColor = new Color(0.15f, 0.05f, 0.18f);
            string captured = glyph;
            b.onClick.AddListener(() => OnGlyphTapped(captured));
        }

        private static void OnGlyphTapped(string glyph)
        {
            if (_entered.Length >= CODE_LEN) return;
            _entered.Append(glyph);
            int idx = _entered.Length - 1;
            if (_slots != null && idx < _slots.Length && _slots[idx] != null)
            {
                _slots[idx].text = glyph;
                // Pop animation
                var rt = _slots[idx].rectTransform;
                if (rt != null) rt.localScale = Vector3.one * 1.3f;
            }
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            // Auto-submit on full
            if (_entered.Length == CODE_LEN && !_setupMode)
            {
                // small delay so user sees the last slot fill
                EnsureRunner();
                if (_runner != null) _runner.StartCoroutine(AutoSubmit());
            }
        }

        private static System.Collections.IEnumerator AutoSubmit()
        {
            yield return new WaitForSeconds(0.25f);
            OnSubmit();
        }

        private static void OnSubmit()
        {
            if (_entered.Length < CODE_LEN)
            {
                if (_hint != null) _hint.text = $"Pick {CODE_LEN - _entered.Length} more glyph(s)…";
                return;
            }
            string code = _entered.ToString();
            if (_setupMode)
            {
                Sparq.Systems.JournalCodex.Set(code);
                if (_hint != null) _hint.text = "✓ Codex sealed.";
                Close(true);
            }
            else
            {
                if (Sparq.Systems.JournalCodex.Verify(code))
                {
                    if (_hint != null) _hint.text = "✓ Welcome back.";
                    Close(true);
                }
                else
                {
                    if (_hint != null) _hint.text = "✗ Wrong sequence — try again.";
                    _entered.Clear();
                    foreach (var s in _slots) if (s != null) s.text = "·";
                    try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Hit); } catch {}
                }
            }
        }

        private static void Close(bool success)
        {
            var cb = _onClose;
            _onClose = null;
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            cb?.Invoke(success);
        }

        // ─── helpers ───
        private static MonoBehaviour _runner;
        private static void EnsureRunner()
        {
            if (_runner != null && _runner.gameObject != null) return;
            var go = GameObject.Find("CodexLockRunner");
            if (go == null) { go = new GameObject("CodexLockRunner"); UnityEngine.Object.DontDestroyOnLoad(go); }
            _runner = go.AddComponent<RunnerStub>();
        }
        private class RunnerStub : MonoBehaviour {}

        private static void ApplyRound(Button btn)
        {
            var img = btn.GetComponent<Image>();
            if (img != null) { img.sprite = LoadRoundedSprite(20); img.type = Image.Type.Sliced; }
        }

        private static System.Collections.Generic.Dictionary<int, Sprite> _roundedCache = new System.Collections.Generic.Dictionary<int, Sprite>();
        private static Sprite LoadRoundedSprite(int radius)
        {
            if (_roundedCache.TryGetValue(radius, out var sp) && sp != null) return sp;
            int size = radius * 2 + 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside; int dx = 0, dy = 0;
                if (x < radius && y < radius) { dx = radius - x; dy = radius - y; inside = dx*dx+dy*dy <= radius*radius; }
                else if (x >= size-radius && y < radius) { dx = x-(size-radius-1); dy = radius-y; inside = dx*dx+dy*dy <= radius*radius; }
                else if (x < radius && y >= size-radius) { dx = radius-x; dy = y-(size-radius-1); inside = dx*dx+dy*dy <= radius*radius; }
                else if (x >= size-radius && y >= size-radius) { dx = x-(size-radius-1); dy = y-(size-radius-1); inside = dx*dx+dy*dy <= radius*radius; }
                else inside = true;
                tex.SetPixel(x, y, inside ? Color.white : new Color(0,0,0,0));
            }
            tex.Apply();
            sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            _roundedCache[radius] = sp;
            return sp;
        }

        private static GameObject MakeRounded(Transform parent, string name, Color color, int radius)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = LoadRoundedSprite(radius); img.type = Image.Type.Sliced; img.color = color;
            return go;
        }
        private static GameObject MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }
        private static TMP_Text MakeText(Transform parent, string name, string text,
            float size, FontStyles style, Color color,
            Vector2 amin, Vector2 amax, Vector2 anch, Vector2 sd)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.pivot = new Vector2((amin.x + amax.x) * 0.5f, (amin.y + amax.y) * 0.5f);
            rt.anchoredPosition = anch; rt.sizeDelta = sd;
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text; tm.fontSize = size; tm.fontStyle = style; tm.color = color;
            tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }
        private static Button MakeBtn(Transform parent, string name, string label,
            Vector2 amin, Vector2 amax, Vector2 anch, Vector2 sd,
            Color bg, Color fg, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.pivot = new Vector2((amin.x + amax.x) * 0.5f, (amin.y + amax.y) * 0.5f);
            rt.anchoredPosition = anch; rt.sizeDelta = sd;
            go.GetComponent<Image>().color = bg;
            var t = new GameObject("Lbl", typeof(RectTransform));
            t.transform.SetParent(go.transform, false);
            var trt = t.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var tm = t.AddComponent<TextMeshProUGUI>();
            tm.text = label; tm.fontSize = fontSize; tm.fontStyle = FontStyles.Bold;
            tm.color = fg; tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset; tm.raycastTarget = false;
            return go.GetComponent<Button>();
        }
    }
}
