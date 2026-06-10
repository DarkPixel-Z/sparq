// SpoonCheckPanel.cs — the morning "How much energy do you have today?"
// tile. Maps the user's pick (Low / Medium / High) onto a MoodService
// entry so the existing mood-log + journal calendar pick it up, AND so
// QuestManager.InferEnergyLevel() can adapt the daily quest list to the
// user's spoon level (see SPARQ_DESIGN_NOTES.md §6 — Energy-Adaptive).
//
// Low    → MoodService.Mood.Tired      → EnergyLevel.Low    (smaller quests)
// Medium → MoodService.Mood.Calm       → EnergyLevel.Medium (standard pool)
// High   → MoodService.Mood.Focused    → EnergyLevel.High   (full pool)
//
// Also auto-completes the daily `spoon_check` quest if one is active —
// the user gets the XP for free for the act of checking in.

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class SpoonCheckPanel
    {
        // ── Palette — matches JournalPanel's bright & colourful theme ──
        private static readonly Color BACKDROP   = new Color(0.69f, 0.84f, 0.83f, 1f);  // soft teal popup backdrop
        private static readonly Color TITLEBAR   = new Color(0.11f, 0.55f, 0.53f, 1f);  // vibrant teal spine
        private static readonly Color INK        = new Color(0.11f, 0.13f, 0.16f, 1f);  // near-black charcoal — titles
        private static readonly Color CREAM      = new Color(0.18f, 0.20f, 0.24f, 1f);  // charcoal body text
        private static readonly Color INK_SOFT   = new Color(0.20f, 0.23f, 0.28f, 1f);  // darker slate secondary
        private static readonly Color GOLD       = new Color(0.96f, 0.66f, 0.10f, 1f);  // bright gold accent

        // Spoon-tile colors — Low/Med/High map onto the existing crystal palette.
        private static readonly Color LOW_COLOR  = new Color(0.65f, 0.55f, 0.85f, 1f);  // weary purple
        private static readonly Color MED_COLOR  = new Color(0.55f, 0.85f, 1.00f, 1f);  // calm blue
        private static readonly Color HIGH_COLOR = new Color(1.00f, 0.85f, 0.35f, 1f);  // focused gold

        private const string POPUP_BG     = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Popup/Popup_Box_Bg.png";
        private const string POPUP_BORDER = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Popup/Popup_Box_Border.png";
        private const string CIRCLE_BG    = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Border_Circle_H67_White_Bg.png";

        public enum SpoonLevel { Low, Medium, High }

        private static GameObject _root;

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC API
        // ─────────────────────────────────────────────────────────────────

        public static void Show()
        {
            if (_root != null) { Hide(); return; }
            EnsureEventSystem();

            _root = new GameObject("Sparq_SpoonCheckPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            Stretch(rrt);

            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 15000;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 20;

            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim — tap to close
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
            crt.sizeDelta = new Vector2(940, 1340);
            var cardImg = card.GetComponent<Image>();
            var bgSp = LoadSprite(POPUP_BG);
            if (bgSp != null) { cardImg.sprite = bgSp; cardImg.type = Image.Type.Sliced; }
            cardImg.color = BACKDROP;

            // Border
            var border = NewGO("Border", card.transform, typeof(Image));
            Stretch(border.GetComponent<RectTransform>());
            var brImg = border.GetComponent<Image>();
            var brSp = LoadSprite(POPUP_BORDER);
            if (brSp != null) { brImg.sprite = brSp; brImg.type = Image.Type.Sliced; brImg.color = TITLEBAR; }
            else brImg.color = new Color(0.11f, 0.55f, 0.53f, 0.6f);
            brImg.raycastTarget = false;

            BuildTitleBar(card.transform);
            BuildPrompt(card.transform);
            BuildTiles(card.transform);
            BuildSkip(card.transform);

            Debug.Log("[SpoonCheckPanel] Opened.");
        }

        public static void Hide()
        {
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
        }

        /// <summary>True if the user has already done their Spoon Check
        /// today (any mood log of today counts).</summary>
        public static bool CheckedToday()
        {
            try { return Sparq.Systems.MoodService.LoggedToday(); }
            catch { return false; }
        }

        // ─────────────────────────────────────────────────────────────────
        // BUILDERS
        // ─────────────────────────────────────────────────────────────────

        private static void BuildTitleBar(Transform card)
        {
            var bar = NewGO("TitleBar", card, typeof(Image));
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -14);
            rt.sizeDelta = new Vector2(-36, 120);
            bar.GetComponent<Image>().color = TITLEBAR;

            var title = MakeText(bar.transform, "Title", "SPOON CHECK", 56, FontStyles.Bold, GOLD);
            Stretch(title.rectTransform); title.alignment = TextAlignmentOptions.Center;
            try { title.outlineWidth = 0.25f; title.outlineColor = new Color(0.05f, 0.22f, 0.21f); } catch {}

            var close = NewGO("Close", bar.transform, typeof(Image), typeof(Button));
            var xrt = close.GetComponent<RectTransform>();
            xrt.anchorMin = new Vector2(1, 0.5f); xrt.anchorMax = new Vector2(1, 0.5f);
            xrt.pivot = new Vector2(1, 0.5f);
            xrt.anchoredPosition = new Vector2(-20, 0);
            xrt.sizeDelta = new Vector2(78, 78);
            close.GetComponent<Image>().color = new Color(0.82f, 0.26f, 0.26f, 1f);
            var xl = MakeText(close.transform, "X", "X", 44, FontStyles.Bold, Color.white);
            Stretch(xl.rectTransform); xl.alignment = TextAlignmentOptions.Center;
            close.GetComponent<Button>().onClick.AddListener(Hide);
        }

        private static void BuildPrompt(Transform card)
        {
            var head = MakeText(card, "Head", "How many spoons today?", 50, FontStyles.Bold, INK);
            var hRT = head.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.offsetMin = new Vector2(40, -228); hRT.offsetMax = new Vector2(-40, -140);
            head.alignment = TextAlignmentOptions.Center;

            var sub = MakeText(card, "Sub",
                "Your answer shapes today's quests — low days get smaller asks.",
                34, FontStyles.Bold, CREAM);
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0.5f, 1);
            sRT.offsetMin = new Vector2(40, -322); sRT.offsetMax = new Vector2(-40, -228);
            sub.alignment = TextAlignmentOptions.Center;
            sub.textWrappingMode = TextWrappingModes.Normal;
        }

        // Three big choice tiles — Low / Medium / High.
        private static void BuildTiles(Transform card)
        {
            BuildTile(card, SpoonLevel.Low,
                "Low",     "Save your spoons",
                "Tired, drained, or sensory-cooked. Today is for tiny wins.",
                LOW_COLOR, anchorY: -340);

            BuildTile(card, SpoonLevel.Medium,
                "Medium",  "Steady day",
                "Balanced. You can take on the regular quest list.",
                MED_COLOR, anchorY: -610);

            BuildTile(card, SpoonLevel.High,
                "High",    "Take on the hard thing",
                "Charged up. Today's a great day to attack the hard quest first.",
                HIGH_COLOR, anchorY: -880);
        }

        private static void BuildTile(Transform card, SpoonLevel level,
            string heading, string blurb, string body, Color accent, float anchorY)
        {
            var tile = NewGO("Tile_" + level, card, typeof(Image), typeof(Button));
            var rt = tile.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(40, anchorY - 240);
            rt.offsetMax = new Vector2(-40, anchorY);
            var img = tile.GetComponent<Image>();
            img.color = new Color(0.99f, 0.99f, 0.98f, 1f);   // bright white page
            img.raycastTarget = true;
            var btn = tile.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = true;

            // Spoon visual (left column) — 1/2/3 stylized spoons that make
            // the "spoon theory" metaphor literal. No text inside the
            // bubble (the in-disc text was cramped and unreadable).
            int spoonCount = level == SpoonLevel.Low ? 1 : level == SpoonLevel.Medium ? 2 : 3;
            var holder = NewGO("Spoons", tile.transform, typeof(HorizontalLayoutGroup));
            var hRT = holder.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 0.5f); hRT.anchorMax = new Vector2(0, 0.5f);
            hRT.pivot = new Vector2(0, 0.5f);
            hRT.anchoredPosition = new Vector2(22, 0);
            hRT.sizeDelta = new Vector2(140, 150);
            var hlg = holder.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = -8;
            // LEFT-align so the cluster hugs the tile's left edge and never drifts
            // right toward the text (centering let the 3-spoon High tile run in).
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false; hlg.childControlHeight = false;
            for (int i = 0; i < spoonCount; i++) BuildSpoon(holder.transform, accent);

            // Text column starts well clear of the spoons (cluster ends ~x118; text
            // begins at 220 → a guaranteed gap on every tile, incl. 3-spoon High).
            const float TEXT_LEFT = 220f;

            // Heading (right column) — big and dark.
            var blurbT = MakeText(tile.transform, "Blurb", blurb, 48, FontStyles.Bold, INK);
            var bRT = blurbT.rectTransform;
            bRT.anchorMin = new Vector2(0, 1); bRT.anchorMax = new Vector2(1, 1);
            bRT.pivot = new Vector2(0, 1);
            bRT.offsetMin = new Vector2(TEXT_LEFT, -100); bRT.offsetMax = new Vector2(-24, -22);
            blurbT.alignment = TextAlignmentOptions.MidlineLeft;

            // Body description (right column).
            var bodyT = MakeText(tile.transform, "Body", body, 32, FontStyles.Normal, CREAM);
            var byRT = bodyT.rectTransform;
            byRT.anchorMin = new Vector2(0, 0); byRT.anchorMax = new Vector2(1, 1);
            byRT.offsetMin = new Vector2(TEXT_LEFT, 22); byRT.offsetMax = new Vector2(-24, -110);
            bodyT.alignment = TextAlignmentOptions.TopLeft;
            bodyT.textWrappingMode = TextWrappingModes.Normal;

            btn.onClick.AddListener(() => OnPick(level));
        }

        // One stylized spoon: an oval bowl on top + a thin handle below,
        // both tinted in the tile's accent colour. Built from the same
        // circle sprite stretched two ways so it scales cleanly with the
        // HorizontalLayoutGroup parent.
        private static void BuildSpoon(Transform parent, Color color)
        {
            var spoon = NewGO("Spoon", parent, typeof(LayoutElement));
            var le = spoon.GetComponent<LayoutElement>();
            le.preferredWidth = 30; le.preferredHeight = 118;

            var circ = LoadSprite(CIRCLE_BG);

            var bowl = NewGO("Bowl", spoon.transform, typeof(Image));
            var bRT = bowl.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.5f, 1); bRT.anchorMax = new Vector2(0.5f, 1);
            bRT.pivot = new Vector2(0.5f, 1);
            bRT.anchoredPosition = new Vector2(0, 0);
            bRT.sizeDelta = new Vector2(30, 40);          // narrower oval bowl
            var bImg = bowl.GetComponent<Image>();
            if (circ != null) bImg.sprite = circ;
            bImg.color = color;
            bImg.raycastTarget = false;

            var handle = NewGO("Handle", spoon.transform, typeof(Image));
            var hRT2 = handle.GetComponent<RectTransform>();
            hRT2.anchorMin = new Vector2(0.5f, 1); hRT2.anchorMax = new Vector2(0.5f, 1);
            hRT2.pivot = new Vector2(0.5f, 1);
            hRT2.anchoredPosition = new Vector2(0, -36);   // tucked just under the bowl
            hRT2.sizeDelta = new Vector2(10, 72);
            var hImg = handle.GetComponent<Image>();
            if (circ != null) hImg.sprite = circ;          // stretched circle = rounded bar
            hImg.color = color;
            hImg.raycastTarget = false;
        }

        private static void BuildSkip(Transform card)
        {
            var btn = NewGO("Skip", card, typeof(Image), typeof(Button));
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 36);
            rt.sizeDelta = new Vector2(440, 88);
            var img = btn.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0);   // transparent — text-only "Skip" link
            img.raycastTarget = true;
            var b = btn.GetComponent<Button>();
            b.targetGraphic = img; b.interactable = true;
            var l = MakeText(btn.transform, "L", "Skip for now", 38, FontStyles.Bold, INK);
            Stretch(l.rectTransform); l.alignment = TextAlignmentOptions.Center;
            try { l.fontStyle |= FontStyles.Underline; } catch {}
            b.onClick.AddListener(Hide);
        }

        // ─────────────────────────────────────────────────────────────────
        // PICK HANDLER — log to MoodService, complete the spoon_check quest
        // ─────────────────────────────────────────────────────────────────

        private static void OnPick(SpoonLevel level)
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}

            // Map Spoon level → MoodService mood. Reuses the existing log so
            // the journal calendar / streak count pick it up automatically.
            Sparq.Systems.MoodService.Mood mood;
            switch (level)
            {
                case SpoonLevel.Low:    mood = Sparq.Systems.MoodService.Mood.Tired;   break;
                case SpoonLevel.High:   mood = Sparq.Systems.MoodService.Mood.Focused; break;
                default:                mood = Sparq.Systems.MoodService.Mood.Calm;    break;
            }
            try { Sparq.Systems.MoodService.Log(mood); }
            catch (System.Exception ex)
            { Debug.LogError($"[SpoonCheckPanel] MoodService.Log failed: {ex.Message}"); }

            // Auto-complete today's spoon_check quest if present — the user
            // earns its XP just for checking in.
            TryCompleteSpoonCheckQuest();

            // Bump the daily quest list now so any energy-based filtering
            // re-applies on the very next open.
            try { Sparq.Systems.QuestManager.Instance?.ForceRefresh(); } catch {}

            Debug.Log($"[SpoonCheckPanel] {level} ({mood}) logged.");
            Hide();
        }

        private static void TryCompleteSpoonCheckQuest()
        {
            try
            {
                var data = Sparq.Core.SaveService.Data;
                if (data?.customTasks == null) return;
                foreach (var t in data.customTasks)
                {
                    if (t == null || t.done) continue;
                    if (t.questId == "spoon_check")
                    {
                        Sparq.Systems.QuestManager.Instance?.CompleteQuest(t);
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[SpoonCheckPanel] Quest auto-complete failed: {ex.Message}"); }
        }

        // ─────────────────────────────────────────────────────────────────
        // SHELL HELPERS — same pattern as JournalPanel
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

        private static Sprite LoadSprite(string assetPath) => Sparq.Core.SpriteLoader.Load(assetPath);

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
