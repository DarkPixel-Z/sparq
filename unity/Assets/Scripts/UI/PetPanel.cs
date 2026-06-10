using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Pet companion screen — one active companion (Finch-style) with feed,
    /// gear (3 slots), roster swap, and a shop for new pets + items.
    /// </summary>
    public static class PetPanel
    {
        private static readonly Color GOLD       = new Color(1f, 0.82f, 0.30f);
        // Palette aligned with PetCarePanel — charcoal card on dim dim,
        // gold accents, cream text. Previously purple-on-cream which made
        // the two pet panels feel like they belonged to different games.
        private static readonly Color CREAM      = new Color(1.00f, 0.97f, 0.85f);
        private static readonly Color DEEP_NAVY  = new Color(0.11f, 0.13f, 0.16f);  // ink
        private static readonly Color CARD_BG    = new Color(0.17f, 0.17f, 0.20f, 1f);  // charcoal card
        private static readonly Color TITLE_BG   = new Color(0.22f, 0.22f, 0.28f, 1f);  // dark slate banner
        private static readonly Color BANNER_BG  = new Color(0.24f, 0.24f, 0.30f, 1f);  // slightly lighter slate
        private static readonly Color ROW_BG     = new Color(0.22f, 0.22f, 0.28f, 1f);  // row in scroll list
        private static readonly Color SLOT_BG    = new Color(0.13f, 0.13f, 0.17f, 1f);  // empty slot well
        private static readonly Color HUNGER_FG  = new Color(1.00f, 0.55f, 0.28f);
        // Light parchment used inside the hero card. Kept warm so the
        // colorful pet sprite still pops, but desaturated.
        private static readonly Color HERO_PARCH = new Color(0.94f, 0.92f, 0.86f, 1f);

        public enum Tab { Gear, Roster, Shop }
        private static Tab _tab = Tab.Gear;

        // Convenience for callers that want to land on a specific tab.
        // (e.g. PetCarePanel's Manage tile → Roster; Shop tile → Shop.)
        public static void Show(Tab initialTab)
        {
            _tab = initialTab;
            Show();
        }

        private static GameObject _root;
        private static Transform _listParent, _slotsParent, _heroParent;
        private static TMP_Text _nameTm, _lvTm, _statsTm, _hungerTm, _coinsTm;
        private static Slider _hungerBar;
        private static RectTransform _petRT;     // tracked for feed-time heart spawns + scale-bump
        private static Image[] _slotIcons;
        private static TMP_Text[] _slotLabels;
        private static Button _gearTabBtn, _rosterTabBtn, _shopTabBtn;
        private static readonly Dictionary<int, Sprite> _roundedCache = new Dictionary<int, Sprite>();
        private static Sprite _circleSp;

        public static void Show()
        {
            if (_root != null) { Hide(); return; }

            _root = new GameObject("PetPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 14600;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Dim
            var dim = MakeImage(_root.transform, "Dim", new Color(0, 0, 0, 0.85f));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Hide);

            // Stroke + card
            var stroke = MakeRounded(_root.transform, "Stroke", TITLE_BG, 30);
            var srt = stroke.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(36, 136); srt.offsetMax = new Vector2(-36, -76);

            var card = MakeRounded(_root.transform, "Card", CARD_BG, 28);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 0); crt.anchorMax = new Vector2(1, 1);
            crt.offsetMin = new Vector2(40, 140); crt.offsetMax = new Vector2(-40, -80);
            BuildFunkyBackdrop(card.transform);

            // Title bar + back
            var titleShadow = MakeRounded(card.transform, "TitleShadow", new Color(0, 0, 0, 0.35f), 24);
            var tshrt = titleShadow.GetComponent<RectTransform>();
            tshrt.anchorMin = new Vector2(0, 1); tshrt.anchorMax = new Vector2(1, 1);
            tshrt.pivot = new Vector2(0.5f, 1f);
            tshrt.anchoredPosition = new Vector2(0, -26);
            tshrt.sizeDelta = new Vector2(-40, 110);

            var titleBar = MakeRounded(card.transform, "TitleBar", TITLE_BG, 24);
            var tbrt = titleBar.GetComponent<RectTransform>();
            tbrt.anchorMin = new Vector2(0, 1); tbrt.anchorMax = new Vector2(1, 1);
            tbrt.pivot = new Vector2(0.5f, 1f);
            tbrt.anchoredPosition = new Vector2(0, -20);
            tbrt.sizeDelta = new Vector2(-40, 110);
            var title = MakeText(titleBar.transform, "Title", "PETS",
                52, FontStyles.Bold, new Color(1f, 0.92f, 0.55f),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            title.alignment = TextAlignmentOptions.Center;
            title.outlineWidth = 0.28f;
            title.outlineColor = new Color(0.45f, 0.05f, 0.22f, 1f);

            var backBtn = MakeBtn(card.transform, "BackBtn", "←  BACK",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-115, -38), new Vector2(190, 80),
                GOLD, DEEP_NAVY, 28);
            backBtn.onClick.AddListener(Hide);
            var bImg = backBtn.GetComponent<Image>();
            bImg.sprite = LoadRoundedSprite(28); bImg.type = Image.Type.Sliced;
            var bLbl = backBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (bLbl != null) { bLbl.fontStyle = FontStyles.Bold; bLbl.outlineWidth = 0.22f; bLbl.outlineColor = new Color(1f, 0.95f, 0.7f); }

            // Coin chip — matches home-page BrightPill style: bright gold pill, dark "G" glyph circle, navy text
            var coinChip = MakeRounded(card.transform, "Coins", GOLD, 14);
            var ccrt = coinChip.GetComponent<RectTransform>();
            ccrt.anchorMin = new Vector2(0, 1); ccrt.anchorMax = new Vector2(0, 1);
            ccrt.pivot = new Vector2(0, 1);
            ccrt.anchoredPosition = new Vector2(75, -55);
            ccrt.sizeDelta = new Vector2(150, 56);

            // Real Layer Lab gold coin sprite on the left
            var glyph = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
            glyph.transform.SetParent(coinChip.transform, false);
            var grt = glyph.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0, 0.5f); grt.anchorMax = new Vector2(0, 0.5f);
            grt.pivot = new Vector2(0, 0.5f);
            grt.anchoredPosition = new Vector2(6, 0);
            grt.sizeDelta = new Vector2(46, 46);
            var glyphImg = glyph.GetComponent<Image>();
            glyphImg.preserveAspect = true;
            #if UNITY_EDITOR
            const string COIN_PATH = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/ItemIcon_Coin_Gold.png";
            var coinSp = Sparq.Core.SpriteLoader.Load(COIN_PATH);
            if (coinSp == null && !Application.isPlaying)
            {
                var imp = UnityEditor.AssetImporter.GetAtPath(COIN_PATH) as UnityEditor.TextureImporter;
                if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite)
                {
                    imp.textureType = UnityEditor.TextureImporterType.Sprite;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                    coinSp = Sparq.Core.SpriteLoader.Load(COIN_PATH);
                }
            }
            if (coinSp != null) glyphImg.sprite = coinSp;
            else { glyphImg.sprite = LoadCircleSprite(); glyphImg.color = new Color(0.95f, 0.65f, 0.10f); }
            #else
            glyphImg.sprite = LoadCircleSprite(); glyphImg.color = new Color(0.95f, 0.65f, 0.10f);
            #endif

            // Value text — navy with cream halo for that home-page punch
            _coinsTm = MakeText(coinChip.transform, "CT", $"{Sparq.Systems.PetService.Coins():N0}",
                26, FontStyles.Bold, DEEP_NAVY,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            _coinsTm.alignment = TextAlignmentOptions.MidlineLeft;
            var ccTRT = _coinsTm.rectTransform;
            ccTRT.anchorMin = new Vector2(0, 0); ccTRT.anchorMax = new Vector2(1, 1);
            ccTRT.offsetMin = new Vector2(56, 0); ccTRT.offsetMax = new Vector2(-12, 0);
            _coinsTm.outlineWidth = 0.18f; _coinsTm.outlineColor = new Color(1, 1, 1, 0.7f);

            // ── Hero card: portrait + name + level + hunger ──
            BuildHeroSection(card.transform);

            // ── Tab buttons row ──
            BuildTabRow(card.transform);

            // ── Scroll list (content depends on _tab) ──
            BuildScrollList(card.transform);

            // (FEED button moved inside the hero card — see BuildHeroContent)

            try { Sparq.Systems.PetService.OnChanged += Refresh; } catch {}
            Refresh();
        }

        public static void Hide()
        {
            try { Sparq.Systems.PetService.OnChanged -= Refresh; } catch {}
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            // Re-open the lobby — Sparq's true home page now.
            try { Sparq.UI.HomeLobbyPanel.Show(); }
            catch (System.Exception ex) { Debug.LogError($"[PetPanel] Failed to reopen lobby: {ex.Message}"); }
        }

        // ───────── Hero section ─────────
        private static void BuildHeroSection(Transform parent)
        {
            var hero = MakeRounded(parent, "Hero", BANNER_BG, 22);
            var hrt = hero.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0, -160);
            hrt.sizeDelta = new Vector2(-40, 540);
            _heroParent = hero.transform;
        }

        private static void BuildHeroContent()
        {
            // Wipe first
            for (int i = _heroParent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_heroParent.GetChild(i).gameObject);

            var p = Sparq.Systems.PetService.Active();
            if (p == null) return;
            var sp = Sparq.Systems.PetService.FindSpecies(p.speciesId);
            var stats = Sparq.Systems.PetService.StatsOf(p);

            // Outer gold border (rendered first so it sits behind the cream body, showing as a frame)
            var border = MakeRounded(_heroParent, "Border", GOLD, 24);
            var brRT = border.GetComponent<RectTransform>();
            brRT.anchorMin = Vector2.zero; brRT.anchorMax = Vector2.one;
            brRT.pivot = new Vector2(0.5f, 0.5f);
            brRT.offsetMin = new Vector2(20, 10); brRT.offsetMax = new Vector2(-20, -10);

            // Outer card — charcoal "stage" matching PetCarePanel theme.
            // Was cream parchment; now reads as a dark portrait studio.
            var card = MakeRounded(_heroParent, "Card", CARD_BG, 22);
            var cRT = card.GetComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one;
            cRT.pivot = new Vector2(0.5f, 0.5f);
            cRT.offsetMin = new Vector2(26, 16); cRT.offsetMax = new Vector2(-26, -16);

            // Top header band (rarity-tinted, fades from element color)
            Color rarityColor = RarityColor(sp.rarity);
            var hdr = MakeRounded(card.transform, "Hdr", rarityColor, 18);
            var hRT = hdr.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1f);
            hRT.anchoredPosition = new Vector2(0, -12);
            hRT.sizeDelta = new Vector2(-24, 64);

            // Pet name (left side of header) — sized to match the bigger LV pill
            _nameTm = MakeText(hdr.transform, "Name", p.nickname,
                44, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(0.62f, 1), new Vector2(24, 0), Vector2.zero);
            _nameTm.alignment = TextAlignmentOptions.MidlineLeft;
            _nameTm.outlineWidth = 0.32f;
            _nameTm.outlineColor = new Color(0.10f, 0.05f, 0.18f, 1f);
            _nameTm.enableAutoSizing = true; _nameTm.fontSizeMin = 30; _nameTm.fontSizeMax = 46;

            // LV pill — bigger so the level reads at a glance.
            var lvPill = MakeRounded(hdr.transform, "Lv", GOLD, 14);
            var lpRT = lvPill.GetComponent<RectTransform>();
            lpRT.anchorMin = new Vector2(1, 0.5f); lpRT.anchorMax = new Vector2(1, 0.5f);
            lpRT.pivot = new Vector2(1, 0.5f);
            lpRT.anchoredPosition = new Vector2(-16, 0);
            lpRT.sizeDelta = new Vector2(140, 54);
            _lvTm = MakeText(lvPill.transform, "LT", $"LV {p.level}",
                32, FontStyles.Bold, DEEP_NAVY,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            _lvTm.alignment = TextAlignmentOptions.Center;
            _lvTm.outlineWidth = 0.22f; _lvTm.outlineColor = new Color(1f, 0.95f, 0.7f);

            // Image frame — center area with element-tinted radial bg
            var frame = MakeRounded(card.transform, "Frame",
                Color.Lerp(sp.tint, Color.white, 0.55f), 14);
            var fRT = frame.GetComponent<RectTransform>();
            fRT.anchorMin = new Vector2(0, 0); fRT.anchorMax = new Vector2(1, 1);
            fRT.offsetMin = new Vector2(170, 184); fRT.offsetMax = new Vector2(-20, -82);

            // Inner soft glow
            var glow = MakeRounded(frame.transform, "Glow",
                Color.Lerp(sp.tint, Color.white, 0.85f), 80);
            var gRT = glow.GetComponent<RectTransform>();
            gRT.anchorMin = new Vector2(0.5f, 0.5f); gRT.anchorMax = new Vector2(0.5f, 0.5f);
            gRT.pivot = new Vector2(0.5f, 0.5f);
            gRT.sizeDelta = new Vector2(180, 180);
            var gImg = glow.GetComponent<Image>();
            gImg.sprite = LoadCircleSprite();
            gImg.type = Image.Type.Simple;
            gImg.raycastTarget = false;

            // Pet sprite — alpha-cropped via HeroPortrait so the figure fills
            // its container instead of being drowned by transparent PNG padding.
            // Was previously raw-loaded which made the slime look tiny.
            var pet = new GameObject("Sprite", typeof(RectTransform), typeof(Image));
            pet.transform.SetParent(frame.transform, false);
            var pRT = pet.GetComponent<RectTransform>();
            pRT.anchorMin = Vector2.zero; pRT.anchorMax = Vector2.one;
            // Tight inset — was 20/16, now 6/6 so the cropped sprite reaches the frame edges.
            pRT.offsetMin = new Vector2(6, 6); pRT.offsetMax = new Vector2(-6, -6);
            var pImg = pet.GetComponent<Image>();
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;
            _petRT = pRT;     // remember for Tamagotchi feedback later
            #if UNITY_EDITOR
            if (!string.IsNullOrEmpty(sp.spritePath))
            {
                Sprite cropped = null;
                try { cropped = Sparq.UI.HeroPortrait.LoadCropped(sp.spritePath); } catch {}
                if (cropped != null) pImg.sprite = cropped;
                else
                {
                    // Fall back to raw asset if the crop helper isn't available.
                    var imp = UnityEditor.AssetImporter.GetAtPath(sp.spritePath) as UnityEditor.TextureImporter;
                    if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite && !Application.isPlaying)
                    {
                        imp.textureType = UnityEditor.TextureImporterType.Sprite;
                        imp.alphaIsTransparency = true;
                        imp.SaveAndReimport();
                    }
                    var psp = Sparq.Core.SpriteLoader.Load(sp.spritePath);
                    if (psp != null) pImg.sprite = psp;
                }
            }
            #endif
            if (pImg.sprite == null)
            {
                // Fallback: tinted disc with letter
                pImg.sprite = LoadCircleSprite();
                pImg.color = sp.tint;
                var fb = MakeText(pet.transform, "Letter", sp.name.Substring(0, 1).ToUpper(),
                    100, FontStyles.Bold, Color.white,
                    new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
                fb.alignment = TextAlignmentOptions.Center;
                fb.outlineWidth = 0.30f;
                fb.outlineColor = new Color(0.10f, 0.05f, 0.18f, 0.85f);
            }

            // Element + rarity ribbon (top-right corner of frame) — sized so
            // both words actually read instead of the tiny 14pt strip.
            var elemRibbon = MakeRounded(frame.transform, "Elem",
                rarityColor, 12);
            var erRT = elemRibbon.GetComponent<RectTransform>();
            erRT.anchorMin = new Vector2(1, 1); erRT.anchorMax = new Vector2(1, 1);
            erRT.pivot = new Vector2(1, 1);
            erRT.anchoredPosition = new Vector2(-10, -10);
            erRT.sizeDelta = new Vector2(190, 46);
            var erTm = MakeText(elemRibbon.transform, "ET",
                $"{sp.element} · {sp.rarity}",
                22, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            erTm.alignment = TextAlignmentOptions.Center;
            erTm.characterSpacing = 2f;
            erTm.outlineWidth = 0.28f;
            erTm.outlineColor = new Color(0.05f, 0.03f, 0.10f, 1f);

            // Bottom stats panel — slate band matching the title bar, so the
            // ATK/DEF/HP chips sit on a coherent dark theme.
            var bottom = MakeRounded(card.transform, "Bottom", BANNER_BG, 14);
            var bRT = bottom.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0, 0); bRT.anchorMax = new Vector2(1, 0);
            bRT.pivot = new Vector2(0.5f, 0);
            bRT.anchoredPosition = new Vector2(0, 12);
            bRT.sizeDelta = new Vector2(-24, 130);

            // Three stat chips: ATK / DEF / HP
            BuildStatChip(bottom.transform, 0.0f, 0.33f, "ATK", stats.atk, new Color(0.95f, 0.45f, 0.40f));
            BuildStatChip(bottom.transform, 0.33f, 0.66f, "DEF", stats.def, new Color(0.45f, 0.65f, 0.95f));
            BuildStatChip(bottom.transform, 0.66f, 1.0f, "HP",  stats.hp,  new Color(0.55f, 0.85f, 0.45f));

            // Hunger bar at top of bottom panel — narrow strip just below frame
            var hungerWrap = new GameObject("HungerWrap", typeof(RectTransform));
            hungerWrap.transform.SetParent(card.transform, false);
            var hwRT = hungerWrap.GetComponent<RectTransform>();
            hwRT.anchorMin = new Vector2(0, 0); hwRT.anchorMax = new Vector2(1, 0);
            hwRT.pivot = new Vector2(0.5f, 0);
            hwRT.anchoredPosition = new Vector2(0, 150);
            hwRT.sizeDelta = new Vector2(-24, 56); // taller hunger bar wrap

            var hungerLbl = MakeText(hungerWrap.transform, "HL", "HUNGER",
                28, FontStyles.Bold, CREAM,
                new Vector2(0, 0), new Vector2(0.25f, 1), Vector2.zero, Vector2.zero);
            hungerLbl.alignment = TextAlignmentOptions.MidlineLeft;
            hungerLbl.characterSpacing = 5f;
            hungerLbl.outlineWidth = 0.24f;
            hungerLbl.outlineColor = new Color(0, 0, 0, 0.95f);

            var track = MakeRounded(hungerWrap.transform, "Track", new Color(0.30f, 0.20f, 0.10f, 0.95f), 14);
            var trRT = track.GetComponent<RectTransform>();
            trRT.anchorMin = new Vector2(0.25f, 0); trRT.anchorMax = new Vector2(0.95f, 1);
            trRT.offsetMin = new Vector2(0, 6); trRT.offsetMax = new Vector2(0, -6);

            var fill = MakeRounded(track.transform, "Fill", HUNGER_FG, 12);
            var fxrt = fill.GetComponent<RectTransform>();
            float pct = Mathf.Clamp01(p.hunger / 100f);
            fxrt.anchorMin = new Vector2(0, 0); fxrt.anchorMax = new Vector2(pct, 1);
            fxrt.offsetMin = new Vector2(3, 3); fxrt.offsetMax = new Vector2(-3, -3);

            _hungerTm = MakeText(track.transform, "HT", $"{p.hunger}/100",
                30, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            _hungerTm.alignment = TextAlignmentOptions.Center;
            _hungerTm.outlineWidth = 0.32f;
            _hungerTm.outlineColor = new Color(0.05f, 0.03f, 0.10f, 1f);

            // FEED button INSIDE the hero card, anchored to the LEFT of the pet image area
            var feedInline = MakeBtn(card.transform, "FeedInline", "🍓\nFEED",
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(16, 210), new Vector2(140, 150),
                new Color(0.30f, 0.80f, 0.42f), Color.white, 28);
            feedInline.onClick.AddListener(OpenFoodPicker);
            var fImg = feedInline.GetComponent<Image>();
            fImg.sprite = LoadRoundedSprite(20); fImg.type = Image.Type.Sliced;
            var fLbl = feedInline.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (fLbl != null) { fLbl.color = DEEP_NAVY; fLbl.outlineWidth = 0.24f; fLbl.outlineColor = new Color(0.85f, 1f, 0.85f); }
        }

        private static Color RarityColor(string rarity)
        {
            switch (rarity)
            {
                case "Mythic":    return new Color(1.00f, 0.55f, 0.10f);  // hot orange-gold
                case "Legendary": return new Color(0.62f, 0.40f, 0.92f);  // royal purple
                case "Epic":      return new Color(0.95f, 0.45f, 0.50f);  // pink-red
                case "Rare":      return new Color(0.30f, 0.55f, 0.85f);  // royal blue
                default:          return new Color(0.55f, 0.60f, 0.65f);  // grey
            }
        }

        private static void BuildStatChip(Transform parent, float aMin, float aMax, string label, int val, Color tint)
        {
            var chip = MakeRounded(parent, $"C_{label}", tint, 14);
            var rt = chip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(aMin, 0); rt.anchorMax = new Vector2(aMax, 1);
            rt.offsetMin = new Vector2(8, 8); rt.offsetMax = new Vector2(-8, -8);

            // Label header band (top ~32%) — bigger label
            var lbl = MakeText(chip.transform, "L", label,
                22, FontStyles.Bold, Color.white,
                new Vector2(0, 0.65f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.characterSpacing = 4f;
            lbl.outlineWidth = 0.32f;
            lbl.outlineColor = new Color(0, 0, 0, 0.85f);

            // Value — compact size
            var v = MakeText(chip.transform, "V", val.ToString(),
                32, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 0.65f), Vector2.zero, Vector2.zero);
            v.alignment = TextAlignmentOptions.Center;
            v.outlineWidth = 0.30f;
            v.outlineColor = new Color(0, 0, 0, 0.92f);
            v.enableAutoSizing = true;
            v.fontSizeMin = 22; v.fontSizeMax = 36;
        }

        // ───────── Tab row ─────────
        private static void BuildTabRow(Transform parent)
        {
            var row = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(1, 1);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0, -720);
            rrt.sizeDelta = new Vector2(-40, 70);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            _gearTabBtn   = MakeTabBtn(row.transform, "GEAR",   new Color(0.55f, 0.85f, 0.45f), () => { _tab = Tab.Gear;   RebuildList(); });
            _rosterTabBtn = MakeTabBtn(row.transform, "ROSTER", new Color(0.55f, 0.62f, 0.95f), () => { _tab = Tab.Roster; RebuildList(); });
            _shopTabBtn   = MakeTabBtn(row.transform, "SHOP",   new Color(0.92f, 0.55f, 0.85f), () => { _tab = Tab.Shop;   RebuildList(); });
        }

        // Tab buttons keep their identity color; selected = vivid, unselected = darkened
        private static readonly Dictionary<Button, Color> _tabColors = new Dictionary<Button, Color>();

        private static Button MakeTabBtn(Transform parent, string label, Color color, System.Action onClick)
        {
            var btn = MakeBtn(parent, $"T_{label}", label,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero,
                color, Color.white, 24);
            var img = btn.GetComponent<Image>();
            img.sprite = LoadRoundedSprite(20); img.type = Image.Type.Sliced;
            var lbl = btn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (lbl != null) { lbl.outlineWidth = 0.26f; lbl.outlineColor = new Color(0.05f, 0.02f, 0.18f); }
            btn.onClick.AddListener(() => onClick());
            _tabColors[btn] = color;
            return btn;
        }

        private static void UpdateTabVisual()
        {
            void Apply(Button b, bool sel)
            {
                if (b == null || !_tabColors.TryGetValue(b, out var c)) return;
                // Selected = vivid; unselected = same hue but desaturated/darker
                var img = b.GetComponent<Image>();
                if (img != null) img.color = sel ? c : Color.Lerp(c, new Color(0.20f, 0.18f, 0.32f), 0.55f);
                var t = b.transform.Find("Lbl")?.GetComponent<TMP_Text>();
                if (t != null)
                {
                    t.color = Color.white;
                    t.outlineWidth = sel ? 0.32f : 0.22f;
                    t.outlineColor = sel ? new Color(0.05f, 0.02f, 0.18f) : new Color(0, 0, 0, 0.6f);
                }
            }
            Apply(_gearTabBtn,   _tab == Tab.Gear);
            Apply(_rosterTabBtn, _tab == Tab.Roster);
            Apply(_shopTabBtn,   _tab == Tab.Shop);
        }

        // ───────── Scroll list ─────────
        private static void BuildScrollList(Transform parent)
        {
            // Equipped slots row (only visible in Gear mode)
            BuildSlotsRow(parent);

            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(parent, false);
            var scrt = scrollGO.GetComponent<RectTransform>();
            scrt.anchorMin = new Vector2(0, 0); scrt.anchorMax = new Vector2(1, 1);
            // Top dropped to -1150 so the first roster row sits well clear of
            // the slots row (which spans down to ~-1030) — ~120px of breathing
            // room instead of butting against the slot tiles.
            scrt.offsetMin = new Vector2(30, 30); scrt.offsetMax = new Vector2(-30, -1150);
            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 35f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vrt = viewport.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
            var vpImg = viewport.GetComponent<Image>();
            vpImg.sprite = LoadRoundedSprite(20); vpImg.type = Image.Type.Sliced;
            vpImg.color = new Color(0, 0, 0, 0.25f);
            viewport.GetComponent<Mask>().showMaskGraphic = true;
            sr.viewport = vrt;

            var content = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cct = content.GetComponent<RectTransform>();
            cct.anchorMin = new Vector2(0, 1); cct.anchorMax = new Vector2(1, 1);
            cct.pivot = new Vector2(0.5f, 1f);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 16, 16);
            vlg.spacing = 12;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = cct;
            _listParent = content.transform;
        }

        private static void BuildSlotsRow(Transform parent)
        {
            var row = new GameObject("Slots", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(1, 1);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0, -810);
            rrt.sizeDelta = new Vector2(-40, 220);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            _slotsParent = row.transform;

            _slotIcons = new Image[3];
            _slotLabels = new TMP_Text[3];
            string[] slotNames = { "HAT", "BODY", "TRINKET" };
            for (int i = 0; i < 3; i++)
            {
                int captured = i;
                var slot = MakeRounded(row.transform, $"S{i}", SLOT_BG, 16);
                var slotBtn = slot.AddComponent<Button>();
                slotBtn.onClick.AddListener(() =>
                {
                    var pet = Sparq.Systems.PetService.Active();
                    if (pet == null) return;
                    Sparq.Systems.PetService.Unequip(pet.instanceId,
                        (Sparq.Systems.PetService.Slot)captured);
                });

                // Slot type label at top — bigger and bolder, reads at-a-glance.
                var typeLbl = MakeText(slot.transform, "Type", slotNames[i],
                    28, FontStyles.Bold, GOLD,
                    new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, Vector2.zero);
                typeLbl.alignment = TextAlignmentOptions.Center;
                typeLbl.characterSpacing = 4f;
                typeLbl.outlineWidth = 0.26f;
                typeLbl.outlineColor = new Color(0.05f, 0.02f, 0.08f, 1f);
                var tlRT = typeLbl.rectTransform;
                tlRT.anchorMin = new Vector2(0, 1); tlRT.anchorMax = new Vector2(1, 1);
                tlRT.pivot = new Vector2(0.5f, 1f);
                tlRT.anchoredPosition = new Vector2(0, -6);
                tlRT.sizeDelta = new Vector2(0, 36);

                // Slot icon background disc — bigger and darker so the icon
                // actually pops against the slimmer/lower tile body.
                var iconBg = MakeRounded(slot.transform, "IconBg", new Color(0.10f, 0.10f, 0.14f, 1f), 22);
                var ibRT = iconBg.GetComponent<RectTransform>();
                ibRT.anchorMin = new Vector2(0.5f, 1); ibRT.anchorMax = new Vector2(0.5f, 1);
                ibRT.pivot = new Vector2(0.5f, 1f);
                ibRT.anchoredPosition = new Vector2(0, -50);
                ibRT.sizeDelta = new Vector2(110, 110);
                var ibImg = iconBg.GetComponent<Image>();
                ibImg.sprite = LoadCircleSprite();
                ibImg.type = Image.Type.Simple;
                _slotIcons[i] = ibImg;

                // Real Layer Lab equip icon drawn on top of the disc — bigger inset
                var iconArt = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconArt.transform.SetParent(iconBg.transform, false);
                var iaRT = iconArt.GetComponent<RectTransform>();
                iaRT.anchorMin = Vector2.zero; iaRT.anchorMax = Vector2.one;
                iaRT.offsetMin = new Vector2(8, 8); iaRT.offsetMax = new Vector2(-8, -8);
                var iaImg = iconArt.GetComponent<Image>();
                iaImg.preserveAspect = true;
                iaImg.raycastTarget = false;
                iaImg.sprite = LoadSlotIconSprite((Sparq.Systems.PetService.Slot)i);

                // Item name (or "Empty") — bumped from 17→24 + dark outline so
                // it reads even when the tile background is dark.
                var name = MakeText(slot.transform, "ItemName", "Empty",
                    24, FontStyles.Bold, new Color(1, 1, 1, 0.7f),
                    new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero);
                name.alignment = TextAlignmentOptions.Center;
                name.outlineWidth = 0.22f;
                name.outlineColor = new Color(0, 0, 0, 0.85f);
                name.enableAutoSizing = true;
                name.fontSizeMin = 18; name.fontSizeMax = 26;
                name.textWrappingMode = TextWrappingModes.NoWrap;
                name.overflowMode = TextOverflowModes.Ellipsis;
                var nRT = name.rectTransform;
                nRT.anchorMin = new Vector2(0, 0); nRT.anchorMax = new Vector2(1, 0);
                nRT.pivot = new Vector2(0.5f, 0);
                nRT.anchoredPosition = new Vector2(0, 42);
                nRT.sizeDelta = new Vector2(-8, 32);

                // Stats line — bumped from 14→22 + outline.
                var stats = MakeText(slot.transform, "Stats", "—",
                    22, FontStyles.Bold, new Color(1f, 0.92f, 0.55f),
                    new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero);
                stats.alignment = TextAlignmentOptions.Center;
                stats.outlineWidth = 0.22f;
                stats.outlineColor = new Color(0, 0, 0, 0.9f);
                var sRT = stats.rectTransform;
                sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 0);
                sRT.pivot = new Vector2(0.5f, 0);
                sRT.anchoredPosition = new Vector2(0, 10);
                sRT.sizeDelta = new Vector2(-8, 30);
                _slotLabels[i] = stats;
            }
        }

        // Layer Lab equipment icon per slot type (loaded once and cached)
        private static readonly Dictionary<Sparq.Systems.PetService.Slot, Sprite> _slotIconCache = new Dictionary<Sparq.Systems.PetService.Slot, Sprite>();
        private static Sprite LoadSlotIconSprite(Sparq.Systems.PetService.Slot slot)
        {
            if (_slotIconCache.TryGetValue(slot, out var cached) && cached != null) return cached;
            #if UNITY_EDITOR
            string path = slot switch {
                Sparq.Systems.PetService.Slot.Hat     => "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/ItemIcon_Gear_Helmet.png",
                Sparq.Systems.PetService.Slot.Body    => "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/ItemIcon_Gear_Armor.png",
                Sparq.Systems.PetService.Slot.Trinket => "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/ItemIcon_Gear_Ring.png",
                _ => null,
            };
            if (path != null)
            {
                var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite && !Application.isPlaying)
                {
                    imp.textureType = UnityEditor.TextureImporterType.Sprite;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                }
                var sp = Sparq.Core.SpriteLoader.Load(path);
                if (sp != null) { _slotIconCache[slot] = sp; return sp; }
            }
            #endif
            return null;
        }

        private static Sprite LoadFoodSprite(Sparq.Systems.PetService.Food f)
        {
            #if UNITY_EDITOR
            if (!string.IsNullOrEmpty(f.spritePath))
            {
                var imp = UnityEditor.AssetImporter.GetAtPath(f.spritePath) as UnityEditor.TextureImporter;
                if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite && !Application.isPlaying)
                {
                    imp.textureType = UnityEditor.TextureImporterType.Sprite;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                }
                var sp = Sparq.Core.SpriteLoader.Load(f.spritePath);
                if (sp != null) return sp;
            }
            #endif
            return null;
        }

        // Procedural slot shape so empty slots are visually distinct (kept as fallback)
        private static void BuildSlotShape(Transform parent, Sparq.Systems.PetService.Slot slot)
        {
            switch (slot)
            {
                case Sparq.Systems.PetService.Slot.Hat:
                    // Triangular hat (rotated rounded rect)
                    DrawShape(parent, new Color(1f, 0.97f, 0.85f, 0.85f), 0.5f, 0.6f, 32, 18, true, 6, 0);
                    DrawShape(parent, new Color(1f, 0.97f, 0.85f, 0.85f), 0.5f, 0.35f, 42, 8, true, 4, 0);
                    break;
                case Sparq.Systems.PetService.Slot.Body:
                    // T-shirt: torso + arms
                    DrawShape(parent, new Color(1f, 0.97f, 0.85f, 0.85f), 0.5f, 0.5f, 26, 32, true, 6, 0);
                    DrawShape(parent, new Color(1f, 0.97f, 0.85f, 0.85f), 0.25f, 0.65f, 14, 14, true, 4, 0);
                    DrawShape(parent, new Color(1f, 0.97f, 0.85f, 0.85f), 0.75f, 0.65f, 14, 14, true, 4, 0);
                    break;
                case Sparq.Systems.PetService.Slot.Trinket:
                    // Star shape — circle + 5 rays
                    DrawCircleAt(parent, new Color(1f, 0.97f, 0.85f, 0.85f), 0.5f, 0.5f, 16);
                    for (int i = 0; i < 5; i++)
                    {
                        float ang = -90f + i * 72f;
                        float rad = ang * Mathf.Deg2Rad;
                        float ax = 0.5f + Mathf.Cos(rad) * 0.30f;
                        float ay = 0.5f + Mathf.Sin(rad) * 0.30f;
                        DrawShape(parent, new Color(1f, 0.97f, 0.85f, 0.85f), ax, ay, 8, 14, true, 3, ang + 90f);
                    }
                    break;
            }
        }

        private static void DrawShape(Transform parent, Color color, float ax, float ay,
            float w, float h, bool rounded, int radius, float angle)
        {
            var go = new GameObject("Shape", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            if (angle != 0) rt.localRotation = Quaternion.Euler(0, 0, angle);
            var img = go.GetComponent<Image>();
            img.sprite = rounded ? LoadRoundedSprite(radius) : LoadCircleSprite();
            if (rounded) img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
        }

        private static void DrawCircleAt(Transform parent, Color color, float ax, float ay, float size)
        {
            var go = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = LoadCircleSprite();
            img.color = color;
            img.raycastTarget = false;
        }

        private static void UpdateSlots()
        {
            if (_slotIcons == null || _heroParent == null) return;
            var p = Sparq.Systems.PetService.Active();
            if (p == null) return;
            string[] equipped = { p.hatId, p.bodyId, p.trinkId };
            for (int i = 0; i < 3; i++)
            {
                var item = string.IsNullOrEmpty(equipped[i]) ? null
                    : Sparq.Systems.PetService.FindItem(equipped[i]);
                _slotIcons[i].color = item != null ? item.tint : new Color(0.30f, 0.28f, 0.45f, 1f);

                // Walk up to the slot card to find ItemName + Stats children
                var slotT = _slotIcons[i].transform.parent;
                var nameT = slotT.Find("ItemName");
                if (nameT != null)
                {
                    var t = nameT.GetComponent<TMP_Text>();
                    if (t != null)
                    {
                        t.text = item != null ? item.name : "Empty";
                        t.color = item != null ? Color.white : new Color(1, 1, 1, 0.5f);
                    }
                }
                if (_slotLabels[i] != null)
                {
                    if (item != null)
                    {
                        var parts = new System.Collections.Generic.List<string>();
                        if (item.atk > 0) parts.Add($"⚔+{item.atk}");
                        if (item.def > 0) parts.Add($"🛡+{item.def}");
                        if (item.hp  > 0) parts.Add($"❤+{item.hp}");
                        _slotLabels[i].text = string.Join("  ", parts);
                        _slotLabels[i].color = new Color(1f, 0.92f, 0.55f);
                    }
                    else
                    {
                        _slotLabels[i].text = "—";
                        _slotLabels[i].color = new Color(1, 1, 1, 0.4f);
                    }
                }
            }
        }

        // ───────── Rebuild list per tab ─────────
        private static void RebuildList()
        {
            UpdateTabVisual();
            UpdateSlots();
            if (_listParent == null) return;
            for (int i = _listParent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_listParent.GetChild(i).gameObject);

            switch (_tab)
            {
                case Tab.Gear:   BuildGearList(); break;
                case Tab.Roster: BuildRosterList(); break;
                case Tab.Shop:   BuildShopList(); break;
            }
            // Force the ScrollRect to recompute content size now that rows exist
            var contentRT = _listParent as RectTransform;
            if (contentRT != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
        }

        private static void BuildGearList()
        {
            var owned = Sparq.Systems.PetService.OwnedItems();
            if (owned.Count == 0)
            {
                BuildEmpty("No items owned. Visit SHOP to buy gear.");
                return;
            }
            foreach (var id in owned)
            {
                var it = Sparq.Systems.PetService.FindItem(id);
                if (it == null) continue;
                BuildItemRow(it, equipAction: () =>
                {
                    var p = Sparq.Systems.PetService.Active();
                    if (p != null) Sparq.Systems.PetService.Equip(p.instanceId, id);
                }, sellAction: () => Sparq.Systems.PetService.SellItem(id), action: "EQUIP");
            }
        }

        private static void BuildRosterList()
        {
            var roster = Sparq.Systems.PetService.Roster();
            string activeId = Sparq.Systems.PetService.ActiveId();
            foreach (var p in roster)
            {
                var sp = Sparq.Systems.PetService.FindSpecies(p.speciesId);
                bool isActive = p.instanceId == activeId;
                BuildPetRow(p, sp, isActive,
                    activateAction: () => Sparq.Systems.PetService.SetActive(p.instanceId),
                    sellAction: () => Sparq.Systems.PetService.SellPet(p.instanceId));
            }
        }

        private static void BuildShopList()
        {
            // Section: Pets for sale
            var hdr1 = MakeText(_listParent, "PetHdr", "—  PETS  —",
                20, FontStyles.Bold, GOLD,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            hdr1.alignment = TextAlignmentOptions.Center;
            hdr1.characterSpacing = 8f;
            var le1 = hdr1.gameObject.AddComponent<LayoutElement>();
            le1.preferredHeight = 36;

            // Group pets by rarity tier so the long list is browsable
            string[] tiers = { "Common", "Rare", "Epic", "Legendary", "Mythic" };
            foreach (var tier in tiers)
            {
                bool first = true;
                foreach (var sp in Sparq.Systems.PetService.CATALOG)
                {
                    if (sp.cost == 0 || sp.rarity != tier) continue;
                    if (first)
                    {
                        var trHdr = MakeText(_listParent, $"TH_{tier}", $"·  {tier.ToUpper()}  ·",
                            18, FontStyles.Bold, RarityColor(tier),
                            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
                        trHdr.alignment = TextAlignmentOptions.Center;
                        trHdr.characterSpacing = 8f;
                        trHdr.outlineWidth = 0.22f;
                        trHdr.outlineColor = new Color(0.05f, 0.02f, 0.18f, 1f);
                        var leT = trHdr.gameObject.AddComponent<LayoutElement>();
                        leT.preferredHeight = 30;
                        first = false;
                    }
                    BuildShopPetRow(sp);
                }
            }

            // Section: Items
            var hdr2 = MakeText(_listParent, "ItemHdr", "—  GEAR  —",
                20, FontStyles.Bold, GOLD,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            hdr2.alignment = TextAlignmentOptions.Center;
            hdr2.characterSpacing = 8f;
            var le2 = hdr2.gameObject.AddComponent<LayoutElement>();
            le2.preferredHeight = 36;

            foreach (var it in Sparq.Systems.PetService.ITEMS)
            {
                BuildShopItemRow(it);
            }

            // Section: Food
            var hdr3 = MakeText(_listParent, "FoodHdr", "—  FOOD  —",
                20, FontStyles.Bold, GOLD,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            hdr3.alignment = TextAlignmentOptions.Center;
            hdr3.characterSpacing = 8f;
            var le3 = hdr3.gameObject.AddComponent<LayoutElement>();
            le3.preferredHeight = 36;
            foreach (var f in Sparq.Systems.PetService.FOODS) BuildShopFoodRow(f);
        }

        private static void BuildShopFoodRow(Sparq.Systems.PetService.Food f)
        {
            var row = MakeRounded(_listParent, $"SF_{f.id}", ROW_BG, 14);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 100; le.minHeight = 100;

            var disc = MakeRounded(row.transform, "D", f.tint, 26);
            var dRT = disc.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0, 0.5f); dRT.anchorMax = new Vector2(0, 0.5f);
            dRT.pivot = new Vector2(0, 0.5f);
            dRT.anchoredPosition = new Vector2(16, 0);
            dRT.sizeDelta = new Vector2(64, 64);
            disc.GetComponent<Image>().sprite = LoadCircleSprite();
            var sp = LoadFoodSprite(f);
            if (sp != null)
            {
                var art = new GameObject("Art", typeof(RectTransform), typeof(Image));
                art.transform.SetParent(disc.transform, false);
                var aRT = art.GetComponent<RectTransform>();
                aRT.anchorMin = Vector2.zero; aRT.anchorMax = Vector2.one;
                aRT.offsetMin = new Vector2(8, 8); aRT.offsetMax = new Vector2(-8, -8);
                var aImg = art.GetComponent<Image>();
                aImg.sprite = sp; aImg.preserveAspect = true; aImg.raycastTarget = false;
            }

            int owned = Sparq.Systems.PetService.FoodCount(f.id);
            var name = MakeText(row.transform, "N", $"{f.name}   <color=#FFE9A8>×{owned}</color>",
                22, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            name.richText = true;
            name.alignment = TextAlignmentOptions.MidlineLeft;
            var nRT = name.rectTransform;
            nRT.anchorMin = new Vector2(0, 0.55f); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0, 0.5f);
            nRT.offsetMin = new Vector2(94, 0); nRT.offsetMax = new Vector2(-150, 0);
            name.outlineWidth = 0.20f; name.outlineColor = new Color(0, 0, 0, 0.7f);

            var sub = MakeText(row.transform, "S", $"+{f.hungerRestore} HUNGER",
                14, FontStyles.Bold, new Color(1f, 0.92f, 0.55f),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            sub.alignment = TextAlignmentOptions.MidlineLeft;
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 0.55f);
            sRT.pivot = new Vector2(0, 0.5f);
            sRT.offsetMin = new Vector2(94, 6); sRT.offsetMax = new Vector2(-150, 0);

            bool canBuy = Sparq.Systems.PetService.Coins() >= f.cost;
            var buyBtn = MakeBtn(row.transform, "B", $"BUY\n{f.cost}g",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(110, 56),
                canBuy ? GOLD : new Color(0.40f, 0.38f, 0.50f), Color.white, 14);
            ApplyRound(buyBtn);
            var bL = buyBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (bL != null) { bL.color = DEEP_NAVY; bL.outlineWidth = 0.22f; bL.outlineColor = new Color(1f, 0.95f, 0.7f); }
            buyBtn.interactable = canBuy;
            string captured = f.id;
            buyBtn.onClick.AddListener(() => Sparq.Systems.PetService.BuyFood(captured));
        }

        private static void BuildEmpty(string msg)
        {
            var t = MakeText(_listParent, "Empty", msg,
                22, FontStyles.Italic, new Color(1, 1, 1, 0.6f),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            t.alignment = TextAlignmentOptions.Center;
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 80;
        }

        // ───────── Row builders ─────────
        private static void BuildItemRow(Sparq.Systems.PetService.Item it,
            System.Action equipAction, System.Action sellAction, string action)
        {
            var row = MakeRounded(_listParent, $"I_{it.id}", ROW_BG, 14);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 110; le.minHeight = 110;

            // Disc on left
            var disc = MakeRounded(row.transform, "D", it.tint, 30);
            var dRT = disc.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0, 0.5f); dRT.anchorMax = new Vector2(0, 0.5f);
            dRT.pivot = new Vector2(0, 0.5f);
            dRT.anchoredPosition = new Vector2(16, 0);
            dRT.sizeDelta = new Vector2(72, 72);
            disc.GetComponent<Image>().sprite = LoadCircleSprite();
            disc.GetComponent<Image>().type = Image.Type.Simple;

            // Name + slot + stats
            var name = MakeText(row.transform, "N", it.name,
                24, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            var nRT = name.rectTransform;
            nRT.anchorMin = new Vector2(0, 0.55f); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0, 0.5f);
            nRT.offsetMin = new Vector2(102, 0); nRT.offsetMax = new Vector2(-280, 0);
            name.outlineWidth = 0.20f; name.outlineColor = new Color(0, 0, 0, 0.7f);

            string statStr = $"{it.slot} · ⚔+{it.atk} 🛡+{it.def} ❤+{it.hp}";
            var sub = MakeText(row.transform, "S", statStr,
                16, FontStyles.Bold, new Color(1f, 0.92f, 0.55f),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            sub.alignment = TextAlignmentOptions.MidlineLeft;
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 0.55f);
            sRT.pivot = new Vector2(0, 0.5f);
            sRT.offsetMin = new Vector2(102, 8); sRT.offsetMax = new Vector2(-280, 0);

            // Equip button (right)
            var equipBtn = MakeBtn(row.transform, "E", action,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(110, 56),
                new Color(0.30f, 0.80f, 0.42f), Color.white, 18);
            ApplyRound(equipBtn);
            var eL = equipBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (eL != null) { eL.color = DEEP_NAVY; eL.outlineWidth = 0.22f; eL.outlineColor = new Color(0.85f, 1f, 0.85f); }
            equipBtn.onClick.AddListener(() => equipAction());

            // Sell button
            var sellBtn = MakeBtn(row.transform, "Sl", $"SELL\n{it.sellValue}g",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(110, 56),
                new Color(0.92f, 0.35f, 0.42f), Color.white, 16);
            ApplyRound(sellBtn);
            var sL = sellBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (sL != null) { sL.outlineWidth = 0.22f; sL.outlineColor = new Color(0.10f, 0.05f, 0.20f); }
            sellBtn.onClick.AddListener(() => sellAction());
        }

        private static void BuildPetRow(Sparq.Systems.PetService.Pet p,
            Sparq.Systems.PetService.Species sp, bool isActive,
            System.Action activateAction, System.Action sellAction)
        {
            // Taller row + a beveled gold border when active so the player
            // can spot the active pet without squinting at small "ACTIVE" text.
            var row = MakeRounded(_listParent, $"P_{p.instanceId}",
                isActive ? new Color(0.30f, 0.25f, 0.10f, 1f) : ROW_BG, 14);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 150; le.minHeight = 150;

            // Optional gold rim when active — sliced rounded sprite tinted gold.
            if (isActive)
            {
                var rim = new GameObject("Rim", typeof(RectTransform), typeof(Image));
                rim.transform.SetParent(row.transform, false);
                var rRT = rim.GetComponent<RectTransform>();
                rRT.anchorMin = Vector2.zero; rRT.anchorMax = Vector2.one;
                rRT.offsetMin = new Vector2(-4, -4); rRT.offsetMax = new Vector2(4, 4);
                var rImg = rim.GetComponent<Image>();
                rImg.sprite = LoadRoundedSprite(16); rImg.type = Image.Type.Sliced;
                rImg.color = new Color(1.00f, 0.78f, 0.20f, 0.85f);
                rImg.raycastTarget = false;
                rim.transform.SetAsFirstSibling();   // sits BEHIND the row body
            }

            // Disc with the actual cropped pet sprite (was just a letter glyph
            // — felt unfinished, especially for the active pet that the player
            // is identifying).
            var disc = MakeRounded(row.transform, "D",
                Color.Lerp(sp.tint, Color.white, 0.30f), 30);
            var dRT = disc.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0, 0.5f); dRT.anchorMax = new Vector2(0, 0.5f);
            dRT.pivot = new Vector2(0, 0.5f);
            dRT.anchoredPosition = new Vector2(16, 0);
            dRT.sizeDelta = new Vector2(110, 110);
            disc.GetComponent<Image>().sprite = LoadCircleSprite();

            // Cropped pet sprite layered on top of the disc.
            #if UNITY_EDITOR
            if (!string.IsNullOrEmpty(sp.spritePath))
            {
                Sprite cropped = null;
                try { cropped = Sparq.UI.HeroPortrait.LoadCropped(sp.spritePath); } catch {}
                if (cropped != null)
                {
                    var fig = new GameObject("Fig", typeof(RectTransform), typeof(Image));
                    fig.transform.SetParent(disc.transform, false);
                    var fRT = fig.GetComponent<RectTransform>();
                    fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
                    fRT.offsetMin = new Vector2(8, 8); fRT.offsetMax = new Vector2(-8, -8);
                    var fImg = fig.GetComponent<Image>();
                    fImg.sprite = cropped;
                    fImg.preserveAspect = true;
                    fImg.raycastTarget = false;
                }
                else
                {
                    var letter = MakeText(disc.transform, "L", sp.name.Substring(0, 1).ToUpper(),
                        48, FontStyles.Bold, Color.white,
                        new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
                    letter.alignment = TextAlignmentOptions.Center;
                    letter.outlineWidth = 0.30f; letter.outlineColor = new Color(0, 0, 0, 0.85f);
                }
            }
            #endif

            // Name — bigger, with the rarity color injected so each pet has
            // its own visual identity (was monolithic gold, hard to tell apart).
            var rarityColor = RarityColor(sp.rarity);
            string nameTxt = $"<color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{p.nickname}</color>" +
                             $"   <color=#{ColorUtility.ToHtmlStringRGB(GOLD)}>· LV {p.level}</color>";
            var name = MakeText(row.transform, "N", nameTxt,
                38, FontStyles.Bold, CREAM,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            name.richText = true;
            name.alignment = TextAlignmentOptions.MidlineLeft;
            var nRT = name.rectTransform;
            nRT.anchorMin = new Vector2(0, 0.55f); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0, 0.5f);
            nRT.offsetMin = new Vector2(140, 0); nRT.offsetMax = new Vector2(-300, 0);
            name.outlineWidth = 0.24f; name.outlineColor = new Color(0, 0, 0, 0.95f);

            // Stats — bigger + outlined so they read on the dark row.
            var stats = Sparq.Systems.PetService.StatsOf(p);
            var sub = MakeText(row.transform, "S",
                $"⚔ {stats.atk}    🛡 {stats.def}    ❤ {stats.hp}",
                30, FontStyles.Bold, CREAM,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            sub.alignment = TextAlignmentOptions.MidlineLeft;
            sub.outlineWidth = 0.22f; sub.outlineColor = new Color(0, 0, 0, 0.95f);
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 0.55f);
            sRT.pivot = new Vector2(0, 0.5f);
            sRT.offsetMin = new Vector2(140, 8); sRT.offsetMax = new Vector2(-300, 0);

            // ACTIVE / USE button — bigger pill, easier tap target.
            var actBtn = MakeBtn(row.transform, "A", isActive ? "ACTIVE" : "USE",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-160, 0), new Vector2(140, 80),
                isActive ? GOLD : new Color(0.30f, 0.80f, 0.42f), Color.white, 26);
            ApplyRound(actBtn);
            var aL = actBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (aL != null) { aL.color = DEEP_NAVY; aL.outlineWidth = 0.26f; aL.outlineColor = new Color(1f, 0.95f, 0.7f); }
            actBtn.onClick.AddListener(() => activateAction());

            // Sell — pillar pill with sell value, disabled if it's the only pet.
            int sellVal = sp.sellValue;
            bool canSell = Sparq.Systems.PetService.Roster().Count > 1;
            var sellBtn = MakeBtn(row.transform, "Sl", canSell ? $"SELL\n{sellVal}g" : "—",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-12, 0), new Vector2(130, 80),
                canSell ? new Color(0.92f, 0.35f, 0.42f) : new Color(0.40f, 0.38f, 0.50f), Color.white, 22);
            ApplyRound(sellBtn);
            var sL = sellBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (sL != null) { sL.outlineWidth = 0.26f; sL.outlineColor = new Color(0.10f, 0.05f, 0.20f); }
            sellBtn.interactable = canSell;
            sellBtn.onClick.AddListener(() => sellAction());
        }

        private static void BuildShopPetRow(Sparq.Systems.PetService.Species sp)
        {
            var row = MakeRounded(_listParent, $"SP_{sp.id}", ROW_BG, 14);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 120; le.minHeight = 120;

            var disc = MakeRounded(row.transform, "D", sp.tint, 30);
            var dRT = disc.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0, 0.5f); dRT.anchorMax = new Vector2(0, 0.5f);
            dRT.pivot = new Vector2(0, 0.5f);
            dRT.anchoredPosition = new Vector2(16, 0);
            dRT.sizeDelta = new Vector2(80, 80);
            disc.GetComponent<Image>().sprite = LoadCircleSprite();
            var letter = MakeText(disc.transform, "L", sp.name.Substring(0, 1).ToUpper(),
                40, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            letter.alignment = TextAlignmentOptions.Center;
            letter.outlineWidth = 0.30f; letter.outlineColor = new Color(0, 0, 0, 0.85f);

            var name = MakeText(row.transform, "N", sp.name,
                24, FontStyles.Bold, GOLD,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            var nRT = name.rectTransform;
            nRT.anchorMin = new Vector2(0, 0.55f); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0, 0.5f);
            nRT.offsetMin = new Vector2(112, 0); nRT.offsetMax = new Vector2(-150, 0);
            name.outlineWidth = 0.20f; name.outlineColor = new Color(0, 0, 0, 0.7f);

            var sub = MakeText(row.transform, "S",
                $"{sp.blurb}    ·    ⚔{sp.baseAtk} 🛡{sp.baseDef} ❤{sp.baseHp}",
                16, FontStyles.Bold, CREAM,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            sub.alignment = TextAlignmentOptions.MidlineLeft;
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 0.55f);
            sRT.pivot = new Vector2(0, 0.5f);
            sRT.offsetMin = new Vector2(112, 8); sRT.offsetMax = new Vector2(-150, 0);

            bool canBuy = Sparq.Systems.PetService.Coins() >= sp.cost;
            var buyBtn = MakeBtn(row.transform, "B", $"BUY\n{sp.cost}g",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(110, 60),
                canBuy ? GOLD : new Color(0.40f, 0.38f, 0.50f), Color.white, 16);
            ApplyRound(buyBtn);
            var bL = buyBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (bL != null) { bL.color = DEEP_NAVY; bL.outlineWidth = 0.22f; bL.outlineColor = new Color(1f, 0.95f, 0.7f); }
            buyBtn.interactable = canBuy;
            string captured = sp.id;
            buyBtn.onClick.AddListener(() => Sparq.Systems.PetService.BuyPet(captured));
        }

        private static void BuildShopItemRow(Sparq.Systems.PetService.Item it)
        {
            var row = MakeRounded(_listParent, $"SI_{it.id}", ROW_BG, 14);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 100; le.minHeight = 100;

            var disc = MakeRounded(row.transform, "D", it.tint, 26);
            var dRT = disc.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0, 0.5f); dRT.anchorMax = new Vector2(0, 0.5f);
            dRT.pivot = new Vector2(0, 0.5f);
            dRT.anchoredPosition = new Vector2(16, 0);
            dRT.sizeDelta = new Vector2(64, 64);
            disc.GetComponent<Image>().sprite = LoadCircleSprite();

            var name = MakeText(row.transform, "N", it.name,
                22, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            var nRT = name.rectTransform;
            nRT.anchorMin = new Vector2(0, 0.55f); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0, 0.5f);
            nRT.offsetMin = new Vector2(94, 0); nRT.offsetMax = new Vector2(-150, 0);
            name.outlineWidth = 0.20f; name.outlineColor = new Color(0, 0, 0, 0.7f);

            var sub = MakeText(row.transform, "S",
                $"{it.slot}  ·  ⚔+{it.atk} 🛡+{it.def} ❤+{it.hp}",
                14, FontStyles.Bold, new Color(1f, 0.92f, 0.55f),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            sub.alignment = TextAlignmentOptions.MidlineLeft;
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 0.55f);
            sRT.pivot = new Vector2(0, 0.5f);
            sRT.offsetMin = new Vector2(94, 6); sRT.offsetMax = new Vector2(-150, 0);

            bool canBuy = Sparq.Systems.PetService.Coins() >= it.cost;
            bool owned = Sparq.Systems.PetService.OwnedItems().Contains(it.id);
            var buyBtn = MakeBtn(row.transform, "B", owned ? "OWNED" : $"BUY\n{it.cost}g",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 0), new Vector2(110, 56),
                owned ? new Color(0.50f, 0.50f, 0.65f) : (canBuy ? GOLD : new Color(0.40f, 0.38f, 0.50f)),
                Color.white, 14);
            ApplyRound(buyBtn);
            var bL = buyBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (bL != null) { bL.color = DEEP_NAVY; bL.outlineWidth = 0.22f; bL.outlineColor = new Color(1f, 0.95f, 0.7f); }
            buyBtn.interactable = !owned && canBuy;
            string captured = it.id;
            buyBtn.onClick.AddListener(() => Sparq.Systems.PetService.BuyItem(captured));
        }

        // ───────── Refresh ─────────
        public static void Refresh()
        {
            if (_root == null) return;
            BuildHeroContent();
            if (_coinsTm != null) _coinsTm.text = $"{Sparq.Systems.PetService.Coins():N0}";
            RebuildList();
        }

        // ───────── Food picker (opens when FEED is tapped) ─────────
        private static void OpenFoodPicker()
        {
            var counts = Sparq.Systems.PetService.FoodCounts();
            int total = Sparq.Systems.PetService.TotalFoodCount();

            var cv = new GameObject("FoodPicker",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var cvRT = cv.GetComponent<RectTransform>();
            cvRT.anchorMin = Vector2.zero; cvRT.anchorMax = Vector2.one;
            cvRT.offsetMin = Vector2.zero; cvRT.offsetMax = Vector2.zero;
            var oc = cv.GetComponent<Canvas>();
            oc.renderMode = RenderMode.ScreenSpaceOverlay;
            oc.sortingOrder = 14800;
            var ocs = cv.GetComponent<CanvasScaler>();
            ocs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            ocs.referenceResolution = new Vector2(1080, 1920);
            ocs.matchWidthOrHeight = 0.5f;

            var dim = MakeImage(cv.transform, "Dim", new Color(0, 0, 0, 0.92f));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(() => UnityEngine.Object.Destroy(cv));

            var card = MakeRounded(cv.transform, "Card", CARD_BG, 28);
            var cRT = card.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0.5f, 0.5f); cRT.anchorMax = new Vector2(0.5f, 0.5f);
            cRT.pivot = new Vector2(0.5f, 0.5f);
            cRT.sizeDelta = new Vector2(900, 1050);

            // Title
            MakeText(card.transform, "T", "🍓  PANTRY",
                40, FontStyles.Bold, GOLD,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, 70))
                .alignment = TextAlignmentOptions.Center;

            if (total == 0)
            {
                MakeText(card.transform, "Empty",
                    "No food in your pantry.\nBuy some in the SHOP\nor earn drops from quests + battles.",
                    24, FontStyles.Italic, new Color(1, 1, 1, 0.7f),
                    new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero)
                    .alignment = TextAlignmentOptions.Center;
            }
            else
            {
                // Scrollable list of owned foods
                var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
                scrollGO.transform.SetParent(card.transform, false);
                var scrt = scrollGO.GetComponent<RectTransform>();
                scrt.anchorMin = new Vector2(0, 0); scrt.anchorMax = new Vector2(1, 1);
                scrt.offsetMin = new Vector2(30, 130); scrt.offsetMax = new Vector2(-30, -120);
                var sr = scrollGO.GetComponent<ScrollRect>();
                sr.horizontal = false; sr.vertical = true;
                sr.movementType = ScrollRect.MovementType.Elastic;

                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
                viewport.transform.SetParent(scrollGO.transform, false);
                var vrt = viewport.GetComponent<RectTransform>();
                vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
                vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
                var vpImg = viewport.GetComponent<Image>();
                vpImg.sprite = LoadRoundedSprite(20); vpImg.type = Image.Type.Sliced;
                vpImg.color = new Color(0, 0, 0, 0.25f);
                viewport.GetComponent<Mask>().showMaskGraphic = true;
                sr.viewport = vrt;

                var content = new GameObject("Content",
                    typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                content.transform.SetParent(viewport.transform, false);
                var cct = content.GetComponent<RectTransform>();
                cct.anchorMin = new Vector2(0, 1); cct.anchorMax = new Vector2(1, 1);
                cct.pivot = new Vector2(0.5f, 1f);
                var vlg = content.GetComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(20, 20, 16, 16);
                vlg.spacing = 12;
                vlg.childForceExpandWidth = true;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandHeight = false;
                var csf = content.GetComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                sr.content = cct;

                foreach (var f in Sparq.Systems.PetService.FOODS)
                {
                    int count = Sparq.Systems.PetService.FoodCount(f.id);
                    if (count <= 0) continue;
                    BuildFoodPickerRow(content.transform, f, count, cv);
                }
            }

            var close = MakeBtn(card.transform, "Close", "Close",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(280, 80),
                new Color(0.40f, 0.36f, 0.55f), Color.white, 26);
            close.onClick.AddListener(() => UnityEngine.Object.Destroy(cv));
            var clImg = close.GetComponent<Image>();
            clImg.sprite = LoadRoundedSprite(20); clImg.type = Image.Type.Sliced;
        }

        private static void BuildFoodPickerRow(Transform parent, Sparq.Systems.PetService.Food f, int count, GameObject cvToClose)
        {
            var row = MakeRounded(parent, $"F_{f.id}", ROW_BG, 14);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 110; le.minHeight = 110;

            // Icon
            var disc = MakeRounded(row.transform, "D", f.tint, 26);
            var dRT = disc.GetComponent<RectTransform>();
            dRT.anchorMin = new Vector2(0, 0.5f); dRT.anchorMax = new Vector2(0, 0.5f);
            dRT.pivot = new Vector2(0, 0.5f);
            dRT.anchoredPosition = new Vector2(16, 0);
            dRT.sizeDelta = new Vector2(72, 72);
            disc.GetComponent<Image>().sprite = LoadCircleSprite();
            // Real food sprite if available
            var sp = LoadFoodSprite(f);
            if (sp != null)
            {
                var art = new GameObject("Art", typeof(RectTransform), typeof(Image));
                art.transform.SetParent(disc.transform, false);
                var aRT = art.GetComponent<RectTransform>();
                aRT.anchorMin = Vector2.zero; aRT.anchorMax = Vector2.one;
                aRT.offsetMin = new Vector2(8, 8); aRT.offsetMax = new Vector2(-8, -8);
                var aImg = art.GetComponent<Image>();
                aImg.sprite = sp; aImg.preserveAspect = true; aImg.raycastTarget = false;
            }

            // Name + restore amount
            var name = MakeText(row.transform, "N", f.name,
                26, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            name.alignment = TextAlignmentOptions.MidlineLeft;
            var nRT = name.rectTransform;
            nRT.anchorMin = new Vector2(0, 0.55f); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0, 0.5f);
            nRT.offsetMin = new Vector2(102, 0); nRT.offsetMax = new Vector2(-180, 0);
            name.outlineWidth = 0.20f; name.outlineColor = new Color(0, 0, 0, 0.7f);

            var sub = MakeText(row.transform, "S", $"+{f.hungerRestore} HUNGER  ·  ×{count} owned",
                18, FontStyles.Bold, new Color(1f, 0.92f, 0.55f),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            sub.alignment = TextAlignmentOptions.MidlineLeft;
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 0.55f);
            sRT.pivot = new Vector2(0, 0.5f);
            sRT.offsetMin = new Vector2(102, 8); sRT.offsetMax = new Vector2(-180, 0);

            // FEED button (consumes 1)
            var feed = MakeBtn(row.transform, "F", "FEED",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-25, 0), new Vector2(150, 70),
                new Color(0.30f, 0.80f, 0.42f), Color.white, 24);
            ApplyRound(feed);
            var fLbl = feed.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (fLbl != null) { fLbl.color = DEEP_NAVY; fLbl.outlineWidth = 0.22f; fLbl.outlineColor = new Color(0.85f, 1f, 0.85f); }
            string captured = f.id;
            feed.onClick.AddListener(() =>
            {
                if (Sparq.Systems.PetService.ConsumeFood(captured))
                {
                    PlayMunch();
                    SpawnFeedHearts();           // Tamagotchi-style hearts pop above pet
                    PetHappyBounce();            // pet does a happy scale-bump
                    UnityEngine.Object.Destroy(cvToClose);
                }
            });
        }

        // Tamagotchi-style: hearts float up around the pet after feeding
        private static void SpawnFeedHearts()
        {
            if (_petRT == null) return;
            EnsureRunner();
            if (_runner == null) return;
            for (int i = 0; i < 6; i++)
            {
                var h = new GameObject("Heart", typeof(RectTransform), typeof(TextMeshProUGUI));
                h.transform.SetParent(_petRT, false);
                var rt = h.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(UnityEngine.Random.Range(-60f, 60f),
                                                  UnityEngine.Random.Range(-30f, 30f));
                rt.sizeDelta = new Vector2(80, 80);
                var tm = h.GetComponent<TextMeshProUGUI>();
                tm.text = "♥";
                tm.fontSize = UnityEngine.Random.Range(36, 56);
                tm.color = new Color(1f, 0.45f + UnityEngine.Random.Range(0f, 0.25f), 0.55f);
                tm.alignment = TextAlignmentOptions.Center;
                tm.raycastTarget = false;
                tm.outlineWidth = 0.30f;
                tm.outlineColor = new Color(0.95f, 0.20f, 0.30f, 0.85f);
                _runner.StartCoroutine(FloatHeart(rt, tm, 1.0f + UnityEngine.Random.Range(0f, 0.6f)));
            }
        }

        private static System.Collections.IEnumerator FloatHeart(RectTransform rt, TMP_Text tm, float life)
        {
            float t = 0f;
            Vector2 start = rt.anchoredPosition;
            float drift = UnityEngine.Random.Range(-40f, 40f);
            while (t < life && rt != null)
            {
                t += Time.deltaTime;
                float k = t / life;
                rt.anchoredPosition = start + new Vector2(drift * k, 160f * k);
                if (tm != null)
                {
                    var c = tm.color; c.a = Mathf.Clamp01(1f - k);
                    tm.color = c;
                }
                yield return null;
            }
            if (rt != null) UnityEngine.Object.Destroy(rt.gameObject);
        }

        // Quick scale-pulse: pet bumps up briefly to show happiness
        private static void PetHappyBounce()
        {
            if (_petRT == null) return;
            EnsureRunner();
            if (_runner == null) return;
            _runner.StartCoroutine(HappyBounceCoroutine(_petRT));
        }

        private static System.Collections.IEnumerator HappyBounceCoroutine(RectTransform rt)
        {
            Vector3 baseScale = rt.localScale;
            float t = 0f;
            const float DUR = 0.45f;
            while (t < DUR && rt != null)
            {
                t += Time.deltaTime;
                float k = t / DUR;
                // Easing: pop up to 1.18x then back down
                float pop = 1f + 0.18f * Mathf.Sin(k * Mathf.PI);
                rt.localScale = baseScale * pop;
                yield return null;
            }
            if (rt != null) rt.localScale = baseScale;
        }

        // Monster eating: body thuds + crunches + a satisfied creature roar at the end
        private static AudioClip[] _munchCrunchClips;     // grass = chewy
        private static AudioClip[] _munchBiteClips;       // body = wet thud
        private static AudioClip   _munchWoodClip;        // wood = hard crunch on first bite
        private static AudioClip   _munchRoarClip;        // creature roar = satisfied burp
        private static AudioSource _munchSrc;

        private static void PlayMunch()
        {
            #if UNITY_EDITOR
            if (_munchCrunchClips == null)
            {
                _munchCrunchClips = new AudioClip[]
                {
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Feel/NiceVibrations/HapticSamples/Footsteps/FootstepGrass1.wav"),
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Feel/NiceVibrations/HapticSamples/Footsteps/FootstepGrass2.wav"),
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Feel/NiceVibrations/HapticSamples/Footsteps/FootstepGrass3.wav"),
                };
            }
            if (_munchBiteClips == null)
            {
                _munchBiteClips = new AudioClip[]
                {
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Feel/NiceVibrations/HapticSamples/Impacts/Body1.wav"),
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Feel/NiceVibrations/HapticSamples/Impacts/Body2.wav"),
                    UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Feel/NiceVibrations/HapticSamples/Impacts/Body3.wav"),
                };
            }
            if (_munchWoodClip == null)
                _munchWoodClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Feel/NiceVibrations/HapticSamples/Impacts/Wood1.wav");
            if (_munchRoarClip == null)
                _munchRoarClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Feel/NiceVibrations/HapticSamples/Nature/CreatureRoar1.wav");
            #endif
            if (_munchSrc == null)
            {
                var go = new GameObject("MunchPlayer");
                UnityEngine.Object.DontDestroyOnLoad(go);
                _munchSrc = go.AddComponent<AudioSource>();
                _munchSrc.playOnAwake = false;
                _munchSrc.spatialBlend = 0f;
                _munchSrc.volume = 1f;
            }
            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(MunchSequence());
        }

        private static System.Collections.IEnumerator MunchSequence()
        {
            // CHOMP! — pitched-up bite + crunch (cartoon-loud)
            if (_munchSrc != null)
            {
                if (_munchBiteClips != null && _munchBiteClips.Length > 0)
                {
                    var bite = _munchBiteClips[UnityEngine.Random.Range(0, _munchBiteClips.Length)];
                    if (bite != null) { _munchSrc.pitch = 1.55f; _munchSrc.PlayOneShot(bite, 1f); }
                }
                if (_munchWoodClip != null) { _munchSrc.pitch = 1.7f; _munchSrc.PlayOneShot(_munchWoodClip, 0.9f); }
            }
            SpawnChompText("CHOMP!");
            yield return new WaitForSeconds(0.16f);

            // 3 quick high-pitched chews — "NOM NOM NOM"
            string[] noms = { "NOM!", "OM NOM!", "MUNCH!", "NOM NOM!" };
            for (int i = 0; i < 3; i++)
            {
                if (_munchSrc != null)
                {
                    var crunch = (_munchCrunchClips != null && _munchCrunchClips.Length > 0)
                        ? _munchCrunchClips[UnityEngine.Random.Range(0, _munchCrunchClips.Length)] : null;
                    _munchSrc.pitch = UnityEngine.Random.Range(1.50f, 2.10f); // way pitched up = comic
                    if (crunch != null) _munchSrc.PlayOneShot(crunch, 0.95f);
                }
                SpawnChompText(noms[UnityEngine.Random.Range(0, noms.Length)]);
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.13f, 0.18f));
            }

            // Squeaky-happy creature noise (pitch way up = cute squeak)
            if (_munchSrc != null && _munchRoarClip != null)
            {
                _munchSrc.pitch = UnityEngine.Random.Range(2.2f, 2.6f);
                _munchSrc.PlayOneShot(_munchRoarClip, 0.55f);
            }
            SpawnChompText("YUM! ♥");
        }

        // Comic-style text burst — "CHOMP!" / "NOM NOM!" floats up from the pet
        private static void SpawnChompText(string text)
        {
            if (_root == null) return;
            var go = new GameObject("Chomp", typeof(RectTransform));
            go.transform.SetParent(_root.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(
                UnityEngine.Random.Range(-180f, 180f),
                UnityEngine.Random.Range(40f, 180f));
            rt.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-12f, 12f));
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = UnityEngine.Random.Range(48, 64);
            tm.fontStyle = FontStyles.Bold;
            tm.color = new Color(1f, 0.92f, 0.30f);   // gold
            tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.outlineWidth = 0.35f;
            tm.outlineColor = new Color(0.40f, 0.05f, 0.10f, 1f);
            tm.raycastTarget = false;
            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(ChompTextLife(rt, tm));
        }

        private static System.Collections.IEnumerator ChompTextLife(RectTransform rt, TMP_Text tm)
        {
            float t = 0f, dur = 0.7f;
            Vector2 start = rt.anchoredPosition;
            float drift = UnityEngine.Random.Range(80f, 140f);
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float k = t / dur;
                float ease = 1f - (1f - k) * (1f - k);
                rt.anchoredPosition = start + new Vector2(0, drift * ease);
                // Pop in then shrink + fade out
                float scale = k < 0.18f ? Mathf.Lerp(0.4f, 1.15f, k / 0.18f)
                                        : Mathf.Lerp(1.15f, 0.9f, (k - 0.18f) / 0.82f);
                rt.localScale = Vector3.one * scale;
                if (tm != null)
                {
                    var c = tm.color;
                    c.a = k < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (k - 0.6f) / 0.4f);
                    tm.color = c;
                }
                yield return null;
            }
            if (rt != null) UnityEngine.Object.Destroy(rt.gameObject);
        }
        private static MonoBehaviour _runner;
        private static void EnsureRunner()
        {
            if (_runner != null && _runner.gameObject != null) return;
            var go = GameObject.Find("PetPanelRunner");
            if (go == null) { go = new GameObject("PetPanelRunner"); UnityEngine.Object.DontDestroyOnLoad(go); }
            _runner = go.AddComponent<RunnerStub>();
        }
        private class RunnerStub : MonoBehaviour {}

        // ───────── Helpers (procedural sprites + funky bg, mirror RemindPanel) ─────────
        private static void ApplyRound(Button btn)
        {
            var img = btn.GetComponent<Image>();
            if (img != null) { img.sprite = LoadRoundedSprite(20); img.type = Image.Type.Sliced; }
        }

        // Fantasy backdrop — full-card forest scene (BattleOfHeroes Stage
        // Backgrounds pack), darkened so the UI on top still reads. Was a
        // procedural blob "funky" backdrop which felt flat.
        private const string FANTASY_BG_PATH =
            "Assets/BattleOfHeroes/UI/Png/Stage Backgrounds/Background02/Sample.png";

        private static void BuildFunkyBackdrop(Transform card)
        {
            var mask = new GameObject("FantasyMask",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            mask.transform.SetParent(card, false);
            var mrt = mask.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;
            var mImg = mask.GetComponent<Image>();
            mImg.sprite = LoadRoundedSprite(28); mImg.type = Image.Type.Sliced;
            mImg.color = Color.white;
            mask.GetComponent<Mask>().showMaskGraphic = false;

            // Layer 1: the fantasy stage image, stretched across the card.
            var bg = new GameObject("FantasyBg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(mask.transform, false);
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            var sp = LoadFantasyBgSprite();
            if (sp != null)
            {
                bgImg.sprite = sp;
                bgImg.type = Image.Type.Simple;
                bgImg.preserveAspect = false;
                bgImg.color = new Color(1f, 1f, 1f, 0.55f);   // dim so UI still pops
            }
            else bgImg.color = new Color(0.18f, 0.22f, 0.20f, 1f);   // forest-tinted fallback
            bgImg.raycastTarget = false;

            // Layer 2: charcoal vignette so the bright forest doesn't compete
            // with the foreground card/text.
            var dim = new GameObject("BgDim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(mask.transform, false);
            var dRT = dim.GetComponent<RectTransform>();
            dRT.anchorMin = Vector2.zero; dRT.anchorMax = Vector2.one;
            dRT.offsetMin = Vector2.zero; dRT.offsetMax = Vector2.zero;
            var dImg = dim.GetComponent<Image>();
            dImg.color = new Color(0.05f, 0.06f, 0.10f, 0.55f);
            dImg.raycastTarget = false;

            mask.transform.SetAsFirstSibling();
        }

        // One-shot sprite loader for the fantasy backdrop — kept inside the
        // panel so we don't drag a global texture cache in.
        private static Sprite _fantasyBgSp;
        private static Sprite LoadFantasyBgSprite()
        {
            if (_fantasyBgSp != null) return _fantasyBgSp;
            #if UNITY_EDITOR
            try
            {
                var imp = UnityEditor.AssetImporter.GetAtPath(FANTASY_BG_PATH) as UnityEditor.TextureImporter;
                if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite && !Application.isPlaying)
                {
                    imp.textureType = UnityEditor.TextureImporterType.Sprite;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                }
                _fantasyBgSp = Sparq.Core.SpriteLoader.Load(FANTASY_BG_PATH);
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[PetPanel] Fantasy bg load failed: {ex.Message}"); }
            #endif
            return _fantasyBgSp;
        }

        private static Sprite LoadCircleSprite()
        {
            if (_circleSp != null) return _circleSp;
            const int s = 96;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            float r = s * 0.48f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                tex.SetPixel(x, y, d <= r ? Color.white : new Color(0,0,0,0));
            }
            tex.Apply();
            _circleSp = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
            return _circleSp;
        }
        private static Sprite LoadRoundedSprite(int radius)
        {
            if (_roundedCache.TryGetValue(radius, out var sp) && sp != null) return sp;
            int size = radius * 2 + 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool inside;
                int dx = 0, dy = 0;
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
