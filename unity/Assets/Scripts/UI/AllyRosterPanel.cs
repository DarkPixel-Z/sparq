// AllyRosterPanel.cs — manage the squad's 3rd slot.
//
// Scrollable grid of all 8 allies showing portrait, name, blurb, scaled stats,
// and a status badge (Locked / Equip / Active). Tap an UNLOCKED card → it
// becomes the active ally on slot 3 of the squad.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class AllyRosterPanel
    {
        private static readonly Color CARD_BG    = new Color(0.15f, 0.15f, 0.20f, 1f);
        private static readonly Color ROW_BG     = new Color(0.21f, 0.21f, 0.27f, 1f);
        private static readonly Color CREAM      = new Color(1f, 0.97f, 0.85f, 1f);
        private static readonly Color INK_SOFT   = new Color(0.80f, 0.82f, 0.90f, 1f);
        private static readonly Color GOLD       = new Color(0.99f, 0.80f, 0.30f, 1f);
        private static readonly Color BTN_GREEN  = new Color(0.32f, 0.74f, 0.42f, 1f);
        private static readonly Color BTN_GRAY   = new Color(0.34f, 0.36f, 0.44f, 1f);
        private static readonly Color LOCKED_TINT= new Color(0.40f, 0.40f, 0.45f, 1f);
        private static readonly Color ACTIVE_GLOW= new Color(0.99f, 0.80f, 0.30f, 0.20f);

        private static GameObject _root;
        private static Transform  _contentParent;

        public static void Show()
        {
            if (_root != null) { Hide(); return; }
            EnsureEventSystem();

            _root = new GameObject("Sparq_AllyRosterPanel",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Stretch(_root.GetComponent<RectTransform>());
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 15500;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort) maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 20;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            var dim = NewGO("Dim", _root.transform, typeof(Image), typeof(Button));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.82f);
            dim.GetComponent<Button>().onClick.AddListener(Hide);

            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(940, 1620);
            card.GetComponent<Image>().color = CARD_BG;

            // Title bar
            var titleBar = NewGO("TitleBar", card.transform, typeof(Image));
            var tbRT = titleBar.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 1); tbRT.anchorMax = new Vector2(1, 1);
            tbRT.pivot = new Vector2(0.5f, 1);
            tbRT.anchoredPosition = Vector2.zero; tbRT.sizeDelta = new Vector2(0, 130);
            titleBar.GetComponent<Image>().color = new Color(0.55f, 0.42f, 0.92f, 1f);
            var title = MakeText(titleBar.transform, "T", "SQUAD ALLY", 52, FontStyles.Bold, CREAM);
            try { title.outlineWidth = 0.25f; title.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            Stretch(title.rectTransform); title.alignment = TextAlignmentOptions.Center;

            // Close X
            var close = NewGO("Close", titleBar.transform, typeof(Image), typeof(Button));
            var cbRT = close.GetComponent<RectTransform>();
            cbRT.anchorMin = new Vector2(1, 0.5f); cbRT.anchorMax = new Vector2(1, 0.5f);
            cbRT.pivot = new Vector2(1, 0.5f); cbRT.anchoredPosition = new Vector2(-22, 0);
            cbRT.sizeDelta = new Vector2(82, 82);
            close.GetComponent<Image>().color = new Color(0.85f, 0.26f, 0.26f, 1f);
            var cLbl = MakeText(close.transform, "X", "X", 46, FontStyles.Bold, Color.white);
            Stretch(cLbl.rectTransform); cLbl.alignment = TextAlignmentOptions.Center;
            close.GetComponent<Button>().onClick.AddListener(Hide);

            // Scroll list of ally cards
            var scrollGO = NewGO("Scroll", card.transform, typeof(Image), typeof(ScrollRect));
            var srRT = scrollGO.GetComponent<RectTransform>();
            srRT.anchorMin = new Vector2(0, 0); srRT.anchorMax = new Vector2(1, 1);
            srRT.offsetMin = new Vector2(24, 24); srRT.offsetMax = new Vector2(-24, -150);
            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 32f;

            var vp = NewGO("VP", scrollGO.transform, typeof(Image), typeof(RectMask2D));
            Stretch(vp.GetComponent<RectTransform>());
            vp.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            var content = NewGO("Content", vp.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var ctRT = content.GetComponent<RectTransform>();
            ctRT.anchorMin = new Vector2(0, 1); ctRT.anchorMax = new Vector2(1, 1);
            ctRT.pivot = new Vector2(0.5f, 1);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14; vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            var fit = content.GetComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vp.GetComponent<RectTransform>(); sr.content = ctRT;
            _contentParent = content.transform;

            int lvl = Sparq.Core.SaveService.Data?.level ?? 1;
            string activeId = Sparq.Systems.AllyRoster.Active()?.id ?? "";
            for (int i = 0; i < Sparq.Systems.AllyRoster.ALL.Length; i++)
                BuildAllyCard(Sparq.Systems.AllyRoster.ALL[i], i, lvl, activeId);

            Debug.Log("[AllyRoster] Opened.");
        }

        public static void Hide()
        {
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
            _contentParent = null;
        }

        private static void BuildAllyCard(Sparq.Systems.AllyRoster.Ally a, int idx, int lvl, string activeId)
        {
            bool unlocked = Sparq.Systems.AllyRoster.IsUnlocked(a.id);
            bool active   = a.id == activeId;
            int hp  = a.baseHp + lvl * a.hpPerLevel;
            int atk = a.baseAtk + lvl * a.atkPerLevel;
            int def = a.baseDef + lvl * a.defPerLevel;

            var row = NewGO("Ally_" + a.id, _contentParent, typeof(Image), typeof(Button), typeof(LayoutElement));
            var le = row.GetComponent<LayoutElement>();
            le.preferredHeight = 248; le.minHeight = 248;
            var rImg = row.GetComponent<Image>();
            rImg.color = active ? new Color(ROW_BG.r + 0.06f, ROW_BG.g + 0.04f, ROW_BG.b + 0.10f, 1f) : ROW_BG;
            var rBtn = row.GetComponent<Button>();
            rBtn.targetGraphic = rImg; rBtn.interactable = unlocked && !active;
            string capId = a.id;
            rBtn.onClick.AddListener(() =>
            {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                Sparq.Systems.AllyRoster.SetActive(capId);
                // Rebuild so the new active state shows.
                Hide();
                Show();
            });

            // Active glow
            if (active)
            {
                var glow = NewGO("Glow", row.transform, typeof(Image));
                var gRT = glow.GetComponent<RectTransform>();
                gRT.anchorMin = Vector2.zero; gRT.anchorMax = Vector2.one;
                gRT.offsetMin = new Vector2(-4, -4); gRT.offsetMax = new Vector2(4, 4);
                glow.GetComponent<Image>().color = ACTIVE_GLOW;
                glow.GetComponent<Image>().raycastTarget = false;
                glow.transform.SetAsFirstSibling();
            }

            // Portrait (left)
            var port = NewGO("Port", row.transform, typeof(Image));
            var pRT = port.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0, 0.5f); pRT.anchorMax = new Vector2(0, 0.5f);
            pRT.pivot = new Vector2(0, 0.5f);
            pRT.anchoredPosition = new Vector2(16, 0);
            pRT.sizeDelta = new Vector2(200, 200);
            var pImg = port.GetComponent<Image>();
            var sp = LoadPortrait(a.idleBase);
            if (sp != null) { pImg.sprite = sp; pImg.preserveAspect = true; }
            else pImg.color = new Color(0.5f, 0.7f, 1f);
            pImg.color = unlocked ? Color.white : LOCKED_TINT;   // locked = greyed
            pImg.raycastTarget = false;

            // Name (top-right of portrait)
            var nameTxt = MakeText(row.transform, "Name", a.name, 34, FontStyles.Bold, CREAM);
            try { nameTxt.outlineWidth = 0.22f; nameTxt.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var nRT = nameTxt.rectTransform;
            nRT.anchorMin = new Vector2(0, 1); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0, 1);
            nRT.anchoredPosition = new Vector2(232, -16);
            nRT.sizeDelta = new Vector2(-260, 44);
            nameTxt.alignment = TextAlignmentOptions.MidlineLeft;

            // Blurb
            var blurb = MakeText(row.transform, "Blurb", unlocked ? a.blurb : $"Locked — clear zone {idx + 1} to recruit.",
                22, FontStyles.Normal, INK_SOFT);
            var bRT = blurb.rectTransform;
            bRT.anchorMin = new Vector2(0, 1); bRT.anchorMax = new Vector2(1, 1);
            bRT.pivot = new Vector2(0, 1);
            bRT.anchoredPosition = new Vector2(232, -64);
            bRT.sizeDelta = new Vector2(-260, 64);
            blurb.alignment = TextAlignmentOptions.TopLeft;
            blurb.textWrappingMode = TextWrappingModes.Normal;

            // Stats line
            var statsTxt = MakeText(row.transform, "Stats",
                unlocked ? $"<color=#FF8888>HP {hp}</color>   <color=#FFCB66>ATK {atk}</color>   <color=#88CCFF>DEF {def}</color>"
                         : "<color=#FFFFFF88>—</color>",
                24, FontStyles.Bold, CREAM);
            statsTxt.richText = true;
            try { statsTxt.outlineWidth = 0.18f; statsTxt.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var sRT = statsTxt.rectTransform;
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 0);
            sRT.pivot = new Vector2(0, 0);
            sRT.anchoredPosition = new Vector2(232, 22);
            sRT.sizeDelta = new Vector2(-260, 38);
            statsTxt.alignment = TextAlignmentOptions.MidlineLeft;

            // Status badge (right)
            var badge = NewGO("Badge", row.transform, typeof(Image));
            var bdRT = badge.GetComponent<RectTransform>();
            bdRT.anchorMin = new Vector2(1, 0.5f); bdRT.anchorMax = new Vector2(1, 0.5f);
            bdRT.pivot = new Vector2(1, 0.5f);
            bdRT.anchoredPosition = new Vector2(-20, 0);
            bdRT.sizeDelta = new Vector2(170, 64);
            Color badgeCol = active ? GOLD : (unlocked ? BTN_GREEN : BTN_GRAY);
            badge.GetComponent<Image>().color = badgeCol;
            string badgeText = active ? "ACTIVE" : (unlocked ? "EQUIP" : "LOCKED");
            var bLbl = MakeText(badge.transform, "L", badgeText, 24, FontStyles.Bold, active ? new Color(0.10f, 0.05f, 0.18f) : Color.white);
            try { bLbl.outlineWidth = 0.18f; bLbl.outlineColor = new Color(0, 0, 0, 0.6f); } catch {}
            Stretch(bLbl.rectTransform); bLbl.alignment = TextAlignmentOptions.Center;
        }

        private static Sprite LoadPortrait(string idleBase)
        {
#if UNITY_EDITOR
            try { return Sparq.UI.HeroPortrait.LoadCropped(idleBase + "000.png"); }
            catch { return null; }
#else
            return null;
#endif
        }

        // ── primitives ──
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

        private static TMP_Text MakeText(Transform parent, string name, string text, float size, FontStyles style, Color color)
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
            var existing = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
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
