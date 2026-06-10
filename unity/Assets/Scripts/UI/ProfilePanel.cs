using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Player Profile screen — level, XP bar, lifetime stats, equipped-gear summary,
    /// and achievement medals. Same visual language as QuestsPanel / JournalPanel.
    /// </summary>
    public static class ProfilePanel
    {
        private static readonly Color GOLD       = new Color(1f, 0.82f, 0.30f);
        private static readonly Color CREAM      = new Color(1f, 0.97f, 0.85f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.13f, 0.28f);
        private static readonly Color CARD_BG    = new Color(0.22f, 0.20f, 0.40f, 1f);
        private static readonly Color TITLE_BG   = new Color(0.42f, 0.22f, 0.68f, 1f);
        private static readonly Color BANNER_BG  = new Color(0.30f, 0.18f, 0.46f, 1f);
        private static readonly Color TILE_BG    = new Color(0.36f, 0.32f, 0.60f, 1f);

        private static GameObject _root;
        private static readonly Dictionary<int, Sprite> _roundedCache = new Dictionary<int, Sprite>();
        private static Sprite _circleSp;

        public static void Show()
        {
            if (_root != null) { Hide(); return; }

            _root = new GameObject("ProfilePanel",
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

            // Stroke
            var stroke = MakeRounded(_root.transform, "Stroke", TITLE_BG, 30);
            var srt = stroke.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(36, 136); srt.offsetMax = new Vector2(-36, -76);

            // Card — taller, more vertical headroom for all four sections
            var card = MakeRounded(_root.transform, "Card", CARD_BG, 28);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 0); crt.anchorMax = new Vector2(1, 1);
            crt.offsetMin = new Vector2(40, 140); crt.offsetMax = new Vector2(-40, -80);
            BuildFunkyBackdrop(card.transform);

            // Title shadow + bar
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

            var title = MakeText(titleBar.transform, "Title", "PROFILE",
                52, FontStyles.Bold, new Color(1f, 0.92f, 0.55f),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            title.alignment = TextAlignmentOptions.Center;
            title.outlineWidth = 0.28f;
            title.outlineColor = new Color(0.45f, 0.05f, 0.22f, 1f);

            // Back button
            var backBtn = MakeBtn(card.transform, "BackBtn", "←  BACK",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-115, -55), new Vector2(190, 80),
                GOLD, DEEP_NAVY, 28);
            backBtn.onClick.AddListener(Hide);
            var bImg = backBtn.GetComponent<Image>();
            bImg.sprite = LoadRoundedSprite(28);
            bImg.type = Image.Type.Sliced;
            var bLbl = backBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (bLbl != null) { bLbl.fontStyle = FontStyles.Bold; bLbl.outlineWidth = 0.22f; bLbl.outlineColor = new Color(1f, 0.95f, 0.7f); }

            // ── Hero card: Karu portrait + name + level + XP bar ──
            BuildHeroCard(card.transform);

            // ── Lifetime stats grid ──
            var statsHdr = MakeText(card.transform, "StatsHdr", "·  LIFETIME  ·",
                22, FontStyles.Bold, GOLD,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -470), new Vector2(0, 32));
            statsHdr.alignment = TextAlignmentOptions.Center;
            statsHdr.characterSpacing = 12f;
            statsHdr.outlineWidth = 0.18f; statsHdr.outlineColor = new Color(0.10f, 0.05f, 0);

            BuildStatsGrid(card.transform);

            // ── Gear summary ──
            var gearHdr = MakeText(card.transform, "GearHdr", "·  EQUIPPED  ·",
                22, FontStyles.Bold, GOLD,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -760), new Vector2(0, 32));
            gearHdr.alignment = TextAlignmentOptions.Center;
            gearHdr.characterSpacing = 12f;
            gearHdr.outlineWidth = 0.18f; gearHdr.outlineColor = new Color(0.10f, 0.05f, 0);

            BuildGearRow(card.transform);

            // ── Achievements row ──
            var achHdr = MakeText(card.transform, "AchHdr", "·  ACHIEVEMENTS  ·",
                22, FontStyles.Bold, GOLD,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -990), new Vector2(0, 32));
            achHdr.alignment = TextAlignmentOptions.Center;
            achHdr.characterSpacing = 12f;
            achHdr.outlineWidth = 0.18f; achHdr.outlineColor = new Color(0.10f, 0.05f, 0);

            BuildAchievementsRow(card.transform);
        }

        public static void Hide()
        {
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
            // Re-open the lobby — Sparq's true home page now.
            try { Sparq.UI.HomeLobbyPanel.Show(); }
            catch (System.Exception ex) { Debug.LogError($"[ProfilePanel] Failed to reopen lobby: {ex.Message}"); }
        }

        // ─────────── Hero card ───────────
        private static void BuildHeroCard(Transform parent)
        {
            var data = Sparq.Core.SaveService.Data;
            int level = data?.level ?? 1;
            int curXP = data?.currentXP ?? 0;
            int nextXP = data?.xpToNextLevel ?? 100;
            int coins = data?.sparqCoins ?? 0;
            string playerName = string.IsNullOrEmpty(data?.playerName) ? "Karu" : data.playerName;

            // Hero panel
            var hero = MakeRounded(parent, "Hero", BANNER_BG, 22);
            var hrt = hero.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0, -150);
            hrt.sizeDelta = new Vector2(-50, 290);

            // Portrait ring — gold border, even bigger
            var portraitRing = MakeRounded(hero.transform, "Ring", GOLD, 130);
            var prt = portraitRing.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 1); prt.anchorMax = new Vector2(0, 1);
            prt.pivot = new Vector2(0, 1);
            prt.anchoredPosition = new Vector2(20, -16);
            prt.sizeDelta = new Vector2(260, 260);
            var ringImg = portraitRing.GetComponent<Image>();
            ringImg.sprite = LoadCircleSprite();
            ringImg.type = Image.Type.Simple;

            var portrait = MakeRounded(portraitRing.transform, "Portrait", new Color(0.16f, 0.13f, 0.30f, 1f), 100);
            var portRT = portrait.GetComponent<RectTransform>();
            portRT.anchorMin = Vector2.zero; portRT.anchorMax = Vector2.one;
            portRT.offsetMin = new Vector2(8, 8); portRT.offsetMax = new Vector2(-8, -8);
            var portImg = portrait.GetComponent<Image>();
            portImg.sprite = LoadCircleSprite();
            portImg.type = Image.Type.Simple;

            // Karu sprite — fills the portrait disc with light margin.
            // Primary source: the live Karu SpriteRenderer in the home scene.
            // Fallback: load the picked hero class's idle frame straight from disk.
            // (Important during cold starts when Karu's sprite hasn't initialized yet.)
            Sprite karuSprite = null;
            var karuSrc = GameObject.Find("Karu");
            if (karuSrc != null)
            {
                var sr = karuSrc.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null) karuSprite = sr.sprite;
            }
            if (karuSprite == null)
            {
                #if UNITY_EDITOR
                var heroCls = Sparq.Systems.HeroClassResolver.Resolve();
                if (heroCls != null && !string.IsNullOrEmpty(heroCls.idleBase))
                {
                    karuSprite = Sparq.Core.SpriteLoader.Load(heroCls.idleBase + "000.png");
                }
                #endif
            }
            if (karuSprite != null)
            {
                var avatar = new GameObject("Avatar", typeof(RectTransform), typeof(Image));
                avatar.transform.SetParent(portrait.transform, false);
                var art = avatar.GetComponent<RectTransform>();
                art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one;
                art.offsetMin = new Vector2(2, 0); art.offsetMax = new Vector2(-2, 0);
                var aImg = avatar.GetComponent<Image>();
                aImg.sprite = karuSprite;
                aImg.preserveAspect = true;
                aImg.raycastTarget = false;
            }

            // Name (big, sits to the right of the portrait — auto-shrinks if too long)
            var nameTm = MakeText(hero.transform, "Name", playerName,
                46, FontStyles.Bold, GOLD,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), Vector2.zero);
            nameTm.alignment = TextAlignmentOptions.MidlineLeft;
            var nRT = nameTm.rectTransform;
            nRT.anchorMin = new Vector2(0, 1); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0, 1);
            nRT.offsetMin = new Vector2(300, -98);
            nRT.offsetMax = new Vector2(-240, -28);
            nameTm.outlineWidth = 0.22f;
            nameTm.outlineColor = new Color(0.10f, 0.05f, 0);
            nameTm.enableAutoSizing = true;
            nameTm.fontSizeMin = 26; nameTm.fontSizeMax = 46;
            nameTm.textWrappingMode = TextWrappingModes.NoWrap;

            // Make the name TAPPABLE — opens the username editor popup.
            // Subtle ✎ pencil suffix hints at editability without cluttering.
            nameTm.text = playerName + "  <size=70%><color=#FFE078FF>✎</color></size>";
            nameTm.raycastTarget = true;
            var nameBtn = nameTm.gameObject.AddComponent<UnityEngine.UI.Button>();
            nameBtn.targetGraphic = nameTm;
            nameBtn.onClick.AddListener(() => {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                Sparq.UI.UsernameEditPopup.Show(onSaved: () => {
                    // Refresh the panel by closing + reopening (cheap re-render)
                    Hide(); Show();
                });
            });

            // LV badge — directly under the name
            var lvlBadge = MakeRounded(hero.transform, "Lvl", GOLD, 18);
            var lrt = lvlBadge.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(0, 1);
            lrt.pivot = new Vector2(0, 1);
            lrt.anchoredPosition = new Vector2(300, -110);
            lrt.sizeDelta = new Vector2(140, 56);
            var lvlTm = MakeText(lvlBadge.transform, "LvlTxt", $"LV {level}",
                30, FontStyles.Bold, DEEP_NAVY,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            lvlTm.alignment = TextAlignmentOptions.Center;
            lvlTm.outlineWidth = 0.22f; lvlTm.outlineColor = new Color(1f, 0.95f, 0.7f);

            // Coins chip — top-right (deep navy so the gold coin + gold lettering pop)
            var coinChip = MakeRounded(hero.transform, "Coins", new Color(0.10f, 0.08f, 0.22f, 1f), 18);
            // Subtle gold edge around the chip
            var coinEdge = MakeRounded(hero.transform, "CoinsEdge", new Color(1f, 0.82f, 0.30f, 0.9f), 20);
            var ccrt = coinChip.GetComponent<RectTransform>();
            ccrt.anchorMin = new Vector2(1, 1); ccrt.anchorMax = new Vector2(1, 1);
            ccrt.pivot = new Vector2(1, 1);
            ccrt.anchoredPosition = new Vector2(-20, -28);
            ccrt.sizeDelta = new Vector2(260, 90);
            // Position the gold edge (3px outside the chip) and put it BEHIND the chip
            var ceRT = coinEdge.GetComponent<RectTransform>();
            ceRT.anchorMin = new Vector2(1, 1); ceRT.anchorMax = new Vector2(1, 1);
            ceRT.pivot = new Vector2(1, 1);
            ceRT.anchoredPosition = new Vector2(-17, -25);
            ceRT.sizeDelta = new Vector2(266, 96);
            coinEdge.transform.SetSiblingIndex(coinChip.transform.GetSiblingIndex()); // edge directly before chip

            // Real Layer Lab coin sprite on the left
            var coinIcon = new GameObject("CoinIcon", typeof(RectTransform), typeof(Image));
            coinIcon.transform.SetParent(coinChip.transform, false);
            var cdrt = coinIcon.GetComponent<RectTransform>();
            cdrt.anchorMin = new Vector2(0, 0.5f); cdrt.anchorMax = new Vector2(0, 0.5f);
            cdrt.pivot = new Vector2(0, 0.5f);
            cdrt.anchoredPosition = new Vector2(8, 0);
            cdrt.sizeDelta = new Vector2(72, 72);
            var coinImg = coinIcon.GetComponent<Image>();
            #if UNITY_EDITOR
            const string COIN_PATH = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/256/ItemIcon_Coin_Gold.png";
            var coinSprite = Sparq.Core.SpriteLoader.Load(COIN_PATH);
            if (coinSprite == null && !Application.isPlaying)
            {
                var imp = UnityEditor.AssetImporter.GetAtPath(COIN_PATH) as UnityEditor.TextureImporter;
                if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite)
                {
                    imp.textureType = UnityEditor.TextureImporterType.Sprite;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                    coinSprite = Sparq.Core.SpriteLoader.Load(COIN_PATH);
                }
            }
            if (coinSprite != null) { coinImg.sprite = coinSprite; coinImg.preserveAspect = true; }
            else { coinImg.sprite = LoadCircleSprite(); coinImg.color = new Color(0.95f, 0.65f, 0.10f); }
            #else
            coinImg.sprite = LoadCircleSprite(); coinImg.color = new Color(0.95f, 0.65f, 0.10f);
            #endif
            coinImg.raycastTarget = false;

            // BIG "GOLD" caption — gold lettering on dark navy
            var coinCap = MakeText(coinChip.transform, "Cap", "GOLD",
                22, FontStyles.Bold, GOLD,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            coinCap.alignment = TextAlignmentOptions.MidlineRight;
            var capRT = coinCap.rectTransform;
            capRT.anchorMin = new Vector2(0, 0.55f); capRT.anchorMax = new Vector2(1, 1);
            capRT.offsetMin = new Vector2(86, 0); capRT.offsetMax = new Vector2(-18, -4);
            coinCap.characterSpacing = 6f;
            coinCap.outlineWidth = 0.20f; coinCap.outlineColor = new Color(0.10f, 0.05f, 0);

            // Coin balance — white number
            var coinTm = MakeText(coinChip.transform, "CoinTxt", coins.ToString("N0"),
                36, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            coinTm.alignment = TextAlignmentOptions.MidlineRight;
            var ctRT = coinTm.rectTransform;
            ctRT.anchorMin = new Vector2(0, 0); ctRT.anchorMax = new Vector2(1, 0.55f);
            ctRT.offsetMin = new Vector2(86, 4); ctRT.offsetMax = new Vector2(-18, 0);
            coinTm.outlineWidth = 0.24f; coinTm.outlineColor = new Color(0.05f, 0.02f, 0.18f, 1f);

            // XP bar — full-width, taller, sits at the bottom of the hero card
            var xpTrack = MakeRounded(hero.transform, "XPTrack", new Color(0.10f, 0.05f, 0.20f, 0.95f), 14);
            var xtrt = xpTrack.GetComponent<RectTransform>();
            xtrt.anchorMin = new Vector2(0, 0); xtrt.anchorMax = new Vector2(1, 0);
            xtrt.pivot = new Vector2(0.5f, 0);
            xtrt.anchoredPosition = new Vector2(0, 24);
            xtrt.sizeDelta = new Vector2(-48, 50);

            float pct = Mathf.Clamp01((float)curXP / Mathf.Max(1, nextXP));
            var xpFill = MakeRounded(xpTrack.transform, "Fill", GOLD, 12);
            var fxrt = xpFill.GetComponent<RectTransform>();
            fxrt.anchorMin = new Vector2(0, 0); fxrt.anchorMax = new Vector2(pct, 1);
            fxrt.offsetMin = new Vector2(4, 4); fxrt.offsetMax = new Vector2(-4, -4);

            var xpTxt = MakeText(xpTrack.transform, "XPTxt", $"{curXP} / {nextXP} XP",
                24, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            xpTxt.alignment = TextAlignmentOptions.Center;
            xpTxt.outlineWidth = 0.24f; xpTxt.outlineColor = new Color(0, 0, 0, 0.85f);
        }

        // ─────────── Lifetime stats grid (2 rows × 3 cols) ───────────
        private static void BuildStatsGrid(Transform parent)
        {
            var data = Sparq.Core.SaveService.Data;

            int totalXP = data?.totalXP ?? 0;
            int tasks = data?.totalTasksDone ?? 0;
            int streak = data?.streak ?? 0;
            int longest = (int)(data?.GetType().GetField("longestStreak")?.GetValue(data) ?? 0);
            int shields = data?.streakShields ?? 0;
            int fitchLead = (data?.totalXP ?? 0) - (int)(data?.GetType().GetField("fitchXP")?.GetValue(data) ?? 0);

            var grid = new GameObject("StatsGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(parent, false);
            var grt = grid.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0, 1); grt.anchorMax = new Vector2(1, 1);
            grt.pivot = new Vector2(0.5f, 1f);
            grt.anchoredPosition = new Vector2(0, -510);
            grt.sizeDelta = new Vector2(-50, 220);
            var glg = grid.GetComponent<GridLayoutGroup>();
            glg.padding = new RectOffset(10, 10, 0, 0);
            glg.spacing = new Vector2(14, 14);
            glg.cellSize = new Vector2(310, 102);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.childAlignment = TextAnchor.UpperCenter;

            BuildStatTile(grid.transform, "TOTAL XP",   $"{totalXP:N0}", new Color(1f, 0.82f, 0.30f));
            BuildStatTile(grid.transform, "QUESTS",     $"{tasks:N0}",   new Color(0.55f, 0.85f, 0.45f));
            BuildStatTile(grid.transform, "STREAK",     $"{streak}",     new Color(1f, 0.55f, 0.30f));
            BuildStatTile(grid.transform, "BEST",       $"{longest}",    new Color(0.85f, 0.45f, 0.85f));
            BuildStatTile(grid.transform, "SHIELDS",    $"{shields}",    new Color(0.55f, 0.85f, 1f));
            BuildStatTile(grid.transform, "VS FITCH",   fitchLead >= 0 ? $"+{fitchLead}" : $"{fitchLead}",
                          fitchLead >= 0 ? new Color(0.55f, 0.85f, 0.45f) : new Color(0.85f, 0.40f, 0.45f));
        }

        private static void BuildStatTile(Transform parent, string label, string value, Color accent)
        {
            var tile = MakeRounded(parent, $"Tile_{label}", TILE_BG, 16);

            // Top header band — accent color with the label
            var hdr = MakeRounded(tile.transform, "Hdr", accent, 14);
            var srt = hdr.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.offsetMin = new Vector2(6, 0); srt.offsetMax = new Vector2(-6, -6);
            srt.sizeDelta = new Vector2(0, 36);

            var lblTm = MakeText(hdr.transform, "Lbl", label,
                22, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            lblTm.alignment = TextAlignmentOptions.Center;
            lblTm.characterSpacing = 4f;
            lblTm.outlineWidth = 0.32f;
            lblTm.outlineColor = new Color(0.05f, 0.02f, 0.18f, 1f);

            // Big value centered in the body
            var valTm = MakeText(tile.transform, "Val", value,
                46, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            valTm.alignment = TextAlignmentOptions.Center;
            var valRT = valTm.rectTransform;
            valRT.anchorMin = new Vector2(0, 0); valRT.anchorMax = new Vector2(1, 1);
            valRT.offsetMin = new Vector2(0, 6); valRT.offsetMax = new Vector2(0, -42);
            valTm.outlineWidth = 0.26f;
            valTm.outlineColor = new Color(0, 0, 0, 0.85f);
            valTm.enableAutoSizing = true;
            valTm.fontSizeMin = 30; valTm.fontSizeMax = 50;
        }

        // ─────────── Equipped gear summary ───────────
        private static void BuildGearRow(Transform parent)
        {
            (int atk, int def, int hp) = (0, 0, 0);
            try { (atk, def, hp) = Sparq.Systems.EquipmentService.TotalStats(); } catch {}

            var row = new GameObject("GearRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(1, 1);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0, -800);
            rrt.sizeDelta = new Vector2(-50, 160);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 0, 0);
            hlg.spacing = 14;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            BuildGearTile(row.transform, "ATK",  $"+{atk}", new Color(0.95f, 0.45f, 0.40f));
            BuildGearTile(row.transform, "DEF",  $"+{def}", new Color(0.45f, 0.65f, 0.95f));
            BuildGearTile(row.transform, "HP",   $"+{hp}",  new Color(0.55f, 0.85f, 0.45f));
        }

        private static void BuildGearTile(Transform parent, string label, string value, Color tint)
        {
            var tile = MakeRounded(parent, $"Gear_{label}", TILE_BG, 16);

            // Circular badge centered top
            var badge = MakeRounded(tile.transform, "Badge", tint, 40);
            var brt = badge.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 1); brt.anchorMax = new Vector2(0.5f, 1);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.anchoredPosition = new Vector2(0, -10);
            brt.sizeDelta = new Vector2(76, 76);
            var bImg = badge.GetComponent<Image>();
            bImg.sprite = LoadCircleSprite();
            bImg.type = Image.Type.Simple;

            var bTm = MakeText(badge.transform, "BTxt", label,
                26, FontStyles.Bold, Color.white,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            bTm.alignment = TextAlignmentOptions.Center;
            bTm.outlineWidth = 0.30f; bTm.outlineColor = new Color(0, 0, 0, 0.85f);

            // Big value below badge
            var valTm = MakeText(tile.transform, "Val", value,
                42, FontStyles.Bold, GOLD,
                new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero);
            valTm.alignment = TextAlignmentOptions.Center;
            var vRT = valTm.rectTransform;
            vRT.anchorMin = new Vector2(0, 0); vRT.anchorMax = new Vector2(1, 0);
            vRT.pivot = new Vector2(0.5f, 0);
            vRT.anchoredPosition = new Vector2(0, 16);
            vRT.sizeDelta = new Vector2(0, 50);
            valTm.outlineWidth = 0.24f; valTm.outlineColor = new Color(0.10f, 0.05f, 0);
        }

        // ─────────── Achievements row ───────────
        private static void BuildAchievementsRow(Transform parent)
        {
            var data = Sparq.Core.SaveService.Data;
            int totalXP = data?.totalXP ?? 0;
            int tasks = data?.totalTasksDone ?? 0;
            int streak = data?.streak ?? 0;
            int level = data?.level ?? 1;

            // Pre-wired achievement criteria
            var medals = new (string label, bool earned)[]
            {
                ("FIRST QUEST",  tasks >= 1),
                ("10 QUESTS",    tasks >= 10),
                ("50 QUESTS",    tasks >= 50),
                ("3-DAY STREAK", streak >= 3),
                ("7-DAY STREAK", streak >= 7),
                ("LV 5",         level >= 5),
                ("LV 10",        level >= 10),
                ("1K XP",        totalXP >= 1000),
            };

            var grid = new GameObject("AchGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(parent, false);
            var rrt = grid.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0, 1); rrt.anchorMax = new Vector2(1, 1);
            rrt.pivot = new Vector2(0.5f, 1f);
            rrt.anchoredPosition = new Vector2(0, -1040);
            rrt.sizeDelta = new Vector2(-50, 320);
            var glg = grid.GetComponent<GridLayoutGroup>();
            glg.padding = new RectOffset(10, 10, 0, 0);
            glg.spacing = new Vector2(14, 14);
            glg.cellSize = new Vector2(238, 150);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;
            glg.childAlignment = TextAnchor.UpperCenter;

            foreach (var (lbl, earned) in medals) BuildMedal(grid.transform, lbl, earned);
        }

        private static void BuildMedal(Transform parent, string label, bool earned)
        {
            var tile = MakeRounded(parent, $"Med_{label}", TILE_BG, 16);

            // Medal disc — gold ring around inner disc
            var ring = MakeRounded(tile.transform, "Ring",
                earned ? GOLD : new Color(0.30f, 0.28f, 0.45f, 1f), 50);
            var drt = ring.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.5f, 1); drt.anchorMax = new Vector2(0.5f, 1);
            drt.pivot = new Vector2(0.5f, 1f);
            drt.anchoredPosition = new Vector2(0, -14);
            drt.sizeDelta = new Vector2(94, 94);
            var rImg = ring.GetComponent<Image>();
            rImg.sprite = LoadCircleSprite();
            rImg.type = Image.Type.Simple;

            var disc = MakeRounded(ring.transform, "Disc",
                earned ? new Color(1f, 0.92f, 0.55f) : new Color(0.40f, 0.38f, 0.55f, 1f), 44);
            var diRT = disc.GetComponent<RectTransform>();
            diRT.anchorMin = Vector2.zero; diRT.anchorMax = Vector2.one;
            diRT.offsetMin = new Vector2(8, 8); diRT.offsetMax = new Vector2(-8, -8);
            var diImg = disc.GetComponent<Image>();
            diImg.sprite = LoadCircleSprite();
            diImg.type = Image.Type.Simple;

            // Star — bigger and bolder
            Color rayColor = earned ? DEEP_NAVY : new Color(0.65f, 0.62f, 0.78f, 1f);
            DrawCircleAt(disc.transform, rayColor, 0.5f, 0.5f, 26);
            for (int i = 0; i < 5; i++)
            {
                float ang = -90f + i * 72f;
                float rad = ang * Mathf.Deg2Rad;
                float ax = 0.5f + Mathf.Cos(rad) * 0.32f;
                float ay = 0.5f + Mathf.Sin(rad) * 0.32f;
                DrawRectAt(disc.transform, rayColor, ax, ay, 12, 22, ang + 90f);
            }

            // Label — bigger and auto-fit so longer names like "3-DAY STREAK" fit
            var lblTm = MakeText(tile.transform, "Lbl", label,
                17, FontStyles.Bold, earned ? CREAM : new Color(1, 1, 1, 0.55f),
                new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, Vector2.zero);
            lblTm.alignment = TextAlignmentOptions.Center;
            var lblRT = lblTm.rectTransform;
            lblRT.anchorMin = new Vector2(0, 0); lblRT.anchorMax = new Vector2(1, 0);
            lblRT.pivot = new Vector2(0.5f, 0);
            lblRT.anchoredPosition = new Vector2(0, 12);
            lblRT.sizeDelta = new Vector2(-16, 44);
            lblTm.characterSpacing = 3f;
            lblTm.outlineWidth = 0.22f;
            lblTm.outlineColor = new Color(0, 0, 0, 0.8f);
            lblTm.enableAutoSizing = true;
            lblTm.fontSizeMin = 12; lblTm.fontSizeMax = 18;
            lblTm.textWrappingMode = TextWrappingModes.Normal;
        }

        private static void DrawCircleAt(Transform parent, Color color, float anchorX, float anchorY, float size)
        {
            var go = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorX, anchorY); rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.sprite = LoadCircleSprite(); img.color = color; img.raycastTarget = false;
        }
        private static void DrawRectAt(Transform parent, Color color, float anchorX, float anchorY,
            float w, float h, float angleDeg)
        {
            var go = new GameObject("Ray", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(anchorX, anchorY); rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.localRotation = Quaternion.Euler(0, 0, angleDeg);
            var img = go.GetComponent<Image>();
            img.sprite = LoadRoundedSprite(4); img.type = Image.Type.Sliced;
            img.color = color; img.raycastTarget = false;
        }

        // ─────────── helpers ───────────
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
            img.sprite = LoadRoundedSprite(radius);
            img.type = Image.Type.Sliced;
            img.color = color;
            return go;
        }
        // Funky pastel-blob backdrop — clipped to the rounded card edge
        private static void BuildFunkyBackdrop(Transform card)
        {
            var mask = new GameObject("FunkyMask",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            mask.transform.SetParent(card, false);
            var mrt = mask.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;
            var mImg = mask.GetComponent<Image>();
            mImg.sprite = LoadRoundedSprite(28); mImg.type = Image.Type.Sliced;
            mImg.color = Color.white;
            mask.GetComponent<Mask>().showMaskGraphic = false;
            (float ax, float ay, float size, Color col)[] blobs = {
                (0.10f, 0.95f, 320, new Color(0.42f, 0.22f, 0.68f, 0.22f)),
                (0.95f, 0.78f, 280, new Color(1.00f, 0.82f, 0.30f, 0.20f)),
                (0.60f, 0.55f, 380, new Color(0.45f, 0.85f, 0.65f, 0.18f)),
                (0.05f, 0.40f, 260, new Color(0.55f, 0.62f, 0.95f, 0.22f)),
                (0.85f, 0.18f, 320, new Color(0.92f, 0.55f, 0.85f, 0.22f)),
                (0.25f, 0.10f, 240, new Color(0.55f, 0.85f, 1.00f, 0.20f)),
            };
            foreach (var b in blobs)
            {
                var go = new GameObject("Blob", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(mask.transform, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(b.ax, b.ay); rt.anchorMax = new Vector2(b.ax, b.ay);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(b.size, b.size);
                var img = go.GetComponent<Image>();
                img.sprite = LoadCircleSprite(); img.color = b.col; img.raycastTarget = false;
            }
            for (int i = 0; i < 40; i++)
            {
                var dot = new GameObject("Sparkle", typeof(RectTransform), typeof(Image));
                dot.transform.SetParent(mask.transform, false);
                var rt = dot.GetComponent<RectTransform>();
                float ax = ((i * 73) % 100) / 100f;
                float ay = ((i * 47 + 13) % 100) / 100f;
                rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(ax, ay);
                rt.pivot = new Vector2(0.5f, 0.5f);
                float s = 4 + (i % 5) * 2;
                rt.sizeDelta = new Vector2(s, s);
                var img = dot.GetComponent<Image>();
                img.sprite = LoadCircleSprite();
                img.color = new Color(1f, 0.97f, 0.65f, 0.22f + (i % 4) * 0.05f);
                img.raycastTarget = false;
            }
            mask.transform.SetAsFirstSibling();
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
            rt.anchoredPosition = anch;
            rt.sizeDelta = sd;
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = size;
            tm.fontStyle = style;
            tm.color = color;
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
            rt.anchoredPosition = anch;
            rt.sizeDelta = sd;
            go.GetComponent<Image>().color = bg;
            var t = new GameObject("Lbl", typeof(RectTransform));
            t.transform.SetParent(go.transform, false);
            var trt = t.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var tm = t.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = fontSize;
            tm.fontStyle = FontStyles.Bold;
            tm.color = fg;
            tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return go.GetComponent<Button>();
        }
    }
}
