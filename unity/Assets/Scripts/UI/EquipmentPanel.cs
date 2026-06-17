// EquipmentPanel.cs — polished weapons & armour screen modelled on
// Layer Lab's GUI Pro-FantasyHero 6_Equipment.png + 7_Equipment_ItemInfoPopup
// + 9_PopupFullScreen_Hero_LevelUp templates. The FORGE is built in as a
// "Level Up" button on each item — no separate forge panel needed.
//
// Layout:
//   • Title bar with "EQUIPMENT" + close X
//   • Hero portrait, 5 slot rings (Weapon / Helm / Chest / Boots / Trinket)
//   • Stats row (⚔ Atk / 🛡 Def / ❤ HP)
//   • Equip All (auto-equip best owned) + Level Up (upgrade selected) pills
//   • Slot filter tabs (All / per-slot)
//   • 4-column grid of owned items with rarity-tinted frames + level pips
//   • Tap a tile → item info popup with Equip + Level Up
//   • Successful upgrade → full-screen result popup with green stat deltas

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class EquipmentPanel
    {
        // ── Palette — light-grey card with darker accents for hierarchy ──
        private static readonly Color CARD_BG    = new Color(0.66f, 0.66f, 0.70f, 1f);  // soft light-medium grey
        private static readonly Color TITLE_BG   = new Color(0.32f, 0.32f, 0.40f, 1f);  // dark slate banner
        private static readonly Color SLOT_BG    = new Color(0.50f, 0.50f, 0.56f, 1f);  // mid-grey filled slot
        private static readonly Color SLOT_EMPTY = new Color(0.42f, 0.42f, 0.48f, 0.92f); // empty slot, slightly darker
        private static readonly Color STATS_BG   = new Color(0.42f, 0.42f, 0.50f, 1f);  // mid-dark grey stats strip
        private static readonly Color GOLD       = new Color(0.99f, 0.78f, 0.20f, 1f);
        private static readonly Color CREAM      = new Color(1.00f, 0.97f, 0.85f, 1f);
        private static readonly Color INK        = new Color(0.13f, 0.10f, 0.20f, 1f);
        private static readonly Color INK_SOFT   = new Color(0.55f, 0.55f, 0.70f, 1f);
        private static readonly Color BTN_EQUIP  = new Color(0.42f, 0.72f, 1.00f, 1f);
        private static readonly Color BTN_LEVEL  = new Color(0.99f, 0.78f, 0.20f, 1f);
        private static readonly Color BTN_GREEN  = new Color(0.40f, 0.85f, 0.55f, 1f);
        private static readonly Color TAB_OFF    = new Color(0.46f, 0.46f, 0.52f, 1f);  // unselected tab — mid-grey
        private static readonly Color TAB_ON     = new Color(0.99f, 0.78f, 0.20f, 1f);  // selected tab — gold accent
        private static readonly Color DELTA_PLUS = new Color(0.40f, 0.95f, 0.55f, 1f);

        // ── Sprites ─────────────────────────────────────────────────────
        private const string POPUP_BG = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Popup/Popup_Box_Bg.png";
        private const string FRAME_DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/";
        private const string ATK_ICON = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Equipment_Weapon_Sword02.png";
        private const string DEF_ICON = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Heart01_Red.png";
        private const string HP_ICON  = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Heart01_Red.png";

        private static readonly string[] FRAMES_BY_RARITY = {
            FRAME_DIR + "ItemFrame_Square_01_Gray.png",     // Common
            FRAME_DIR + "ItemFrame_Square_01_Blue.png",     // Rare
            FRAME_DIR + "ItemFrame_Square_01_Purple.png",   // Epic
            FRAME_DIR + "ItemFrame_Square_01_Yellow.png",   // Legendary
        };

        // ── Filter / selection state ────────────────────────────────────
        // Default to Weapon (slot filter only — the "All" tab was removed
        // because mixed-slot tiles can't meaningfully compare against the
        // currently-equipped item).
        private static Sparq.Systems.EquipmentService.Slot _filter
            = Sparq.Systems.EquipmentService.Slot.Weapon;
        private static string _selectedItemId;

        // ── Runtime refs ────────────────────────────────────────────────
        private static GameObject _root;
        private static Transform  _gridParent;
        private static TMP_Text   _atkLbl, _defLbl, _hpLbl;
        private static GameObject _popupRoot;
        private static GameObject _levelUpRoot;
        private static readonly Dictionary<Sparq.Systems.EquipmentService.Slot, GameObject> _slotRings
            = new Dictionary<Sparq.Systems.EquipmentService.Slot, GameObject>();

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC
        // ─────────────────────────────────────────────────────────────────

        public static void Show()
        {
            if (_root != null) { Hide(); return; }
            EnsureEventSystem();

            _root = new GameObject("Sparq_EquipmentPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>(); Stretch(rrt);
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

            // Backdrop dim
            var dim = NewGO("Dim", _root.transform, typeof(Image), typeof(Button));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.82f);
            dim.GetComponent<Button>().onClick.AddListener(Hide);

            // Card
            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(1000, 1740);
            var cardImg = card.GetComponent<Image>();
            var bgSp = LoadSprite(POPUP_BG);
            if (bgSp != null) { cardImg.sprite = bgSp; cardImg.type = Image.Type.Sliced; }
            cardImg.color = CARD_BG;

            BuildTitleBar(card.transform);
            BuildHeroAndSlots(card.transform);
            BuildStatsRow(card.transform);
            BuildActionButtons(card.transform);
            BuildSlotFilterStrip(card.transform);
            BuildGrid(card.transform);

            RebuildAll();
            Debug.Log("[EquipmentPanel] Opened.");
        }

        public static void Hide()
        {
            HidePopup();
            HideLevelUpFx();
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
            _gridParent = null;
            _atkLbl = _defLbl = _hpLbl = null;
            _slotRings.Clear();
        }

        // ─────────────────────────────────────────────────────────────────
        // SHELL BUILDERS
        // ─────────────────────────────────────────────────────────────────

        private static void BuildTitleBar(Transform card)
        {
            var bar = NewGO("TitleBar", card, typeof(Image));
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -18);
            rt.sizeDelta = new Vector2(-40, 140);
            bar.GetComponent<Image>().color = TITLE_BG;

            var title = MakeText(bar.transform, "Title", "EQUIPMENT", 64, FontStyles.Bold, CREAM);
            Stretch(title.rectTransform); title.alignment = TextAlignmentOptions.Center;
            try { title.outlineWidth = 0.25f; title.outlineColor = new Color(0.05f, 0.03f, 0.10f); } catch {}

            var close = NewGO("Close", bar.transform, typeof(Image), typeof(Button));
            var xrt = close.GetComponent<RectTransform>();
            xrt.anchorMin = new Vector2(1, 0.5f); xrt.anchorMax = new Vector2(1, 0.5f);
            xrt.pivot = new Vector2(1, 0.5f);
            xrt.anchoredPosition = new Vector2(-20, 0);
            xrt.sizeDelta = new Vector2(90, 90);
            close.GetComponent<Image>().color = new Color(0.82f, 0.26f, 0.26f, 1f);
            var xl = MakeText(close.transform, "X", "X", 50, FontStyles.Bold, Color.white);
            Stretch(xl.rectTransform); xl.alignment = TextAlignmentOptions.Center;
            close.GetComponent<Button>().onClick.AddListener(Hide);
        }

        private static void BuildHeroAndSlots(Transform card)
        {
            // Hero portrait — bigger so the slot rings have room to read.
            var hero = NewGO("Hero", card, typeof(Image));
            var hRT = hero.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0.5f, 1); hRT.anchorMax = new Vector2(0.5f, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.anchoredPosition = new Vector2(0, -200);
            hRT.sizeDelta = new Vector2(340, 440);
            var heroBg = hero.GetComponent<Image>();
            heroBg.color = new Color(0.10f, 0.08f, 0.22f, 0.65f);

            // Actual hero figure layered on top of the dark backdrop. Uses
            // HeroPortrait.LoadIdle (alpha-cropped, weapon excluded so the
            // figure isn't shrunk by the spear/sword padding).
            try
            {
                var heroDef = Sparq.Systems.HeroClassResolver.Resolve();
                var loaded = Sparq.UI.HeroPortrait.LoadIdle(heroDef, excludeWeapon: true);
                if (loaded.ok && loaded.sprite != null)
                {
                    var figGO = NewGO("Figure", hero.transform, typeof(Image));
                    var fRT = figGO.GetComponent<RectTransform>();
                    fRT.anchorMin = new Vector2(0.5f, 0.5f); fRT.anchorMax = new Vector2(0.5f, 0.5f);
                    fRT.pivot = new Vector2(0.5f, 0.5f);
                    fRT.anchoredPosition = new Vector2(0, 24);
                    fRT.sizeDelta = new Vector2(460, 540);   // bumped per tester ask: hero too small on Weapons
                    var fImg = figGO.GetComponent<Image>();
                    fImg.sprite = loaded.sprite;
                    fImg.preserveAspect = true;
                    fImg.raycastTarget = false;
                }
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[EquipmentPanel] Hero portrait load failed: {ex.Message}"); }

            BuildHeroLevelLabel(hero.transform);

            // 5 slot rings — 220×220 each, arranged in two columns flanking
            // the hero (3 on the right, 2 on the left). Keeps the centre
            // clear for the portrait so the helm doesn't sit on the hero's
            // head.
            BuildSlotRing(card, Sparq.Systems.EquipmentService.Slot.Helm,    new Vector2( 340, -210));
            BuildSlotRing(card, Sparq.Systems.EquipmentService.Slot.Trinket, new Vector2( 340, -460));
            BuildSlotRing(card, Sparq.Systems.EquipmentService.Slot.Chest,   new Vector2( 340, -710));
            BuildSlotRing(card, Sparq.Systems.EquipmentService.Slot.Weapon,  new Vector2(-340, -340));
            BuildSlotRing(card, Sparq.Systems.EquipmentService.Slot.Boots,   new Vector2(-340, -610));
        }

        private static void BuildHeroLevelLabel(Transform hero)
        {
            int lvl = 1;
            try { lvl = Mathf.Max(1, Sparq.Core.SaveService.Data?.level ?? 1); } catch {}
            var lbl = MakeText(hero, "Lvl", $"LV.{lvl}", 38, FontStyles.Bold, CREAM);
            var lRT = lbl.rectTransform;
            lRT.anchorMin = new Vector2(0, 0); lRT.anchorMax = new Vector2(1, 0);
            lRT.pivot = new Vector2(0.5f, 0);
            lRT.anchoredPosition = new Vector2(0, 12);
            lRT.sizeDelta = new Vector2(0, 52);
            lbl.alignment = TextAlignmentOptions.Center;
        }

        private static void BuildSlotRing(Transform card,
            Sparq.Systems.EquipmentService.Slot slot, Vector2 pos)
        {
            var go = NewGO("Slot_" + slot, card, typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(220, 220);
            var bgImg = go.GetComponent<Image>();
            bgImg.color = SLOT_BG;
            bgImg.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = bgImg;
            btn.interactable = true;
            btn.onClick.AddListener(() => {
                var eq = Sparq.Systems.EquipmentService.EquippedIn(slot);
                if (eq != null) ShowItemPopup(eq);
                else { _filter = slot; _selectedItemId = null; RebuildAll(); }
            });
            _slotRings[slot] = go;
        }

        private static void BuildStatsRow(Transform card)
        {
            var row = NewGO("Stats", card, typeof(Image), typeof(HorizontalLayoutGroup));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -960);
            rt.sizeDelta = new Vector2(860, 130);
            row.GetComponent<Image>().color = STATS_BG;
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.padding = new RectOffset(24, 24, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            _atkLbl = BuildStatChip(row.transform, "Atk", ATK_ICON, new Color(0.95f, 0.55f, 0.30f, 1f));
            _defLbl = BuildStatChip(row.transform, "Def", DEF_ICON, new Color(0.55f, 0.85f, 1.00f, 1f));
            _hpLbl  = BuildStatChip(row.transform, "HP",  HP_ICON,  new Color(0.95f, 0.40f, 0.45f, 1f));
        }

        private static TMP_Text BuildStatChip(Transform parent, string label, string iconPath, Color iconTint)
        {
            var go = NewGO("Stat_" + label, parent, typeof(Image));
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            var ico = NewGO("Icon", go.transform, typeof(Image));
            var iRT = ico.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0, 0.5f); iRT.anchorMax = new Vector2(0, 0.5f);
            iRT.pivot = new Vector2(0, 0.5f);
            iRT.anchoredPosition = new Vector2(10, 0);
            iRT.sizeDelta = new Vector2(72, 72);
            var iImg = ico.GetComponent<Image>();
            var sp = LoadSprite(iconPath);
            if (sp != null) { iImg.sprite = sp; iImg.preserveAspect = true; }
            iImg.color = iconTint;
            iImg.raycastTarget = false;

            var lbl = MakeText(go.transform, "L", "0", 46, FontStyles.Bold, CREAM);
            var lRT = lbl.rectTransform;
            lRT.anchorMin = new Vector2(0, 0); lRT.anchorMax = new Vector2(1, 1);
            lRT.offsetMin = new Vector2(94, 0); lRT.offsetMax = new Vector2(-8, 0);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            return lbl;
        }

        private static void BuildActionButtons(Transform card)
        {
            // Pinned to the bottom of the card so the busy hero/slot area
            // up top isn't competing with two huge pills.
            var row = NewGO("ActionRow", card, typeof(HorizontalLayoutGroup));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 40);
            rt.sizeDelta = new Vector2(900, 140);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 28;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            BuildPill(row.transform, "Equip All", BTN_GREEN, () => {
                try { Sparq.Systems.EquipmentService.EquipBest(); } catch {}
                RebuildAll();
            });
            BuildPill(row.transform, "Level Up", BTN_LEVEL, () => {
                string id = _selectedItemId;
                if (string.IsNullOrEmpty(id))
                {
                    var eq = Sparq.Systems.EquipmentService.EquippedIn(_filter);
                    if (eq != null) id = eq.id;
                }
                if (string.IsNullOrEmpty(id))
                {
                    Debug.Log("[EquipmentPanel] Level Up tapped with no selection.");
                    return;
                }
                TryUpgrade(id);
            });
        }

        private static void BuildPill(Transform parent, string label, Color bg, System.Action onClick)
        {
            var go = NewGO("Pill_" + label, parent, typeof(Image), typeof(Button));
            var img = go.GetComponent<Image>();
            img.color = bg; img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            var lbl = MakeText(go.transform, "L", label, 44, FontStyles.Bold, INK);
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        private static void BuildSlotFilterStrip(Transform card)
        {
            var strip = NewGO("FilterStrip", card, typeof(HorizontalLayoutGroup));
            var rt = strip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -1110);
            rt.sizeDelta = new Vector2(940, 100);
            var hlg = strip.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            BuildFilterTab(strip.transform, "Weapon",  Sparq.Systems.EquipmentService.Slot.Weapon);
            BuildFilterTab(strip.transform, "Helm",    Sparq.Systems.EquipmentService.Slot.Helm);
            BuildFilterTab(strip.transform, "Chest",   Sparq.Systems.EquipmentService.Slot.Chest);
            BuildFilterTab(strip.transform, "Boots",   Sparq.Systems.EquipmentService.Slot.Boots);
            BuildFilterTab(strip.transform, "Trinket", Sparq.Systems.EquipmentService.Slot.Trinket);
        }

        private static void BuildFilterTab(Transform parent, string label,
            Sparq.Systems.EquipmentService.Slot slot)
        {
            var go = NewGO("Filter_" + label, parent, typeof(Image), typeof(Button));
            var img = go.GetComponent<Image>();
            bool active = (slot == _filter);
            img.color = active ? TAB_ON : TAB_OFF;
            img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            btn.onClick.AddListener(() => { _filter = slot; _selectedItemId = null; RebuildAll(); });
            var lbl = MakeText(go.transform, "L", label, 32, FontStyles.Bold,
                active ? INK : CREAM);
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
        }

        private static void BuildGrid(Transform card)
        {
            var scrollGO = NewGO("Scroll", card, typeof(Image), typeof(ScrollRect));
            var srRT = scrollGO.GetComponent<RectTransform>();
            srRT.anchorMin = new Vector2(0, 0); srRT.anchorMax = new Vector2(1, 1);
            // Top edge sits below the filter strip (~y=-1110 + height) and
            // bottom edge clears the Equip All / Level Up bar pinned at the
            // card's bottom (~200 from the card floor).
            srRT.offsetMin = new Vector2(30, 200); srRT.offsetMax = new Vector2(-30, -1230);
            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 30f;

            var vp = NewGO("VP", scrollGO.transform, typeof(Image), typeof(RectMask2D));
            var vpRT = vp.GetComponent<RectTransform>(); Stretch(vpRT);
            vp.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            var content = NewGO("Content", vp.transform,
                typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            var ctRT = content.GetComponent<RectTransform>();
            ctRT.anchorMin = new Vector2(0, 1); ctRT.anchorMax = new Vector2(1, 1);
            ctRT.pivot = new Vector2(0.5f, 1);
            ctRT.anchoredPosition = Vector2.zero;
            ctRT.sizeDelta = new Vector2(0, ctRT.sizeDelta.y);
            var grid = content.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(220, 220);
            grid.spacing = new Vector2(14, 14);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperLeft;
            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vpRT; sr.content = ctRT;
            _gridParent = content.transform;
        }

        // ─────────────────────────────────────────────────────────────────
        // REBUILDS
        // ─────────────────────────────────────────────────────────────────

        private static void RebuildAll()
        {
            RefreshSlotRings();
            RefreshStats();
            RebuildGrid();
        }

        private static void RefreshSlotRings()
        {
            foreach (var kv in _slotRings)
            {
                var slot = kv.Key;
                var go = kv.Value;
                if (go == null) continue;
                for (int i = go.transform.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.Destroy(go.transform.GetChild(i).gameObject);

                var eq = Sparq.Systems.EquipmentService.EquippedIn(slot);
                if (eq == null)
                {
                    go.GetComponent<Image>().color = SLOT_EMPTY;
                    var hint = MakeText(go.transform, "Hint", slot.ToString(), 30, FontStyles.Bold, CREAM);
                    Stretch(hint.rectTransform); hint.alignment = TextAlignmentOptions.Center;
                    continue;
                }

                int rarityIdx = Mathf.Clamp((int)eq.rarity, 0, FRAMES_BY_RARITY.Length - 1);
                var frame = LoadSprite(FRAMES_BY_RARITY[rarityIdx]);
                var bg = go.GetComponent<Image>();
                if (frame != null) { bg.sprite = frame; bg.type = Image.Type.Sliced; bg.color = Color.white; }
                else bg.color = Sparq.Systems.EquipmentService.RarityColor(eq.rarity);

                // Icon dominates the slot — bigger so the item art reads
                // at a glance. Sits in the upper portion; stats line below.
                BuildItemIconVisual(go.transform, eq, size: 170, anchoredPos: new Vector2(0, 30));

                // Per-piece stats line: <orange>Atk</> <blue>Def</> <red>HP</>
                // Only the non-zero values are shown so we don't waste pixels.
                var s = Sparq.Systems.EquipmentService.EffectiveStats(eq);
                var sb = new System.Text.StringBuilder();
                if (s.atk > 0) sb.Append("<color=#F08A4F><b>A</b> ").Append(s.atk).Append("</color>");
                if (s.def > 0) { if (sb.Length > 0) sb.Append("  "); sb.Append("<color=#7FCFFF><b>D</b> ").Append(s.def).Append("</color>"); }
                if (s.hp  > 0) { if (sb.Length > 0) sb.Append("  "); sb.Append("<color=#F0656E><b>H</b> ").Append(s.hp).Append("</color>"); }
                if (sb.Length > 0)
                {
                    var statLine = MakeText(go.transform, "Stats", sb.ToString(), 28, FontStyles.Bold, CREAM);
                    var slRT = statLine.rectTransform;
                    slRT.anchorMin = new Vector2(0, 0); slRT.anchorMax = new Vector2(1, 0);
                    slRT.pivot = new Vector2(0.5f, 0);
                    slRT.anchoredPosition = new Vector2(0, 8);
                    slRT.sizeDelta = new Vector2(-6, 56);
                    statLine.alignment = TextAlignmentOptions.Center;
                    statLine.richText = true;
                    try { statLine.outlineWidth = 0.15f; statLine.outlineColor = new Color(0, 0, 0); } catch {}
                }

                int lvl = Sparq.Systems.EquipmentService.LevelOf(eq.id);
                if (lvl > 0)
                {
                    var pip = NewGO("Lv", go.transform, typeof(Image));
                    var pRT = pip.GetComponent<RectTransform>();
                    pRT.anchorMin = new Vector2(1, 1); pRT.anchorMax = new Vector2(1, 1);
                    pRT.pivot = new Vector2(1, 1);
                    pRT.anchoredPosition = new Vector2(-6, -6);
                    pRT.sizeDelta = new Vector2(68, 34);
                    pip.GetComponent<Image>().color = GOLD;
                    pip.GetComponent<Image>().raycastTarget = false;
                    var l = MakeText(pip.transform, "L", "Lv." + lvl, 22, FontStyles.Bold, INK);
                    Stretch(l.rectTransform); l.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private static void RefreshStats()
        {
            var s = (atk: 0, def: 0, hp: 0);
            try { s = Sparq.Systems.EquipmentService.TotalStats(); } catch {}
            if (_atkLbl != null) _atkLbl.text = s.atk.ToString();
            if (_defLbl != null) _defLbl.text = s.def.ToString();
            if (_hpLbl  != null) _hpLbl.text  = s.hp.ToString();
        }

        private static void RebuildGrid()
        {
            if (_gridParent == null) return;
            for (int i = _gridParent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_gridParent.GetChild(i).gameObject);

            List<Sparq.Systems.EquipmentService.Item> items;
            try { items = Sparq.Systems.EquipmentService.OwnedItems(); }
            catch { items = new List<Sparq.Systems.EquipmentService.Item>(); }

            var equippedDict = new Dictionary<Sparq.Systems.EquipmentService.Slot, string>();
            try { equippedDict = Sparq.Systems.EquipmentService.Equipped(); } catch {}

            foreach (var it in items)
            {
                if (it == null) continue;
                if (it.slot != _filter) continue;
                bool equipped = equippedDict.TryGetValue(it.slot, out var eqId) && eqId == it.id;
                bool selected = it.id == _selectedItemId;
                BuildGridTile(it, equipped, selected);
            }
        }

        private static void BuildGridTile(Sparq.Systems.EquipmentService.Item it, bool equipped, bool selected)
        {
            var tile = NewGO("Tile_" + it.id, _gridParent, typeof(Image), typeof(Button));
            var img = tile.GetComponent<Image>();
            int rarityIdx = Mathf.Clamp((int)it.rarity, 0, FRAMES_BY_RARITY.Length - 1);
            var frame = LoadSprite(FRAMES_BY_RARITY[rarityIdx]);
            if (frame != null) { img.sprite = frame; img.type = Image.Type.Sliced; img.color = selected ? GOLD : Color.white; }
            else img.color = Sparq.Systems.EquipmentService.RarityColor(it.rarity);
            img.raycastTarget = true;
            var btn = tile.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            btn.onClick.AddListener(() => {
                _selectedItemId = it.id;
                ShowItemPopup(it);
            });

            // Real item icon (with letter-glyph fallback when the sprite
            // file for this entry isn't shipped).
            BuildItemIconVisual(tile.transform, it, size: 170, anchoredPos: new Vector2(0, 22));

            // Tile item name — bumped 22 → 26 with a stronger outline so names
            // like "Dragonscale" / "Hunter Bow" read cleanly on every rarity tile.
            var nm = MakeText(tile.transform, "N", it.name ?? it.id, 26, FontStyles.Bold, CREAM);
            var nRT = nm.rectTransform;
            nRT.anchorMin = new Vector2(0, 0); nRT.anchorMax = new Vector2(1, 0);
            nRT.pivot = new Vector2(0.5f, 0);
            nRT.anchoredPosition = new Vector2(0, 8);
            nRT.sizeDelta = new Vector2(-12, 50);
            nm.alignment = TextAlignmentOptions.Center;
            nm.textWrappingMode = TextWrappingModes.Normal;
            try { nm.outlineWidth = 0.22f; nm.outlineColor = new Color(0, 0, 0); } catch {}

            int lvl = Sparq.Systems.EquipmentService.LevelOf(it.id);
            if (lvl > 0)
            {
                var pip = NewGO("Lv", tile.transform, typeof(Image));
                var pRT = pip.GetComponent<RectTransform>();
                pRT.anchorMin = new Vector2(0, 1); pRT.anchorMax = new Vector2(0, 1);
                pRT.pivot = new Vector2(0, 1);
                pRT.anchoredPosition = new Vector2(8, -8);
                pRT.sizeDelta = new Vector2(58, 30);
                pip.GetComponent<Image>().color = GOLD;
                pip.GetComponent<Image>().raycastTarget = false;
                var l = MakeText(pip.transform, "L", "Lv." + lvl, 18, FontStyles.Bold, INK);
                Stretch(l.rectTransform); l.alignment = TextAlignmentOptions.Center;
            }

            if (equipped)
            {
                var tick = NewGO("EquippedTick", tile.transform, typeof(Image));
                var tRT = tick.GetComponent<RectTransform>();
                tRT.anchorMin = new Vector2(1, 1); tRT.anchorMax = new Vector2(1, 1);
                tRT.pivot = new Vector2(1, 1);
                tRT.anchoredPosition = new Vector2(-8, -8);
                tRT.sizeDelta = new Vector2(48, 48);
                tick.GetComponent<Image>().color = BTN_GREEN;
                tick.GetComponent<Image>().raycastTarget = false;
                var l = MakeText(tick.transform, "L", "OK", 22, FontStyles.Bold, INK);
                Stretch(l.rectTransform); l.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                // Up / down indicator vs currently-equipped item in this slot.
                int cmp = Sparq.Systems.EquipmentService.CompareToEquipped(it);
                Color clr;
                string sym;
                if      (cmp > 0) { clr = new Color(0.40f, 0.85f, 0.55f, 1f); sym = "↑"; }
                else if (cmp < 0) { clr = new Color(0.95f, 0.40f, 0.45f, 1f); sym = "↓"; }
                else              { clr = new Color(0.55f, 0.55f, 0.60f, 1f); sym = "="; }
                var chip = NewGO("Cmp", tile.transform, typeof(Image));
                var cRT = chip.GetComponent<RectTransform>();
                cRT.anchorMin = new Vector2(1, 1); cRT.anchorMax = new Vector2(1, 1);
                cRT.pivot = new Vector2(1, 1);
                cRT.anchoredPosition = new Vector2(-8, -8);
                cRT.sizeDelta = new Vector2(54, 54);
                chip.GetComponent<Image>().color = clr;
                chip.GetComponent<Image>().raycastTarget = false;
                var lbl = MakeText(chip.transform, "L", sym, 36, FontStyles.Bold, INK);
                Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // ITEM INFO POPUP — Equip / Level Up
        // ─────────────────────────────────────────────────────────────────

        private static void ShowItemPopup(Sparq.Systems.EquipmentService.Item it)
        {
            HidePopup();
            if (_root == null) return;

            _popupRoot = new GameObject("ItemPopup",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            _popupRoot.transform.SetParent(_root.transform, false);
            Stretch(_popupRoot.GetComponent<RectTransform>());
            var canv = _popupRoot.GetComponent<Canvas>();
            canv.overrideSorting = true;
            canv.sortingOrder = _root.GetComponent<Canvas>().sortingOrder + 5;

            var dim = NewGO("Dim", _popupRoot.transform, typeof(Image), typeof(Button));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.75f);
            dim.GetComponent<Button>().onClick.AddListener(HidePopup);

            var box = NewGO("Box", _popupRoot.transform, typeof(Image));
            var brt = box.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f); brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(820, 720);
            box.GetComponent<Image>().color = new Color(0.92f, 0.88f, 0.98f, 1f);

            // Header — rarity colour band
            var head = NewGO("Head", box.transform, typeof(Image));
            var hRT = head.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.sizeDelta = new Vector2(0, 240);
            head.GetComponent<Image>().color = Sparq.Systems.EquipmentService.RarityColor(it.rarity);

            int rarityIdx = Mathf.Clamp((int)it.rarity, 0, FRAMES_BY_RARITY.Length - 1);
            var icoBox = NewGO("IcoBox", head.transform, typeof(Image));
            var iRT = icoBox.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0, 0.5f); iRT.anchorMax = new Vector2(0, 0.5f);
            iRT.pivot = new Vector2(0, 0.5f);
            iRT.anchoredPosition = new Vector2(20, 0);
            iRT.sizeDelta = new Vector2(190, 190);
            var icoFrame = LoadSprite(FRAMES_BY_RARITY[rarityIdx]);
            var icoImg = icoBox.GetComponent<Image>();
            if (icoFrame != null) { icoImg.sprite = icoFrame; icoImg.type = Image.Type.Sliced; icoImg.color = Color.white; }
            else icoImg.color = Color.white;
            icoImg.raycastTarget = false;
            BuildItemIconVisual(icoBox.transform, it, size: 180, anchoredPos: Vector2.zero);

            var nm = MakeText(head.transform, "N", it.name ?? it.id, 44, FontStyles.Bold, CREAM);
            var nRT = nm.rectTransform;
            nRT.anchorMin = new Vector2(0, 0.5f); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0, 1);
            nRT.offsetMin = new Vector2(228, -8); nRT.offsetMax = new Vector2(-20, -32);
            nm.alignment = TextAlignmentOptions.BottomLeft;
            try { nm.outlineWidth = 0.25f; nm.outlineColor = new Color(0.10f, 0.05f, 0.18f); } catch {}

            int curLevel = Sparq.Systems.EquipmentService.LevelOf(it.id);
            var rar = MakeText(head.transform, "R",
                $"{it.rarity.ToString().ToUpper()}  •  Lv.{curLevel}/{Sparq.Systems.EquipmentService.MAX_LEVEL}",
                26, FontStyles.Bold, new Color(1f, 1f, 1f, 0.92f));
            var rRT = rar.rectTransform;
            rRT.anchorMin = new Vector2(0, 0); rRT.anchorMax = new Vector2(1, 0.5f);
            rRT.pivot = new Vector2(0, 1);
            rRT.offsetMin = new Vector2(228, -54); rRT.offsetMax = new Vector2(-20, -10);
            rar.alignment = TextAlignmentOptions.MidlineLeft;

            // Stats block
            var eff = Sparq.Systems.EquipmentService.EffectiveStats(it);
            var statsBox = NewGO("Stats", box.transform, typeof(Image));
            var sRT = statsBox.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0.5f, 1);
            sRT.anchoredPosition = new Vector2(0, -260);
            sRT.sizeDelta = new Vector2(-40, 220);
            statsBox.GetComponent<Image>().color = new Color(0.18f, 0.16f, 0.34f, 1f);

            var delta = Sparq.Systems.EquipmentService.StatDeltaVsEquipped(it);
            BuildPopupStatRow(statsBox.transform, "Attack",  eff.atk, delta.atk, ATK_ICON, new Color(0.95f, 0.55f, 0.30f, 1f),   -10);
            BuildPopupStatRow(statsBox.transform, "Defense", eff.def, delta.def, DEF_ICON, new Color(0.55f, 0.85f, 1.00f, 1f),   -80);
            BuildPopupStatRow(statsBox.transform, "HP",      eff.hp,  delta.hp,  HP_ICON,  new Color(0.95f, 0.40f, 0.45f, 1f),  -150);

            int cost = curLevel < Sparq.Systems.EquipmentService.MAX_LEVEL
                ? Sparq.Systems.EquipmentService.UpgradeCost(curLevel)
                : -1;
            int coins = 0;
            try { coins = Sparq.Core.SaveService.Data?.sparqCoins ?? 0; } catch {}
            string costText = cost < 0
                ? "Max level reached"
                : (coins >= cost ? $"Cost: {cost:N0} coins   (you have {coins:N0})"
                                 : $"Cost: {cost:N0} coins   (need {cost - coins:N0} more)");
            var costLbl = MakeText(box.transform, "Cost", costText, 24, FontStyles.Bold,
                cost < 0 || coins < cost ? new Color(0.55f, 0.30f, 0.30f, 1f) : new Color(0.20f, 0.40f, 0.20f, 1f));
            var cRT = costLbl.rectTransform;
            cRT.anchorMin = new Vector2(0, 0); cRT.anchorMax = new Vector2(1, 0);
            cRT.pivot = new Vector2(0.5f, 0);
            cRT.anchoredPosition = new Vector2(0, 168);
            cRT.sizeDelta = new Vector2(-40, 36);
            costLbl.alignment = TextAlignmentOptions.Center;

            // Equip + Level Up buttons
            var equip = NewGO("Equip", box.transform, typeof(Image), typeof(Button));
            var eRT = equip.GetComponent<RectTransform>();
            eRT.anchorMin = new Vector2(0, 0); eRT.anchorMax = new Vector2(0.5f, 0);
            eRT.pivot = new Vector2(0.5f, 0);
            eRT.anchoredPosition = new Vector2(220, 60);
            eRT.sizeDelta = new Vector2(320, 100);
            equip.GetComponent<Image>().color = BTN_EQUIP;
            equip.GetComponent<Image>().raycastTarget = true;
            var eBtn = equip.GetComponent<Button>();
            eBtn.targetGraphic = equip.GetComponent<Image>(); eBtn.interactable = true;
            var eLbl = MakeText(equip.transform, "L", "Equip", 36, FontStyles.Bold, INK);
            Stretch(eLbl.rectTransform); eLbl.alignment = TextAlignmentOptions.Center;
            eBtn.onClick.AddListener(() => {
                try { Sparq.Systems.EquipmentService.Equip(it.id); } catch {}
                HidePopup();
                RebuildAll();
            });

            var lvlUp = NewGO("LevelUp", box.transform, typeof(Image), typeof(Button));
            var lRT2 = lvlUp.GetComponent<RectTransform>();
            lRT2.anchorMin = new Vector2(0.5f, 0); lRT2.anchorMax = new Vector2(1, 0);
            lRT2.pivot = new Vector2(0.5f, 0);
            lRT2.anchoredPosition = new Vector2(-220, 60);
            lRT2.sizeDelta = new Vector2(320, 100);
            bool canLv = Sparq.Systems.EquipmentService.CanUpgrade(it.id);
            lvlUp.GetComponent<Image>().color = canLv ? BTN_LEVEL : new Color(0.55f, 0.55f, 0.60f, 1f);
            lvlUp.GetComponent<Image>().raycastTarget = true;
            var lvBtn = lvlUp.GetComponent<Button>();
            lvBtn.targetGraphic = lvlUp.GetComponent<Image>();
            lvBtn.interactable = canLv;
            var lvLbl = MakeText(lvlUp.transform, "L", "Level Up", 32, FontStyles.Bold, INK);
            Stretch(lvLbl.rectTransform); lvLbl.alignment = TextAlignmentOptions.Center;
            lvBtn.onClick.AddListener(() => { HidePopup(); TryUpgrade(it.id); });

            // Bottom X
            var x = NewGO("X", _popupRoot.transform, typeof(Image), typeof(Button));
            var xRT = x.GetComponent<RectTransform>();
            xRT.anchorMin = new Vector2(0.5f, 0); xRT.anchorMax = new Vector2(0.5f, 0);
            xRT.pivot = new Vector2(0.5f, 0);
            xRT.anchoredPosition = new Vector2(0, 60);
            xRT.sizeDelta = new Vector2(110, 110);
            x.GetComponent<Image>().color = new Color(0.82f, 0.26f, 0.26f, 1f);
            x.GetComponent<Image>().raycastTarget = true;
            var xBtn = x.GetComponent<Button>();
            xBtn.targetGraphic = x.GetComponent<Image>(); xBtn.interactable = true;
            var xLbl = MakeText(x.transform, "X", "X", 48, FontStyles.Bold, Color.white);
            Stretch(xLbl.rectTransform); xLbl.alignment = TextAlignmentOptions.Center;
            xBtn.onClick.AddListener(HidePopup);
        }

        private static void BuildPopupStatRow(Transform parent, string label, int value, int delta,
            string iconPath, Color iconTint, float yOffset)
        {
            var ico = NewGO("Ico_" + label, parent, typeof(Image));
            var iRT = ico.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0, 1); iRT.anchorMax = new Vector2(0, 1);
            iRT.pivot = new Vector2(0, 0.5f);
            iRT.anchoredPosition = new Vector2(20, yOffset);
            iRT.sizeDelta = new Vector2(48, 48);
            var sp = LoadSprite(iconPath);
            var iImg = ico.GetComponent<Image>();
            if (sp != null) { iImg.sprite = sp; iImg.preserveAspect = true; }
            iImg.color = iconTint;
            iImg.raycastTarget = false;

            var lbl = MakeText(parent, "Lbl_" + label, $"{label}: {value}", 30, FontStyles.Bold, CREAM);
            var lRT = lbl.rectTransform;
            lRT.anchorMin = new Vector2(0, 1); lRT.anchorMax = new Vector2(1, 1);
            lRT.pivot = new Vector2(0, 0.5f);
            lRT.offsetMin = new Vector2(80, yOffset - 24); lRT.offsetMax = new Vector2(-260, yOffset + 24);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            // Delta vs currently-equipped item in this slot. +N in green,
            // -N in red, 0 hidden so we don't add visual noise.
            if (delta != 0)
            {
                Color dClr = delta > 0
                    ? new Color(0.40f, 0.95f, 0.55f, 1f)
                    : new Color(0.95f, 0.40f, 0.45f, 1f);
                string dText = delta > 0 ? $"+{delta}" : delta.ToString();
                var dLbl = MakeText(parent, "Delta_" + label, dText, 30, FontStyles.Bold, dClr);
                var dRT = dLbl.rectTransform;
                dRT.anchorMin = new Vector2(1, 1); dRT.anchorMax = new Vector2(1, 1);
                dRT.pivot = new Vector2(1, 0.5f);
                dRT.anchoredPosition = new Vector2(-20, yOffset);
                dRT.sizeDelta = new Vector2(220, 48);
                dLbl.alignment = TextAlignmentOptions.MidlineRight;
            }
        }

        private static void HidePopup()
        {
            if (_popupRoot != null) { UnityEngine.Object.Destroy(_popupRoot); _popupRoot = null; }
        }

        // ─────────────────────────────────────────────────────────────────
        // FORGE — upgrade + result popup
        // ─────────────────────────────────────────────────────────────────

        private static void TryUpgrade(string itemId)
        {
            var it = Sparq.Systems.EquipmentService.ById(itemId);
            if (it == null) return;
            var deltas = Sparq.Systems.EquipmentService.UpgradeDeltas(it);
            int newLevel = Sparq.Systems.EquipmentService.LevelOf(it.id) + 1;
            bool ok = Sparq.Systems.EquipmentService.Upgrade(it.id);
            if (!ok)
            {
                Debug.Log("[EquipmentPanel] Upgrade rejected (insufficient coins or maxed).");
                return;
            }
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            RebuildAll();
            ShowLevelUpFx(it, newLevel, deltas);
        }

        private static void ShowLevelUpFx(Sparq.Systems.EquipmentService.Item it,
            int newLevel, (int atk, int def, int hp) deltas)
        {
            HideLevelUpFx();
            if (_root == null) return;

            _levelUpRoot = new GameObject("LevelUpFx",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            _levelUpRoot.transform.SetParent(_root.transform, false);
            Stretch(_levelUpRoot.GetComponent<RectTransform>());
            var canv = _levelUpRoot.GetComponent<Canvas>();
            canv.overrideSorting = true;
            canv.sortingOrder = _root.GetComponent<Canvas>().sortingOrder + 8;

            var dim = NewGO("Dim", _levelUpRoot.transform, typeof(Image), typeof(Button));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0.10f, 0.05f, 0.25f, 0.94f);
            dim.GetComponent<Button>().onClick.AddListener(HideLevelUpFx);

            var box = NewGO("Box", _levelUpRoot.transform, typeof(Image));
            var brt = box.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f); brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(820, 1080);
            box.GetComponent<Image>().color = new Color(0.18f, 0.13f, 0.38f, 1f);

            int rarityIdx = Mathf.Clamp((int)it.rarity, 0, FRAMES_BY_RARITY.Length - 1);
            var portrait = NewGO("Portrait", box.transform, typeof(Image));
            var pRT = portrait.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.5f, 1); pRT.anchorMax = new Vector2(0.5f, 1);
            pRT.pivot = new Vector2(0.5f, 1);
            pRT.anchoredPosition = new Vector2(0, -110);
            pRT.sizeDelta = new Vector2(360, 360);
            var portFrame = LoadSprite(FRAMES_BY_RARITY[rarityIdx]);
            var pImg = portrait.GetComponent<Image>();
            if (portFrame != null) { pImg.sprite = portFrame; pImg.type = Image.Type.Sliced; pImg.color = Color.white; }
            else pImg.color = Sparq.Systems.EquipmentService.RarityColor(it.rarity);
            pImg.raycastTarget = false;
            BuildItemIconVisual(portrait.transform, it, size: 320, anchoredPos: Vector2.zero);

            var nm = MakeText(box.transform, "N", it.name ?? it.id, 50, FontStyles.Bold, CREAM);
            var nRT = nm.rectTransform;
            nRT.anchorMin = new Vector2(0, 1); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0.5f, 1);
            nRT.offsetMin = new Vector2(20, -50); nRT.offsetMax = new Vector2(-20, -8);
            nm.alignment = TextAlignmentOptions.Center;

            var rar = MakeText(box.transform, "R", it.rarity.ToString().ToUpper(), 28, FontStyles.Bold,
                Sparq.Systems.EquipmentService.RarityColor(it.rarity));
            var rRT = rar.rectTransform;
            rRT.anchorMin = new Vector2(0, 1); rRT.anchorMax = new Vector2(1, 1);
            rRT.pivot = new Vector2(0.5f, 1);
            rRT.offsetMin = new Vector2(20, -88); rRT.offsetMax = new Vector2(-20, -54);
            rar.alignment = TextAlignmentOptions.Center;

            var lvBox = NewGO("LvBox", box.transform, typeof(Image));
            var lbRT = lvBox.GetComponent<RectTransform>();
            lbRT.anchorMin = new Vector2(0.5f, 1); lbRT.anchorMax = new Vector2(0.5f, 1);
            lbRT.pivot = new Vector2(0.5f, 1);
            lbRT.anchoredPosition = new Vector2(0, -488);
            lbRT.sizeDelta = new Vector2(300, 90);
            lvBox.GetComponent<Image>().color = GOLD;
            lvBox.GetComponent<Image>().raycastTarget = false;
            var lvLbl = MakeText(lvBox.transform, "L", $"Lv.{newLevel}", 50, FontStyles.Bold, INK);
            Stretch(lvLbl.rectTransform); lvLbl.alignment = TextAlignmentOptions.Center;

            var statsBox = NewGO("Stats", box.transform, typeof(Image));
            var sRT = statsBox.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 0);
            sRT.pivot = new Vector2(0.5f, 0);
            sRT.anchoredPosition = new Vector2(0, 180);
            sRT.sizeDelta = new Vector2(-40, 260);
            statsBox.GetComponent<Image>().color = new Color(0.10f, 0.08f, 0.22f, 1f);

            BuildDeltaRow(statsBox.transform, "Attack",  deltas.atk, ATK_ICON, new Color(0.95f, 0.55f, 0.30f, 1f),  32);
            BuildDeltaRow(statsBox.transform, "Defense", deltas.def, DEF_ICON, new Color(0.55f, 0.85f, 1.00f, 1f), -36);
            BuildDeltaRow(statsBox.transform, "HP",      deltas.hp,  HP_ICON,  new Color(0.95f, 0.40f, 0.45f, 1f), -104);

            var ok = NewGO("OK", box.transform, typeof(Image), typeof(Button));
            var oRT = ok.GetComponent<RectTransform>();
            oRT.anchorMin = new Vector2(0.5f, 0); oRT.anchorMax = new Vector2(0.5f, 0);
            oRT.pivot = new Vector2(0.5f, 0);
            oRT.anchoredPosition = new Vector2(0, 40);
            oRT.sizeDelta = new Vector2(420, 110);
            ok.GetComponent<Image>().color = BTN_GREEN;
            ok.GetComponent<Image>().raycastTarget = true;
            var oBtn = ok.GetComponent<Button>();
            oBtn.targetGraphic = ok.GetComponent<Image>(); oBtn.interactable = true;
            var oLbl = MakeText(ok.transform, "L", "OK", 40, FontStyles.Bold, INK);
            Stretch(oLbl.rectTransform); oLbl.alignment = TextAlignmentOptions.Center;
            oBtn.onClick.AddListener(HideLevelUpFx);
        }

        private static void BuildDeltaRow(Transform parent, string label, int delta,
            string iconPath, Color iconTint, float yFromTop)
        {
            var ico = NewGO("Ico_" + label, parent, typeof(Image));
            var iRT = ico.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0, 0.5f); iRT.anchorMax = new Vector2(0, 0.5f);
            iRT.pivot = new Vector2(0, 0.5f);
            iRT.anchoredPosition = new Vector2(28, yFromTop);
            iRT.sizeDelta = new Vector2(54, 54);
            var sp = LoadSprite(iconPath);
            var iImg = ico.GetComponent<Image>();
            if (sp != null) { iImg.sprite = sp; iImg.preserveAspect = true; }
            iImg.color = iconTint;
            iImg.raycastTarget = false;

            var lbl = MakeText(parent, "Lbl_" + label, label, 32, FontStyles.Bold, CREAM);
            var lRT = lbl.rectTransform;
            lRT.anchorMin = new Vector2(0, 0.5f); lRT.anchorMax = new Vector2(1, 0.5f);
            lRT.pivot = new Vector2(0, 0.5f);
            lRT.offsetMin = new Vector2(100, yFromTop - 22); lRT.offsetMax = new Vector2(-260, yFromTop + 22);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            var delt = MakeText(parent, "D_" + label, delta > 0 ? "+" + delta : delta.ToString(),
                36, FontStyles.Bold, DELTA_PLUS);
            var dRT = delt.rectTransform;
            dRT.anchorMin = new Vector2(1, 0.5f); dRT.anchorMax = new Vector2(1, 0.5f);
            dRT.pivot = new Vector2(1, 0.5f);
            dRT.anchoredPosition = new Vector2(-30, yFromTop);
            dRT.sizeDelta = new Vector2(240, 44);
            delt.alignment = TextAlignmentOptions.MidlineRight;
        }

        private static void HideLevelUpFx()
        {
            if (_levelUpRoot != null) { UnityEngine.Object.Destroy(_levelUpRoot); _levelUpRoot = null; }
        }

        // ─────────────────────────────────────────────────────────────────
        // PRIMITIVES
        // ─────────────────────────────────────────────────────────────────

        private static string FirstGlyph(string s)
        {
            if (string.IsNullOrEmpty(s)) return "?";
            return s.Substring(0, 1).ToUpper();
        }

        /// <summary>Resolves an item's iconPath (catalog value like
        /// "FP_SwordT1" or "Crown_1") to an actual Sprite, trying the
        /// known asset locations. Returns null on miss — caller renders
        /// a letter glyph as fallback.</summary>
        private static Sprite ResolveItemSprite(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath)) return null;
            // Most catalog entries use the FP_ prefix; the actual files
            // sit under Assets/FantasyIconPack/128/ without the prefix.
            if (iconPath.StartsWith("FP_"))
            {
                string bare = iconPath.Substring(3);
                var sp = LoadSprite("Assets/FantasyIconPack/128/" + bare + ".png");
                if (sp != null) return sp;
            }
            // Try Resources.Load by raw name (Crown_1 etc may live there).
            try { var rs = Resources.Load<Sprite>(iconPath); if (rs != null) return rs; } catch {}
            // Final fallback — try a direct asset path as-given.
            try { return LoadSprite(iconPath); } catch { return null; }
        }

        /// <summary>Builds the icon visual inside a tile / slot / popup —
        /// either a real Image with the resolved sprite, or a coloured
        /// letter glyph as fallback. Returns the icon GameObject so the
        /// caller can lay decorations (Lv pip, ✓ tick) on top.</summary>
        private static GameObject BuildItemIconVisual(Transform parent,
            Sparq.Systems.EquipmentService.Item it, float size, Vector2 anchoredPos)
        {
            var go = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;

            var sp = ResolveItemSprite(it?.iconPath);
            if (sp != null)
            {
                img.sprite = sp;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                // Fallback: hide the Image, drop a big letter glyph in its place.
                img.color = new Color(0, 0, 0, 0);
                var glyph = MakeText(go.transform, "Glyph", FirstGlyph(it?.name), size * 0.62f,
                    FontStyles.Bold, CREAM);
                Stretch(glyph.rectTransform); glyph.alignment = TextAlignmentOptions.Center;
            }
            return go;
        }

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
