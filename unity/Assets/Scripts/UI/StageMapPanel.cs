using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Sparq.Systems;

namespace Sparq.UI
{
    /// <summary>
    /// Stage map — painted top-down view using Fantasy World 2D square tiles.
    /// Grass field with a winding path, scattered trees + shrubs + stones,
    /// 8 stage nodes positioned along the path.
    /// </summary>
    public static class StageMapPanel
    {
        // Palette
        private static readonly Color GOLD       = new Color(1f, 0.78f, 0.22f);
        private static readonly Color CREAM      = new Color(1f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f);

        // Asset roots
        private const string FW = "Assets/Fantasy World 2D/Sprites/PNG/";
        private const string TILE_GRASS = FW + "tiles/tile_grass_1/tile_grass_1_1.png";
        private const string TILE_PATH  = FW + "tiles/tile_path/tile_path_1.png";
        private const string TILE_EARTH = FW + "tiles/tile_earth_1/tile_earth_1_1.png";

        // Grid config (square tiles)
        private const int   GRID_COLS = 8;
        private const int   GRID_ROWS = 9;
        private const float TILE_SIZE = 140f;

        // Stage positions (col, row) along a winding path — bottom = small index
        private static readonly (int col, int row)[] STAGE_TILES = new[]
        {
            (1, 0), (3, 1), (5, 2), (3, 3), (5, 4), (2, 5), (5, 6), (3, 8),
        };

        // Path tiles connecting stages — visible path on the ground
        private static readonly (int col, int row)[] PATH_TILES = new[]
        {
            (1,0),(2,0),(2,1),(3,1),(4,1),(4,2),(5,2),(5,3),(4,3),(3,3),
            (3,4),(4,4),(5,4),(4,5),(3,5),(2,5),(3,6),(4,6),(5,6),(4,7),(3,7),(3,8),
        };

        // Decor placement: (col, row, type)  T=tree (1-22), S=shrub (1-13), s=stone (1-12)
        // Skip cells used by stage nodes & path tiles.
        private static readonly (int col, int row, char type, int variant)[] DECOR = new[]
        {
            (0, 0, 'T', 1), (4, 0, 'S', 5),  (7, 0, 'T', 4),
            (0, 1, 'T', 7), (6, 1, 'T', 9),
            (0, 2, 'S', 3), (3, 2, 'T', 11), (7, 2, 'T', 12),
            (0, 3, 'T', 14), (6, 3, 's', 1),
            (1, 4, 'T', 6), (7, 4, 'T', 16),
            (0, 5, 'T', 18), (4, 5, 's', 4), (6, 5, 'T', 20),
            (0, 6, 'S', 7), (3, 6, 'T', 22),  (7, 6, 'T', 2),
            (0, 7, 'T', 8), (5, 7, 'T', 13), (6, 7, 'S', 9),
            (0, 8, 'T', 15), (1, 8, 'T', 19), (5, 8, 'T', 17), (7, 8, 'T', 5),
        };

        private static GameObject _root;

        public static void Show()
        {
            if (_root != null) Object.Destroy(_root);

            // Pause home BGM and start map BGM so the map has its own soundtrack
            try { Sparq.UI.HomeBgm.Pause(); } catch {}
            try { Sparq.UI.MapBgm.Ensure(); } catch {}

            // CRITICAL: Unity 6 scenes can ship without an EventSystem. Without
            // one, no Button.onClick fires anywhere — clicks just hit dead air.
            // Be defensive: create one if missing.
            EnsureEventSystem();

            _root = new GameObject("StageMapPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var c = _root.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            // Sort above every other canvas in the scene so nothing can sit on top
            // and intercept our clicks. Walk all canvases and bump if anything
            // happens to outrank us (e.g. a stale popup, a VFX overlay).
            int maxSort = 14400;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            }
            c.sortingOrder = maxSort + 10;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // Solid dim bg behind everything
            var bg = MakeImg(_root.transform, "Bg", new Color(0.06f, 0.05f, 0.10f, 1f));
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero; brt.offsetMax = Vector2.zero;

            // Map area — anchored to bottom-center, offset upward so it never crashes header
            var mapArea = new GameObject("MapArea", typeof(RectTransform));
            mapArea.transform.SetParent(_root.transform, false);
            var mart = mapArea.GetComponent<RectTransform>();
            mart.anchorMin = new Vector2(0.5f, 0f); mart.anchorMax = new Vector2(0.5f, 0f);
            mart.pivot = new Vector2(0.5f, 0f);
            mart.anchoredPosition = new Vector2(0f, 80f);
            mart.sizeDelta = new Vector2(1, 1);

            // Try to use the polished BattleOfHeroes LvlMap background first.
            // If found, skip the procedural grass/path/decor entirely — the
            // pre-painted LvlMap art is the entire scene at once.
            if (TryBuildBattleOfHeroesMap(mapArea.transform))
            {
                BuildStageNodes(mapArea.transform);
            }
            else
            {
                BuildTileGrid(mapArea.transform);
                BuildPathTiles(mapArea.transform);
                BuildDecor(mapArea.transform);
                BuildStageNodes(mapArea.transform);
            }

            // Top header bar (rendered LAST so always on top)
            var topBar = MakeImg(_root.transform, "TopBar", new Color(0, 0, 0, 0.85f));
            var tbrt = topBar.GetComponent<RectTransform>();
            tbrt.anchorMin = new Vector2(0, 1); tbrt.anchorMax = new Vector2(1, 1);
            tbrt.pivot = new Vector2(0.5f, 1);
            tbrt.sizeDelta = new Vector2(0, 130);
            MakeText(topBar.transform, "Hdr", "FOREST OF TRIALS",
                40, FontStyles.Bold, GOLD,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -16), new Vector2(0, 50))
                .alignment = TextAlignmentOptions.Center;
            MakeText(topBar.transform, "Sub", $"Chapter 1   ·   {StageService.TotalStars()} / {StageService.CHAPTER1.Length * 3} stars",
                22, FontStyles.Bold, CREAM,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -72), new Vector2(0, 32))
                .alignment = TextAlignmentOptions.Center;
            topBar.transform.SetAsLastSibling();

            // ← BACK button matching the rest of the panels
            var back = MakeBtn(_root.transform, "BackBtn", "←  BACK",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(110, -55), new Vector2(190, 80),
                GOLD, DEEP_NAVY, 28);
            back.onClick.AddListener(() => {
                // Stop map BGM and resume home BGM when the map closes
                try { Sparq.UI.MapBgm.Stop(); } catch {}
                try { Sparq.UI.HomeBgm.Resume(); } catch {}
                Object.Destroy(_root);
                // Re-open the lobby — that's our home now, not the legacy scene.
                try { Sparq.UI.HomeLobbyPanel.Show(); }
                catch (System.Exception ex) { Debug.LogError($"[StageMapPanel] Failed to reopen lobby: {ex.Message}"); }
            });
            // Style the label: bold navy with cream halo (matches every other panel)
            var bLbl = back.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (bLbl != null) { bLbl.fontStyle = FontStyles.Bold; bLbl.outlineWidth = 0.22f; bLbl.outlineColor = new Color(1f, 0.95f, 0.7f); }
            back.transform.SetAsLastSibling();
        }

        private static Vector2 TileToPos(int col, int row)
        {
            float x = col * TILE_SIZE - (GRID_COLS - 1) * TILE_SIZE * 0.5f;
            float y = row * TILE_SIZE; // start at bottom of map area
            return new Vector2(x, y);
        }

        // ───────── Grass field ─────────
        // Try to use the BattleOfHeroes pre-painted LvlMap art as the full
        // background. Returns true if the sprite loaded — caller skips the
        // procedural grass/path/decor in that case.
        private static bool TryBuildBattleOfHeroesMap(Transform parent)
        {
            #if UNITY_EDITOR
            string[] candidates = {
                // FantasyMaps — colorful lush painted level select (best Top Heroes feel)
                "Assets/FantasyMaps/_PNG/01/map01_preview-01.png",
                "Assets/FantasyMaps/_PNG/02/map02_preview-01.png",
                // BattleOfHeroes pre-painted maps
                "Assets/BattleOfHeroes/UI/Png/User interfaces/LvlMap01.png",
                "Assets/BattleOfHeroes/UI/Png/User interfaces/LvlMap02.png",
                "Assets/BattleOfHeroes/UI/Png/User interfaces/LvlMap03.png",
                // LevelMapAssets — dark night theme — last resort
                "Assets/LevelMapAssets/Png/LevelAreaFull.png",
            };
            Sprite mapSp = null;
            foreach (var p in candidates)
            {
                // Force-import as Sprite if not already (works in play mode too — just reimports synchronously).
                var imp = UnityEditor.AssetImporter.GetAtPath(p) as UnityEditor.TextureImporter;
                if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite)
                {
                    imp.textureType = UnityEditor.TextureImporterType.Sprite;
                    imp.alphaIsTransparency = true;
                    imp.spriteImportMode = UnityEditor.SpriteImportMode.Single;
                    imp.SaveAndReimport();
                    UnityEditor.AssetDatabase.ImportAsset(p,
                        UnityEditor.ImportAssetOptions.ForceSynchronousImport);
                }
                mapSp = Sparq.Core.SpriteLoader.Load(p);
                if (mapSp != null) { Debug.Log($"[StageMap] Loaded background: {p}"); break; }
            }
            if (mapSp == null)
            {
                Debug.LogWarning("[StageMap] No FantasyMaps/BoH map sprite loaded — falling back to procedural grass tiles.");
                return false;
            }

            // Parent the map background to the PANEL ROOT (not mapArea — which
            // is a tiny 1×1 anchor container) so it fills the visible space.
            var bgParent = parent;
            var rootCanvas = parent.GetComponentInParent<Canvas>();
            if (rootCanvas != null) bgParent = rootCanvas.transform;
            var bg = new GameObject("LvlMapBg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(bgParent, false);
            bg.transform.SetAsFirstSibling();      // behind everything else
            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = bg.GetComponent<Image>();
            img.sprite = mapSp;
            img.preserveAspect = false;            // STRETCH to fill the panel
            img.raycastTarget = false;
            img.color = Color.white;
            // Ensure the dark "Bg" placeholder created earlier is below us
            var darkBg = bgParent.Find("Bg");
            if (darkBg != null) darkBg.SetSiblingIndex(0);
            bg.transform.SetSiblingIndex(1);       // sit just above the dark Bg
            return true;
            #else
            return false;
            #endif
        }

        private static void BuildTileGrid(Transform parent)
        {
#if UNITY_EDITOR
            EnsureSpriteSimple(TILE_GRASS);
#endif
            var grass = Sparq.Core.SpriteLoader.Load(TILE_GRASS);

            for (int row = 0; row < GRID_ROWS; row++)
            for (int col = 0; col < GRID_COLS; col++)
            {
                var t = MakeImg(parent, "G", Color.white);
                var rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = TileToPos(col, row);
                rt.sizeDelta = new Vector2(TILE_SIZE, TILE_SIZE);
                var img = t.GetComponent<Image>();
                if (grass != null) { img.sprite = grass; img.preserveAspect = false; }
                else img.color = new Color(0.45f, 0.65f, 0.30f);
                img.raycastTarget = false;
            }
        }

        // ───────── Path tiles overlaid on grass ─────────
        private static void BuildPathTiles(Transform parent)
        {
#if UNITY_EDITOR
            EnsureSpriteSimple(TILE_PATH);
#endif
            var path = Sparq.Core.SpriteLoader.Load(TILE_PATH);
            if (path == null) return;

            foreach (var p in PATH_TILES)
            {
                var t = MakeImg(parent, "P", new Color(1, 1, 1, 0.92f));
                var rt = t.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = TileToPos(p.col, p.row);
                rt.sizeDelta = new Vector2(TILE_SIZE, TILE_SIZE);
                var img = t.GetComponent<Image>();
                img.sprite = path;
                img.raycastTarget = false;
            }
        }

        // ───────── Decor (trees, shrubs, stones) ─────────
        private static void BuildDecor(Transform parent)
        {
            foreach (var d in DECOR)
            {
                string folder = d.type switch {
                    'T' => "trees/cartoon_world_tree_",
                    'S' => "shrubs/cartoon_world_shrub_",
                    's' => "stones/cartoon_world_stone_",
                    _ => null
                };
                if (folder == null) continue;
                string spritePath = FW + folder + d.variant + ".png";
#if UNITY_EDITOR
                EnsureSpriteSimple(spritePath);
#endif
                var sp = Sparq.Core.SpriteLoader.Load(spritePath);
                if (sp == null) continue;

                var prop = MakeImg(parent, "Decor", Color.white);
                var rt = prop.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
                rt.pivot = new Vector2(0.5f, 0.2f); // pivot near base for natural standing
                rt.anchoredPosition = TileToPos(d.col, d.row);
                rt.sizeDelta = d.type == 'T'
                    ? new Vector2(TILE_SIZE * 1.4f, TILE_SIZE * 1.6f)
                    : new Vector2(TILE_SIZE * 0.9f, TILE_SIZE * 0.9f);
                var img = prop.GetComponent<Image>();
                img.sprite = sp;
                img.preserveAspect = true;
                img.raycastTarget = false;
            }
        }

        // ───────── Stage nodes ─────────
        private static void BuildStageNodes(Transform parent)
        {
            for (int i = 0; i < StageService.CHAPTER1.Length; i++)
            {
                var stage = StageService.CHAPTER1[i];
                var (col, row) = STAGE_TILES[i];
                Vector2 pos = TileToPos(col, row);

                // Particle trail from previous completed → this current node
                if (i > 0)
                {
                    var prevStage = StageService.CHAPTER1[i - 1];
                    bool prevCompleted = StageService.IsCompleted(prevStage.index);
                    bool thisCurrent   = StageService.IsUnlocked(stage.index)
                                         && !StageService.IsCompleted(stage.index);
                    if (prevCompleted && thisCurrent)
                    {
                        var (pc, pr) = STAGE_TILES[i - 1];
                        Vector2 prevPos = TileToPos(pc, pr);
                        BuildTrail(parent, prevPos, pos);
                    }
                }

                BuildNode(parent, stage, pos);
            }
        }

        // Glowing dotted path from completed stage → current "tap me" stage
        private static void BuildTrail(Transform parent, Vector2 fromPos, Vector2 toPos)
        {
            const int DOT_COUNT = 7;
            for (int i = 1; i <= DOT_COUNT; i++)
            {
                float t = (float)i / (DOT_COUNT + 1);
                Vector2 p = Vector2.Lerp(fromPos, toPos, t);

                var dot = MakeImg(parent, $"Trail_{i}", new Color(1f, 0.82f, 0.30f, 0.85f));
                dot.GetComponent<Image>().sprite = LoadCircleSprite();
                dot.GetComponent<Image>().raycastTarget = false;
                var rt = dot.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = p;
                rt.sizeDelta = new Vector2(22, 22);

                // Each dot pulses with a phase offset → flowing-energy effect
                var pulse = dot.AddComponent<HaloPulse>();
                pulse.minScale = 0.55f; pulse.maxScale = 1.15f;
                pulse.minAlpha = 0.30f; pulse.maxAlpha = 0.95f;
                pulse.speed = 1.4f;
                pulse.phase = i * 0.18f; // staggered so dots "march" toward the goal
            }
        }

        private static void BuildNode(Transform parent, StageService.Stage stage, Vector2 pos)
        {
            bool unlocked  = StageService.IsUnlocked(stage.index);
            bool completed = StageService.IsCompleted(stage.index);
            bool current   = unlocked && !completed;
            int  stars     = StageService.StarsFor(stage.index);

            // Drop shadow
            var shadow = MakeImg(parent, "Sh", new Color(0, 0, 0, 0.55f));
            shadow.GetComponent<Image>().sprite = LoadCircleSprite();
            shadow.GetComponent<Image>().raycastTarget = false;
            var shrt = shadow.GetComponent<RectTransform>();
            shrt.anchorMin = new Vector2(0.5f, 0); shrt.anchorMax = new Vector2(0.5f, 0);
            shrt.pivot = new Vector2(0.5f, 0.5f);
            shrt.anchoredPosition = pos + new Vector2(0, -10);
            shrt.sizeDelta = new Vector2(140, 140);

            // ── Pulsing GLOW HALO on the current stage (Top Heroes-style "tap me") ──
            if (current)
            {
                // Outer slow halo
                var haloOuter = MakeImg(parent, "HaloOuter", new Color(1f, 0.92f, 0.40f, 0.30f));
                haloOuter.GetComponent<Image>().sprite = LoadCircleSprite();
                haloOuter.GetComponent<Image>().raycastTarget = false;
                var hoRT = haloOuter.GetComponent<RectTransform>();
                hoRT.anchorMin = new Vector2(0.5f, 0); hoRT.anchorMax = new Vector2(0.5f, 0);
                hoRT.pivot = new Vector2(0.5f, 0.5f);
                hoRT.anchoredPosition = pos;
                hoRT.sizeDelta = new Vector2(260, 260);
                var ha = haloOuter.AddComponent<HaloPulse>();
                ha.minScale = 0.95f; ha.maxScale = 1.25f;
                ha.minAlpha = 0.10f; ha.maxAlpha = 0.45f;
                ha.speed = 1.2f;

                // Inner bright pulse
                var haloInner = MakeImg(parent, "HaloInner", new Color(1f, 0.78f, 0.20f, 0.55f));
                haloInner.GetComponent<Image>().sprite = LoadCircleSprite();
                haloInner.GetComponent<Image>().raycastTarget = false;
                var hiRT = haloInner.GetComponent<RectTransform>();
                hiRT.anchorMin = new Vector2(0.5f, 0); hiRT.anchorMax = new Vector2(0.5f, 0);
                hiRT.pivot = new Vector2(0.5f, 0.5f);
                hiRT.anchoredPosition = pos;
                hiRT.sizeDelta = new Vector2(190, 190);
                var hi = haloInner.AddComponent<HaloPulse>();
                hi.minScale = 1.0f; hi.maxScale = 1.12f;
                hi.minAlpha = 0.40f; hi.maxAlpha = 0.85f;
                hi.speed = 2.0f; hi.phase = 0.5f;
            }

            // Sparkle for completed
            if (completed)
            {
                var sparkle = MakeImg(parent, "Sk", new Color(GOLD.r, GOLD.g, GOLD.b, 0.5f));
                sparkle.GetComponent<Image>().sprite = LoadStarBurstSprite();
                sparkle.GetComponent<Image>().raycastTarget = false;
                var sprt = sparkle.GetComponent<RectTransform>();
                sprt.anchorMin = new Vector2(0.5f, 0); sprt.anchorMax = new Vector2(0.5f, 0);
                sprt.pivot = new Vector2(0.5f, 0.5f);
                sprt.anchoredPosition = pos;
                sprt.sizeDelta = new Vector2(220, 220);
                sparkle.AddComponent<SparkleSpinner>();
            }

            // Node circle
            var node = new GameObject($"Stage_{stage.index}",
                typeof(RectTransform), typeof(Image), typeof(Button));
            node.transform.SetParent(parent, false);
            var rt = node.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(150, 150);

            // ── LevelMapAssets pack — dedicated stage-node icons ──
            // AvailableLvl.png = unlocked/active, LockLvl.png = locked
            // (Previously this whole block was #if UNITY_EDITOR — that's why
            //  stage nodes rendered as white placeholder circles in builds.)
            string nodePath = !unlocked
                ? "Assets/LevelMapAssets/Png/LockLvl.png"
                : "Assets/LevelMapAssets/Png/AvailableLvl.png";
#if UNITY_EDITOR
            EnsureSpriteSimple(nodePath);
#endif
            var circleSp = Sparq.Core.SpriteLoader.Load(nodePath);
            // Fallback chain — FantasyMaps then Layer Lab
            if (circleSp == null)
            {
                string fmBtnPath =
                    !unlocked  ? "Assets/FantasyMaps/_PNG/Parts/buttons/button04.png" :
                    completed  ? "Assets/FantasyMaps/_PNG/Parts/buttons/button02.png" :
                    current    ? "Assets/FantasyMaps/_PNG/Parts/buttons/button01.png" :
                                 "Assets/FantasyMaps/_PNG/Parts/buttons/button03.png";
#if UNITY_EDITOR
                EnsureSpriteSimple(fmBtnPath);
#endif
                circleSp = Sparq.Core.SpriteLoader.Load(fmBtnPath);
            }
            if (circleSp == null)
            {
                string circlePath = !unlocked
                    ? "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Button/Button_Circle_01_Gray.Png"
                    : completed
                        ? "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Button/Button_Circle_01_Yellow.Png"
                        : "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Button/Button_Circle_01_Red.Png";
                circleSp = Sparq.Core.SpriteLoader.Load(circlePath);
            }

            var img = node.GetComponent<Image>();
            if (circleSp != null) { img.sprite = circleSp; img.preserveAspect = true; }
            else img.sprite = LoadCircleSprite();

            // Number — try FantasyMaps' numbered PNGs (colored or gray) first
            #if UNITY_EDITOR
            Sprite numSp = null;
            if (stage.index >= 0 && stage.index <= 9)
            {
                string suffix = unlocked ? ".png" : "-gray.png";
                string numPath = $"Assets/FantasyMaps/_PNG/Parts/buttons/num0{stage.index}{suffix}";
                EnsureSpriteSimple(numPath);
                numSp = Sparq.Core.SpriteLoader.Load(numPath);
            }
            if (numSp != null)
            {
                // Number sprite is small + tall (15×40-ish) — center it at moderate
                // size so it fits inside the button without overflowing.
                var numGO = new GameObject("Num", typeof(RectTransform), typeof(Image));
                numGO.transform.SetParent(node.transform, false);
                var nrt = numGO.GetComponent<RectTransform>();
                nrt.anchorMin = new Vector2(0.5f, 0.5f); nrt.anchorMax = new Vector2(0.5f, 0.5f);
                nrt.pivot = new Vector2(0.5f, 0.5f);
                nrt.anchoredPosition = Vector2.zero;
                nrt.sizeDelta = new Vector2(34, 48);   // matches PNG aspect (15:40), tightened so the digit sits inside the badge
                var nImg = numGO.GetComponent<Image>();
                nImg.sprite = numSp;
                nImg.preserveAspect = true;
                nImg.raycastTarget = false;
            }
            else
            #endif
            {
                var num = MakeText(node.transform, "Num", stage.index.ToString(),
                    54, FontStyles.Bold, !unlocked ? new Color(1, 1, 1, 0.45f) : Color.white,
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                num.alignment = TextAlignmentOptions.Center;
                num.outlineWidth = 0.32f;
                num.outlineColor = new Color(0.10f, 0.05f, 0.02f, 0.95f);
            }

            // Pulse on current
            if (current)
            {
                var p = node.AddComponent<PulseAnimator>();
                p.minScale = 0.97f; p.maxScale = 1.10f; p.speed = 1.4f;
            }

            // Lock overlay
            if (!unlocked)
            {
                #if UNITY_EDITOR
                var lockSp = Sparq.Core.SpriteLoader.Load(
                    "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/256/PictoIcon_Lock.Png");
                if (lockSp != null)
                {
                    var lk = MakeImg(node.transform, "Lk", new Color(1, 1, 1, 0.9f));
                    lk.GetComponent<Image>().sprite = lockSp;
                    lk.GetComponent<Image>().preserveAspect = true;
                    lk.GetComponent<Image>().raycastTarget = false;
                    var lrt = lk.GetComponent<RectTransform>();
                    lrt.anchorMin = new Vector2(0.5f, 0.5f); lrt.anchorMax = new Vector2(0.5f, 0.5f);
                    lrt.pivot = new Vector2(0.5f, 0.5f);
                    lrt.anchoredPosition = Vector2.zero;
                    lrt.sizeDelta = new Vector2(80, 80);
                }
                #endif
            }

            // Stars below
            if (unlocked)
            {
                var starRow = new GameObject("Stars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                starRow.transform.SetParent(parent, false);
                var sRT = starRow.GetComponent<RectTransform>();
                sRT.anchorMin = new Vector2(0.5f, 0); sRT.anchorMax = new Vector2(0.5f, 0);
                sRT.pivot = new Vector2(0.5f, 0.5f);
                sRT.anchoredPosition = pos + new Vector2(0, -100);
                sRT.sizeDelta = new Vector2(120, 30);
                var hlg = starRow.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 4; hlg.childAlignment = TextAnchor.MiddleCenter;

                #if UNITY_EDITOR
                var starSp = Sparq.Core.SpriteLoader.Load(
                    "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/256/PictoIcon_Star.Png");
                #else
                Sprite starSp = null;
                #endif
                for (int i = 0; i < 3; i++)
                {
                    bool earned = i < stars;
                    if (starSp != null)
                    {
                        var s = new GameObject($"S{i}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                        s.transform.SetParent(starRow.transform, false);
                        s.GetComponent<LayoutElement>().preferredWidth = 30;
                        s.GetComponent<LayoutElement>().preferredHeight = 30;
                        var sImg = s.GetComponent<Image>();
                        sImg.sprite = starSp;
                        sImg.preserveAspect = true;
                        sImg.color = earned ? GOLD : new Color(0, 0, 0, 0.55f);
                        sImg.raycastTarget = false;
                    }
                }
            }

            // ── Inline reward chip (Top Heroes-style — see prize before tapping) ──
            if (unlocked && !completed)
            {
                var chip = MakeImg(parent, "Reward", new Color(0.10f, 0.05f, 0.20f, 0.92f));
                var crt = chip.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0.5f, 0); crt.anchorMax = new Vector2(0.5f, 0);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = pos + new Vector2(0, -150);
                crt.sizeDelta = new Vector2(170, 38);
                chip.GetComponent<Image>().raycastTarget = false;

                // Gold edge for the chip
                var chipEdge = MakeImg(parent, "RewardEdge", new Color(1f, 0.82f, 0.30f, 0.85f));
                var ceRT = chipEdge.GetComponent<RectTransform>();
                ceRT.anchorMin = new Vector2(0.5f, 0); ceRT.anchorMax = new Vector2(0.5f, 0);
                ceRT.pivot = new Vector2(0.5f, 0.5f);
                ceRT.anchoredPosition = pos + new Vector2(0, -150);
                ceRT.sizeDelta = new Vector2(176, 44);
                chipEdge.GetComponent<Image>().raycastTarget = false;
                chipEdge.transform.SetSiblingIndex(chip.transform.GetSiblingIndex());

                var rwTm = MakeText(chip.transform, "RT",
                    $"+{stage.xpReward} XP  •  +{stage.goldReward}g",
                    18, FontStyles.Bold, new Color(1f, 0.92f, 0.55f),
                    Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                rwTm.alignment = TextAlignmentOptions.Center;
                rwTm.outlineWidth = 0.22f;
                rwTm.outlineColor = new Color(0.05f, 0.02f, 0.18f, 1f);
            }

            // Banner above
            if (unlocked)
            {
                #if UNITY_EDITOR
                var flagSp = Sparq.Core.SpriteLoader.Load(
                    "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Label-Title/Label_Flag_02_Bg.png");
                #else
                Sprite flagSp = null;
                #endif

                var nameBg = MakeImg(parent, "Name", completed
                    ? new Color(GOLD.r, GOLD.g, GOLD.b, 1f)
                    : new Color(0.55f, 0.30f, 0.15f, 1f));
                var nrt = nameBg.GetComponent<RectTransform>();
                nrt.anchorMin = new Vector2(0.5f, 0); nrt.anchorMax = new Vector2(0.5f, 0);
                nrt.pivot = new Vector2(0.5f, 0.5f);
                nrt.anchoredPosition = pos + new Vector2(0, 110);
                nrt.sizeDelta = new Vector2(260, 60);
                var nbImg = nameBg.GetComponent<Image>();
                if (flagSp != null) { nbImg.sprite = flagSp; nbImg.type = Image.Type.Sliced; }
                nbImg.raycastTarget = false;

                var nameText = MakeText(nameBg.transform, "T", stage.name,
                    20, FontStyles.Bold, completed ? DEEP_NAVY : CREAM,
                    new Vector2(0, 0), new Vector2(1, 1),
                    new Vector2(0, 0), new Vector2(-24, -16));
                nameText.alignment = TextAlignmentOptions.Center;
                nameText.outlineWidth = 0.30f;
                nameText.outlineColor = completed
                    ? new Color(1f, 0.95f, 0.75f, 0.85f)
                    : new Color(0, 0, 0, 0.95f);
            }

            // Click → battle
            int idx = stage.index;
            node.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (!StageService.IsUnlocked(idx))
                {
                    XPFloater.Spawn(_root.transform,
                        node.transform.position + new Vector3(0, 60, 0),
                        "Locked — clear previous stage first",
                        new Color(0.85f, 0.45f, 0.45f));
                    return;
                }

                // Hidden mythic-egg discovery — 3 stages in the map are
                // wired to award a Mythic egg the first time the player
                // taps them (PetService remembers which have been claimed).
                try
                {
                    string stageId = $"stage_{idx:D2}";
                    if (Sparq.Systems.PetService.TryCollectMythicEggAtStage(stageId))
                    {
                        XPFloater.Spawn(_root.transform,
                            node.transform.position + new Vector3(0, 100, 0),
                            "✨  MYTHIC EGG!",
                            new Color(1.0f, 0.40f, 0.55f));
                    }
                }
                catch (System.Exception ex)
                { Debug.LogWarning($"[StageMapPanel] Mythic egg check failed: {ex.Message}"); }

                LaunchStage(stage);
            });
        }

        private static void LaunchStage(StageService.Stage stage)
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}

            // Stash stage state for the battle scene (legacy fallback)
            BattleScene.CurrentStageIdx = stage.index;
            BattleScene.StageHpMul = stage.hpMul;
            BattleScene.StageDmgMul = stage.dmgMul;
            BattleScene.StageXpReward = stage.xpReward;
            BattleScene.StageGoldReward = stage.goldReward;
            BattleScene.StageOverrideName = stage.name;

            if (_root != null) Object.Destroy(_root);
            // Stop map BGM — the explorable RPG map (or battle, for bosses) takes over from here
            try { Sparq.UI.MapBgm.Stop(); } catch {}

            string biome = BiomeForStage(stage.name);
            bool isBoss = stage.index == StageService.CHAPTER1.Length || stage.name.ToLower().Contains("boss");

            // ── New flow: tap stage → opens the EXPLORABLE RPG MAP for that stage.
            //     Player walks around, encounters enemies → SquadBattle launches.
            //     (Phase 2 will add enemy markers + battle triggers on the world map.)
            //     Boss stages still fire the cinematic intro before opening the map.
            if (isBoss)
                BossIntro.Show(stage.name, () => Sparq.UI.WorldExplorePanel.Show(biome));
            else
                Sparq.UI.WorldExplorePanel.Show(biome);
        }

        // Pick a biome string for SquadBattle's background tinting based on the stage name.
        private static string BiomeForStage(string stageName)
        {
            string n = (stageName ?? "").ToLower();
            if (n.Contains("haunt") || n.Contains("cave") || n.Contains("dungeon") || n.Contains("crypt") || n.Contains("phantom")) return "haunted";
            if (n.Contains("moon")  || n.Contains("night") || n.Contains("wolf")    || n.Contains("shadow")) return "moonlit";
            if (n.Contains("desert")|| n.Contains("sand") || n.Contains("stone")    || n.Contains("rock")  || n.Contains("brute")) return "rocky";
            return "forest";
        }

        // ───────── Helpers ─────────
        #if UNITY_EDITOR
        private static void EnsureSpriteSimple(string path)
        {
            var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
            if (imp == null) return;
            bool changed = false;
            if (imp.textureType != UnityEditor.TextureImporterType.Sprite)
            { imp.textureType = UnityEditor.TextureImporterType.Sprite; changed = true; }
            if (!imp.alphaIsTransparency)
            { imp.alphaIsTransparency = true; changed = true; }
            if (changed) imp.SaveAndReimport();
        }
        #endif

        private static Sprite _circleSprite, _starBurstSprite;
        private static Sprite LoadCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;
            const int s = 128;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            float r = s * 0.49f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                tex.SetPixel(x, y, d <= r ? Color.white : new Color(0, 0, 0, 0));
            }
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
            return _circleSprite;
        }

        private static Sprite LoadStarBurstSprite()
        {
            if (_starBurstSprite != null) return _starBurstSprite;
            const int s = 256;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(s * 0.5f, s * 0.5f);
            float maxR = s * 0.5f;
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                Vector2 d = new Vector2(x, y) - c;
                float r = d.magnitude / maxR;
                float angle = Mathf.Atan2(d.y, d.x);
                float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 4f)), 6f);
                float falloff = Mathf.Clamp01(1f - r);
                float alpha = ray * falloff * falloff;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            tex.Apply();
            _starBurstSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
            return _starBurstSprite;
        }

        private static GameObject MakeImg(Transform parent, string name, Color color)
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
            MakeText(go.transform, "Lbl", label, fontSize, FontStyles.Bold, fg,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero)
                .alignment = TextAlignmentOptions.Center;
            return go.GetComponent<Button>();
        }

        // Make absolutely sure an EventSystem exists before any UI panel shows.
        // Without one, clicks register on the OS but nothing ever calls
        // Button.onClick — the symptom is "buttons look right but do nothing".
        // Unity 6 doesn't auto-add one; some Sparq scenes never had one wired in.
        private static void EnsureEventSystem()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null && existing.isActiveAndEnabled) return;

            // Re-use a disabled one if found, else create.
            var go = existing != null ? existing.gameObject : new GameObject("EventSystem");
            if (existing == null)
            {
                go.AddComponent<EventSystem>();
                // Try the new Input System's InputSystemUIInputModule first via reflection
                // (avoids a hard package dependency); fall back to StandaloneInputModule
                // if the package isn't installed.
                go.AddComponent<StandaloneInputModule>();   // Old Input Manager only — Input System package not installed.
            }
            go.SetActive(true);
            var es = go.GetComponent<EventSystem>();
            if (es != null) es.enabled = true;
            Debug.Log("[StageMapPanel] EventSystem ensured — UI clicks should now fire.");
        }
    }
}
