using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Hero class picker — 4 cards (Knight / Elf Archer / Time Keeper Mage /
    /// Assassin) in a 2×2 grid over the Layer Lab "mystic shrine" backdrop.
    ///
    /// Visual approach (learned the hard way):
    ///   - Backdrop      : Background_01.png — atmospheric shrine, NOT a flat dim
    ///   - Card panel    : RewardFrame_01_Bg.png is pure WHITE — it's meant to
    ///                     be color-TINTED. We tint it deep slate, not pastel.
    ///   - Card border   : RewardFrame_01_Default_Border.png (purple frame)
    ///   - Selected      : RewardFrame_01_Focus_Border.png (gold) + scale-up
    ///   - Portraits     : real chibi idle PNGs, sized BIG to fill the card.
    ///                     The Knight frame is 1800×980 (long spear) so it gets
    ///                     a cropped sub-sprite; the other three are clean 900².
    ///
    /// Two-step confirm: tap a card to select → bottom CTA commits.
    /// onPicked() fires after the CTA tap. forcePick disables escape routes
    /// (used for first-launch onboarding).
    /// </summary>
    public static class HeroSelectPanel
    {
        // ─────────────────────────────────────────────────────────────────
        // CLASS DATA
        // ─────────────────────────────────────────────────────────────────
        // Portrait sprites come from HeroClassResolver + HeroPortrait (the
        // shared alpha-crop helper) — keyed by `id`. No paths/crops here.
        private class ClassOption
        {
            public string id;             // matches PlayerData.heroClass
            public string displayName;
            public string flavor;
            public Color  accent;         // class identity color (ribbon / glow)
        }

        private static readonly ClassOption[] CLASSES = new[]
        {
            new ClassOption {
                id = "knight",   displayName = "KNIGHT",
                flavor = "Steel-clad melee.\nBalanced ATK / DEF.",
                accent = new Color(0.45f, 0.62f, 0.95f),
            },
            new ClassOption {
                id = "archer",   displayName = "ELF ARCHER",
                flavor = "Ranged precision.\nHigh crit, deadly.",
                accent = new Color(0.40f, 0.85f, 0.50f),
            },
            new ClassOption {
                id = "mage",     displayName = "TIME KEEPER",
                flavor = "Arcane caster.\nDevastating bursts.",
                accent = new Color(0.70f, 0.45f, 1.00f),
            },
            new ClassOption {
                id = "assassin", displayName = "ASSASSIN",
                flavor = "Shadow striker.\nMassive crits.",
                accent = new Color(0.95f, 0.40f, 0.45f),
            },
        };

        // Layer Lab sprite paths.
        private const string BACKDROP_PATH    = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Demo/Demo_Background/Background_01.png";
        private const string CARD_BG_PATH     = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/RewardFrame_01_Bg.png";
        private const string CARD_BORDER_PATH = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/RewardFrame_01_Default_Border.png";
        private const string CARD_FOCUS_PATH  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/RewardFrame_01_Focus_Border.png";

        // Colors
        private static readonly Color CARD_DARK   = new Color(0.13f, 0.11f, 0.20f, 0.98f);  // deep slate panel
        private static readonly Color CARD_DARK_SEL = new Color(0.20f, 0.16f, 0.30f, 1f);   // lifted when selected
        private static readonly Color GOLD        = new Color(1f, 0.80f, 0.26f);
        private static readonly Color INK         = new Color(0.10f, 0.06f, 0.16f);

        // Runtime state
        private static GameObject _root;
        private static int        _selectedIndex = -1;
        private static System.Collections.Generic.List<CardRefs> _cards;
        private static Button     _confirmBtn;
        private static TMP_Text   _confirmLbl;

        private class CardRefs
        {
            public GameObject card;
            public Image bg;
            public Image focus;
        }

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC API (signature unchanged — old callers still work)
        // ─────────────────────────────────────────────────────────────────

        public static void Show(bool forcePick = false, System.Action onPicked = null)
        {
            if (_root != null) Object.Destroy(_root);
            _cards = new System.Collections.Generic.List<CardRefs>();
            _selectedIndex = -1;
            EnsureEventSystem();

            // Pre-select the user's current class, if any.
            string activeId = Sparq.Core.SaveService.Data?.heroClass ?? "";
            for (int i = 0; i < CLASSES.Length; i++)
                if (CLASSES[i].id == activeId) { _selectedIndex = i; break; }

            // ── Overlay canvas ──
            _root = new GameObject("Sparq_HeroSelectPanel",
                typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rrt = _root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 14000;
            foreach (var other in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort)
                    maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 40;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            // ── Backdrop: atmospheric shrine art (fills screen, cropped) ──
            var bg = NewGO("Backdrop", _root.transform, typeof(Image));
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            var bgImg = bg.GetComponent<Image>();
            var bgSp = LoadSprite(BACKDROP_PATH);
            if (bgSp != null)
            {
                bgImg.sprite = bgSp;
                bgImg.type = Image.Type.Simple;
                bgImg.preserveAspect = false;     // stretch to fill — it's an abstract scene
                bgImg.color = new Color(0.62f, 0.62f, 0.68f, 1f);  // darkened so cards read
            }
            else
            {
                bgImg.color = new Color(0.05f, 0.06f, 0.13f, 1f);
            }
            // Vignette / legibility wash
            var wash = NewGO("Wash", _root.transform, typeof(Image));
            var wRT = wash.GetComponent<RectTransform>();
            wRT.anchorMin = Vector2.zero; wRT.anchorMax = Vector2.one;
            wRT.offsetMin = Vector2.zero; wRT.offsetMax = Vector2.zero;
            wash.GetComponent<Image>().color = new Color(0.04f, 0.03f, 0.10f, forcePick ? 0.55f : 0.45f);
            if (!forcePick)
            {
                var washBtn = wash.AddComponent<Button>();
                washBtn.transition = Selectable.Transition.None;
                washBtn.onClick.AddListener(Hide);
            }

            // ── Title ── (no symbol glyphs — they render as tofu in this font)
            var titleGO = NewGO("Title", _root.transform);
            var tRT = titleGO.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0.5f, 1); tRT.anchorMax = new Vector2(0.5f, 1);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.anchoredPosition = new Vector2(0, -90);
            tRT.sizeDelta = new Vector2(1040, 110);
            var titleTm = titleGO.AddComponent<TextMeshProUGUI>();
            titleTm.text = "CHOOSE YOUR HERO";
            titleTm.fontSize = 76;
            titleTm.fontStyle = FontStyles.Bold;
            titleTm.color = GOLD;
            titleTm.font = TMP_Settings.defaultFontAsset;
            titleTm.alignment = TextAlignmentOptions.Center;
            titleTm.textWrappingMode = TextWrappingModes.NoWrap;
            titleTm.raycastTarget = false;
            try {
                titleTm.outlineWidth = 0.30f;
                titleTm.outlineColor = new Color(0.22f, 0.08f, 0.02f);
            } catch {}

            var subGO = NewGO("Sub", _root.transform);
            var subRT = subGO.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0.5f, 1); subRT.anchorMax = new Vector2(0.5f, 1);
            subRT.pivot = new Vector2(0.5f, 1);
            subRT.anchoredPosition = new Vector2(0, -205);
            subRT.sizeDelta = new Vector2(960, 44);
            var subTm = subGO.AddComponent<TextMeshProUGUI>();
            subTm.text = forcePick
                ? "Pick a class to begin your adventure"
                : "Swap classes any time from Settings";
            subTm.fontSize = 36;
            subTm.fontStyle = FontStyles.Bold;
            subTm.color = Color.white;
            subTm.font = TMP_Settings.defaultFontAsset;
            subTm.alignment = TextAlignmentOptions.Center;
            subTm.raycastTarget = false;
            // Outline — the subtitle sits directly over the backdrop art.
            try {
                subTm.outlineWidth = 0.22f;
                subTm.outlineColor = new Color(0.05f, 0.03f, 0.10f, 1f);
            } catch {}

            // ── 2×2 card grid ──
            const float CARD_W = 472;
            const float CARD_H = 600;
            const float COL_GAP = 34;
            const float ROW_GAP = 34;
            float halfW = (CARD_W + COL_GAP) / 2f;

            // Top-row card center sits this far below the canvas top.
            float topRowCenterY = -290 - CARD_H / 2f;
            float botRowCenterY = topRowCenterY - CARD_H - ROW_GAP;

            var positions = new[]
            {
                new Vector2(-halfW, topRowCenterY),   // 0 Knight   TL
                new Vector2( halfW, topRowCenterY),   // 1 Archer   TR
                new Vector2(-halfW, botRowCenterY),   // 2 Mage     BL
                new Vector2( halfW, botRowCenterY),   // 3 Assassin BR
            };

            for (int i = 0; i < CLASSES.Length; i++)
                BuildCard(_root.transform, CLASSES[i], positions[i], CARD_W, CARD_H, i);

            // ── Bottom CTA ──
            var ctaGO = NewGO("CTA", _root.transform, typeof(Image), typeof(Button));
            var ctaRT = ctaGO.GetComponent<RectTransform>();
            ctaRT.anchorMin = new Vector2(0.5f, 0); ctaRT.anchorMax = new Vector2(0.5f, 0);
            ctaRT.pivot = new Vector2(0.5f, 0);
            ctaRT.anchoredPosition = new Vector2(0, 110);
            ctaRT.sizeDelta = new Vector2(760, 150);
            var ctaImg = ctaGO.GetComponent<Image>();
            ctaImg.color = GOLD;
            _confirmBtn = ctaGO.GetComponent<Button>();
            _confirmBtn.targetGraphic = ctaImg;

            var ctaLbl = NewGO("Lbl", ctaGO.transform);
            var clRT = ctaLbl.GetComponent<RectTransform>();
            clRT.anchorMin = Vector2.zero; clRT.anchorMax = Vector2.one;
            clRT.offsetMin = Vector2.zero; clRT.offsetMax = Vector2.zero;
            _confirmLbl = ctaLbl.AddComponent<TextMeshProUGUI>();
            _confirmLbl.text = "PICK A HERO";
            _confirmLbl.fontSize = 50;
            _confirmLbl.fontStyle = FontStyles.Bold;
            _confirmLbl.color = INK;
            _confirmLbl.font = TMP_Settings.defaultFontAsset;
            _confirmLbl.alignment = TextAlignmentOptions.Center;
            _confirmLbl.raycastTarget = false;
            _confirmBtn.onClick.AddListener(() => OnConfirm(onPicked));

            // ── Optional close button ──
            if (!forcePick)
            {
                var close = NewGO("Close", _root.transform, typeof(Image), typeof(Button));
                var xRT = close.GetComponent<RectTransform>();
                xRT.anchorMin = new Vector2(1, 1); xRT.anchorMax = new Vector2(1, 1);
                xRT.pivot = new Vector2(1, 1);
                xRT.anchoredPosition = new Vector2(-38, -38);
                xRT.sizeDelta = new Vector2(92, 92);
                close.GetComponent<Image>().color = new Color(0.82f, 0.24f, 0.24f, 1f);
                var xLbl = NewGO("X", close.transform);
                var xlRT = xLbl.GetComponent<RectTransform>();
                xlRT.anchorMin = Vector2.zero; xlRT.anchorMax = Vector2.one;
                xlRT.offsetMin = Vector2.zero; xlRT.offsetMax = Vector2.zero;
                var xTm = xLbl.AddComponent<TextMeshProUGUI>();
                xTm.text = "X";
                xTm.fontSize = 52;
                xTm.fontStyle = FontStyles.Bold;
                xTm.color = Color.white;
                xTm.font = TMP_Settings.defaultFontAsset;
                xTm.alignment = TextAlignmentOptions.Center;
                xTm.raycastTarget = false;
                close.GetComponent<Button>().onClick.AddListener(Hide);
            }

            RefreshSelection();
            Debug.Log($"[HeroSelectPanel] Opened (forcePick={forcePick}, preselected={_selectedIndex}).");
        }

        public static void Hide()
        {
            if (_root != null) { Object.Destroy(_root); _root = null; }
            _cards = null;
            _confirmBtn = null;
            _confirmLbl = null;
        }

        // ─────────────────────────────────────────────────────────────────
        // CARD BUILDER
        // ─────────────────────────────────────────────────────────────────

        private static void BuildCard(Transform parent, ClassOption c, Vector2 pos,
                                      float w, float h, int index)
        {
            var card = NewGO("Card_" + c.id, parent, typeof(Image), typeof(Button));
            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(w, h);

            // Panel background — white RewardFrame sprite TINTED dark slate.
            var bgImg = card.GetComponent<Image>();
            var bgSp = LoadSprite(CARD_BG_PATH);
            if (bgSp != null)
            {
                bgImg.sprite = bgSp;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = CARD_DARK;

            // Accent strip behind the portrait — a soft class-colored glow band
            var glow = NewGO("AccentGlow", card.transform, typeof(Image));
            var glowRT = glow.GetComponent<RectTransform>();
            glowRT.anchorMin = new Vector2(0.5f, 1); glowRT.anchorMax = new Vector2(0.5f, 1);
            glowRT.pivot = new Vector2(0.5f, 1);
            glowRT.anchoredPosition = new Vector2(0, -40);
            glowRT.sizeDelta = new Vector2(w - 70, h * 0.52f);
            var glowImg = glow.GetComponent<Image>();
            glowImg.color = new Color(c.accent.r, c.accent.g, c.accent.b, 0.16f);
            glowImg.raycastTarget = false;

            // Portrait — big, fills the upper card
            var portGO = NewGO("Portrait", card.transform, typeof(Image));
            var portRT = portGO.GetComponent<RectTransform>();
            portRT.anchorMin = new Vector2(0.5f, 1); portRT.anchorMax = new Vector2(0.5f, 1);
            portRT.pivot = new Vector2(0.5f, 1);
            portRT.anchoredPosition = new Vector2(0, -18);
            portRT.sizeDelta = new Vector2(w - 60, h * 0.62f);
            var portImg = portGO.GetComponent<Image>();
            // Portrait via the shared HeroPortrait helper — same alpha-crop the
            // home screen uses, so picker and lobby show an identical hero.
            // excludeWeapon: true → the Knight's long spear is dropped for a
            // clean body shot that fits the card (no effect on other classes).
            var heroSprite = Sparq.Systems.HeroClassResolver.ResolveByClass(c.id);
            var portrait = HeroPortrait.LoadIdle(heroSprite, excludeWeapon: true);
            if (portrait.ok && portrait.sprite != null)
            {
                portImg.sprite = portrait.sprite;
                portImg.preserveAspect = true;
                portImg.color = Color.white;
            }
            else
            {
                portImg.color = new Color(0.30f, 0.25f, 0.45f, 0.4f);
            }
            portImg.raycastTarget = false;

            // Name ribbon — gold strip in the lower third
            var ribbon = NewGO("Ribbon", card.transform, typeof(Image));
            var ribRT = ribbon.GetComponent<RectTransform>();
            ribRT.anchorMin = new Vector2(0.5f, 0); ribRT.anchorMax = new Vector2(0.5f, 0);
            ribRT.pivot = new Vector2(0.5f, 0);
            ribRT.anchoredPosition = new Vector2(0, 150);
            ribRT.sizeDelta = new Vector2(w - 56, 76);
            ribbon.GetComponent<Image>().color = GOLD;
            ribbon.GetComponent<Image>().raycastTarget = false;
            var nmGO = NewGO("Name", ribbon.transform);
            var nmRT = nmGO.GetComponent<RectTransform>();
            nmRT.anchorMin = Vector2.zero; nmRT.anchorMax = Vector2.one;
            nmRT.offsetMin = Vector2.zero; nmRT.offsetMax = Vector2.zero;
            var nmTm = nmGO.AddComponent<TextMeshProUGUI>();
            nmTm.text = c.displayName;
            nmTm.fontSize = 40;
            nmTm.fontStyle = FontStyles.Bold;
            nmTm.color = INK;
            nmTm.font = TMP_Settings.defaultFontAsset;
            nmTm.alignment = TextAlignmentOptions.Center;
            nmTm.textWrappingMode = TextWrappingModes.NoWrap;
            nmTm.raycastTarget = false;

            // Flavor text — below the ribbon
            var fGO = NewGO("Flavor", card.transform);
            var fRT = fGO.GetComponent<RectTransform>();
            fRT.anchorMin = new Vector2(0.5f, 0); fRT.anchorMax = new Vector2(0.5f, 0);
            fRT.pivot = new Vector2(0.5f, 0);
            fRT.anchoredPosition = new Vector2(0, 18);
            fRT.sizeDelta = new Vector2(w - 20, 128);
            var fTm = fGO.AddComponent<TextMeshProUGUI>();
            fTm.text = c.flavor;
            fTm.fontSize = 37;
            fTm.fontStyle = FontStyles.Bold;
            fTm.color = Color.white;
            fTm.font = TMP_Settings.defaultFontAsset;
            fTm.alignment = TextAlignmentOptions.Center;
            fTm.textWrappingMode = TextWrappingModes.Normal;
            fTm.lineSpacing = 8f;
            fTm.raycastTarget = false;
            // Dark outline so the text reads cleanly against the card art.
            try {
                fTm.outlineWidth = 0.22f;
                fTm.outlineColor = new Color(0.05f, 0.03f, 0.10f, 1f);
            } catch {}

            // Default purple border — over the content
            var border = NewGO("Border", card.transform, typeof(Image));
            var brRT = border.GetComponent<RectTransform>();
            brRT.anchorMin = Vector2.zero; brRT.anchorMax = Vector2.one;
            brRT.offsetMin = Vector2.zero; brRT.offsetMax = Vector2.zero;
            var brImg = border.GetComponent<Image>();
            var brSp = LoadSprite(CARD_BORDER_PATH);
            if (brSp != null) { brImg.sprite = brSp; brImg.type = Image.Type.Sliced; brImg.color = Color.white; }
            else brImg.color = new Color(0.55f, 0.42f, 0.30f, 0.5f);
            brImg.raycastTarget = false;

            // Gold focus border — hidden until selected
            var focus = NewGO("Focus", card.transform, typeof(Image));
            var fcRT = focus.GetComponent<RectTransform>();
            fcRT.anchorMin = Vector2.zero; fcRT.anchorMax = Vector2.one;
            fcRT.offsetMin = new Vector2(-14, -14); fcRT.offsetMax = new Vector2(14, 14);
            var fcImg = focus.GetComponent<Image>();
            var fcSp = LoadSprite(CARD_FOCUS_PATH);
            if (fcSp != null) { fcImg.sprite = fcSp; fcImg.type = Image.Type.Sliced; fcImg.color = Color.white; }
            else fcImg.color = new Color(1f, 0.82f, 0.28f, 0.85f);
            fcImg.raycastTarget = false;
            focus.SetActive(false);

            int captured = index;
            card.GetComponent<Button>().onClick.AddListener(() => OnCardTap(captured));

            _cards.Add(new CardRefs { card = card, bg = bgImg, focus = fcImg });
        }

        // ─────────────────────────────────────────────────────────────────
        // SELECTION + CONFIRM
        // ─────────────────────────────────────────────────────────────────

        private static void OnCardTap(int index)
        {
            _selectedIndex = index;
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            RefreshSelection();
        }

        private static void RefreshSelection()
        {
            if (_cards != null)
            {
                for (int i = 0; i < _cards.Count; i++)
                {
                    bool sel = (i == _selectedIndex);
                    var cr = _cards[i];
                    if (cr.focus != null) cr.focus.gameObject.SetActive(sel);
                    if (cr.bg != null)    cr.bg.color = sel ? CARD_DARK_SEL : CARD_DARK;
                    if (cr.card != null)
                        cr.card.GetComponent<RectTransform>().localScale =
                            sel ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one;
                    // Bring the selected card to the front so its scaled edges
                    // and gold border aren't clipped by neighbours.
                    if (sel && cr.card != null) cr.card.transform.SetAsLastSibling();
                }
            }

            if (_confirmLbl != null)
            {
                bool has = _selectedIndex >= 0 && _selectedIndex < CLASSES.Length;
                _confirmLbl.text = has
                    ? "BEGIN AS " + CLASSES[_selectedIndex].displayName
                    : "PICK A HERO";
                if (_confirmBtn != null)
                {
                    _confirmBtn.interactable = has;
                    var img = _confirmBtn.GetComponent<Image>();
                    if (img != null)
                        img.color = has ? GOLD : new Color(0.45f, 0.38f, 0.30f, 0.9f);
                }
                _confirmLbl.color = has ? INK : new Color(0.20f, 0.16f, 0.12f, 0.8f);
            }

            // Keep the CTA + close button on top of any re-sorted cards.
            if (_confirmBtn != null) _confirmBtn.transform.SetAsLastSibling();
            if (_root != null)
            {
                var closeT = _root.transform.Find("Close");
                if (closeT != null) closeT.SetAsLastSibling();
            }
        }

        private static void OnConfirm(System.Action onPicked)
        {
            if (_selectedIndex < 0 || _selectedIndex >= CLASSES.Length) return;
            var picked = CLASSES[_selectedIndex];
            var data = Sparq.Core.SaveService.Data;
            if (data == null) { Debug.LogWarning("[HeroSelectPanel] SaveService.Data is null."); return; }
            data.heroClass = picked.id;
            try { Sparq.Core.SaveService.Save(); }
            catch (System.Exception ex) { Debug.LogError($"[HeroSelectPanel] Save failed: {ex.Message}"); }
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin); } catch {}
            try { HomeChibiUpgrade.RefreshKaruFromHeroClass(); } catch {}

            Hide();

            if (onPicked != null)
            {
                try { onPicked(); }
                catch (System.Exception ex)
                { Debug.LogError($"[HeroSelectPanel] onPicked threw: {ex.Message}"); }
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // ASSET LOADING (editor-only; runtime falls back to flat color)
        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Load a sprite by asset path — used for the Layer Lab frame/backdrop
        /// art. Hero portraits go through HeroPortrait instead (alpha-cropped).
        /// </summary>
        private static Sprite LoadSprite(string assetPath) => Sparq.Core.SpriteLoader.Load(assetPath);

        // ─────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────

        private static GameObject NewGO(string name, Transform parent, params System.Type[] comps)
        {
            var go = new GameObject(name, new System.Type[] { typeof(RectTransform) });
            go.transform.SetParent(parent, false);
            foreach (var c in comps) go.AddComponent(c);
            return go;
        }

        private static void EnsureEventSystem()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
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
