// StorePanel.cs — the main STORE opened from the bottom-nav STORE tab,
// laid out in the standard mobile-RPG / "Top Heroes" pattern:
//
//   ┌─────────────────────────────────────────────┐
//   │  STORE                       [coins] [gems]  │  ← title + currency bar
//   ├──────────┬──────────────────────────────────┤
//   │ Featured │   ┌──────┐  ┌──────┐              │
//   │ Gems     │   │offer │  │offer │   ← 2-col    │
//   │ Coins    │   └──────┘  └──────┘     scroll   │
//   │ Bundles  │   ┌──────┐  ┌──────┐     grid     │
//   │          │   └──────┘  └──────┘              │
//   └──────────┴──────────────────────────────────┘
//      ↑ left category rail
//
// Coin/gem packs are IAP placeholders (tapping shows a "coming soon" toast)
// until a real store SDK is wired in. The "Featured" bundles and the Bundles
// tab are also placeholders. Coins is the only live currency in the game.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class StorePanel
    {
        private static readonly Color CARD_BG    = new Color(0.17f, 0.17f, 0.20f, 1f);
        private static readonly Color CREAM      = new Color(1f, 0.97f, 0.85f, 1f);
        private static readonly Color INK        = new Color(0.11f, 0.13f, 0.16f, 1f);
        private static readonly Color INK_SOFT   = new Color(0.72f, 0.74f, 0.82f, 1f);
        private static readonly Color GOLD       = new Color(0.99f, 0.78f, 0.20f, 1f);
        private static readonly Color GEM_BLUE   = new Color(0.42f, 0.78f, 1.00f, 1f);
        private static readonly Color RAIL_BG    = new Color(0.12f, 0.12f, 0.15f, 1f);
        private static readonly Color TAB_ON     = new Color(0.55f, 0.45f, 0.95f, 1f);  // purple
        private static readonly Color TAB_OFF    = new Color(0.26f, 0.26f, 0.32f, 1f);
        private static readonly Color OFFER_BG   = new Color(0.23f, 0.23f, 0.29f, 1f);
        private static readonly Color BUY_GREEN  = new Color(0.32f, 0.74f, 0.42f, 1f);
        private static readonly Color BADGE_RED  = new Color(0.92f, 0.30f, 0.32f, 1f);

        private const string POPUP_PREFAB = "Assets/Layer Lab/GUI Pro-FantasyRPG/Prefabs/Prefabs_Component_Popups/Popup_01_Basic_White.prefab";

        // ── Icons ──────────────────────────────────────────────────────────
        private const string DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/128/";
        private const string ICON_COIN   = DIR + "ItemIcon_Coin_Gold.png";
        private const string ICON_GEM    = DIR + "ItemIcon_Gem_Diamond_Blue.png";
        private const string ICON_CHEST_WOOD    = DIR + "ItemIcon_Chest_Wood.png";
        private const string ICON_CHEST_SILVER  = DIR + "ItemIcon_Chest_Silver.png";
        private const string ICON_CHEST_GOLD    = DIR + "ItemIcon_Chest_Gold.png";
        private const string ICON_CHEST_PREMIUM = DIR + "ItemIcon_Chest_Premium.png";
        private const string ICON_CHEST_SPECIAL = DIR + "ItemIcon_Chest_Special.png";
        private const string ICON_GIFT_PURPLE   = DIR + "ItemIcon_Gift_Purple.png";
        private const string ICON_GIFT_YELLOW   = DIR + "ItemIcon_Gift_Yellow.png";
        private const string ICON_CROWN  = DIR + "ItemIcon_Crown_2.png";
        private const string ICON_EGG    = "Assets/2D Fantasy Monster Sprite Pack/Monsters/Egg/Mystery-Egg.png";
        private const string BTN_CONVEX  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Convex_Rectangle_01_Gray.png";

        // ── Categories + offer data ─────────────────────────────────────────
        public enum Cat { Featured, Forge, Gems, Coins, Bundles }
        private static Cat _cat = Cat.Featured;

        private struct Offer
        {
            public string title, contents, price, icon, badge, sku;
            public int goldCost;   // 0 = real-money/IAP offer; >0 = spend in-game gold
            // IAP offer (real-money price string).
            public Offer(string t, string c, string p, string i, string b = "")
            { title = t; contents = c; price = p; icon = i; badge = b; sku = ""; goldCost = 0; }
            // Gold-sink offer — costs in-game coins, grants an item by sku.
            public Offer(string t, string c, int gold, string sku, string i, string b = "")
            { title = t; contents = c; price = $"{gold:N0}g"; icon = i; badge = b; this.sku = sku; goldCost = gold; }
        }

        // Pricing is capped at $9.99 — no single offer goes over $10.
        private static readonly Offer[] FEATURED = {
            new Offer("Starter Pack", "300 Gems\n4,000 Coins\nEpic Egg", "$4.99", ICON_CHEST_SPECIAL, "BEST VALUE"),
            new Offer("Daily Deal",   "1,000 Coins\nRefreshes daily",    "FREE",  ICON_GIFT_YELLOW,   "HOT"),
            new Offer("Adventurer's Box", "200 Gems\n6,000 Coins",       "$6.99", ICON_CHEST_PREMIUM),
            new Offer("Hero Crown",    "VIP perks\n+10% coins for 30d",  "$9.99", ICON_CROWN),
        };
        private static readonly Offer[] GEMS = {
            new Offer("Pile of Gems",   "80 Gems",    "$0.99", ICON_GEM),
            new Offer("Bag of Gems",    "300 Gems",   "$2.99", ICON_GEM),
            new Offer("Chest of Gems",  "650 Gems",   "$4.99", ICON_GEM),
            new Offer("Crate of Gems",  "1,100 Gems", "$6.99", ICON_GEM, "POPULAR"),
            new Offer("Hoard of Gems",  "1,500 Gems", "$8.99", ICON_GEM),
            new Offer("Vault of Gems",  "1,800 Gems", "$9.99", ICON_GEM, "BEST VALUE"),
        };
        private static readonly Offer[] COINS = {
            new Offer("Handful", "1,000 Coins",  "$0.99", ICON_CHEST_WOOD),
            new Offer("Pouch",   "4,000 Coins",  "$2.99", ICON_CHEST_SILVER),
            new Offer("Sack",    "9,000 Coins",  "$5.99", ICON_CHEST_GOLD, "POPULAR"),
            new Offer("Vault",   "16,000 Coins", "$9.99", ICON_CHEST_PREMIUM, "BEST VALUE"),
        };
        // FORGE — spend the gold you earn (AFK/loot/battles) on real items. This
        // is the game's gold SINK: gear chests feed the equipment chase, eggs feed
        // the pet loop. Functional (unlike the IAP tabs), processed in OnOfferTapped.
        private static readonly Offer[] FORGE = {
            new Offer("Gear Chest",  "1 random gear item",              600,  "gear",      ICON_CHEST_SILVER),
            new Offer("Epic Cache",  "Best of 4 rolls\n(skews rare+)",  2500, "gear_epic", ICON_CHEST_GOLD, "VALUE"),
            new Offer("Mystery Egg", "1 pet egg to hatch",              1200, "egg",       ICON_EGG),
        };
        private static readonly Offer[] BUNDLES = {
            new Offer("Newbie Bundle", "200 Gems\n2,000 Coins\nRare Egg",          "$1.99", ICON_GIFT_PURPLE),
            new Offer("Growth Fund",   "Unlock up to\n2,000 Gems",                 "$4.99", ICON_GIFT_YELLOW, "HOT"),
            new Offer("Mega Bundle",   "1,200 Gems\n18,000 Coins\nLegendary Egg",  "$9.99", ICON_CHEST_SPECIAL, "BEST VALUE"),
        };

        private static GameObject _root;
        private static Transform  _card;
        private static Transform  _contentParent;   // scroll content, rebuilt per category
        private static TMP_Text   _coinChipTxt;      // top-bar gold counter — refreshed on purchase
        private static MonoBehaviour _runner;
        private class StoreRunner : MonoBehaviour {}
        private static readonly Dictionary<Cat, Image> _tabBgs = new Dictionary<Cat, Image>();
        private static readonly Dictionary<Cat, TMP_Text> _tabLbls = new Dictionary<Cat, TMP_Text>();

        /// <summary>Open the store on a specific tab (e.g., Cat.Coins from a "+" tap).</summary>
        public static void Show(Cat startCat)
        {
            _cat = startCat;
            _showWithCat = true;   // tells parameterless Show() to keep _cat
            if (_root != null) Hide();   // force a clean reopen on the requested tab
            Show();
        }

        // Per-call flag — set true by Show(Cat) so the parameterless Show() body
        // can tell a deep-link from a default open and not reset _cat.
        private static bool _showWithCat;

        public static void Show()
        {
            if (_root != null) { Hide(); return; }
            // Default entries (bottom-nav STORE tab) always land on Featured.
            // Show(Cat) is the deep-link path; it sets _cat then calls into here,
            // so we mustn't clobber it. Distinguish via _showWithCat below.
            if (!_showWithCat) _cat = Cat.Featured;
            _showWithCat = false;
            EnsureEventSystem();
            _tabBgs.Clear(); _tabLbls.Clear();

            _root = new GameObject("Sparq_StorePanel",
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

            var dim = NewGO("Dim", _root.transform, typeof(Image), typeof(Button));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.08f, 0.92f);
            dim.GetComponent<Button>().onClick.AddListener(Hide);

            GameObject card;
            var prefab = LoadLayerLabPrefab(POPUP_PREFAB);
            if (prefab != null)
            {
                var inst = UnityEngine.Object.Instantiate(prefab, _root.transform);
                inst.name = "Card";
                card = inst;
                var crt = inst.GetComponent<RectTransform>() ?? inst.AddComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = new Vector2(980, 1560);
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                {
                    if (t == null) continue;
                    var n = t.gameObject.name;
                    if (n == "Text_Info" || n == "Button_OK" || n == "Content_Demo")
                        t.gameObject.SetActive(false);
                }
                foreach (var tmp in inst.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp != null && tmp.gameObject.name == "Text_Title")
                    {
                        tmp.text = "Store";
                        tmp.fontSize = 54;
                        tmp.alignment = TextAlignmentOptions.MidlineLeft;
                        tmp.color = CREAM;
                        // Constrain the title to the left third so it can never
                        // run into the currency chips on the right.
                        var trt = tmp.rectTransform;
                        trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(0, 1);
                        trt.pivot = new Vector2(0, 1);
                        trt.anchoredPosition = new Vector2(44, -42);
                        trt.sizeDelta = new Vector2(320, 80);
                        try { tmp.outlineWidth = 0.18f; tmp.outlineColor = new Color(0.05f, 0.03f, 0.10f); } catch {}
                    }
                }
                foreach (var img in inst.GetComponentsInChildren<Image>(true))
                    if (img != null && img.gameObject.name == "Bg") img.color = CARD_BG;
            }
            else
            {
                card = NewGO("Card", _root.transform, typeof(Image));
                var crt = card.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = Vector2.zero;
                crt.sizeDelta = new Vector2(980, 1560);
                card.GetComponent<Image>().color = CARD_BG;
                var fbTitle = MakeText(card.transform, "Title", "Store", 60, FontStyles.Bold, CREAM);
                var fbRT = fbTitle.rectTransform;
                fbRT.anchorMin = new Vector2(0, 1); fbRT.anchorMax = new Vector2(1, 1);
                fbRT.pivot = new Vector2(0.5f, 1);
                fbRT.offsetMin = new Vector2(48, -140); fbRT.offsetMax = new Vector2(-48, -40);
                fbTitle.alignment = TextAlignmentOptions.MidlineLeft;
            }
            _card = card.transform;

            // Currency bar (coins + gems) — top-right beside the title.
            BuildCurrencyBar(card.transform);

            // Back chevron.
            var back = NewGO("Back", card.transform, typeof(Image), typeof(Button));
            var bRT = back.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(1, 1); bRT.anchorMax = new Vector2(1, 1);
            bRT.pivot = new Vector2(1, 1);
            bRT.anchoredPosition = new Vector2(-26, -26);
            bRT.sizeDelta = new Vector2(80, 80);
            back.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            back.GetComponent<Image>().raycastTarget = true;
            var bBtn = back.GetComponent<Button>();
            bBtn.targetGraphic = back.GetComponent<Image>(); bBtn.interactable = true;
            var bLbl = MakeText(back.transform, "L", "<", 52, FontStyles.Bold, CREAM);
            Stretch(bLbl.rectTransform); bLbl.alignment = TextAlignmentOptions.Center;
            bBtn.onClick.AddListener(Hide);

            BuildCategoryRail(card.transform);
            BuildScrollArea(card.transform);
            RebuildOffers();
            Debug.Log("[StorePanel] Opened.");
        }

        public static void Hide()
        {
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
            _card = null; _contentParent = null; _runner = null; _coinChipTxt = null;
            _tabBgs.Clear(); _tabLbls.Clear();
        }

        // ── Currency bar ──────────────────────────────────────────────────
        private static void BuildCurrencyBar(Transform card)
        {
            int coins = 0; try { coins = Sparq.Core.SaveService.Data?.sparqCoins ?? 0; } catch {}
            // Gems are a placeholder currency (no PlayerData field yet) → 0.
            // Both chips packed against the right edge so they clear the "Store"
            // title on the left (the gem chip was crowding it before).
            _coinChipTxt = BuildCurrencyChip(card, ICON_COIN, coins.ToString("N0"), GOLD,    new Vector2(-130, -44));
            BuildCurrencyChip(card, ICON_GEM,  "0",                  GEM_BLUE, new Vector2(-370, -44));
        }

        // Refresh the gold counter after a Forge purchase.
        private static void RefreshCurrency()
        {
            if (_coinChipTxt == null) return;
            int coins = 0; try { coins = Sparq.Core.SaveService.Data?.sparqCoins ?? 0; } catch {}
            _coinChipTxt.text = coins.ToString("N0");
        }

        private static TMP_Text BuildCurrencyChip(Transform card, string iconPath, string value, Color valColor, Vector2 anchoredPos)
        {
            var chip = NewGO("Chip", card, typeof(Image));
            var rt = chip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(228, 62);
            var img = chip.GetComponent<Image>();
            var sp = LoadSprite(BTN_CONVEX);
            if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
            img.color = new Color(0.09f, 0.09f, 0.12f, 0.95f);
            img.raycastTarget = false;

            var ico = NewGO("Ico", chip.transform, typeof(Image));
            var iRT = ico.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0, 0.5f); iRT.anchorMax = new Vector2(0, 0.5f);
            iRT.pivot = new Vector2(0, 0.5f);
            iRT.anchoredPosition = new Vector2(8, 0);
            iRT.sizeDelta = new Vector2(50, 50);
            var iImg = ico.GetComponent<Image>();
            var icoSp = LoadSprite(iconPath);
            if (icoSp != null) { iImg.sprite = icoSp; iImg.preserveAspect = true; }
            else iImg.color = valColor;
            iImg.raycastTarget = false;

            var txt = MakeText(chip.transform, "T", value, 32, FontStyles.Bold, valColor);
            try { txt.outlineWidth = 0.20f; txt.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var tRT = txt.rectTransform;
            tRT.anchorMin = new Vector2(0, 0); tRT.anchorMax = new Vector2(1, 1);
            tRT.offsetMin = new Vector2(64, 0); tRT.offsetMax = new Vector2(-14, 0);
            txt.alignment = TextAlignmentOptions.MidlineRight;
            return txt;
        }

        // ── Left category rail ──────────────────────────────────────────────
        private static void BuildCategoryRail(Transform card)
        {
            var rail = NewGO("Rail", card, typeof(Image), typeof(VerticalLayoutGroup));
            var rt = rail.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.offsetMin = new Vector2(20, 30); rt.offsetMax = new Vector2(20, -150);
            rt.sizeDelta = new Vector2(210, rt.sizeDelta.y);
            rail.GetComponent<Image>().color = RAIL_BG;
            var vlg = rail.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 14;
            vlg.padding = new RectOffset(14, 14, 18, 18);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true; vlg.childControlHeight = true;

            BuildCatTab(rail.transform, Cat.Featured, "Featured", ICON_CROWN);
            BuildCatTab(rail.transform, Cat.Forge,    "Forge",    ICON_CHEST_GOLD);
            BuildCatTab(rail.transform, Cat.Gems,     "Gems",     ICON_GEM);
            BuildCatTab(rail.transform, Cat.Coins,    "Coins",    ICON_COIN);
            BuildCatTab(rail.transform, Cat.Bundles,  "Bundles",  ICON_GIFT_PURPLE);
        }

        private static void BuildCatTab(Transform rail, Cat cat, string label, string iconPath)
        {
            var go = NewGO("Cat_" + cat, rail, typeof(Image), typeof(Button), typeof(LayoutElement));
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = 150; le.minHeight = 150;
            var img = go.GetComponent<Image>();
            var sp = LoadSprite(BTN_CONVEX);
            if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
            img.color = (cat == _cat) ? TAB_ON : TAB_OFF;
            img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            btn.onClick.AddListener(() => {
                if (_cat == cat) return;
                _cat = cat;
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                ApplyTabStyles();
                RebuildOffers();
            });

            var ico = NewGO("Ico", go.transform, typeof(Image));
            var iRT = ico.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0.5f, 1); iRT.anchorMax = new Vector2(0.5f, 1);
            iRT.pivot = new Vector2(0.5f, 1);
            iRT.anchoredPosition = new Vector2(0, -14);
            iRT.sizeDelta = new Vector2(76, 76);
            var iImg = ico.GetComponent<Image>();
            var icoSp = LoadSprite(iconPath);
            if (icoSp != null) { iImg.sprite = icoSp; iImg.preserveAspect = true; iImg.color = Color.white; }
            iImg.raycastTarget = false;

            var lbl = MakeText(go.transform, "L", label, 26, FontStyles.Bold, CREAM);
            try { lbl.outlineWidth = 0.22f; lbl.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var lRT = lbl.rectTransform;
            lRT.anchorMin = new Vector2(0, 0); lRT.anchorMax = new Vector2(1, 0);
            lRT.pivot = new Vector2(0.5f, 0);
            lRT.anchoredPosition = new Vector2(0, 12);
            lRT.sizeDelta = new Vector2(-6, 40);
            lbl.alignment = TextAlignmentOptions.Center;

            _tabBgs[cat] = img;
            _tabLbls[cat] = lbl;
        }

        private static void ApplyTabStyles()
        {
            foreach (var kv in _tabBgs)
                if (kv.Value != null) kv.Value.color = (kv.Key == _cat) ? TAB_ON : TAB_OFF;
        }

        // ── Right scroll area ────────────────────────────────────────────────
        private static void BuildScrollArea(Transform card)
        {
            var scrollGO = NewGO("Scroll", card, typeof(Image), typeof(ScrollRect));
            var srRT = scrollGO.GetComponent<RectTransform>();
            srRT.anchorMin = new Vector2(0, 0); srRT.anchorMax = new Vector2(1, 1);
            // Left edge clears the 210-wide rail (+20 left margin +20 gap), top
            // clears the title/currency bar, bottom small margin.
            srRT.offsetMin = new Vector2(260, 30); srRT.offsetMax = new Vector2(-24, -150);
            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 32f;

            var vp = NewGO("VP", scrollGO.transform, typeof(Image), typeof(RectMask2D));
            Stretch(vp.GetComponent<RectTransform>());
            vp.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            var content = NewGO("Content", vp.transform, typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            var ctRT = content.GetComponent<RectTransform>();
            ctRT.anchorMin = new Vector2(0, 1); ctRT.anchorMax = new Vector2(1, 1);
            ctRT.pivot = new Vector2(0.5f, 1);
            var glg = content.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(330, 430);
            glg.spacing = new Vector2(16, 16);
            glg.padding = new RectOffset(4, 4, 4, 4);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2;
            glg.childAlignment = TextAnchor.UpperCenter;
            var fit = content.GetComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vp.GetComponent<RectTransform>(); sr.content = ctRT;
            _contentParent = content.transform;
        }

        private static void RebuildOffers()
        {
            if (_contentParent == null) return;
            for (int i = _contentParent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_contentParent.GetChild(i).gameObject);

            Offer[] offers;
            switch (_cat)
            {
                case Cat.Forge:   offers = FORGE;   break;
                case Cat.Gems:    offers = GEMS;    break;
                case Cat.Coins:   offers = COINS;   break;
                case Cat.Bundles: offers = BUNDLES; break;
                default:          offers = FEATURED; break;
            }
            foreach (var o in offers) BuildOfferCard(_contentParent, o);
        }

        // ── Single offer card (icon + title + contents + price button) ───────
        private static void BuildOfferCard(Transform parent, Offer o)
        {
            var card = NewGO("Offer_" + o.title, parent, typeof(Image), typeof(Button));
            var img = card.GetComponent<Image>();
            var sp = LoadSprite(BTN_CONVEX);
            if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
            img.color = OFFER_BG;
            img.raycastTarget = true;
            var btn = card.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            btn.onClick.AddListener(() => OnOfferTapped(o));

            // Optional corner badge
            if (!string.IsNullOrEmpty(o.badge))
            {
                var badge = NewGO("Badge", card.transform, typeof(Image));
                var bRT = badge.GetComponent<RectTransform>();
                bRT.anchorMin = new Vector2(0, 1); bRT.anchorMax = new Vector2(1, 1);
                bRT.pivot = new Vector2(0.5f, 1);
                bRT.anchoredPosition = new Vector2(0, 0);
                bRT.sizeDelta = new Vector2(0, 44);
                var bImg = badge.GetComponent<Image>();
                if (sp != null) { bImg.sprite = sp; bImg.type = Image.Type.Sliced; }
                bImg.color = BADGE_RED;
                bImg.raycastTarget = false;
                var bTxt = MakeText(badge.transform, "T", o.badge, 22, FontStyles.Bold, Color.white);
                try { bTxt.outlineWidth = 0.20f; bTxt.outlineColor = new Color(0, 0, 0, 0.8f); } catch {}
                Stretch(bTxt.rectTransform); bTxt.alignment = TextAlignmentOptions.Center;
                bTxt.characterSpacing = 2f;
            }

            // Icon
            var ico = NewGO("Ico", card.transform, typeof(Image));
            var iRT = ico.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0.5f, 1); iRT.anchorMax = new Vector2(0.5f, 1);
            iRT.pivot = new Vector2(0.5f, 1);
            iRT.anchoredPosition = new Vector2(0, -56);
            iRT.sizeDelta = new Vector2(140, 140);
            var iImg = ico.GetComponent<Image>();
            var icoSp = LoadSprite(o.icon);
            if (icoSp != null) { iImg.sprite = icoSp; iImg.preserveAspect = true; iImg.color = Color.white; }
            else iImg.color = GOLD;
            iImg.raycastTarget = false;

            // Title
            var title = MakeText(card.transform, "Title", o.title, 28, FontStyles.Bold, CREAM);
            try { title.outlineWidth = 0.22f; title.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var tRT = title.rectTransform;
            tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.anchoredPosition = new Vector2(0, -204);
            tRT.sizeDelta = new Vector2(-16, 40);
            title.alignment = TextAlignmentOptions.Center;

            // Contents (multi-line) — sits between the title and the price
            // button. Box is 96px tall (≈3 lines @22pt) and ends clear of the
            // price pill anchored at the card floor.
            var contents = MakeText(card.transform, "Contents", o.contents, 22, FontStyles.Bold, INK_SOFT);
            var cRT = contents.rectTransform;
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.anchoredPosition = new Vector2(0, -244);
            cRT.sizeDelta = new Vector2(-20, 92);
            contents.alignment = TextAlignmentOptions.Top;
            contents.textWrappingMode = TextWrappingModes.Normal;

            // Price button (bottom)
            var price = NewGO("Price", card.transform, typeof(Image));
            var prRT = price.GetComponent<RectTransform>();
            prRT.anchorMin = new Vector2(0.5f, 0); prRT.anchorMax = new Vector2(0.5f, 0);
            prRT.pivot = new Vector2(0.5f, 0);
            prRT.anchoredPosition = new Vector2(0, 14);
            prRT.sizeDelta = new Vector2(270, 70);
            var prImg = price.GetComponent<Image>();
            if (sp != null) { prImg.sprite = sp; prImg.type = Image.Type.Sliced; }
            prImg.color = BUY_GREEN;
            prImg.raycastTarget = false;
            var prTxt = MakeText(price.transform, "P", o.price, 30, FontStyles.Bold, Color.white);
            try { prTxt.outlineWidth = 0.22f; prTxt.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}
            Stretch(prTxt.rectTransform); prTxt.alignment = TextAlignmentOptions.Center;
        }

        private static void OnOfferTapped(Offer o)
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}

            // Gold-sink offers are real: spend coins, grant the item.
            if (o.goldCost > 0) { BuyWithGold(o); return; }

            // IAP offers are still stubbed until a real store SDK is wired.
            if (o.price == "FREE")
                Toast($"{o.title} — daily rewards coming soon!");
            else
                Toast($"{o.title} ({o.price}) — purchases coming soon!");
        }

        // Spend in-game gold for a Forge item. The gold sink that gives all those
        // AFK/loot coins a purpose, feeding the gear + pet loops.
        private static void BuyWithGold(Offer o)
        {
            var d = Sparq.Core.SaveService.Data;
            if (d == null) return;
            if (d.sparqCoins < o.goldCost)
            {
                Toast($"Not enough gold — need {o.goldCost:N0}.");
                return;
            }

            d.sparqCoins -= o.goldCost;
            string result;
            switch (o.sku)
            {
                case "gear":
                {
                    var item = Sparq.Systems.EquipmentService.RollLoot(d.level);
                    if (item != null) { Sparq.Systems.EquipmentService.Grant(item.id); result = $"Got {item.name}!"; }
                    else result = "Got a gear item!";
                    break;
                }
                case "gear_epic":
                {
                    // Best of 4 rolls — skews toward higher rarity, like a boss drop.
                    var best = Sparq.Systems.EquipmentService.RollLoot(d.level);
                    for (int i = 1; i < 4; i++)
                    {
                        var alt = Sparq.Systems.EquipmentService.RollLoot(d.level);
                        if (alt != null && best != null && (int)alt.rarity > (int)best.rarity) best = alt;
                    }
                    if (best != null) { Sparq.Systems.EquipmentService.Grant(best.id); result = $"Got {best.name}!"; }
                    else result = "Got a gear item!";
                    break;
                }
                case "egg":
                {
                    if (d.eggInventory == null) d.eggInventory = new System.Collections.Generic.List<string>();
                    d.eggInventory.Add("rare");
                    result = "Got a Mystery Egg!";
                    break;
                }
                default: result = "Purchased!"; break;
            }

            try { Sparq.Core.SaveService.Save(); } catch {}
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin); } catch {}
            RefreshCurrency();
            Toast(result);
        }

        // ── Toast ─────────────────────────────────────────────────────────
        private static void Toast(string message)
        {
            if (_card == null) return;
            EnsureRunner();
            var go = NewGO("Toast", _card, typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 50);
            rt.sizeDelta = new Vector2(800, 92);
            var img = go.GetComponent<Image>();
            var sp = LoadSprite(BTN_CONVEX);
            if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
            img.color = new Color(0.08f, 0.08f, 0.11f, 0.97f);
            img.raycastTarget = false;
            var txt = MakeText(go.transform, "T", message, 26, FontStyles.Bold, CREAM);
            Stretch(txt.rectTransform); txt.alignment = TextAlignmentOptions.Center;
            txt.textWrappingMode = TextWrappingModes.Normal;
            try { txt.outlineWidth = 0.18f; txt.outlineColor = new Color(0, 0, 0, 0.8f); } catch {}
            if (_runner != null) _runner.StartCoroutine(ToastFade(go, txt, img));
        }

        private static System.Collections.IEnumerator ToastFade(GameObject go, TMP_Text txt, Image img)
        {
            float t = 0f; const float life = 2.0f;
            while (t < life && go != null)
            {
                t += Time.unscaledDeltaTime;
                float a = t < life - 0.5f ? 1f : Mathf.Lerp(1f, 0f, (t - (life - 0.5f)) / 0.5f);
                if (txt != null) { var c = txt.color; c.a = a; txt.color = c; }
                if (img != null) { var c = img.color; c.a = a * 0.97f; img.color = c; }
                yield return null;
            }
            if (go != null) UnityEngine.Object.Destroy(go);
        }

        private static void EnsureRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("StorePanel_Runner");
            if (_root != null) go.transform.SetParent(_root.transform, false);
            _runner = go.AddComponent<StoreRunner>();
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

        private static GameObject LoadLayerLabPrefab(string path)
        {
            // Try Resources first (works in APK + Editor). Strip "Assets/" prefix
            // and ".prefab" suffix to get Resources-relative path.
            string r = path;
            if (r.StartsWith("Assets/")) r = r.Substring(7);
            if (r.EndsWith(".prefab")) r = r.Substring(0, r.Length - 7);
            var go = Resources.Load<GameObject>(r);
            if (go != null) return go;
#if UNITY_EDITOR
            try { return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path); } catch {}
#endif
            return null;
        }

        private static Sprite LoadSprite(string path) => Sparq.Core.SpriteLoader.Load(path);

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
