using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Modal editor for creating a new journal entry. Multiline text input,
    /// optional voice recording, optional codex lock. Saves on tap of ✓ SAVE.
    /// </summary>
    public static class JournalEntryEditor
    {
        // Mystic adventure palette
        private static readonly Color GOLD       = new Color(0.95f, 0.78f, 0.20f);
        private static readonly Color CREAM      = new Color(1f, 0.97f, 0.85f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.05f, 0.18f);
        private static readonly Color CARD_BG    = new Color(0.10f, 0.05f, 0.18f, 1f);    // deep arcane indigo
        private static readonly Color FRAME_BG   = new Color(0.25f, 0.10f, 0.05f, 1f);    // dark leather
        private static readonly Color PARCHMENT  = new Color(0.96f, 0.88f, 0.70f, 1f);    // for the input field
        private static readonly Color INPUT_BG   = PARCHMENT;

        private static GameObject _root;
        private static TMP_InputField _input;
        private static bool _lockRequested;
        private static string _voicePath = "";
        private static TMP_Text _recBtnLbl;
        private static Image _recBtnImg;
        private static AudioSource _playback;
        private static DateTime _selectedDate = DateTime.Now;
        private static TMP_Text  _datePillLbl;        // shows the picked date on the pill
        private static GameObject _calendarRoot;       // inline calendar overlay

        public static void Show()
        {
            if (_root != null) Hide();
            _lockRequested = false;
            _voicePath = "";
            _selectedDate = DateTime.Now;

            _root = new GameObject("JournalEntryEditor",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 14800;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim
            var dim = MakeImage(_root.transform, "Dim", new Color(0, 0, 0, 0.92f));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Hide);

            // Outer leather/wood frame (rendered behind)
            var frame = MakeRounded(_root.transform, "Frame", FRAME_BG, 30);
            var frt = frame.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0.5f, 0.5f); frt.anchorMax = new Vector2(0.5f, 0.5f);
            frt.pivot = new Vector2(0.5f, 0.5f);
            frt.sizeDelta = new Vector2(920, 1220);

            // Card — mystic deep indigo
            var card = MakeRounded(_root.transform, "Card", CARD_BG, 28);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(900, 1200);

            // Mystic glyph corners (✦ at 4 corners) — purely decorative
            string[] corners = { "✦", "✦", "✦", "✦" };
            Vector2[] cornerPos = {
                new Vector2(  20, -20), new Vector2( -20, -20),
                new Vector2(  20,  20), new Vector2( -20,  20),
            };
            Vector2[] cornerAnch = {
                new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(1, 0),
            };
            for (int i = 0; i < 4; i++)
            {
                var glyph = MakeText(card.transform, $"Cn{i}", corners[i],
                    44, FontStyles.Bold, new Color(GOLD.r, GOLD.g, GOLD.b, 0.65f),
                    cornerAnch[i], cornerAnch[i], cornerPos[i], new Vector2(60, 60));
                glyph.alignment = TextAlignmentOptions.Center;
                glyph.outlineWidth = 0.30f;
                glyph.outlineColor = new Color(FRAME_BG.r, FRAME_BG.g, FRAME_BG.b, 1f);
            }

            // ── Mystic title with fantasy banner backing ──
            // Banner backdrop (deep wine/plum)
            var titleBanner = MakeImage(card.transform, "TitleBanner", new Color(0.40f, 0.10f, 0.30f, 1f));
            var tbRT = titleBanner.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0.5f, 1); tbRT.anchorMax = new Vector2(0.5f, 1);
            tbRT.pivot = new Vector2(0.5f, 1);
            tbRT.anchoredPosition = new Vector2(0, -28);
            tbRT.sizeDelta = new Vector2(740, 100);
            #if UNITY_EDITOR
            const string FLAG_PATH = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Label/Label_Flag_01_Bg.png";
            var fimp = UnityEditor.AssetImporter.GetAtPath(FLAG_PATH) as UnityEditor.TextureImporter;
            if (fimp != null && !Application.isPlaying)
            {
                bool changed = false;
                if (fimp.textureType != UnityEditor.TextureImporterType.Sprite)
                { fimp.textureType = UnityEditor.TextureImporterType.Sprite; changed = true; }
                if (!fimp.alphaIsTransparency) { fimp.alphaIsTransparency = true; changed = true; }
                var s = new UnityEditor.TextureImporterSettings();
                fimp.ReadTextureSettings(s);
                if (s.spriteBorder == Vector4.zero)
                { s.spriteBorder = new Vector4(60, 30, 60, 30); fimp.SetTextureSettings(s); changed = true; }
                if (changed) fimp.SaveAndReimport();
            }
            var flagSp = Sparq.Core.SpriteLoader.Load(FLAG_PATH);
            if (flagSp != null)
            {
                var bImg = titleBanner.GetComponent<Image>();
                bImg.sprite = flagSp;
                bImg.type = (flagSp.border == Vector4.zero) ? Image.Type.Simple : Image.Type.Sliced;
                bImg.color = new Color(0.40f, 0.10f, 0.30f, 1f); // wine tint
                bImg.raycastTarget = false;
            }
            #endif

            // Title text on the banner — bigger, golden, dramatic outline
            var titleTm = MakeText(titleBanner.transform, "T", "✦  INSCRIBE THY THOUGHTS  ✦",
                40, FontStyles.Bold, new Color(1f, 0.95f, 0.55f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            titleTm.alignment = TextAlignmentOptions.Center;
            titleTm.outlineWidth = 0.40f;
            titleTm.outlineColor = new Color(0.20f, 0.04f, 0.10f, 1f);
            titleTm.characterSpacing = 4f;

            // Side ornament glyphs flanking the banner — gold sparkle clusters
            for (int side = 0; side < 2; side++)
            {
                bool right = side == 1;
                var orn = MakeText(card.transform, $"Orn{side}", "✦",
                    32, FontStyles.Bold, new Color(GOLD.r, GOLD.g, GOLD.b, 0.85f),
                    new Vector2(right ? 1 : 0, 1), new Vector2(right ? 1 : 0, 1),
                    new Vector2(right ? -60 : 60, -64), new Vector2(40, 40));
                orn.alignment = TextAlignmentOptions.Center;
                orn.outlineWidth = 0.30f;
                orn.outlineColor = new Color(FRAME_BG.r, FRAME_BG.g, FRAME_BG.b);
            }

            // Decorative divider under title
            var divUnder = MakeImage(card.transform, "DivT", new Color(GOLD.r, GOLD.g, GOLD.b, 0.40f));
            var duRT = divUnder.GetComponent<RectTransform>();
            duRT.anchorMin = new Vector2(0.15f, 1); duRT.anchorMax = new Vector2(0.85f, 1);
            duRT.pivot = new Vector2(0.5f, 1);
            duRT.anchoredPosition = new Vector2(0, -110);
            duRT.sizeDelta = new Vector2(0, 2);
            divUnder.GetComponent<Image>().raycastTarget = false;

            // ── DATE row — gilded date pill that opens a calendar overlay ──
            // Pill background: rich plum w/ gold rim
            var datePill = MakeRounded(card.transform, "DatePill", new Color(0.30f, 0.10f, 0.30f, 1f), 18);
            var dpRT = datePill.GetComponent<RectTransform>();
            dpRT.anchorMin = new Vector2(0.5f, 1); dpRT.anchorMax = new Vector2(0.5f, 1);
            dpRT.pivot = new Vector2(0.5f, 1);
            dpRT.anchoredPosition = new Vector2(0, -150);
            dpRT.sizeDelta = new Vector2(680, 80);
            // Gold rim glow behind the pill
            var pillGlow = MakeRounded(card.transform, "PillGlow", new Color(GOLD.r, GOLD.g, GOLD.b, 0.35f), 22);
            var pgRT = pillGlow.GetComponent<RectTransform>();
            pgRT.anchorMin = new Vector2(0.5f, 1); pgRT.anchorMax = new Vector2(0.5f, 1);
            pgRT.pivot = new Vector2(0.5f, 1);
            pgRT.anchoredPosition = new Vector2(0, -146);
            pgRT.sizeDelta = new Vector2(692, 88);
            pgRT.SetSiblingIndex(datePill.transform.GetSiblingIndex());
            pillGlow.GetComponent<Image>().raycastTarget = false;

            // Calendar glyph on the left
            MakeText(datePill.transform, "Cal", "🗓",
                36, FontStyles.Normal, new Color(GOLD.r, GOLD.g, GOLD.b, 0.95f),
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(46, 46))
                .alignment = TextAlignmentOptions.Center;

            // Date label (the actual displayed date)
            _datePillLbl = MakeText(datePill.transform, "Lbl", _selectedDate.ToString("dddd, MMMM d, yyyy"),
                28, FontStyles.Bold, new Color(1f, 0.95f, 0.55f),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var dpLR = _datePillLbl.GetComponent<RectTransform>();
            dpLR.offsetMin = new Vector2(80, 0); dpLR.offsetMax = new Vector2(-110, 0);
            _datePillLbl.alignment = TextAlignmentOptions.MidlineLeft;
            _datePillLbl.outlineWidth = 0.30f;
            _datePillLbl.outlineColor = new Color(0.10f, 0.04f, 0.10f);
            _datePillLbl.characterSpacing = 2f;

            // Right edit chevron
            MakeText(datePill.transform, "Edit", "edit",
                20, FontStyles.Bold, new Color(GOLD.r, GOLD.g, GOLD.b, 0.85f),
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(90, 30))
                .alignment = TextAlignmentOptions.Right;

            // Make the pill clickable
            var pillBtn = datePill.AddComponent<Button>();
            pillBtn.transition = Selectable.Transition.ColorTint;
            var cb = pillBtn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            pillBtn.colors = cb;
            pillBtn.targetGraphic = datePill.GetComponent<Image>();
            pillBtn.onClick.AddListener(ShowCalendar);

            // Body label — italic flavor text (shifted down to make room for date row)
            MakeText(card.transform, "BL", "What stirs in thy mind today?",
                24, FontStyles.Italic, new Color(GOLD.r, GOLD.g, GOLD.b, 0.85f),
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -250), new Vector2(0, 36))
                .alignment = TextAlignmentOptions.Left;

            // Multiline text input
            var inputGO = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGO.transform.SetParent(card.transform, false);
            var iRT = inputGO.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0, 1); iRT.anchorMax = new Vector2(1, 1);
            iRT.pivot = new Vector2(0.5f, 1f);
            iRT.anchoredPosition = new Vector2(0, -300);
            iRT.sizeDelta = new Vector2(-80, 380);
            inputGO.GetComponent<Image>().color = INPUT_BG;
            _input = inputGO.GetComponent<TMP_InputField>();
            _input.lineType = TMP_InputField.LineType.MultiLineNewline;

            var iTxtGO = new GameObject("Text", typeof(RectTransform));
            iTxtGO.transform.SetParent(inputGO.transform, false);
            var itr = iTxtGO.GetComponent<RectTransform>();
            itr.anchorMin = Vector2.zero; itr.anchorMax = Vector2.one;
            itr.offsetMin = new Vector2(20, 16); itr.offsetMax = new Vector2(-20, -16);
            var iTm = iTxtGO.AddComponent<TextMeshProUGUI>();
            iTm.fontSize = 28; iTm.color = new Color(0.18f, 0.10f, 0.04f); // ink on parchment
            iTm.alignment = TextAlignmentOptions.TopLeft;
            iTm.textWrappingMode = TextWrappingModes.Normal;
            iTm.raycastTarget = false;
            _input.textComponent = iTm; _input.text = "";
            _input.targetGraphic = inputGO.GetComponent<Image>();
            _input.shouldHideMobileInput = false;
            _input.contentType = TMP_InputField.ContentType.Standard;
            _input.interactable = true;
            _input.readOnly = false;
            _input.richText = false;
            // (no auto-focus — user taps the field they want; otherwise body input
            //  steals focus and the date field can feel "uneditable" on first try)

            var phGO = new GameObject("PH", typeof(RectTransform));
            phGO.transform.SetParent(inputGO.transform, false);
            var phr = phGO.GetComponent<RectTransform>();
            phr.anchorMin = Vector2.zero; phr.anchorMax = Vector2.one;
            phr.offsetMin = new Vector2(20, 16); phr.offsetMax = new Vector2(-20, -16);
            var phTm = phGO.AddComponent<TextMeshProUGUI>();
            phTm.text = "<i>Speak or write — let thy thoughts flow upon this page...</i>";
            phTm.richText = true;
            phTm.fontSize = 26;
            phTm.color = new Color(0.42f, 0.30f, 0.18f, 0.65f);  // faded ink
            phTm.fontStyle = FontStyles.Italic;
            phTm.alignment = TextAlignmentOptions.TopLeft;
            phTm.textWrappingMode = TextWrappingModes.Normal;
            phTm.raycastTarget = false;
            _input.placeholder = phTm;

            // ── 4 unified action buttons — same size, same font ──
            // Stacked tightly at the bottom of the card so the top row doesn't
            // float in space.  Bottom row anchors to card-bottom; top row sits
            // just above it.
            const float BTN_W = 340f, BTN_H = 100f;
            const float BTN_FONT = 24f;
            const float ROW_GAP = 24f;
            // Bottom-row baseline (anchored from bottom of card)
            const float BOTTOM_Y = 90f;
            // Top row sits one button-height + gap above the bottom row,
            // measured from the card top.  Card height = 1200.
            //   bottom row top edge from card-top  = 1200 - (BOTTOM_Y + BTN_H)
            //   top row Y from card-top            = bottom-row-top - ROW_GAP - BTN_H ... but expressed
            //   in MakeBtn coords (anchor 0,1, pivot 0,1) so it's just negative.
            // Top row's TOP edge (in card-top-down coords) sits one BTN_H above the
            // bottom-row's TOP edge, plus the row gap.
            //   bottom-row top edge (from card-top) = CARD_H - (BOTTOM_Y + BTN_H) = 1010
            //   top-row top edge = 1010 - ROW_GAP - BTN_H = 886
            // Anchor (0,1) pivot (0,1) means anchoredPosition.y = -(top-edge).
            float topRowY = -(1200f - (BOTTOM_Y + BTN_H) - ROW_GAP - BTN_H);

            // ── Mic record button (top-left) ──
            var recBtn = MakeBtn(card.transform, "RecBtn", "RECORD",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(50, topRowY), new Vector2(BTN_W, BTN_H),
                new Color(0.95f, 0.45f, 0.50f), Color.white, BTN_FONT);
            ApplyFantasyBtn(recBtn, "Red");
            AddFunIcon(recBtn, FunIcon.Sound, false);
            _recBtnImg = recBtn.GetComponent<Image>();
            _recBtnLbl = recBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (_recBtnLbl != null) { _recBtnLbl.color = Color.white; _recBtnLbl.outlineWidth = 0.30f; _recBtnLbl.outlineColor = new Color(0.30f, 0.05f, 0.10f); }
            recBtn.onClick.AddListener(ToggleRecord);

            // ── Codex lock toggle (top-right) ──
            var lockBtn = MakeBtn(card.transform, "LockBtn", "LOCK",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-50, topRowY), new Vector2(BTN_W, BTN_H),
                new Color(0.45f, 0.30f, 0.65f), Color.white, BTN_FONT);
            ApplyFantasyBtn(lockBtn, "Purple");
            AddFunIcon(lockBtn, FunIcon.Lock, false);
            var lockLbl = lockBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (lockLbl != null) { lockLbl.color = Color.white; lockLbl.outlineWidth = 0.30f; lockLbl.outlineColor = new Color(0.10f, 0.05f, 0.20f); }
            lockBtn.onClick.AddListener(() => ToggleLock(lockBtn, lockLbl));

            // ── SAVE (bottom-left) ──
            var save = MakeBtn(card.transform, "Save", "SAVE",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-185, 90), new Vector2(BTN_W, BTN_H),
                new Color(0.30f, 0.80f, 0.42f), Color.white, BTN_FONT);
            ApplyFantasyBtn(save, "Green");
            AddFunIcon(save, FunIcon.Scroll, true, 84f);
            var sLbl = save.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (sLbl != null) { sLbl.color = DEEP_NAVY; sLbl.outlineWidth = 0.30f; sLbl.outlineColor = new Color(0.85f, 1f, 0.85f); }
            save.onClick.AddListener(SaveEntry);

            // ── CANCEL (bottom-right) ──
            var cancel = MakeBtn(card.transform, "Cancel", "CANCEL",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(185, 90), new Vector2(BTN_W, BTN_H),
                new Color(0.92f, 0.35f, 0.42f), Color.white, BTN_FONT);
            ApplyFantasyBtn(cancel, "Brown");
            AddFunIcon(cancel, FunIcon.Back, false);
            var cLbl = cancel.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (cLbl != null) { cLbl.color = Color.white; cLbl.outlineWidth = 0.30f; cLbl.outlineColor = new Color(0.10f, 0.05f, 0.20f); }
            cancel.onClick.AddListener(Hide);
        }

        // ── Fun icons enum + loader ──
        private enum FunIcon { Sound, Lock, Check, Back, Scroll }
        private static void AddFunIcon(Button btn, FunIcon kind, bool fromFantasyPack, float iconSize = 60f)
        {
            #if UNITY_EDITOR
            string path;
            switch (kind)
            {
                case FunIcon.Scroll: path = "Assets/FantasyIconPack/256/Scroll.png"; break;
                case FunIcon.Sound:  path = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/256/PictoIcon_Sound.Png"; break;
                case FunIcon.Lock:   path = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/256/PictoIcon_Lock.Png"; break;
                case FunIcon.Check:  path = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/256/PictoIcon_Check.Png"; break;
                case FunIcon.Back:   path = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/256/PictoIcon_Back.Png"; break;
                default: return;
            }
            var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite && !Application.isPlaying)
            {
                imp.textureType = UnityEditor.TextureImporterType.Sprite;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            var sp = Sparq.Core.SpriteLoader.Load(path);
            if (sp == null) return;

            // Strip any stale HorizontalLayoutGroup from previous attempts.
            var oldHLG = btn.GetComponent<HorizontalLayoutGroup>();
            if (oldHLG != null) UnityEngine.Object.DestroyImmediate(oldHLG);

            // Grab text + style from the existing label, then DESTROY it and
            // rebuild from scratch — mutating its anchors in place was unreliable.
            var oldLbl = btn.transform.Find("Lbl");
            string lblText = "";
            float lblFontSize = 24f;
            Color lblColor = Color.white;
            float lblOutlineW = 0f;
            Color lblOutlineC = Color.black;
            if (oldLbl != null)
            {
                var oldTm = oldLbl.GetComponent<TMP_Text>();
                if (oldTm != null)
                {
                    lblText = oldTm.text;
                    lblFontSize = oldTm.fontSize;
                    lblColor = oldTm.color;
                    lblOutlineW = oldTm.outlineWidth;
                    lblOutlineC = oldTm.outlineColor;
                }
                UnityEngine.Object.DestroyImmediate(oldLbl.gameObject);
            }

            // Measure text width with a temp TMP (since we destroyed the original).
            float textW = Mathf.Max(40f, lblText.Length * lblFontSize * 0.55f);

            const float GAP = 10f;
            float groupW = iconSize + GAP + textW;
            float startX = -groupW * 0.5f;

            // Icon — anchored to button center, left edge of the group
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(btn.transform, false);
            var rt = icon.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0, 0.5f);
            rt.sizeDelta = new Vector2(iconSize, iconSize);
            rt.anchoredPosition = new Vector2(startX, 0);
            var img = icon.GetComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = true;
            img.color = fromFantasyPack ? Color.white : new Color(1f, 0.95f, 0.7f);
            img.raycastTarget = false;

            // Fresh label, anchored in the middle, sized exactly to its text
            var lblGO = new GameObject("Lbl", typeof(RectTransform));
            lblGO.transform.SetParent(btn.transform, false);
            var lrt = lblGO.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.5f, 0.5f);
            lrt.anchorMax = new Vector2(0.5f, 0.5f);
            lrt.pivot     = new Vector2(0, 0.5f);
            lrt.sizeDelta = new Vector2(textW + 8f, lblFontSize + 12f);
            lrt.anchoredPosition = new Vector2(startX + iconSize + GAP, 0);
            var tm = lblGO.AddComponent<TextMeshProUGUI>();
            tm.text = lblText;
            tm.fontSize = lblFontSize;
            tm.fontStyle = FontStyles.Bold;
            tm.color = lblColor;
            tm.alignment = TextAlignmentOptions.Left;
            tm.textWrappingMode = TextWrappingModes.NoWrap;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            tm.outlineWidth = lblOutlineW;
            tm.outlineColor = lblOutlineC;

            // Re-measure with real TMP and re-snap positions so it's pixel-tight.
            // ForceMeshUpdate so font metrics are populated this frame.
            tm.ForceMeshUpdate();
            float realW = tm.GetRenderedValues(false).x;
            if (realW <= 1f) realW = tm.GetPreferredValues(lblText).x;
            if (realW <= 1f) realW = textW;
            float realGroupW = iconSize + GAP + realW;
            float realStartX = -realGroupW * 0.5f;
            rt.anchoredPosition = new Vector2(realStartX, 0);
            lrt.anchoredPosition = new Vector2(realStartX + iconSize + GAP, 0);
            lrt.sizeDelta = new Vector2(realW + 8f, lblFontSize + 12f);
            #endif
        }

        // ─── Mystic calendar overlay ───
        private static int _calMonth, _calYear;

        private static void ShowCalendar()
        {
            HideCalendar();
            _calMonth = _selectedDate.Month;
            _calYear  = _selectedDate.Year;

            _calendarRoot = new GameObject("CalendarOverlay",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            _calendarRoot.transform.SetParent(_root.transform, false);
            var crt = _calendarRoot.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var cc = _calendarRoot.GetComponent<Canvas>();
            cc.overrideSorting = true;
            cc.sortingOrder = 14900;

            // Backdrop dim — taps it to dismiss
            var dim = MakeImage(_calendarRoot.transform, "Dim", new Color(0, 0, 0, 0.65f));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(HideCalendar);

            // Frame (gold rim glow)
            var glow = MakeRounded(_calendarRoot.transform, "Glow",
                new Color(GOLD.r, GOLD.g, GOLD.b, 0.55f), 28);
            var ggrt = glow.GetComponent<RectTransform>();
            ggrt.anchorMin = ggrt.anchorMax = new Vector2(0.5f, 0.5f);
            ggrt.pivot = new Vector2(0.5f, 0.5f);
            ggrt.sizeDelta = new Vector2(820, 920);
            glow.GetComponent<Image>().raycastTarget = false;

            // Card — deep arcane indigo (matches editor)
            var calCard = MakeRounded(_calendarRoot.transform, "Card", CARD_BG, 26);
            var cardRT = calCard.GetComponent<RectTransform>();
            cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(800, 900);

            // Header banner (wine plum)
            var hdr = MakeRounded(calCard.transform, "Hdr", new Color(0.40f, 0.10f, 0.30f, 1f), 22);
            var hRT = hdr.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0.5f, 1); hRT.anchorMax = new Vector2(0.5f, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.anchoredPosition = new Vector2(0, -20);
            hRT.sizeDelta = new Vector2(740, 100);
            hdr.GetComponent<Image>().raycastTarget = false;

            // ◀ prev / month-year title / next ▶
            var prevBtn = MakeBtn(hdr.transform, "Prev", "◀",
                new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                new Vector2(40, 0), new Vector2(60, 60),
                new Color(GOLD.r, GOLD.g, GOLD.b, 0.30f), Color.white, 32f);
            var prevImg = prevBtn.GetComponent<Image>();
            prevImg.sprite = LoadRoundedSprite(14); prevImg.type = Image.Type.Sliced;
            prevBtn.onClick.AddListener(() => { ShiftMonth(-1); });

            var nextBtn = MakeBtn(hdr.transform, "Next", "▶",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f),
                new Vector2(-40, 0), new Vector2(60, 60),
                new Color(GOLD.r, GOLD.g, GOLD.b, 0.30f), Color.white, 32f);
            var nextImg = nextBtn.GetComponent<Image>();
            nextImg.sprite = LoadRoundedSprite(14); nextImg.type = Image.Type.Sliced;
            nextBtn.onClick.AddListener(() => { ShiftMonth(1); });

            // Title (month year) — placed in header
            var titleGO = new GameObject("MY", typeof(RectTransform));
            titleGO.transform.SetParent(hdr.transform, false);
            var trt2 = titleGO.GetComponent<RectTransform>();
            trt2.anchorMin = new Vector2(0, 0); trt2.anchorMax = new Vector2(1, 1);
            trt2.offsetMin = new Vector2(110, 0); trt2.offsetMax = new Vector2(-110, 0);
            var tTm = titleGO.AddComponent<TextMeshProUGUI>();
            tTm.font = TMP_Settings.defaultFontAsset;
            tTm.fontSize = 36; tTm.fontStyle = FontStyles.Bold;
            tTm.color = new Color(1f, 0.95f, 0.55f);
            tTm.alignment = TextAlignmentOptions.Center;
            tTm.raycastTarget = false; tTm.outlineWidth = 0.30f;
            tTm.outlineColor = new Color(0.20f, 0.04f, 0.10f);
            tTm.name = "MYTitle";
            tTm.text = $"✦  {DateTimeFormatInfo.CurrentInfo.GetMonthName(_calMonth)}  {_calYear}  ✦";

            // Day-of-week header row
            string[] dows = { "S", "M", "T", "W", "T", "F", "S" };
            for (int i = 0; i < 7; i++)
            {
                var dt = MakeText(calCard.transform, $"DOW{i}", dows[i],
                    24, FontStyles.Bold, new Color(GOLD.r, GOLD.g, GOLD.b, 0.85f),
                    new Vector2(0, 1), new Vector2(0, 1),
                    new Vector2(40 + 102 * i + 6, -160), new Vector2(80, 40));
                dt.alignment = TextAlignmentOptions.Center;
            }

            // Grid container (rebuildable)
            var gridGO = new GameObject("Grid", typeof(RectTransform));
            gridGO.transform.SetParent(calCard.transform, false);
            var grt = gridGO.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0, 1); grt.anchorMax = new Vector2(1, 1);
            grt.pivot = new Vector2(0.5f, 1);
            grt.anchoredPosition = new Vector2(0, -200);
            grt.sizeDelta = new Vector2(0, 580);
            BuildCalendarGrid(gridGO.transform);

            // Bottom buttons: TODAY (left) + DONE (right)
            var todayB = MakeBtn(calCard.transform, "TodayB", "✦ TODAY",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(60, 40), new Vector2(280, 80),
                new Color(0.95f, 0.78f, 0.20f), new Color(0.20f, 0.05f, 0.10f), 26f);
            var tBI = todayB.GetComponent<Image>();
            tBI.sprite = LoadRoundedSprite(16); tBI.type = Image.Type.Sliced;
            todayB.onClick.AddListener(() => {
                _selectedDate = DateTime.Now.Date;
                _calMonth = _selectedDate.Month; _calYear = _selectedDate.Year;
                RefreshCalendar();
                UpdatePillLabel();
            });

            var doneB = MakeBtn(calCard.transform, "DoneB", "✓ DONE",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-60, 40), new Vector2(280, 80),
                new Color(0.30f, 0.80f, 0.42f), Color.white, 26f);
            var dBI = doneB.GetComponent<Image>();
            dBI.sprite = LoadRoundedSprite(16); dBI.type = Image.Type.Sliced;
            doneB.onClick.AddListener(() => { UpdatePillLabel(); HideCalendar(); });
        }

        private static void ShiftMonth(int delta)
        {
            _calMonth += delta;
            if (_calMonth < 1)  { _calMonth = 12; _calYear--; }
            if (_calMonth > 12) { _calMonth = 1;  _calYear++; }
            RefreshCalendar();
        }

        private static void RefreshCalendar()
        {
            if (_calendarRoot == null) return;
            var titleTm = _calendarRoot.transform.Find("Card/Hdr/MY")?.GetComponent<TMP_Text>();
            if (titleTm != null)
                titleTm.text = $"✦  {DateTimeFormatInfo.CurrentInfo.GetMonthName(_calMonth)}  {_calYear}  ✦";
            var grid = _calendarRoot.transform.Find("Card/Grid");
            if (grid != null) BuildCalendarGrid(grid);
        }

        private static void BuildCalendarGrid(Transform grid)
        {
            // Clear existing day cells
            for (int i = grid.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(grid.GetChild(i).gameObject);

            var first = new DateTime(_calYear, _calMonth, 1);
            int leadBlanks = (int)first.DayOfWeek;            // Sunday=0
            int days = DateTime.DaysInMonth(_calYear, _calMonth);
            DateTime today = DateTime.Now.Date;

            const float CELL = 92f;
            const float GAP  = 10f;
            const float LEFT_PAD = 40f;

            for (int d = 1; d <= days; d++)
            {
                int slot = leadBlanks + d - 1;
                int row = slot / 7;
                int col = slot % 7;
                float x = LEFT_PAD + col * (CELL + GAP);
                float y = -row * (CELL + GAP);

                var cell = new GameObject($"D{d}", typeof(RectTransform), typeof(Image), typeof(Button));
                cell.transform.SetParent(grid, false);
                var crt = cell.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(0, 1);
                crt.pivot = new Vector2(0, 1);
                crt.anchoredPosition = new Vector2(x, y);
                crt.sizeDelta = new Vector2(CELL, CELL);

                var img = cell.GetComponent<Image>();
                img.sprite = LoadRoundedSprite(14);
                img.type = Image.Type.Sliced;

                var thisDate = new DateTime(_calYear, _calMonth, d);
                bool isSelected = thisDate.Date == _selectedDate.Date;
                bool isToday    = thisDate.Date == today;
                if (isSelected)
                    img.color = new Color(0.95f, 0.78f, 0.20f, 1f);     // gold
                else if (isToday)
                    img.color = new Color(0.40f, 0.10f, 0.30f, 1f);     // wine
                else
                    img.color = new Color(0.18f, 0.10f, 0.28f, 1f);     // deep indigo

                // Day number
                var t = new GameObject("N", typeof(RectTransform));
                t.transform.SetParent(cell.transform, false);
                var trt3 = t.GetComponent<RectTransform>();
                trt3.anchorMin = Vector2.zero; trt3.anchorMax = Vector2.one;
                trt3.offsetMin = Vector2.zero; trt3.offsetMax = Vector2.zero;
                var tm = t.AddComponent<TextMeshProUGUI>();
                tm.font = TMP_Settings.defaultFontAsset;
                tm.text = d.ToString();
                tm.fontSize = 30; tm.fontStyle = FontStyles.Bold;
                tm.color = isSelected ? new Color(0.18f, 0.05f, 0.10f) : new Color(1f, 0.95f, 0.55f);
                tm.alignment = TextAlignmentOptions.Center;
                tm.raycastTarget = false;

                // Today dot
                if (isToday && !isSelected)
                {
                    var dot = MakeRounded(cell.transform, "Dot", new Color(0.95f, 0.78f, 0.20f), 6);
                    var drt2 = dot.GetComponent<RectTransform>();
                    drt2.anchorMin = new Vector2(0.5f, 0); drt2.anchorMax = new Vector2(0.5f, 0);
                    drt2.pivot = new Vector2(0.5f, 0);
                    drt2.anchoredPosition = new Vector2(0, 8);
                    drt2.sizeDelta = new Vector2(10, 10);
                    dot.GetComponent<Image>().raycastTarget = false;
                }

                int dayCaptured = d;
                var btn = cell.GetComponent<Button>();
                btn.transition = Selectable.Transition.ColorTint;
                var cb2 = btn.colors;
                cb2.normalColor = Color.white;
                cb2.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
                cb2.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
                btn.colors = cb2;
                btn.onClick.AddListener(() => {
                    _selectedDate = new DateTime(_calYear, _calMonth, dayCaptured);
                    RefreshCalendar();
                    UpdatePillLabel();
                });
            }
        }

        private static void UpdatePillLabel()
        {
            if (_datePillLbl != null)
                _datePillLbl.text = _selectedDate.ToString("dddd, MMMM d, yyyy");
        }

        private static void HideCalendar()
        {
            if (_calendarRoot != null) UnityEngine.Object.Destroy(_calendarRoot);
            _calendarRoot = null;
        }

        public static void Hide()
        {
            // Stop recording if active
            if (Sparq.Systems.JournalService.IsRecording)
            {
                _voicePath = Sparq.Systems.JournalService.StopRecordingAndSave();
            }
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
        }

        private static void ToggleRecord()
        {
            if (Sparq.Systems.JournalService.IsRecording)
            {
                _voicePath = Sparq.Systems.JournalService.StopRecordingAndSave();
                if (_recBtnLbl != null) _recBtnLbl.text = string.IsNullOrEmpty(_voicePath) ? "🎙  RECORD" : "🎙  RE-RECORD";
                if (_recBtnImg != null) _recBtnImg.color = string.IsNullOrEmpty(_voicePath)
                    ? new Color(0.95f, 0.45f, 0.50f)
                    : new Color(0.30f, 0.80f, 0.42f);
            }
            else
            {
                if (Sparq.Systems.JournalService.StartRecording())
                {
                    if (_recBtnLbl != null) _recBtnLbl.text = "■  STOP";
                    if (_recBtnImg != null) _recBtnImg.color = new Color(1f, 0.55f, 0.30f);
                }
                else
                {
                    if (_recBtnLbl != null) _recBtnLbl.text = "(no mic)";
                }
            }
        }

        private static void ToggleLock(Button lockBtn, TMP_Text lockLbl)
        {
            _lockRequested = !_lockRequested;
            if (_lockRequested)
            {
                lockBtn.GetComponent<Image>().color = GOLD;
                if (lockLbl != null) { lockLbl.text = "🔒  LOCKED"; lockLbl.color = DEEP_NAVY; lockLbl.outlineColor = new Color(1f, 0.95f, 0.7f); }
                // First time? Run codex setup
                if (!Sparq.Systems.JournalCodex.IsSet) Sparq.UI.JournalCodexLock.ShowSetup();
            }
            else
            {
                lockBtn.GetComponent<Image>().color = new Color(0.45f, 0.30f, 0.65f);
                if (lockLbl != null) { lockLbl.text = "🔒  LOCK WITH CODEX"; lockLbl.color = Color.white; lockLbl.outlineColor = new Color(0.10f, 0.05f, 0.20f); }
            }
        }

        private static void SaveEntry()
        {
            // Stop active recording first
            if (Sparq.Systems.JournalService.IsRecording)
                _voicePath = Sparq.Systems.JournalService.StopRecordingAndSave();
            string text = _input != null ? (_input.text ?? "").Trim() : "";
            // Allow empty text only if there's a voice clip
            if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(_voicePath)) { Hide(); return; }

            // Use the calendar-picked date; pin time-of-day to "now" so entries
            // on the same date still sort by save order.
            var combined = _selectedDate.Date + DateTime.Now.TimeOfDay;
            long unixOverride = new DateTimeOffset(combined, TimeZoneInfo.Local.GetUtcOffset(combined))
                .ToUnixTimeSeconds();
            Sparq.Systems.JournalService.Add(text, "", _lockRequested, _voicePath, unixOverride);
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.QuestComplete); } catch {}
            Hide();
        }

        // ─── auto-focus + runner ───
        private static MonoBehaviour _runner;
        private static void EnsureRunner()
        {
            if (_runner != null && _runner.gameObject != null) return;
            var go = GameObject.Find("JournalEditorRunner");
            if (go == null) { go = new GameObject("JournalEditorRunner"); UnityEngine.Object.DontDestroyOnLoad(go); }
            _runner = go.AddComponent<RunnerStub>();
        }
        private class RunnerStub : MonoBehaviour {}

        private static IEnumerator AutoFocusInput()
        {
            yield return null; // wait one frame so EventSystem registers the panel
            yield return null;
            if (_input != null && _input.gameObject != null && _input.gameObject.activeInHierarchy)
            {
                if (UnityEngine.EventSystems.EventSystem.current != null)
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(_input.gameObject);
                _input.Select();
                _input.ActivateInputField();
            }
        }

        // Add a Layer Lab picto icon on the left side of a fantasy button
        private static void AddPictoIcon(Button btn, string iconName)
        {
            #if UNITY_EDITOR
            string path = $"Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/256/PictoIcon_{iconName}.Png";
            var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite && !Application.isPlaying)
            {
                imp.textureType = UnityEditor.TextureImporterType.Sprite;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            var sp = Sparq.Core.SpriteLoader.Load(path);
            if (sp == null) return;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(btn.transform, false);
            var rt = icon.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(14, 0);
            rt.sizeDelta = new Vector2(54, 54);
            var img = icon.GetComponent<Image>();
            img.sprite = sp;
            img.preserveAspect = true;
            img.color = new Color(1f, 0.95f, 0.7f);
            img.raycastTarget = false;

            // Shift label right to make room
            var lbl = btn.transform.Find("Lbl");
            if (lbl != null)
            {
                var lrt = lbl.GetComponent<RectTransform>();
                if (lrt != null)
                {
                    lrt.offsetMin = new Vector2(70, lrt.offsetMin.y);
                    lrt.offsetMax = new Vector2(-8, lrt.offsetMax.y);
                }
            }
            #endif
        }

        // Layer Lab fantasy button — convex brown stone with proper sprite
        private static void ApplyFantasyBtn(Button btn, string colorName)
        {
            #if UNITY_EDITOR
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            string path = $"Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Button/Button_Rectangle_01_Convex_{colorName}.Png";
            var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (imp != null && !Application.isPlaying)
            {
                bool changed = false;
                if (imp.textureType != UnityEditor.TextureImporterType.Sprite)
                { imp.textureType = UnityEditor.TextureImporterType.Sprite; changed = true; }
                if (!imp.alphaIsTransparency)
                { imp.alphaIsTransparency = true; changed = true; }
                var settings = new UnityEditor.TextureImporterSettings();
                imp.ReadTextureSettings(settings);
                if (settings.spriteBorder == Vector4.zero)
                { settings.spriteBorder = new Vector4(40, 40, 40, 40); imp.SetTextureSettings(settings); changed = true; }
                if (changed) imp.SaveAndReimport();
            }
            var sp = Sparq.Core.SpriteLoader.Load(path);
            if (sp != null)
            {
                img.sprite = sp;
                img.type = (sp.border == Vector4.zero) ? Image.Type.Simple : Image.Type.Sliced;
                img.color = Color.white;
            }
            #endif
        }

        // ─── helpers (matches RemindPanel pattern) ───
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
