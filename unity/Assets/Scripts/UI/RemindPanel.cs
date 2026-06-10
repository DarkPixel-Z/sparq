using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Reminders screen — list of active reminders with toggle + delete,
    /// today's upcoming bell preview at the top, and a "+ ADD" creator.
    /// Same indigo / pink-titlebar / gold-back palette as the other panels.
    /// </summary>
    public static class RemindPanel
    {
        private static readonly Color GOLD       = new Color(1f, 0.82f, 0.30f);
        private static readonly Color CREAM      = new Color(1f, 0.97f, 0.85f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.13f, 0.28f);
        // De-purpled palette — neutral charcoal/slate greys. Gold + green
        // accents do the heavy lifting; the surfaces stay quiet.
        private static readonly Color CARD_BG    = new Color(0.17f, 0.17f, 0.20f, 1f);  // dark charcoal
        private static readonly Color TITLE_BG   = new Color(0.40f, 0.40f, 0.46f, 1f);  // (unused now; flag dropped)
        private static readonly Color BANNER_BG  = new Color(0.22f, 0.22f, 0.26f, 1f);  // slightly lighter slate
        private static readonly Color ROW_ON     = new Color(0.28f, 0.28f, 0.33f, 1f);  // mid grey row
        private static readonly Color ROW_OFF    = new Color(0.20f, 0.20f, 0.24f, 1f);  // dark grey row

        private static GameObject _root;
        private static Transform _listParent;
        private static TMP_Text _todayText, _countText;
        private static readonly Dictionary<int, Sprite> _roundedCache = new Dictionary<int, Sprite>();
        private static Sprite _circleSp;

        // ── Polished popup PREFAB — the actual designer-composed shell
        //    from Layer Lab's FantasyRPG pack. Instantiating this gives
        //    us the real bevels / drop-shadows / inner gradients that
        //    procedural reconstruction was flattening out.
        private const string POPUP_PREFAB = "Assets/Layer Lab/GUI Pro-FantasyRPG/Prefabs/Prefabs_Component_Popups/Popup_01_Basic_White.prefab";

        private static GameObject LoadLayerLabPrefab(string path)
        {
            string r = path;
            if (r.StartsWith("Assets/")) r = r.Substring(7);
            if (r.EndsWith(".prefab")) r = r.Substring(0, r.Length - 7);
            var go = Resources.Load<GameObject>(r);
            if (go != null) return go;
#if UNITY_EDITOR
            try { return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path); }
            catch (System.Exception ex)
            { Debug.LogWarning($"[RemindPanel] LoadLayerLabPrefab('{path}') failed: {ex.Message}"); }
#endif
            return null;
        }

        // ── Layer Lab GUI Pro-FantasyRPG sprites (more polished aesthetic
        //    than the FantasyHero pack — beveled rows, gradient backdrop,
        //    convex buttons). Matches the Missions preview look.
        private const string POPUP_BG     = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Popup/Popup_02_White_Bg.png";
        private const string POPUP_BORDER = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Popup/Popup_02_White_Border.png";
        private const string POPUP_DECO_BORDER = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Popup/Popup_02_White_DecoBorder.png";
        private const string PANEL_FRAME_BG = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/PanelFrame_01_Bg.png";
        // Row background — beveled list-row sprite (used per-mission in
        // the 20_Missions preview).
        private const string LIST_BG      = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/Listframe_01~02_Bg.png";
        private const string LIST_BORDER  = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/Listframe_01~02_Border.png";
        private const string LIST_INNER_BORDER = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/Listframe_01~02_InnerBorder.png";
        // Beveled square frame for the bell badge.
        private const string ITEM_FRAME_GOLD = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/BasicFrame_Square_l.png";
        private const string ITEM_FRAME_DIM  = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Frame/BasicFrame_Square_l.png";
        // Bell icon — FantasyHero picto pack (RPG pack uses a different
        // sprite system; the Hero white bell tints fine on any backdrop).
        private const string BELL_ICON    = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/128/PictoIcon_Bell.Png";
        private const string BELL_MUTE    = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/128/PictoIcon_Bell_Mute.Png";
        // RPG pack's convex beveled buttons — cleaner shape than the
        // FantasyHero pills, matches the Missions "Claim" button.
        private const string BUTTON_GREEN = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Button/Button_Rectangle_01_Convex_Green.Png";
        private const string BUTTON_GOLD  = "Assets/Layer Lab/GUI Pro-FantasyRPG/ResourcesData/Sprites/Component/Button/Button_Rectangle_01_Convex_Yellow.Png";
        // Removed scene/pattern overlays — RPG popup uses a clean solid
        // gradient backdrop, not an illustrated scene.
        private const string SCENE_BG    = "";
        private const string SCENE_PATTERN = "";

        private static Sprite LoadLayerLabSprite(string assetPath)
        {
#if UNITY_EDITOR
            try
            {
                var imp = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
                if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite)
                { imp.textureType = UnityEditor.TextureImporterType.Sprite; imp.SaveAndReimport(); }
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[RemindPanel] LoadLayerLabSprite('{assetPath}') failed: {ex.Message}"); }
#endif
            return Sparq.Core.SpriteLoader.Load(assetPath);
        }

        private static void ApplySlicedSprite(GameObject go, string path, Color tint)
        {
            var img = go.GetComponent<Image>();
            if (img == null) return;
            var sp = LoadLayerLabSprite(path);
            if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; }
            img.color = tint;
        }

        public static void Show()
        {
            if (_root != null) { Hide(); return; }

            _root = new GameObject("RemindPanel",
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

            // Dim — neutral near-black backdrop (no purple cast).
            var dim = MakeImage(_root.transform, "Dim", new Color(0.06f, 0.06f, 0.08f, 0.92f));
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            var dimBtn = dim.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(Hide);

            // ── Card shell — try the Layer Lab polished popup PREFAB first.
            // If the prefab loads (editor), we instantiate the actual
            // designer-composed Bg + Border + Text_Title and just hide the
            // bits we don't use (Text_Info, Button_OK, Content_Demo).
            // If the prefab can't load (runtime build without an
            // AssetDatabase or asset missing), fall back to the procedural
            // shell so nothing breaks.
            GameObject card;
            var popupPrefab = LoadLayerLabPrefab(POPUP_PREFAB);
            if (popupPrefab != null)
            {
                var inst = UnityEngine.Object.Instantiate(popupPrefab, _root.transform);
                inst.name = "Card";
                card = inst;

                // Resize to fill the same area the procedural card used.
                var prefabRT = inst.GetComponent<RectTransform>();
                if (prefabRT == null) prefabRT = inst.AddComponent<RectTransform>();
                prefabRT.anchorMin = new Vector2(0, 0); prefabRT.anchorMax = new Vector2(1, 1);
                prefabRT.pivot = new Vector2(0.5f, 0.5f);
                prefabRT.offsetMin = new Vector2(40, 140);
                prefabRT.offsetMax = new Vector2(-40, -80);

                // Configure the prefab's known children:
                //   Text_Title → set to "Reminders"
                //   Text_Info / Button_OK / Content_Demo → hide (we replace)
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                {
                    if (t == null) continue;
                    var name = t.gameObject.name;
                    if (name == "Text_Info" || name == "Button_OK" || name == "Content_Demo")
                    {
                        t.gameObject.SetActive(false);
                    }
                }
                foreach (var tmp in inst.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    if (tmp.gameObject.name == "Text_Title")
                    {
                        tmp.text = "Reminders";
                        tmp.fontSize = 64;
                        tmp.alignment = TextAlignmentOptions.MidlineLeft;
                        // Prefab ships with near-black title text — invisible
                        // on our charcoal Bg tint. Force light cream.
                        tmp.color = new Color(1f, 0.97f, 0.85f, 1f);
                        try { tmp.outlineWidth = 0.18f; tmp.outlineColor = new Color(0.05f, 0.03f, 0.10f); } catch {}
                    }
                }

                // Tint the prefab's white Bg to our neutral charcoal so the
                // panel doesn't read pure white.
                foreach (var img in inst.GetComponentsInChildren<Image>(true))
                {
                    if (img == null) continue;
                    if (img.gameObject.name == "Bg") img.color = CARD_BG;
                }

                // Back chevron at top-right (the prefab doesn't ship one).
                var backBtn = MakeBtn(card.transform, "BackBtn", "<",
                    new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -80), new Vector2(110, 92),
                    new Color(0, 0, 0, 0), Color.white, 56);
                backBtn.onClick.AddListener(Hide);
                var bLbl = backBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
                if (bLbl != null) { bLbl.fontStyle = FontStyles.Bold; bLbl.color = new Color(1f, 1f, 1f, 0.85f); }

                Debug.Log("[RemindPanel] Card shell = Layer Lab Popup_01_Basic_White.prefab (polished).");
            }
            else
            {
                // ── Fallback procedural shell (sprite + procedural rect) ──
                var stroke = MakeRounded(_root.transform, "Stroke", TITLE_BG, 30);
                var srt = stroke.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 1);
                srt.offsetMin = new Vector2(36, 136); srt.offsetMax = new Vector2(-36, -76);
                ApplySlicedSprite(stroke, POPUP_BORDER, new Color(0.99f, 0.78f, 0.20f, 1f));

                card = MakeRounded(_root.transform, "Card", CARD_BG, 28);
                var crt = card.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0, 0); crt.anchorMax = new Vector2(1, 1);
                crt.offsetMin = new Vector2(40, 140); crt.offsetMax = new Vector2(-40, -80);
                ApplySlicedSprite(card, POPUP_BG, CARD_BG);

                BuildFunkyBackdrop(card.transform);

                var title = MakeText(card.transform, "Title", "Reminders",
                    72, FontStyles.Bold, Color.white,
                    new Vector2(0, 1), new Vector2(1, 1), new Vector2(48, -150), new Vector2(-48, -40));
                title.alignment = TextAlignmentOptions.MidlineLeft;

                var sep = MakeImage(card.transform, "Sep", new Color(1f, 1f, 1f, 0.18f));
                var sepRT = sep.GetComponent<RectTransform>();
                sepRT.anchorMin = new Vector2(0, 1); sepRT.anchorMax = new Vector2(1, 1);
                sepRT.pivot = new Vector2(0.5f, 1f);
                sepRT.anchoredPosition = new Vector2(0, -160);
                sepRT.sizeDelta = new Vector2(-60, 2);

                var backBtn = MakeBtn(card.transform, "BackBtn", "<",
                    new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -86), new Vector2(110, 92),
                    new Color(0, 0, 0, 0), Color.white, 56);
                backBtn.onClick.AddListener(Hide);

                Debug.LogWarning("[RemindPanel] Popup prefab missing — using procedural shell.");
            }

            // Today summary banner — uses Layer Lab ListFrame_01_Bg sprite
            // (sliced, soft inner gradient) tinted to match the panel theme.
            var banner = MakeRounded(card.transform, "Today", BANNER_BG, 18);
            var brrt = banner.GetComponent<RectTransform>();
            brrt.anchorMin = new Vector2(0, 1); brrt.anchorMax = new Vector2(1, 1);
            brrt.pivot = new Vector2(0.5f, 1f);
            brrt.anchoredPosition = new Vector2(0, -200);   // below taller flag
            brrt.sizeDelta = new Vector2(-50, 140);
            ApplySlicedSprite(banner, LIST_BG, BANNER_BG);

            _todayText = MakeText(banner.transform, "Tdy", "★  Next reminder: —",
                38, FontStyles.Bold, GOLD,
                new Vector2(0, 0), new Vector2(0.65f, 1), new Vector2(24, 0), Vector2.zero);
            _todayText.alignment = TextAlignmentOptions.MidlineLeft;
            _todayText.outlineWidth = 0.18f;
            _todayText.outlineColor = new Color(0.05f, 0.02f, 0.08f);

            _countText = MakeText(banner.transform, "Cnt", "0 today",
                34, FontStyles.Bold, CREAM,
                new Vector2(0.65f, 0), new Vector2(1, 1), new Vector2(-24, 0), Vector2.zero);
            _countText.alignment = TextAlignmentOptions.MidlineRight;

            // Section header
            var hdr = MakeText(card.transform, "Hdr", "·  ALL REMINDERS  ·",
                28, FontStyles.Bold, GOLD,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -360), new Vector2(0, 40));
            hdr.alignment = TextAlignmentOptions.Center;
            hdr.characterSpacing = 12f;
            hdr.outlineWidth = 0.18f; hdr.outlineColor = new Color(0.10f, 0.05f, 0);

            // Scroll list
            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(card.transform, false);
            var scrt = scrollGO.GetComponent<RectTransform>();
            scrt.anchorMin = new Vector2(0, 0); scrt.anchorMax = new Vector2(1, 1);
            scrt.pivot = new Vector2(0.5f, 0.5f);
            scrt.offsetMin = new Vector2(30, 240);
            scrt.offsetMax = new Vector2(-30, -420);
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
            vpImg.sprite = LoadRoundedSprite(20);
            vpImg.type = Image.Type.Sliced;
            vpImg.color = new Color(0, 0, 0, 0.25f);
            viewport.GetComponent<Mask>().showMaskGraphic = true;
            sr.viewport = vrt;

            var content = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cct = content.GetComponent<RectTransform>();
            cct.anchorMin = new Vector2(0, 1); cct.anchorMax = new Vector2(1, 1);
            cct.pivot = new Vector2(0.5f, 1f);
            cct.anchoredPosition = Vector2.zero;
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 14;
            vlg.childForceExpandWidth = true;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            var csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = cct;
            _listParent = content.transform;

            // Bottom: Add reminder — Layer Lab green polished button sprite
            // (with proper bevel + drop shadow baked in) instead of a flat
            // rounded rect. Falls back to flat green if the sprite misses.
            var addBtn = MakeBtn(card.transform, "AddBtn", "+  ADD REMINDER",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(540, 130),
                Color.white, Color.white, 38);
            addBtn.onClick.AddListener(OpenCreator);
            var aImg = addBtn.GetComponent<Image>();
            var addBtnSp = LoadLayerLabSprite(BUTTON_GREEN);
            if (addBtnSp != null) { aImg.sprite = addBtnSp; aImg.type = Image.Type.Sliced; aImg.color = Color.white; }
            else { aImg.sprite = LoadRoundedSprite(28); aImg.type = Image.Type.Sliced; aImg.color = new Color(0.30f, 0.80f, 0.42f); }
            var aLbl = addBtn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (aLbl != null) { aLbl.color = DEEP_NAVY; aLbl.outlineWidth = 0.22f; aLbl.outlineColor = new Color(0.85f, 1f, 0.85f); }

            try { Sparq.Systems.RemindService.OnChanged += OnChanged; } catch {}

            UpdateBanner();
            RebuildList();
        }

        public static void Hide()
        {
            try { Sparq.Systems.RemindService.OnChanged -= OnChanged; } catch {}
            if (_root != null) UnityEngine.Object.Destroy(_root);
            _root = null;
        }

        private static void OnChanged() { UpdateBanner(); RebuildList(); }

        // ─────────── Funky pastel backdrop ───────────
        private static void BuildFunkyBackdrop(Transform card)
        {
            // Soft mask container — clips the blobs to the rounded card edges
            var mask = new GameObject("FunkyMask",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            mask.transform.SetParent(card, false);
            var mrt = mask.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero; mrt.offsetMax = Vector2.zero;
            var mImg = mask.GetComponent<Image>();
            mImg.sprite = LoadRoundedSprite(28);
            mImg.type = Image.Type.Sliced;
            mImg.color = Color.white;
            mask.GetComponent<Mask>().showMaskGraphic = false;

            // Pastel blobs scattered across the card
            (float ax, float ay, float size, Color col)[] blobs = {
                (0.10f, 0.95f, 320, new Color(0.42f, 0.22f, 0.68f, 0.22f)),  // pink top-left
                (0.95f, 0.78f, 280, new Color(1.00f, 0.82f, 0.30f, 0.20f)),  // gold top-right
                (0.60f, 0.55f, 380, new Color(0.45f, 0.85f, 0.65f, 0.18f)),  // mint mid
                (0.05f, 0.40f, 260, new Color(0.55f, 0.62f, 0.95f, 0.22f)),  // periwinkle mid-left
                (0.85f, 0.18f, 320, new Color(0.92f, 0.55f, 0.85f, 0.22f)),  // magenta lower-right
                (0.25f, 0.10f, 240, new Color(0.55f, 0.85f, 1.00f, 0.20f)),  // sky lower-left
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
                img.sprite = LoadCircleSprite();
                img.color = b.col;
                img.raycastTarget = false;
            }

            // Tiny scattered sparkles (small dots) for texture
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

            // Mask sits at the back so all card UI renders on top
            mask.transform.SetAsFirstSibling();
        }

        private static void UpdateBanner()
        {
            var today = Sparq.Systems.RemindService.Today();
            if (_countText != null) _countText.text = $"{today.Count} today";

            if (_todayText != null)
            {
                if (today.Count == 0) _todayText.text = "★  No reminders today";
                else
                {
                    var nextR = today[0];
                    var now = System.DateTime.Now;
                    foreach (var r in today)
                    {
                        if (r.hour > now.Hour || (r.hour == now.Hour && r.minute >= now.Minute))
                        { nextR = r; break; }
                    }
                    _todayText.text = $"★  Next: {Sparq.Systems.RemindService.FormatTime(nextR.hour, nextR.minute)}";
                }
            }
        }

        private static void RebuildList()
        {
            if (_listParent == null) return;
            for (int i = _listParent.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(_listParent.GetChild(i).gameObject);

            var all = Sparq.Systems.RemindService.All();
            if (all.Count == 0)
            {
                var empty = MakeText(_listParent, "Empty",
                    "No reminders yet — tap + ADD REMINDER below.",
                    30, FontStyles.Italic, new Color(1, 1, 1, 0.65f),
                    new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
                empty.alignment = TextAlignmentOptions.Center;
                var le = empty.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = 110;
                return;
            }
            foreach (var r in all) BuildRow(_listParent, r);
        }

        private static void BuildRow(Transform parent, Sparq.Systems.RemindService.Reminder r)
        {
            // Row background — Layer Lab ListFrame_01_Bg sprite (sliced,
            // proper rounded corners + subtle inner gradient) tinted by
            // enabled state instead of a flat colored rectangle.
            var row = MakeRounded(parent, $"R_{r.id}", r.enabled ? ROW_ON : ROW_OFF, 16);
            ApplySlicedSprite(row, LIST_BG, r.enabled ? ROW_ON : ROW_OFF);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 175;
            le.minHeight = 175;

            // Bell badge (left) — Layer Lab ItemFrame square frame as the
            // backdrop, with a bright-orange PictoIcon_Bell overlay. Orange
            // pops on the grey row much better than the previous navy-on-
            // yellow combo (and reads instantly as "alert / notification").
            var bell = MakeRounded(row.transform, "Bell", Color.white, 36);
            var brt = bell.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0.5f); brt.anchorMax = new Vector2(0, 0.5f);
            brt.pivot = new Vector2(0, 0.5f);
            brt.anchoredPosition = new Vector2(24, 0);
            brt.sizeDelta = new Vector2(120, 120);
            // Tint the frame charcoal so the orange bell pops against it.
            ApplySlicedSprite(bell, ITEM_FRAME_DIM, new Color(0.14f, 0.14f, 0.18f, 1f));

            // Bell icon — bright orange when active, muted grey-orange when off.
            var bellIcoGO = new GameObject("BellIco", typeof(RectTransform), typeof(Image));
            bellIcoGO.transform.SetParent(bell.transform, false);
            var biRT = bellIcoGO.GetComponent<RectTransform>();
            biRT.anchorMin = new Vector2(0.5f, 0.5f); biRT.anchorMax = new Vector2(0.5f, 0.5f);
            biRT.pivot = new Vector2(0.5f, 0.5f);
            biRT.anchoredPosition = Vector2.zero;
            biRT.sizeDelta = new Vector2(72, 72);
            var biImg = bellIcoGO.GetComponent<Image>();
            var bellSp = LoadLayerLabSprite(r.enabled ? BELL_ICON : BELL_MUTE);
            if (bellSp != null) { biImg.sprite = bellSp; biImg.preserveAspect = true; }
            biImg.color = r.enabled
                ? new Color(1.00f, 0.55f, 0.15f, 1f)               // vivid orange
                : new Color(0.55f, 0.42f, 0.32f, 1f);              // dim warm grey
            biImg.raycastTarget = false;

            // Title (top) — 28→36pt.
            var titleTm = MakeText(row.transform, "Title", r.title,
                36, FontStyles.Bold, r.enabled ? Color.white : new Color(1, 1, 1, 0.55f),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            titleTm.alignment = TextAlignmentOptions.MidlineLeft;
            var ttRT = titleTm.rectTransform;
            ttRT.anchorMin = new Vector2(0, 0.5f); ttRT.anchorMax = new Vector2(1, 1);
            ttRT.pivot = new Vector2(0, 0.5f);
            ttRT.offsetMin = new Vector2(170, 0); ttRT.offsetMax = new Vector2(-240, -10);
            titleTm.outlineWidth = 0.20f;
            titleTm.outlineColor = new Color(0, 0, 0, 0.7f);

            // Time + days (bottom) — bumped 26→32pt for legibility.
            string sub = $"{Sparq.Systems.RemindService.FormatTime(r.hour, r.minute)}   ·   {Sparq.Systems.RemindService.DayBitsToShort(r.days)}";
            var subTm = MakeText(row.transform, "Sub", sub,
                32, FontStyles.Bold, r.enabled ? new Color(1f, 0.92f, 0.65f) : new Color(1, 1, 1, 0.55f),
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            subTm.alignment = TextAlignmentOptions.MidlineLeft;
            var stRT = subTm.rectTransform;
            stRT.anchorMin = new Vector2(0, 0); stRT.anchorMax = new Vector2(1, 0.5f);
            stRT.pivot = new Vector2(0, 0.5f);
            stRT.offsetMin = new Vector2(170, 14); stRT.offsetMax = new Vector2(-240, 0);

            // Toggle pill (right) — bigger pill + knob.
            var togBg = MakeRounded(row.transform, "Toggle",
                r.enabled ? new Color(0.30f, 0.80f, 0.42f) : new Color(0.35f, 0.30f, 0.45f, 1f), 24);
            var tgrt = togBg.GetComponent<RectTransform>();
            tgrt.anchorMin = new Vector2(1, 0.5f); tgrt.anchorMax = new Vector2(1, 0.5f);
            tgrt.pivot = new Vector2(1, 0.5f);
            tgrt.anchoredPosition = new Vector2(-120, 0);
            tgrt.sizeDelta = new Vector2(108, 54);
            var togBtn = togBg.AddComponent<Button>();
            string capId = r.id;
            togBtn.onClick.AddListener(() => Sparq.Systems.RemindService.Toggle(capId));
            // Knob
            var knob = MakeRounded(togBg.transform, "Knob", Color.white, 22);
            var krt = knob.GetComponent<RectTransform>();
            krt.anchorMin = new Vector2(r.enabled ? 1 : 0, 0.5f);
            krt.anchorMax = new Vector2(r.enabled ? 1 : 0, 0.5f);
            krt.pivot = new Vector2(r.enabled ? 1 : 0, 0.5f);
            krt.anchoredPosition = new Vector2(r.enabled ? -5 : 5, 0);
            krt.sizeDelta = new Vector2(42, 42);
            var kImg = knob.GetComponent<Image>();
            kImg.sprite = LoadCircleSprite();
            kImg.type = Image.Type.Simple;

            // Delete (×) — far right, bigger.
            var delBtn = MakeBtn(row.transform, "Del", "✕",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-28, 0), new Vector2(68, 68),
                new Color(0.95f, 0.35f, 0.35f, 1f), Color.white, 34);
            var dImg = delBtn.GetComponent<Image>();
            dImg.sprite = LoadCircleSprite();
            dImg.type = Image.Type.Simple;
            string capId2 = r.id;
            delBtn.onClick.AddListener(() => Sparq.Systems.RemindService.Delete(capId2));
        }

        // Procedural bell glyph helpers
        private static void DrawShape(Transform parent, Color color, float ax, float ay, float w, float h, bool rounded)
        {
            var go = new GameObject("S", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            var img = go.GetComponent<Image>();
            img.sprite = rounded ? LoadRoundedSprite(8) : LoadCircleSprite();
            if (rounded) img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
        }

        // ─────────── Inline reminder creator ───────────
        private static void OpenCreator()
        {
            var cv = new GameObject("RemindCreator",
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

            var cdim = MakeImage(cv.transform, "CDim", new Color(0, 0, 0, 0.92f));
            var cdrt = cdim.GetComponent<RectTransform>();
            cdrt.anchorMin = Vector2.zero; cdrt.anchorMax = Vector2.one;
            cdrt.offsetMin = Vector2.zero; cdrt.offsetMax = Vector2.zero;
            var cdimBtn = cdim.AddComponent<Button>();
            cdimBtn.transition = Selectable.Transition.None;
            cdimBtn.onClick.AddListener(() => UnityEngine.Object.Destroy(cv));

            var card = MakeRounded(cv.transform, "Card", CARD_BG, 28);
            var ccrt = card.GetComponent<RectTransform>();
            ccrt.anchorMin = new Vector2(0.5f, 0.5f); ccrt.anchorMax = new Vector2(0.5f, 0.5f);
            ccrt.pivot = new Vector2(0.5f, 0.5f);
            ccrt.sizeDelta = new Vector2(900, 1000);

            // Funky pastel-blob backdrop
            BuildFunkyBackdrop(card.transform);

            // Title
            MakeText(card.transform, "T", "✨  ADD REMINDER",
                40, FontStyles.Bold, GOLD,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -50), new Vector2(0, 70))
                .alignment = TextAlignmentOptions.Center;

            // Title input
            MakeText(card.transform, "TLbl", "What to remember",
                24, FontStyles.Bold, CREAM,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -140), new Vector2(0, 40))
                .alignment = TextAlignmentOptions.Left;

            var inputGO = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputGO.transform.SetParent(card.transform, false);
            var irt = inputGO.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 1); irt.anchorMax = new Vector2(1, 1);
            irt.pivot = new Vector2(0.5f, 1f);
            irt.anchoredPosition = new Vector2(0, -190);
            irt.sizeDelta = new Vector2(-80, 90);
            inputGO.GetComponent<Image>().color = new Color(0.10f, 0.08f, 0.20f, 0.85f);

            var inputField = inputGO.GetComponent<TMP_InputField>();
            var iTxtGO = new GameObject("Text", typeof(RectTransform));
            iTxtGO.transform.SetParent(inputGO.transform, false);
            var itrt = iTxtGO.GetComponent<RectTransform>();
            itrt.anchorMin = Vector2.zero; itrt.anchorMax = Vector2.one;
            itrt.offsetMin = new Vector2(20, 8); itrt.offsetMax = new Vector2(-20, -8);
            var iTm = iTxtGO.AddComponent<TextMeshProUGUI>();
            iTm.fontSize = 30; iTm.color = Color.white; iTm.alignment = TextAlignmentOptions.Left;
            inputField.textComponent = iTm; inputField.text = "";

            var phGO = new GameObject("PH", typeof(RectTransform));
            phGO.transform.SetParent(inputGO.transform, false);
            var phrt = phGO.GetComponent<RectTransform>();
            phrt.anchorMin = Vector2.zero; phrt.anchorMax = Vector2.one;
            phrt.offsetMin = new Vector2(20, 8); phrt.offsetMax = new Vector2(-20, -8);
            var phTm = phGO.AddComponent<TextMeshProUGUI>();
            phTm.text = "e.g. Take afternoon meds";
            phTm.fontSize = 30; phTm.color = new Color(1, 1, 1, 0.4f);
            phTm.fontStyle = FontStyles.Italic; phTm.alignment = TextAlignmentOptions.Left;
            phTm.raycastTarget = false;
            inputField.placeholder = phTm;

            // Time selector — hour + minute (steppable)
            int[] hour = { 9 };
            int[] minute = { 0 };
            MakeText(card.transform, "TimeLbl", "Time",
                24, FontStyles.Bold, CREAM,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -310), new Vector2(0, 40))
                .alignment = TextAlignmentOptions.Left;

            var timeRow = new GameObject("TimeRow", typeof(RectTransform));
            timeRow.transform.SetParent(card.transform, false);
            var trrt = timeRow.GetComponent<RectTransform>();
            trrt.anchorMin = new Vector2(0, 1); trrt.anchorMax = new Vector2(1, 1);
            trrt.pivot = new Vector2(0.5f, 1f);
            trrt.anchoredPosition = new Vector2(0, -360);
            trrt.sizeDelta = new Vector2(-80, 90);

            var timeDisplay = MakeText(timeRow.transform, "TimeDisp",
                Sparq.Systems.RemindService.FormatTime(hour[0], minute[0]),
                42, FontStyles.Bold, GOLD,
                new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            timeDisplay.alignment = TextAlignmentOptions.Center;
            timeDisplay.outlineWidth = 0.20f; timeDisplay.outlineColor = new Color(0.10f, 0.05f, 0);

            // Hour - / +
            var hMinus = MakeBtn(timeRow.transform, "HM", "−",
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(40, 0), new Vector2(70, 70),
                new Color(0.40f, 0.36f, 0.62f), Color.white, 36);
            ApplyRound(hMinus);
            hMinus.onClick.AddListener(() => { hour[0] = (hour[0] + 23) % 24; timeDisplay.text = Sparq.Systems.RemindService.FormatTime(hour[0], minute[0]); });

            var hPlus = MakeBtn(timeRow.transform, "HP", "+",
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(120, 0), new Vector2(70, 70),
                new Color(0.40f, 0.36f, 0.62f), Color.white, 36);
            ApplyRound(hPlus);
            hPlus.onClick.AddListener(() => { hour[0] = (hour[0] + 1) % 24; timeDisplay.text = Sparq.Systems.RemindService.FormatTime(hour[0], minute[0]); });

            // Minute - / +  (step by 5)
            var mMinus = MakeBtn(timeRow.transform, "MM", "−",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-120, 0), new Vector2(70, 70),
                new Color(0.40f, 0.36f, 0.62f), Color.white, 36);
            ApplyRound(mMinus);
            mMinus.onClick.AddListener(() => { minute[0] = (minute[0] + 55) % 60; timeDisplay.text = Sparq.Systems.RemindService.FormatTime(hour[0], minute[0]); });

            var mPlus = MakeBtn(timeRow.transform, "MP", "+",
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(70, 70),
                new Color(0.40f, 0.36f, 0.62f), Color.white, 36);
            ApplyRound(mPlus);
            mPlus.onClick.AddListener(() => { minute[0] = (minute[0] + 5) % 60; timeDisplay.text = Sparq.Systems.RemindService.FormatTime(hour[0], minute[0]); });

            // Days row
            MakeText(card.transform, "DLbl", "Days",
                24, FontStyles.Bold, CREAM,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -480), new Vector2(0, 40))
                .alignment = TextAlignmentOptions.Left;

            char[] dayBits = "1111111".ToCharArray();
            string[] dayInit = { "M", "T", "W", "T", "F", "S", "S" };
            var dayPills = new Image[7];
            var dayLbls = new TMP_Text[7];
            var dayRow = new GameObject("DayRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            dayRow.transform.SetParent(card.transform, false);
            var drrt = dayRow.GetComponent<RectTransform>();
            drrt.anchorMin = new Vector2(0, 1); drrt.anchorMax = new Vector2(1, 1);
            drrt.pivot = new Vector2(0.5f, 1f);
            drrt.anchoredPosition = new Vector2(0, -540);
            drrt.sizeDelta = new Vector2(-80, 90);
            var dhlg = dayRow.GetComponent<HorizontalLayoutGroup>();
            dhlg.spacing = 10;
            dhlg.childForceExpandWidth = true;
            dhlg.childForceExpandHeight = true;

            for (int i = 0; i < 7; i++)
            {
                int captured = i;
                var pill = MakeRounded(dayRow.transform, $"D{i}",
                    dayBits[i] == '1' ? GOLD : new Color(0.30f, 0.28f, 0.45f), 16);
                var pBtn = pill.AddComponent<Button>();
                var pLbl = MakeText(pill.transform, "DLbl", dayInit[i],
                    28, FontStyles.Bold, dayBits[i] == '1' ? DEEP_NAVY : Color.white,
                    new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
                pLbl.alignment = TextAlignmentOptions.Center;
                pLbl.outlineWidth = 0.22f;
                pLbl.outlineColor = dayBits[i] == '1' ? new Color(1f, 0.95f, 0.7f) : new Color(0, 0, 0, 0.7f);
                dayPills[i] = pill.GetComponent<Image>();
                dayLbls[i] = pLbl;
                pBtn.onClick.AddListener(() =>
                {
                    dayBits[captured] = dayBits[captured] == '1' ? '0' : '1';
                    bool on = dayBits[captured] == '1';
                    dayPills[captured].color = on ? GOLD : new Color(0.30f, 0.28f, 0.45f);
                    dayLbls[captured].color = on ? DEEP_NAVY : Color.white;
                    dayLbls[captured].outlineColor = on ? new Color(1f, 0.95f, 0.7f) : new Color(0, 0, 0, 0.7f);
                });
            }

            // Preset row — Daily / Weekdays / Weekends — auto-fills the day pills
            void SetPreset(string bits)
            {
                for (int i = 0; i < 7; i++)
                {
                    dayBits[i] = bits[i];
                    bool on = bits[i] == '1';
                    if (dayPills[i] != null) dayPills[i].color = on ? GOLD : new Color(0.30f, 0.28f, 0.45f);
                    if (dayLbls[i] != null)
                    {
                        dayLbls[i].color = on ? DEEP_NAVY : Color.white;
                        dayLbls[i].outlineColor = on ? new Color(1f, 0.95f, 0.7f) : new Color(0, 0, 0, 0.7f);
                    }
                }
            }

            var presetRow = new GameObject("PresetRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            presetRow.transform.SetParent(card.transform, false);
            var prRT = presetRow.GetComponent<RectTransform>();
            prRT.anchorMin = new Vector2(0, 1); prRT.anchorMax = new Vector2(1, 1);
            prRT.pivot = new Vector2(0.5f, 1f);
            prRT.anchoredPosition = new Vector2(0, -650);
            prRT.sizeDelta = new Vector2(-80, 70);
            var prHlg = presetRow.GetComponent<HorizontalLayoutGroup>();
            prHlg.spacing = 12;
            prHlg.childForceExpandWidth = true;
            prHlg.childForceExpandHeight = true;

            void AddPreset(string label, string bits, Color tint)
            {
                var btn = MakeBtn(presetRow.transform, $"P_{label}", label,
                    new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero,
                    tint, Color.white, 22);
                ApplyRound(btn);
                var pl = btn.transform.Find("Lbl")?.GetComponent<TMP_Text>();
                if (pl != null) { pl.color = DEEP_NAVY; pl.outlineWidth = 0.22f; pl.outlineColor = new Color(1f, 0.95f, 0.7f); }
                btn.onClick.AddListener(() => SetPreset(bits));
            }
            AddPreset("DAILY",    "1111111", new Color(0.62f, 0.40f, 0.92f));
            AddPreset("WEEKDAYS", "1111100", new Color(0.55f, 0.85f, 0.45f));
            AddPreset("WEEKENDS", "0000011", new Color(0.62f, 0.75f, 1f));

            // Save / Cancel — anchored at bottom-center, side-by-side, no overlap
            var save = MakeBtn(card.transform, "Save", "✓  SAVE",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-200, 80), new Vector2(360, 110),
                new Color(0.30f, 0.80f, 0.42f), Color.white, 30);
            ApplyRound(save);
            var sLbl = save.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (sLbl != null) { sLbl.color = DEEP_NAVY; sLbl.outlineWidth = 0.22f; sLbl.outlineColor = new Color(0.85f, 1f, 0.85f); }
            save.onClick.AddListener(() =>
            {
                string title = inputField.text?.Trim();
                if (string.IsNullOrEmpty(title)) title = "Reminder";
                Sparq.Systems.RemindService.Add(title, hour[0], minute[0], new string(dayBits));
                UnityEngine.Object.Destroy(cv);
            });

            var cancel = MakeBtn(card.transform, "Cancel", "Cancel",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(200, 80), new Vector2(360, 110),
                new Color(0.92f, 0.35f, 0.42f), Color.white, 28);
            ApplyRound(cancel);
            var cLbl = cancel.transform.Find("Lbl")?.GetComponent<TMP_Text>();
            if (cLbl != null) { cLbl.outlineWidth = 0.22f; cLbl.outlineColor = new Color(0.10f, 0.05f, 0.20f); }
            cancel.onClick.AddListener(() => UnityEngine.Object.Destroy(cv));
        }

        private static void ApplyRound(Button btn)
        {
            var img = btn.GetComponent<Image>();
            if (img != null) { img.sprite = LoadRoundedSprite(28); img.type = Image.Type.Sliced; }
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
            tm.fontSize = size; tm.fontStyle = style; tm.color = color;
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
