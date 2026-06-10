// BagPanel.cs — polished inventory grid modelled on Layer Lab's
// GUI Pro-FantasyHero 3_Bag.png template:
//   • Title banner with a hanging chest icon
//   • Three tabs (All / Resource / Gear)
//   • 4-column grid of item tiles using ItemFrame_Square_01_* sprites
//     for rarity-tinted backgrounds
//   • Count badge + alert dot per tile
//   • Tap-to-show tooltip card with name + description + Use action
//   • Big red X close button
//
// Data sources kept intentionally light for v1:
//   Currencies → SaveService.Data.sparqCoins (+ gems placeholder)
//   Foods      → PetService.FoodCounts() / FOODS
//   Gear       → EquipmentService.OwnedItems() (+ rarity colour)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class BagPanel
    {
        // ── Palette ─────────────────────────────────────────────────────
        private static readonly Color CARD_BG    = new Color(0.16f, 0.13f, 0.30f, 1f);  // deep indigo body
        private static readonly Color STROKE     = new Color(0.45f, 0.30f, 0.75f, 1f);  // purple edge
        private static readonly Color TITLEBAR   = new Color(0.25f, 0.18f, 0.50f, 1f);  // banner backdrop
        private static readonly Color GOLD       = new Color(0.99f, 0.78f, 0.20f, 1f);
        private static readonly Color CREAM      = new Color(1.00f, 0.97f, 0.85f, 1f);
        private static readonly Color INK        = new Color(0.13f, 0.10f, 0.20f, 1f);
        private static readonly Color INK_SOFT   = new Color(0.55f, 0.55f, 0.70f, 1f);
        private static readonly Color TAB_OFF    = new Color(0.24f, 0.20f, 0.42f, 1f);
        private static readonly Color TAB_ON     = new Color(0.45f, 0.30f, 0.75f, 1f);
        private static readonly Color TILE_DIM   = new Color(0.10f, 0.08f, 0.18f, 0.55f);  // tooltip dim
        private static readonly Color TOOLTIP_BG = new Color(1.00f, 0.99f, 0.96f, 1f);     // bright cream tooltip

        // ── Sprites — frames per rarity tier, plus chest banner ─────────
        private const string FRAME_DIR  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/";
        private const string CHEST_ICON = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_Chest/128/Chest_Gold.png";
        private const string POPUP_BG   = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Popup/Popup_Box_Bg.png";
        private const string COIN_ICON  = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Coin01_Gold.png";
        private const string GEM_ICON   = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Materials_Gem02_Purple.png";

        private static readonly string[] FRAMES_BY_RARITY = {
            FRAME_DIR + "ItemFrame_Square_01_Gray.png",     // Common
            FRAME_DIR + "ItemFrame_Square_01_Green.png",    // Uncommon
            FRAME_DIR + "ItemFrame_Square_01_Blue.png",     // Rare
            FRAME_DIR + "ItemFrame_Square_01_Purple.png",   // Epic
            FRAME_DIR + "ItemFrame_Square_01_Yellow.png",   // Legendary
        };

        // ── Tab state ───────────────────────────────────────────────────
        public enum Tab { All, Resource, Gear }
        private static Tab _currentTab = Tab.All;

        // ── Runtime refs ────────────────────────────────────────────────
        private static GameObject _root;
        private static Transform  _gridParent;
        private static GameObject _tooltipRoot;

        private static Image _tabImgAll, _tabImgRes, _tabImgGear;
        private static TMP_Text _tabTxtAll, _tabTxtRes, _tabTxtGear;

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC
        // ─────────────────────────────────────────────────────────────────

        public static void Show()
        {
            if (_root != null) { Hide(); return; }
            EnsureEventSystem();

            _root = new GameObject("Sparq_BagPanel",
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

            // Dim
            var dim = NewGO("Dim", _root.transform, typeof(Image), typeof(Button));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.78f);
            dim.GetComponent<Button>().onClick.AddListener(Hide);

            // Card body
            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(980, 1640);
            var cardImg = card.GetComponent<Image>();
            var bgSp = LoadSprite(POPUP_BG);
            if (bgSp != null) { cardImg.sprite = bgSp; cardImg.type = Image.Type.Sliced; }
            cardImg.color = CARD_BG;

            BuildChestBanner(card.transform);
            BuildTabStrip(card.transform);
            BuildGrid(card.transform);
            BuildCloseButton(card.transform);

            ApplyTabStyles();
            RebuildGrid();

            Debug.Log("[BagPanel] Opened.");
        }

        public static void Hide()
        {
            HideTooltip();
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
            _gridParent = null;
            _tabImgAll = _tabImgRes = _tabImgGear = null;
            _tabTxtAll = _tabTxtRes = _tabTxtGear = null;
        }

        // ─────────────────────────────────────────────────────────────────
        // BUILDERS — banner, tabs, grid, close
        // ─────────────────────────────────────────────────────────────────

        private static void BuildChestBanner(Transform card)
        {
            // Banner backdrop bar
            var bar = NewGO("BannerBar", card, typeof(Image));
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0.5f, 1);
            brt.anchoredPosition = new Vector2(0, -90);
            brt.sizeDelta = new Vector2(-60, 96);
            bar.GetComponent<Image>().color = TITLEBAR;

            // "INVENTORY" title
            var title = MakeText(bar.transform, "Title", "INVENTORY", 50, FontStyles.Bold, CREAM);
            Stretch(title.rectTransform); title.alignment = TextAlignmentOptions.Center;
            try { title.outlineWidth = 0.25f; title.outlineColor = new Color(0.05f, 0.03f, 0.10f); } catch {}

            // Chest icon hanging above the bar
            var chest = NewGO("ChestIcon", card, typeof(Image));
            var crt = chest.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 1); crt.anchorMax = new Vector2(0.5f, 1);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = new Vector2(0, -36);
            crt.sizeDelta = new Vector2(140, 140);
            var chestImg = chest.GetComponent<Image>();
            var sp = LoadSprite(CHEST_ICON);
            if (sp != null) { chestImg.sprite = sp; chestImg.preserveAspect = true; }
            chestImg.raycastTarget = false;
        }

        private static void BuildTabStrip(Transform card)
        {
            var strip = NewGO("TabStrip", card, typeof(HorizontalLayoutGroup));
            var rt = strip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -160);
            rt.sizeDelta = new Vector2(-60, 96);
            var hlg = strip.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.padding = new RectOffset(8, 8, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            (_tabImgAll,  _tabTxtAll)  = BuildTab(strip.transform, "All",      Tab.All);
            (_tabImgRes,  _tabTxtRes)  = BuildTab(strip.transform, "Resource", Tab.Resource);
            (_tabImgGear, _tabTxtGear) = BuildTab(strip.transform, "Gear",     Tab.Gear);
        }

        private static (Image bg, TMP_Text lbl) BuildTab(Transform parent, string label, Tab tab)
        {
            var go = NewGO("Tab_" + tab, parent, typeof(Image), typeof(Button));
            var img = go.GetComponent<Image>();
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            btn.onClick.AddListener(() => SetTab(tab));
            var lbl = MakeText(go.transform, "L", label, 32, FontStyles.Bold, CREAM);
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
            return (img, lbl);
        }

        private static void SetTab(Tab t)
        {
            if (_currentTab == t) return;
            _currentTab = t;
            ApplyTabStyles();
            RebuildGrid();
        }

        private static void ApplyTabStyles()
        {
            void Style(Image bg, TMP_Text lbl, bool on)
            {
                if (bg  != null) bg.color  = on ? TAB_ON : TAB_OFF;
                if (lbl != null) lbl.color = on ? CREAM  : INK_SOFT;
            }
            Style(_tabImgAll,  _tabTxtAll,  _currentTab == Tab.All);
            Style(_tabImgRes,  _tabTxtRes,  _currentTab == Tab.Resource);
            Style(_tabImgGear, _tabTxtGear, _currentTab == Tab.Gear);
        }

        private static void BuildGrid(Transform card)
        {
            var scrollGO = NewGO("Scroll", card, typeof(Image), typeof(ScrollRect));
            var srRT = scrollGO.GetComponent<RectTransform>();
            srRT.anchorMin = new Vector2(0, 0); srRT.anchorMax = new Vector2(1, 1);
            srRT.offsetMin = new Vector2(36, 160); srRT.offsetMax = new Vector2(-36, -270);
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
            // 220 (was 196) gives icons + labels more breathing room while
            // still fitting 4 columns inside the 980-wide card.
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

        private static void BuildCloseButton(Transform card)
        {
            // Top-right of the CARD (not the screen) — the old bottom-center
            // placement collided with the lobby's bottom-nav WEAPONS tab, whose
            // higher-sort canvas swallowed taps meant for this X. Parenting to
            // the card keeps the X inside the panel's safe zone.
            var btnGO = NewGO("Close", card, typeof(Image), typeof(Button));
            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-22, -22);
            rt.sizeDelta = new Vector2(90, 90);
            var img = btnGO.GetComponent<Image>();
            img.color = new Color(0.82f, 0.26f, 0.26f, 1f);
            img.raycastTarget = true;
            var btn = btnGO.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            var lbl = MakeText(btnGO.transform, "X", "X", 50, FontStyles.Bold, Color.white);
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
            btn.onClick.AddListener(Hide);
        }

        // ─────────────────────────────────────────────────────────────────
        // GRID CONTENT — feeds tiles to the grid based on the active tab
        // ─────────────────────────────────────────────────────────────────

        private static void RebuildGrid()
        {
            if (_gridParent == null) return;
            for (int i = _gridParent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_gridParent.GetChild(i).gameObject);

            if (_currentTab == Tab.All || _currentTab == Tab.Resource)
            {
                BuildCurrencyTiles();
                BuildFoodTiles();
            }
            if (_currentTab == Tab.All || _currentTab == Tab.Gear)
            {
                BuildGearTiles();
            }

            if (_gridParent.childCount == 0)
                BuildPlaceholderTile("Nothing here yet");
        }

        private static void BuildCurrencyTiles()
        {
            int coins = 0;
            try { coins = Sparq.Core.SaveService.Data?.sparqCoins ?? 0; } catch {}
            BuildTile(
                title: "Coins", desc: "Earned from quests and battles. Spend at the shop.",
                iconPath: COIN_ICON, rarity: 1, countText: FormatCount(coins),
                showAlert: false, onUse: null);

            BuildTile(
                title: "Gems",  desc: "Rare currency. Used for premium upgrades.",
                iconPath: GEM_ICON, rarity: 3, countText: "0",
                showAlert: false, onUse: null);
        }

        private static void BuildFoodTiles()
        {
            Dictionary<string, int> counts = null;
            try { counts = Sparq.Systems.PetService.FoodCounts(); }
            catch { counts = new Dictionary<string, int>(); }

            int totalFood = 0;
            if (counts != null) foreach (var kv in counts) totalFood += kv.Value;
            if (totalFood == 0) return;

            try
            {
                foreach (var food in Sparq.Systems.PetService.FOODS)
                {
                    int n = 0;
                    if (counts != null) counts.TryGetValue(food.id, out n);
                    if (n <= 0) continue;
                    BuildTile(
                        title: food.name ?? food.id,
                        desc: $"Pet food. Feeds your companion for HP and morale.",
                        iconPath: food.spritePath, rarity: 1, countText: "×" + n,
                        showAlert: false, onUse: null);
                }
            }
            catch (System.Exception ex)
            { Debug.LogError($"[BagPanel] Food iteration failed: {ex.Message}"); }
        }

        private static void BuildGearTiles()
        {
            List<Sparq.Systems.EquipmentService.Item> owned = null;
            try { owned = Sparq.Systems.EquipmentService.OwnedItems(); }
            catch { owned = new List<Sparq.Systems.EquipmentService.Item>(); }
            if (owned == null || owned.Count == 0)
            {
                BuildPlaceholderTile("No gear yet");
                return;
            }
            foreach (var it in owned)
            {
                if (it == null) continue;
                int rarityIdx = (int)it.rarity;
                BuildTile(
                    title: it.name ?? it.id,
                    desc:  $"{it.rarity} • {it.slot}\nAtk +{it.atk}   Def +{it.def}   HP +{it.hp}",
                    iconPath: it.iconPath,    // catalog path — resolver handles FP_* / Crown_* / etc.
                    rarity: rarityIdx,
                    countText: "",
                    showAlert: false,
                    onUse: null);
            }
        }

        private static void BuildPlaceholderTile(string text)
        {
            var tile = NewGO("Empty", _gridParent, typeof(Image));
            tile.GetComponent<Image>().color = TAB_OFF;
            var t = MakeText(tile.transform, "T", text, 22, FontStyles.Italic, INK_SOFT);
            Stretch(t.rectTransform); t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
        }

        // ─────────────────────────────────────────────────────────────────
        // TILE BUILDER
        // ─────────────────────────────────────────────────────────────────

        private static void BuildTile(string title, string desc, string iconPath,
            int rarity, string countText, bool showAlert, System.Action onUse)
        {
            var tile = NewGO("Tile", _gridParent, typeof(Image), typeof(Button));
            var img = tile.GetComponent<Image>();
            img.color = Color.white;
            int safeRarity = Mathf.Clamp(rarity, 0, FRAMES_BY_RARITY.Length - 1);
            var frame = LoadSprite(FRAMES_BY_RARITY[safeRarity]);
            if (frame != null) { img.sprite = frame; img.type = Image.Type.Sliced; }
            else img.color = TAB_ON;   // fallback if frame not loaded
            img.raycastTarget = true;
            var btn = tile.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            btn.onClick.AddListener(() => ShowTooltip(title, desc, onUse));

            // Icon centered inside the frame at ~70%. Use the resolver
            // so "FP_X" catalog paths resolve to Assets/FantasyIconPack/128.
            var sp = ResolveItemSprite(iconPath);
            if (sp != null)
            {
                var icon = NewGO("Icon", tile.transform, typeof(Image));
                var iRT = icon.GetComponent<RectTransform>();
                iRT.anchorMin = new Vector2(0.5f, 0.5f); iRT.anchorMax = new Vector2(0.5f, 0.5f);
                iRT.pivot = new Vector2(0.5f, 0.5f);
                iRT.anchoredPosition = new Vector2(0, 6);
                iRT.sizeDelta = new Vector2(150, 150);
                var iImg = icon.GetComponent<Image>();
                iImg.sprite = sp;
                iImg.preserveAspect = true;
                iImg.color = Color.white;
                iImg.raycastTarget = false;
            }
            else
            {
                // Text fallback when no sprite resolves (e.g. Crown_1 / Badge_*
                // items whose art isn't bundled).
                var t = MakeText(tile.transform, "Glyph", FirstGlyph(title), 80, FontStyles.Bold, CREAM);
                var trt = t.rectTransform;
                trt.anchorMin = new Vector2(0, 0.25f); trt.anchorMax = new Vector2(1, 1);
                trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
                t.alignment = TextAlignmentOptions.Center;
            }

            // Title strip across the bottom — bumped 18 → 24 with more vertical
            // room so "Cloth Tunic"/"Iron Sword" etc. actually read.
            if (!string.IsNullOrEmpty(title))
            {
                var nm = MakeText(tile.transform, "N", title, 24, FontStyles.Bold, CREAM);
                var nRT = nm.rectTransform;
                nRT.anchorMin = new Vector2(0, 0); nRT.anchorMax = new Vector2(1, 0);
                nRT.pivot = new Vector2(0.5f, 0);
                nRT.anchoredPosition = new Vector2(0, 10);
                nRT.sizeDelta = new Vector2(-14, 42);
                nm.alignment = TextAlignmentOptions.Center;
                nm.textWrappingMode = TextWrappingModes.Normal;
                try { nm.outlineWidth = 0.22f; nm.outlineColor = new Color(0, 0, 0); } catch {}
            }

            // Count badge bottom-right (yellow pill) — bigger so "10.3k" reads.
            if (!string.IsNullOrEmpty(countText))
            {
                var badge = NewGO("Badge", tile.transform, typeof(Image));
                var bRT = badge.GetComponent<RectTransform>();
                bRT.anchorMin = new Vector2(1, 0); bRT.anchorMax = new Vector2(1, 0);
                bRT.pivot = new Vector2(1, 0);
                bRT.anchoredPosition = new Vector2(-8, 54);
                bRT.sizeDelta = new Vector2(92, 42);
                badge.GetComponent<Image>().color = GOLD;
                badge.GetComponent<Image>().raycastTarget = false;
                var bl = MakeText(badge.transform, "L", countText, 28, FontStyles.Bold, INK);
                Stretch(bl.rectTransform); bl.alignment = TextAlignmentOptions.Center;
            }

            // Red alert dot top-right
            if (showAlert)
            {
                var dot = NewGO("Alert", tile.transform, typeof(Image));
                var dRT = dot.GetComponent<RectTransform>();
                dRT.anchorMin = new Vector2(1, 1); dRT.anchorMax = new Vector2(1, 1);
                dRT.pivot = new Vector2(1, 1);
                dRT.anchoredPosition = new Vector2(-8, -8);
                dRT.sizeDelta = new Vector2(30, 30);
                dot.GetComponent<Image>().color = new Color(0.95f, 0.30f, 0.30f, 1f);
                dot.GetComponent<Image>().raycastTarget = false;
            }
        }

        private static string FormatCount(int n)
        {
            if (n >= 1_000_000) return (n / 1000f).ToString("0.0") + "M";
            if (n >=     1000) return (n / 1000f).ToString("0.#") + "K";
            return n.ToString("N0");
        }

        private static string FirstGlyph(string s)
        {
            if (string.IsNullOrEmpty(s)) return "?";
            return s.Substring(0, 1).ToUpper();
        }

        // ─────────────────────────────────────────────────────────────────
        // TOOLTIP POPUP
        // ─────────────────────────────────────────────────────────────────

        private static void ShowTooltip(string title, string desc, System.Action onUse)
        {
            HideTooltip();
            if (_root == null) return;

            _tooltipRoot = new GameObject("Tooltip",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            _tooltipRoot.transform.SetParent(_root.transform, false);
            Stretch(_tooltipRoot.GetComponent<RectTransform>());
            var canv = _tooltipRoot.GetComponent<Canvas>();
            canv.overrideSorting = true;
            canv.sortingOrder = _root.GetComponent<Canvas>().sortingOrder + 5;

            // Dim — tap anywhere to dismiss
            var dim = NewGO("Dim", _tooltipRoot.transform, typeof(Image), typeof(Button));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = TILE_DIM;
            dim.GetComponent<Button>().onClick.AddListener(HideTooltip);

            // Tooltip card centered
            var box = NewGO("Box", _tooltipRoot.transform, typeof(Image));
            var brt = box.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f); brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(740, 460);
            box.GetComponent<Image>().color = TOOLTIP_BG;
            box.GetComponent<Image>().raycastTarget = true;

            // Title
            var t = MakeText(box.transform, "T", title, 48, FontStyles.Bold, INK);
            var tRT = t.rectTransform;
            tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.offsetMin = new Vector2(28, -100); tRT.offsetMax = new Vector2(-28, -20);
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;

            // Description
            var d = MakeText(box.transform, "D", desc, 32, FontStyles.Normal, new Color(0.30f, 0.30f, 0.36f, 1f));
            var dRT = d.rectTransform;
            dRT.anchorMin = new Vector2(0, 0); dRT.anchorMax = new Vector2(1, 1);
            dRT.offsetMin = new Vector2(28, 140); dRT.offsetMax = new Vector2(-28, -110);
            d.alignment = TextAlignmentOptions.Center;
            d.textWrappingMode = TextWrappingModes.Normal;

            // OK / Use button
            var ok = NewGO("OK", box.transform, typeof(Image), typeof(Button));
            var oRT = ok.GetComponent<RectTransform>();
            oRT.anchorMin = new Vector2(0.5f, 0); oRT.anchorMax = new Vector2(0.5f, 0);
            oRT.pivot = new Vector2(0.5f, 0);
            oRT.anchoredPosition = new Vector2(0, 26);
            oRT.sizeDelta = new Vector2(420, 100);
            var oImg = ok.GetComponent<Image>();
            oImg.color = onUse != null ? new Color(0.40f, 0.85f, 0.55f, 1f) : GOLD;
            oImg.raycastTarget = true;
            var oBtn = ok.GetComponent<Button>();
            oBtn.targetGraphic = oImg; oBtn.interactable = true;
            var ol = MakeText(ok.transform, "L", onUse != null ? "Use" : "OK",
                36, FontStyles.Bold, INK);
            Stretch(ol.rectTransform); ol.alignment = TextAlignmentOptions.Center;
            oBtn.onClick.AddListener(() => { onUse?.Invoke(); HideTooltip(); });
        }

        private static void HideTooltip()
        {
            if (_tooltipRoot != null) { UnityEngine.Object.Destroy(_tooltipRoot); _tooltipRoot = null; }
        }

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

        private static Sprite LoadSprite(string assetPath) => Sparq.Core.SpriteLoader.Load(assetPath);

        /// <summary>Resolves a catalog-style icon path to a Sprite:
        /// "FP_X" → Assets/FantasyIconPack/128/X.png, plus Resources.Load
        /// and raw asset-path fallbacks. Returns null if all miss so the
        /// caller can fall back to a letter glyph.</summary>
        private static Sprite ResolveItemSprite(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath)) return null;
            if (iconPath.StartsWith("FP_"))
            {
                string bare = iconPath.Substring(3);
                var sp = LoadSprite("Assets/FantasyIconPack/128/" + bare + ".png");
                if (sp != null) return sp;
            }
            try { var rs = Resources.Load<Sprite>(iconPath); if (rs != null) return rs; } catch {}
            try { return LoadSprite(iconPath); } catch { return null; }
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
