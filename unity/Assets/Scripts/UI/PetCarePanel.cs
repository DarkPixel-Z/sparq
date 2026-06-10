// PetCarePanel.cs — the Tamagotchi-style care screen. Four need meters
// (Food / Hygiene / Dental / Social) decay over real time via
// PetService.TickNeeds() and PetCareRunner; the player tops them up
// here with Feed / Bathe / Brush / Play actions. If any meter sits at
// 0 for 24h the pet dies — Revive (coins) or Manage Pets to swap.
//
// Polished card shell pattern (instantiates Layer Lab Popup_01_Basic_White
// prefab), tinted charcoal — same family as RemindPanel / GuildPanel /
// RankingsPanel.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class PetCarePanel
    {
        private static readonly Color CARD_BG    = new Color(0.17f, 0.17f, 0.20f, 1f);
        private static readonly Color CREAM      = new Color(1.00f, 0.97f, 0.85f, 1f);
        private static readonly Color INK        = new Color(0.11f, 0.13f, 0.16f, 1f);
        private static readonly Color INK_SOFT   = new Color(0.78f, 0.80f, 0.86f, 1f);
        private static readonly Color GOLD       = new Color(0.99f, 0.78f, 0.20f, 1f);
        private static readonly Color BAR_BG     = new Color(0.10f, 0.10f, 0.13f, 1f);
        private static readonly Color BAR_OK     = new Color(0.40f, 0.85f, 0.55f, 1f);
        private static readonly Color BAR_WARN   = new Color(0.99f, 0.78f, 0.20f, 1f);
        private static readonly Color BAR_DANGER = new Color(0.95f, 0.30f, 0.30f, 1f);

        // Per-need accent colours for the action button + label.
        private static readonly Color FOOD_C   = new Color(1.00f, 0.55f, 0.15f, 1f);  // orange
        private static readonly Color BATH_C   = new Color(0.42f, 0.72f, 1.00f, 1f);  // blue
        private static readonly Color BRUSH_C  = new Color(0.95f, 0.95f, 0.95f, 1f);  // white-ish (toothpaste)
        private static readonly Color PLAY_C   = new Color(0.95f, 0.45f, 0.65f, 1f);  // pink

        private const string POPUP_PREFAB = "Assets/Layer Lab/GUI Pro-FantasyRPG/Prefabs/Prefabs_Component_Popups/Popup_01_Basic_White.prefab";

        // Polish sprites — replace flat rects with proper beveled Layer Lab art.
        private const string BTN_CONVEX_GRAY  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Convex_Rectangle_01_Gray.png";
        private const string BAR_BG_SPRITE    = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/UI_Etc/StatusBar_Bg_Rectangle_01.png";
        private const string ICON_MEAT        = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/128/ItemIcon_Meat.png";
        private const string ICON_DROP        = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/128/ItemIcon_Drop.png";
        private const string ICON_HEART       = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/128/ItemIcon_Heart.png";
        private const string ICON_FRIEND      = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/128/ItemIcon_Friend.png";
        private const string ICON_GEAR_ARMOR  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/128/ItemIcon_Gear_Armor.png";
        private const string ICON_SHOP        = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/128/ItemIcon_Shop.png";
        // Soft circular glow placed behind the pet so it feels lit, not floating.
        private const string CIRCLE_GLOW      = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Border_Circle_H67_White_Bg.png";
        // Egg pip uses the same circular frame for proper bevel.
        private const string CIRCLE_BORDER    = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Border_Circle_H67_White_Border.png";
        // Fancy tab sprites — beveled with angled trim.
        private const string TAB_NORMAL  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Tab_BottomFlush_01_Single_Nomal.png";
        private const string TAB_SELECT  = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Tab_BottomFlush_01_Single_Select.png";

        private const int REVIVE_COST = 250;

        public enum Tab { Care, Gear }
        private static Tab _currentTab = Tab.Care;

        // ── Gear tab state ──────────────────────────────────────────────
        // Modelled on EquipmentPanel — filter selects which slot's items
        // show in the grid; selection is the tile the player tapped (used
        // by the "Equip" action button when no popup is open).
        private static Sparq.Systems.PetService.Slot _gearFilter
            = Sparq.Systems.PetService.Slot.Hat;
        private static string _gearSelectedId;

        // Gear tab — stat icons. Use the FantasyHero PictoIcons so each stat
        // has a DISTINCT glyph (was a bug: DEF and HP both rendered a heart).
        private const string GEAR_ATK_ICON =
            "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/128/PictoIcon_Attack.Png";
        private const string GEAR_DEF_ICON =
            "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/128/PictoIcon_Defense.Png";
        private const string GEAR_HP_ICON  =
            "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_PictoIcons/128/PictoIcon_Health.Png";
        private static readonly string[] GEAR_FRAMES_BY_TIER = {
            "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/ItemFrame_Square_01_Gray.png",
            "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/ItemFrame_Square_01_Blue.png",
            "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/ItemFrame_Square_01_Purple.png",
            "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/ItemFrame_Square_01_Yellow.png",
        };

        // Pet items don't ship a sprite each — instead we use one of three
        // Layer Lab gear icons (helmet / armor / ring) tinted by item.tint so
        // each item still looks visually distinct in its slot.
        private const string GEAR_ICON_HAT     = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/128/ItemIcon_Gear_Helmet.png";
        private const string GEAR_ICON_BODY    = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/128/ItemIcon_Gear_Armor.png";
        private const string GEAR_ICON_TRINKET = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/128/ItemIcon_Gear_Ring.png";

        private static string SlotIconPath(Sparq.Systems.PetService.Slot slot)
        {
            switch (slot)
            {
                case Sparq.Systems.PetService.Slot.Hat:     return GEAR_ICON_HAT;
                case Sparq.Systems.PetService.Slot.Body:    return GEAR_ICON_BODY;
                case Sparq.Systems.PetService.Slot.Trinket: return GEAR_ICON_TRINKET;
            }
            return GEAR_ICON_BODY;
        }

        private static GameObject _root;
        private static Transform  _body;
        private static Transform  _card;
        private static MonoBehaviour _runner;     // hosts bob/hop/floating-text coroutines
        private class CareRunner : MonoBehaviour {}

        // Tab-strip refs (rebuilt every BuildBody).
        private static Image _tabCareBg, _tabGearBg;
        private static TMP_Text _tabCareLbl, _tabGearLbl;

        public static void Show()
        {
            if (_root != null) { Hide(); return; }
            EnsureEventSystem();

            // Always settle the latest decay before showing — covers app
            // resumes where the runner hasn't ticked yet this frame.
            try { Sparq.Systems.PetService.TickNeeds(); } catch {}

            _root = new GameObject("Sparq_PetCarePanel",
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

            // Polished prefab shell (with fallback)
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
                crt.sizeDelta = new Vector2(960, 1880);
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                {
                    if (t == null) continue;
                    var n = t.gameObject.name;
                    if (n == "Text_Info" || n == "Button_OK" || n == "Content_Demo") t.gameObject.SetActive(false);
                }
                foreach (var tmp in inst.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp != null && tmp.gameObject.name == "Text_Title")
                    {
                        var pn = "Pet"; try { pn = Sparq.Core.SaveService.Data?.petName ?? "Pet"; } catch {}
                        tmp.text = pn;
                        tmp.fontSize = 60;
                        tmp.alignment = TextAlignmentOptions.MidlineLeft;
                        tmp.color = CREAM;
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
                crt.sizeDelta = new Vector2(960, 1880);
                card.GetComponent<Image>().color = CARD_BG;
            }
            _card = card.transform;

            // Back chevron top-right
            var back = NewGO("Back", _card, typeof(Image), typeof(Button));
            var bRT = back.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(1, 1); bRT.anchorMax = new Vector2(1, 1);
            bRT.pivot = new Vector2(1, 1);
            bRT.anchoredPosition = new Vector2(-30, -30);
            bRT.sizeDelta = new Vector2(96, 96);
            back.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            back.GetComponent<Image>().raycastTarget = true;
            var bBtn = back.GetComponent<Button>();
            bBtn.targetGraphic = back.GetComponent<Image>(); bBtn.interactable = true;
            var bLbl = MakeText(back.transform, "L", "<", 56, FontStyles.Bold, CREAM);
            Stretch(bLbl.rectTransform); bLbl.alignment = TextAlignmentOptions.Center;
            bBtn.onClick.AddListener(Hide);

            BuildBody();

            try { Sparq.Systems.PetService.OnChanged += OnPetChanged; } catch {}
            Debug.Log("[PetCarePanel] Opened.");
        }

        public static void Hide()
        {
            try { Sparq.Systems.PetService.OnChanged -= OnPetChanged; } catch {}
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
            _body = null; _card = null;
        }

        private static void OnPetChanged() { if (_body != null) BuildBody(); }

        // ─────────────────────────────────────────────────────────────────
        // BODY — switches between alive (care) and dead (revive) state
        // ─────────────────────────────────────────────────────────────────

        private static void BuildBody()
        {
            if (_card == null) return;
            // Wipe previous body (and tab strip — both are rebuilt below).
            for (int i = _card.childCount - 1; i >= 0; i--)
            {
                var c = _card.GetChild(i);
                if (c.name == "Body" || c.name == "TabStrip") UnityEngine.Object.Destroy(c.gameObject);
            }
            BuildTabStrip(_card);

            var bodyGO = NewGO("Body", _card, typeof(RectTransform));
            var bRT = bodyGO.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0, 0); bRT.anchorMax = new Vector2(1, 1);
            // Push the body further down so the tab strip has its own band.
            bRT.offsetMin = new Vector2(0, 0); bRT.offsetMax = new Vector2(0, -260);
            _body = bodyGO.transform;

            bool alive = Sparq.Systems.PetService.IsAlive();
            if (!alive) { BuildDeadBody(_body); return; }

            switch (_currentTab)
            {
                case Tab.Care: BuildAliveBody(_body); break;
                case Tab.Gear: BuildGearTab(_body);   break;
            }
        }

        // ── Tab strip: Care | Gear ────────────────────────────────────────
        private static void BuildTabStrip(Transform card)
        {
            var strip = NewGO("TabStrip", card, typeof(HorizontalLayoutGroup));
            var rt = strip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -180);
            rt.sizeDelta = new Vector2(-80, 90);
            var hlg = strip.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            (_tabCareBg, _tabCareLbl) = BuildTabPill(strip.transform, "Care", Tab.Care);
            (_tabGearBg, _tabGearLbl) = BuildTabPill(strip.transform, "Gear", Tab.Gear);
            ApplyTabStyles();
        }

        private static (Image bg, TMP_Text lbl) BuildTabPill(Transform parent, string label, Tab t)
        {
            var go = NewGO("Tab_" + t, parent, typeof(Image), typeof(Button));
            var img = go.GetComponent<Image>();
            // Sliced Layer Lab tab sprite — proper bevel and angled trim.
            // Sprite is swapped between Normal/Select states in ApplyTabStyles.
            img.type = Image.Type.Sliced;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            btn.onClick.AddListener(() => SetTab(t));
            // Bigger tab label — 32→40pt for readability.
            var lbl = MakeText(go.transform, "L", label, 40, FontStyles.Bold, CREAM);
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
            try { lbl.outlineWidth = 0.22f; lbl.outlineColor = new Color(0, 0, 0, 0.8f); } catch {}
            return (img, lbl);
        }

        private static void SetTab(Tab t)
        {
            if (_currentTab == t) return;
            _currentTab = t;
            BuildBody();
        }

        private static void ApplyTabStyles()
        {
            var normalSp = LoadLayerLabSprite(TAB_NORMAL);
            var selectSp = LoadLayerLabSprite(TAB_SELECT);
            void Style(Image bg, TMP_Text lbl, bool on)
            {
                if (bg != null)
                {
                    var sp = on ? selectSp : normalSp;
                    if (sp != null) { bg.sprite = sp; bg.color = Color.white; }
                    else bg.color = on ? GOLD : new Color(0.24f, 0.24f, 0.30f, 1f);
                }
                if (lbl != null) lbl.color = on ? new Color(1f, 0.97f, 0.85f, 1f) : new Color(0.78f, 0.80f, 0.86f, 1f);
            }
            Style(_tabCareBg, _tabCareLbl, _currentTab == Tab.Care);
            Style(_tabGearBg, _tabGearLbl, _currentTab == Tab.Gear);
        }

        private static void BuildAliveBody(Transform body)
        {
            int lvl = 1; int xp = 0; int xpToNext = 50; string petName = "Pet";
            try
            {
                var d = Sparq.Core.SaveService.Data;
                lvl = Mathf.Max(1, d?.petLevel ?? 1);
                xp = Mathf.Max(0, d?.petXP ?? 0);
                xpToNext = lvl * 50;
                petName = string.IsNullOrEmpty(d?.petName) ? "Pet" : d.petName;
            }
            catch {}

            // Soft warm glow behind the pet — bigger + brighter so the
            // pet reads as the focal point of the panel.
            var glow = NewGO("Glow", body, typeof(Image));
            var glRT = glow.GetComponent<RectTransform>();
            glRT.anchorMin = new Vector2(0.5f, 1); glRT.anchorMax = new Vector2(0.5f, 1);
            glRT.pivot = new Vector2(0.5f, 1);
            glRT.anchoredPosition = new Vector2(0, -10);
            glRT.sizeDelta = new Vector2(580, 580);
            var glowImg = glow.GetComponent<Image>();
            var glowSp = LoadLayerLabSprite(CIRCLE_GLOW);
            if (glowSp != null) glowImg.sprite = glowSp;
            glowImg.color = new Color(1.0f, 0.85f, 0.40f, 0.30f);
            glowImg.raycastTarget = false;

            // Pedestal — soft dark elliptical disc under the pet so it
            // looks like it's standing on something, not floating.
            var pedestal = NewGO("Pedestal", body, typeof(Image));
            var pedRT = pedestal.GetComponent<RectTransform>();
            pedRT.anchorMin = new Vector2(0.5f, 1); pedRT.anchorMax = new Vector2(0.5f, 1);
            pedRT.pivot = new Vector2(0.5f, 0.5f);
            pedRT.anchoredPosition = new Vector2(0, -460);
            pedRT.sizeDelta = new Vector2(380, 60);
            var pedImg = pedestal.GetComponent<Image>();
            if (glowSp != null) pedImg.sprite = glowSp;
            pedImg.color = new Color(0, 0, 0, 0.35f);
            pedImg.raycastTarget = false;

            // BIGGER pet portrait — fills more of the panel, reads as the
            // hero of the screen. Gets an idle bob animation so it feels
            // alive instead of static. Sprite forced to full-bright when
            // loaded so the pet pops against the dark card.
            var portrait = NewGO("Portrait", body, typeof(Image), typeof(Button));
            var pRT = portrait.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.5f, 1); pRT.anchorMax = new Vector2(0.5f, 1);
            pRT.pivot = new Vector2(0.5f, 1);
            pRT.anchoredPosition = new Vector2(0, -40);
            pRT.sizeDelta = new Vector2(480, 480);
            var portImg = portrait.GetComponent<Image>();
            portImg.color = new Color(0.22f, 0.22f, 0.28f, 0.0f);  // hidden until sprite loads
            portImg.raycastTarget = true;
            portImg.preserveAspect = true;
            TryLoadPetSprite(portImg);
            // Force full-bright in case the sprite loader didn't reset color.
            if (portImg.sprite != null) portImg.color = Color.white;

            // Tap pet → quick "happy hop" reaction + tiny social bump.
            var portBtn = portrait.GetComponent<Button>();
            portBtn.targetGraphic = portrait.GetComponent<Image>();
            portBtn.onClick.AddListener(() => {
                EnsureRunner();
                if (_runner != null) _runner.StartCoroutine(PetHopCoroutine(pRT));
                FloatText(body, "♥", new Color(1f, 0.5f, 0.65f, 1f),
                    pRT.anchoredPosition + new Vector2(0, -180));
            });

            // Idle bob — sin-wave Y oscillation, runs continuously while
            // the panel is open.
            EnsureRunner();
            if (_runner != null) _runner.StartCoroutine(IdleBobCoroutine(pRT, pRT.anchoredPosition));

            // Lv pill at bottom-right of portrait (stays on the portrait so it
            // bobs along with the idle animation — feels lively).
            var lvPill = NewGO("LvPill", portrait.transform, typeof(Image));
            var lpRT = lvPill.GetComponent<RectTransform>();
            lpRT.anchorMin = new Vector2(1, 0); lpRT.anchorMax = new Vector2(1, 0);
            lpRT.pivot = new Vector2(1, 0);
            lpRT.anchoredPosition = new Vector2(-12, 12);
            lpRT.sizeDelta = new Vector2(120, 50);
            lvPill.GetComponent<Image>().color = GOLD;
            var lpTxt = MakeText(lvPill.transform, "L", "Lv " + lvl, 30, FontStyles.Bold, INK);
            Stretch(lpTxt.rectTransform); lpTxt.alignment = TextAlignmentOptions.Center;

            // Pet name header — single bold line in the gap between the Lv
            // pill (bottom y=-508) and the XP bar (top y=-560). The Layer Lab
            // prefab's Text_Title at the top of the card wasn't rendering
            // reliably, so we display the name explicitly inside the body.
            // Tag inline after the name: species when healthy, ⚠ warning when
            // any need is critical — keeps the warning visible WITHOUT needing
            // a separate row that collided with the Hungry label band.
            string speciesName = "";
            try
            {
                var act = Sparq.Systems.PetService.Active();
                var spec = act != null ? Sparq.Systems.PetService.FindSpecies(act.speciesId) : null;
                if (spec != null) speciesName = spec.name;
            }
            catch {}
            long warnSecs = -1;
            try { warnSecs = Sparq.Systems.PetService.SecondsUntilDeath(); } catch {}
            bool inDanger = warnSecs >= 0;
            string tag;
            if (inDanger)
            {
                long hrs = warnSecs / 3600;
                tag = $"<size=24><color=#F25050>  ⚠ needs care · {hrs}h</color></size>";
            }
            else if (!string.IsNullOrEmpty(speciesName))
            {
                tag = $"  <size=22><color=#B8BBC4>· {speciesName}</color></size>";
            }
            else tag = "";

            var nameTxt = MakeText(body, "PetName", petName + tag, 36, FontStyles.Bold, CREAM);
            try { nameTxt.outlineWidth = 0.26f; nameTxt.outlineColor = new Color(0, 0, 0, 0.95f); } catch {}
            nameTxt.richText = true;
            var nRT = nameTxt.rectTransform;
            nRT.anchorMin = new Vector2(0, 1); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0.5f, 1);
            nRT.offsetMin = new Vector2(20, -555); nRT.offsetMax = new Vector2(-20, -512);
            nameTxt.alignment = TextAlignmentOptions.Center;

            // XP bar under portrait (kept at -560 — no cascading shift required).
            var xpBg = NewGO("XpBg", body, typeof(Image));
            var xbRT = xpBg.GetComponent<RectTransform>();
            xbRT.anchorMin = new Vector2(0.5f, 1); xbRT.anchorMax = new Vector2(0.5f, 1);
            xbRT.pivot = new Vector2(0.5f, 1);
            xbRT.anchoredPosition = new Vector2(0, -560);
            xbRT.sizeDelta = new Vector2(640, 46);
            xpBg.GetComponent<Image>().color = BAR_BG;
            xpBg.GetComponent<Image>().raycastTarget = false;
            var xpFg = NewGO("XpFg", xpBg.transform, typeof(Image));
            var xfRT = xpFg.GetComponent<RectTransform>();
            xfRT.anchorMin = new Vector2(0, 0); xfRT.anchorMax = new Vector2(1, 1);
            xfRT.offsetMin = new Vector2(3, 3); xfRT.offsetMax = new Vector2(-3, -3);
            var xfImg = xpFg.GetComponent<Image>();
            xfImg.color = GOLD;
            xfImg.type = Image.Type.Filled;
            xfImg.fillMethod = Image.FillMethod.Horizontal;
            xfImg.fillAmount = xpToNext > 0 ? Mathf.Clamp01((float)xp / xpToNext) : 0f;
            xfImg.raycastTarget = false;
            // Dark ink on the gold bar — high contrast, with a CREAM outline so
            // the digits still read on the dark BAR_BG when XP is near 0.
            var xpTxt = MakeText(xpBg.transform, "T", $"{xp} / {xpToNext} XP", 30, FontStyles.Bold, INK);
            Stretch(xpTxt.rectTransform); xpTxt.alignment = TextAlignmentOptions.Center;
            try { xpTxt.outlineWidth = 0.22f; xpTxt.outlineColor = new Color(1f, 0.95f, 0.80f, 0.85f); } catch {}

            // (Death warning moved inline into the pet-name tag above so it
            // doesn't collide with the Hungry row label band.)

            // Four need bars + their action buttons. Tightened spacing 150→130
            // and start raised from -700 → -640 so the whole lower stack lifts
            // up far enough for the bottom action row to clear the viewport.
            float y = -640;
            BuildNeedRow(body, Sparq.Systems.PetService.Need.Food,    "Hungry",   "FEED",    FOOD_C,  ICON_MEAT,  y);     y -= 130;
            BuildNeedRow(body, Sparq.Systems.PetService.Need.Hygiene, "Bath",     "BATHE",   BATH_C,  ICON_DROP,  y);     y -= 130;
            BuildNeedRow(body, Sparq.Systems.PetService.Need.Dental,  "Teeth",    "BRUSH",   BRUSH_C, "",         y);     y -= 130;
            BuildNeedRow(body, Sparq.Systems.PetService.Need.Social,  "Lonely",   "PLAY",    PLAY_C,  ICON_HEART, y);     y -= 130;

            // Care-streak banner — Sims-style reward indicator. Shows
            // current streak + days until next milestone.
            // Last need row ends ~ y=-1150; banner needs ~115 of room.
            BuildStreakBanner(body, new Vector2(0, -1180));

            // Egg inventory header — 25px below the banner bottom (~-1295).
            BuildEggInventory(body, new Vector2(0, -1320));

            // Bottom action row — three iconified buttons replace the
            // single "Manage Pets · Equipment · Shop" catch-all so each
            // destination has its own discoverable affordance. The row is
            // 130 tall (28 for the hint sub-line + 92 for the tiles + a
            // little internal padding). Anchored at y=-1430 so it ends at
            // y=-1560, ~60px above the body bottom.
            BuildBottomActionRow(body, new Vector2(0, -1430));
        }

        // ── Bottom action row: Manage | Equip | Shop ─────────────────────
        // Each tile routes to a distinct destination:
        //   Manage → PetPanel.Roster tab (swap / sell / activate pets)
        //   Equip  → switch THIS panel to the Gear tab (in-place)
        //   Shop   → PetPanel.Shop tab (buy food / pets / items)
        private static void BuildBottomActionRow(Transform parent, Vector2 anchoredPos)
        {
            var row = NewGO("BottomRow", parent, typeof(RectTransform));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(900, 130);

            // Sub-line above the tiles so the destinations read at a glance
            // ("Manage" alone is ambiguous; this clarifies what each tile does).
            // Anchored just below the row's top edge — sits inside the row.
            var hint = MakeText(row.transform, "Hint",
                "Roster   ·   Equipment   ·   Shop", 22, FontStyles.Bold, INK_SOFT);
            try { hint.outlineWidth = 0.18f; hint.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}
            var hRT = hint.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.anchoredPosition = new Vector2(0, -2);
            hRT.sizeDelta = new Vector2(0, 28);
            hint.alignment = TextAlignmentOptions.Center;

            BuildActionTile(row.transform, "Manage", ICON_FRIEND,
                new Color(0.25f, 0.65f, 1.00f, 1f), -290,
                () => OpenPetPanelTab(Sparq.UI.PetPanel.Tab.Roster));
            BuildActionTile(row.transform, "Equip",  ICON_GEAR_ARMOR,
                new Color(1.00f, 0.45f, 0.75f, 1f),    0,
                () => SetTab(Tab.Gear));
            BuildActionTile(row.transform, "Shop",   ICON_SHOP,
                new Color(1.00f, 0.78f, 0.20f, 1f),  290,
                () => OpenPetPanelTab(Sparq.UI.PetPanel.Tab.Shop));
        }

        private static void BuildActionTile(Transform parent, string label, string iconPath,
            Color accent, float xOffset, System.Action onClick)
        {
            var tile = NewGO("Tile_" + label, parent, typeof(Image), typeof(Button));
            var trt = tile.GetComponent<RectTransform>();
            // Tiles sit BELOW the hint sub-line. Anchor top-center of the row
            // so the row's hint label has its own 28px strip up top.
            trt.anchorMin = new Vector2(0.5f, 1); trt.anchorMax = new Vector2(0.5f, 1);
            trt.pivot = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(xOffset, -38);
            trt.sizeDelta = new Vector2(260, 92);
            var img = tile.GetComponent<Image>();
            var btnSp = LoadLayerLabSprite(BTN_CONVEX_GRAY);
            if (btnSp != null) { img.sprite = btnSp; img.type = Image.Type.Sliced; img.color = accent; }
            else img.color = accent;
            img.raycastTarget = true;
            var btn = tile.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.interactable = onClick != null;
            if (onClick != null) btn.onClick.AddListener(() => {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                onClick.Invoke();
            });

            // Icon — left side of the tile, label flows to the right of it.
            // Horizontal layout reads more like a button than the previous
            // icon-over-tiny-label stack.
            var ico = NewGO("Ico", tile.transform, typeof(Image));
            var iRT = ico.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0, 0.5f); iRT.anchorMax = new Vector2(0, 0.5f);
            iRT.pivot = new Vector2(0, 0.5f);
            iRT.anchoredPosition = new Vector2(12, 0);
            iRT.sizeDelta = new Vector2(68, 68);
            var iImg = ico.GetComponent<Image>();
            var sp = LoadLayerLabSprite(iconPath);
            if (sp != null) { iImg.sprite = sp; iImg.preserveAspect = true; iImg.color = Color.white; }
            else iImg.color = INK;
            iImg.raycastTarget = false;

            // Label — right of icon, dark ink with bright outline for contrast.
            var lbl = MakeText(tile.transform, "L", label, 36, FontStyles.Bold, INK);
            try { lbl.outlineWidth = 0.28f; lbl.outlineColor = new Color(1f, 0.97f, 0.85f, 0.95f); } catch {}
            var lRT = lbl.rectTransform;
            lRT.anchorMin = new Vector2(0, 0); lRT.anchorMax = new Vector2(1, 1);
            lRT.offsetMin = new Vector2(88, 4); lRT.offsetMax = new Vector2(-10, -4);
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.enableAutoSizing = true;
            lbl.fontSizeMin = 26;
            lbl.fontSizeMax = 38;
        }

        private static void OpenPetPanel()
        {
            Hide();
            try { Sparq.UI.PetPanel.Show(); }
            catch (System.Exception ex)
            { Debug.LogError($"[PetCarePanel] PetPanel.Show failed: {ex.Message}"); }
        }

        // Routes the tile-tap into PetPanel pre-selected on the right tab —
        // Roster for "Manage", Shop for "Shop", so the player lands exactly
        // where they expect instead of always on the Gear tab.
        private static void OpenPetPanelTab(Sparq.UI.PetPanel.Tab initialTab)
        {
            Hide();
            try { Sparq.UI.PetPanel.Show(initialTab); }
            catch (System.Exception ex)
            { Debug.LogError($"[PetCarePanel] PetPanel.Show({initialTab}) failed: {ex.Message}"); }
        }

        // ── Care-streak banner ────────────────────────────────────────────
        private static void BuildStreakBanner(Transform parent, Vector2 anchoredPos)
        {
            int streak = 0;
            try { streak = Sparq.Core.SaveService.Data?.petCareStreakDays ?? 0; } catch {}

            // Find the next milestone
            int nextMs = -1; string nextRarity = "";
            foreach (var kv in Sparq.Systems.PetService.STREAK_MILESTONES)
            {
                if (kv.Key > streak && (nextMs < 0 || kv.Key < nextMs))
                { nextMs = kv.Key; nextRarity = kv.Value; }
            }

            // Sliced beveled banner sprite for proper framing instead of
            // a flat charcoal rectangle that visually merged with the card.
            var bg = NewGO("StreakBanner", parent, typeof(Image));
            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(840, 115);
            var bgImg = bg.GetComponent<Image>();
            var bSp = LoadLayerLabSprite(BTN_CONVEX_GRAY);
            if (bSp != null) { bgImg.sprite = bSp; bgImg.type = Image.Type.Sliced; }
            // Slightly warmer, lighter charcoal so the banner pops against
            // the card and the GOLD text reads better.
            bgImg.color = new Color(0.30f, 0.27f, 0.22f, 1f);
            bgImg.raycastTarget = false;

            // Small flame/streak chip on the left so the banner has an
            // anchor icon instead of just text floating mid-card.
            var chip = NewGO("Chip", bg.transform, typeof(Image));
            var chRT = chip.GetComponent<RectTransform>();
            chRT.anchorMin = new Vector2(0, 0.5f); chRT.anchorMax = new Vector2(0, 0.5f);
            chRT.pivot = new Vector2(0, 0.5f);
            chRT.anchoredPosition = new Vector2(16, 0);
            chRT.sizeDelta = new Vector2(82, 82);
            var chImg = chip.GetComponent<Image>();
            var glowSp = LoadLayerLabSprite(CIRCLE_GLOW);
            if (glowSp != null) chImg.sprite = glowSp;
            // Orange always — a bright streak-up tone when active, a softer
            // orange when streak is 0 (still inviting, not muted grey).
            chImg.color = streak > 0
                ? new Color(1.00f, 0.55f, 0.20f, 1f)   // bright orange
                : new Color(0.95f, 0.50f, 0.20f, 0.85f); // softer orange
            chImg.raycastTarget = false;
            var chLbl = MakeText(chip.transform, "L",
                streak > 0 ? streak.ToString() : "?",
                streak >= 100 ? 32 : 42, FontStyles.Bold, CREAM);
            Stretch(chLbl.rectTransform); chLbl.alignment = TextAlignmentOptions.Center;
            try { chLbl.outlineWidth = 0.25f; chLbl.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}

            var hdr = MakeText(bg.transform, "H",
                streak > 0 ? $"Care streak: {streak} day{(streak == 1 ? "" : "s")}"
                           : "Keep your pet happy",
                34, FontStyles.Bold, new Color(1.00f, 0.90f, 0.45f, 1f));
            try { hdr.outlineWidth = 0.22f; hdr.outlineColor = new Color(0.10f, 0.06f, 0.02f, 1f); } catch {}
            var hRT = hdr.rectTransform;
            hRT.anchorMin = new Vector2(0, 0.5f); hRT.anchorMax = new Vector2(1, 1);
            hRT.offsetMin = new Vector2(118, 0); hRT.offsetMax = new Vector2(-20, -4);
            hdr.alignment = TextAlignmentOptions.MidlineLeft;

            var sub = MakeText(bg.transform, "S",
                nextMs > 0
                  ? $"{nextMs - streak} day{(nextMs - streak == 1 ? "" : "s")} until {nextRarity.ToUpper()} egg"
                  : "Max milestone reached — legendary care!",
                30, FontStyles.Bold, CREAM);
            try { sub.outlineWidth = 0.22f; sub.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 0); sRT.anchorMax = new Vector2(1, 0.5f);
            sRT.offsetMin = new Vector2(118, 4); sRT.offsetMax = new Vector2(-20, 0);
            sub.alignment = TextAlignmentOptions.MidlineLeft;
        }

        // ── Egg inventory row ─────────────────────────────────────────────
        private static void BuildEggInventory(Transform parent, Vector2 anchoredPos)
        {
            var eggs = Sparq.Systems.PetService.EggInventory();

            var hdr = MakeText(parent, "EggHdr",
                eggs.Count > 0 ? $"Eggs ({eggs.Count})  —  tap to hatch"
                               : "No eggs yet — keep your streak going!",
                34, FontStyles.Bold, CREAM);
            try { hdr.outlineWidth = 0.22f; hdr.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var hRT = hdr.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.offsetMin = new Vector2(48, anchoredPos.y); hRT.offsetMax = new Vector2(-48, anchoredPos.y + 40);
            hdr.alignment = TextAlignmentOptions.MidlineLeft;

            if (eggs.Count == 0) return;

            // Egg row — up to 5 visible side-by-side. Each egg is a beveled
            // circular frame (Layer Lab Button_Border_Circle sprite) tinted
            // by rarity, with a white inner ring overlay and a rarity ribbon
            // beneath it. The 🥚 emoji-as-glyph was unreliable in the default
            // TMP font so we drop it for a clean inner highlight + label.
            var bgSp     = LoadLayerLabSprite(CIRCLE_GLOW);
            var borderSp = LoadLayerLabSprite(CIRCLE_BORDER);
            int show = Mathf.Min(5, eggs.Count);
            // Shrunken vs previous 132 — fits under the egg header without
            // colliding with the bottom action row when eggs exist.
            float tileW = 96, gap = 14;
            float totalW = show * tileW + (show - 1) * gap;
            float startX = -totalW / 2f + tileW / 2f;
            for (int i = 0; i < show; i++)
            {
                int capturedIdx = i;
                string rarity = eggs[i];
                Color rarC = EggColor(rarity);

                // Outer button hit-target
                var egg = NewGO("Egg_" + i, parent, typeof(Image), typeof(Button));
                var eRT = egg.GetComponent<RectTransform>();
                eRT.anchorMin = new Vector2(0.5f, 1); eRT.anchorMax = new Vector2(0.5f, 1);
                eRT.pivot = new Vector2(0.5f, 1);
                // Tight gap (-36 from header) — keeps the whole egg row inside
                // the band between the banner and the bottom action tiles.
                eRT.anchoredPosition = new Vector2(startX + i * (tileW + gap), anchoredPos.y - 36);
                eRT.sizeDelta = new Vector2(tileW, tileW + 18);
                var eImg = egg.GetComponent<Image>();
                eImg.color = new Color(0, 0, 0, 0);    // hit-area only, child sprites carry the visuals
                eImg.raycastTarget = true;
                var btn = egg.GetComponent<Button>();
                btn.targetGraphic = eImg;
                btn.interactable = true;
                btn.onClick.AddListener(() => OnHatchEggTapped(capturedIdx, rarity));

                // Soft outer glow halo (rarity-tinted, semi-transparent)
                var halo = NewGO("Halo", egg.transform, typeof(Image));
                var haRT = halo.GetComponent<RectTransform>();
                haRT.anchorMin = new Vector2(0.5f, 1); haRT.anchorMax = new Vector2(0.5f, 1);
                haRT.pivot = new Vector2(0.5f, 1);
                haRT.anchoredPosition = new Vector2(0, 0);
                haRT.sizeDelta = new Vector2(tileW + 18, tileW + 18);
                var haImg = halo.GetComponent<Image>();
                if (bgSp != null) haImg.sprite = bgSp;
                haImg.color = new Color(rarC.r, rarC.g, rarC.b, 0.35f);
                haImg.raycastTarget = false;

                // Egg body — filled circle in rarity colour
                var body = NewGO("Body", egg.transform, typeof(Image));
                var bRT = body.GetComponent<RectTransform>();
                bRT.anchorMin = new Vector2(0.5f, 1); bRT.anchorMax = new Vector2(0.5f, 1);
                bRT.pivot = new Vector2(0.5f, 1);
                bRT.anchoredPosition = new Vector2(0, -6);
                bRT.sizeDelta = new Vector2(tileW - 16, tileW - 16);
                var bImg = body.GetComponent<Image>();
                if (bgSp != null) bImg.sprite = bgSp;
                bImg.color = rarC;
                bImg.raycastTarget = false;

                // Inner highlight — small bright spot top-left, sells "egg" shape
                var hi = NewGO("Hi", body.transform, typeof(Image));
                var hiRT = hi.GetComponent<RectTransform>();
                hiRT.anchorMin = new Vector2(0.32f, 0.62f); hiRT.anchorMax = new Vector2(0.32f, 0.62f);
                hiRT.pivot = new Vector2(0.5f, 0.5f);
                hiRT.sizeDelta = new Vector2(36, 26);
                var hiImg = hi.GetComponent<Image>();
                if (bgSp != null) hiImg.sprite = bgSp;
                hiImg.color = new Color(1f, 1f, 1f, 0.55f);
                hiImg.raycastTarget = false;

                // White border ring overlay — the Layer Lab circle-border
                // sprite adds the beveled rim that makes it read as a real
                // chunky token instead of a flat dot.
                if (borderSp != null)
                {
                    var ring = NewGO("Ring", body.transform, typeof(Image));
                    var rRT = ring.GetComponent<RectTransform>();
                    Stretch(rRT);
                    var rImg = ring.GetComponent<Image>();
                    rImg.sprite = borderSp;
                    rImg.color = new Color(1f, 1f, 1f, 0.85f);
                    rImg.raycastTarget = false;
                }

                // Rarity ribbon BELOW the egg (was a tiny corner pip — too small to read).
                var pip = NewGO("Pip", egg.transform, typeof(Image));
                var pRT = pip.GetComponent<RectTransform>();
                pRT.anchorMin = new Vector2(0.5f, 0); pRT.anchorMax = new Vector2(0.5f, 0);
                pRT.pivot = new Vector2(0.5f, 0);
                pRT.anchoredPosition = new Vector2(0, 0);
                pRT.sizeDelta = new Vector2(tileW - 4, 26);
                var pipImg = pip.GetComponent<Image>();
                var ribbonSp = LoadLayerLabSprite(BTN_CONVEX_GRAY);
                if (ribbonSp != null) { pipImg.sprite = ribbonSp; pipImg.type = Image.Type.Sliced; }
                pipImg.color = new Color(0.10f, 0.10f, 0.13f, 0.92f);
                pipImg.raycastTarget = false;
                var pTxt = MakeText(pip.transform, "T",
                    rarity.ToUpper(), 18, FontStyles.Bold, rarC);
                Stretch(pTxt.rectTransform); pTxt.alignment = TextAlignmentOptions.Center;
                try { pTxt.outlineWidth = 0.20f; pTxt.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            }
        }

        private static Color EggColor(string rarity)
        {
            switch ((rarity ?? "").ToLowerInvariant())
            {
                case Sparq.Systems.PetService.EGG_COMMON:    return new Color(0.70f, 0.72f, 0.75f, 1f); // grey
                case Sparq.Systems.PetService.EGG_RARE:      return new Color(0.42f, 0.72f, 1.00f, 1f); // blue
                case Sparq.Systems.PetService.EGG_EPIC:      return new Color(0.75f, 0.45f, 1.00f, 1f); // purple
                case Sparq.Systems.PetService.EGG_LEGENDARY: return new Color(1.00f, 0.78f, 0.20f, 1f); // gold
                case Sparq.Systems.PetService.EGG_MYTHIC:    return new Color(1.00f, 0.40f, 0.55f, 1f); // pink-red
            }
            return Color.gray;
        }

        private static void OnHatchEggTapped(int idx, string rarity)
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            var pet = Sparq.Systems.PetService.HatchEgg(idx);
            if (pet == null)
            {
                Debug.Log("[PetCarePanel] Hatch failed (roster full?).");
                return;
            }
            ShowHatchResult(pet, rarity);
        }

        // Simple celebration overlay for a successful hatch.
        private static void ShowHatchResult(Sparq.Systems.PetService.Pet pet, string rarity)
        {
            if (_root == null) return;
            var overlay = new GameObject("HatchResult",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            overlay.transform.SetParent(_root.transform, false);
            Stretch(overlay.GetComponent<RectTransform>());
            var oc = overlay.GetComponent<Canvas>();
            oc.overrideSorting = true;
            oc.sortingOrder = _root.GetComponent<Canvas>().sortingOrder + 5;

            var dim = NewGO("Dim", overlay.transform, typeof(Image), typeof(Button));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);

            var box = NewGO("Box", overlay.transform, typeof(Image));
            var bRT = box.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0.5f, 0.5f); bRT.anchorMax = new Vector2(0.5f, 0.5f);
            bRT.pivot = new Vector2(0.5f, 0.5f);
            bRT.sizeDelta = new Vector2(740, 720);
            box.GetComponent<Image>().color = CARD_BG;

            var tag = MakeText(box.transform, "T", $"{rarity.ToUpper()} EGG HATCHED!",
                32, FontStyles.Bold, EggColor(rarity));
            var tRT = tag.rectTransform;
            tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.offsetMin = new Vector2(20, -80); tRT.offsetMax = new Vector2(-20, -40);
            tag.alignment = TextAlignmentOptions.Center;

            var spec = Sparq.Systems.PetService.FindSpecies(pet.speciesId);
            var portrait = NewGO("Portrait", box.transform, typeof(Image));
            var pRT = portrait.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.5f, 1); pRT.anchorMax = new Vector2(0.5f, 1);
            pRT.pivot = new Vector2(0.5f, 1);
            pRT.anchoredPosition = new Vector2(0, -110);
            pRT.sizeDelta = new Vector2(280, 280);
            portrait.GetComponent<Image>().color = spec != null ? spec.tint : Color.white;
            if (spec != null && !string.IsNullOrEmpty(spec.spritePath))
            {
                var sp = LoadLayerLabSprite(spec.spritePath);
                if (sp != null) { portrait.GetComponent<Image>().sprite = sp; portrait.GetComponent<Image>().preserveAspect = true; portrait.GetComponent<Image>().color = Color.white; }
            }

            var nm = MakeText(box.transform, "N", spec != null ? spec.name : pet.nickname,
                44, FontStyles.Bold, CREAM);
            var nRT = nm.rectTransform;
            nRT.anchorMin = new Vector2(0, 1); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0.5f, 1);
            nRT.offsetMin = new Vector2(20, -460); nRT.offsetMax = new Vector2(-20, -400);
            nm.alignment = TextAlignmentOptions.Center;

            var sub = MakeText(box.transform, "S",
                spec != null ? $"{spec.rarity}  •  {spec.element}" : rarity.ToUpper(),
                26, FontStyles.Bold, INK_SOFT);
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0.5f, 1);
            sRT.offsetMin = new Vector2(20, -500); sRT.offsetMax = new Vector2(-20, -460);
            sub.alignment = TextAlignmentOptions.Center;

            var ok = NewGO("OK", box.transform, typeof(Image), typeof(Button));
            var oRT = ok.GetComponent<RectTransform>();
            oRT.anchorMin = new Vector2(0.5f, 0); oRT.anchorMax = new Vector2(0.5f, 0);
            oRT.pivot = new Vector2(0.5f, 0);
            oRT.anchoredPosition = new Vector2(0, 40);
            oRT.sizeDelta = new Vector2(380, 100);
            ok.GetComponent<Image>().color = new Color(0.40f, 0.85f, 0.55f, 1f);
            var okBtn = ok.GetComponent<Button>();
            okBtn.targetGraphic = ok.GetComponent<Image>(); okBtn.interactable = true;
            var okLbl = MakeText(ok.transform, "L", "AWESOME", 32, FontStyles.Bold, INK);
            Stretch(okLbl.rectTransform); okLbl.alignment = TextAlignmentOptions.Center;
            okBtn.onClick.AddListener(() => UnityEngine.Object.Destroy(overlay));
            dim.GetComponent<Button>().onClick.AddListener(() => UnityEngine.Object.Destroy(overlay));
        }

        // ─────────────────────────────────────────────────────────────────
        // GEAR TAB — Hat / Body / Trinket equipment slots + owned-items grid
        // (mirrors the hero's Equipment screen but trimmed to the pet's
        // 3-slot system in PetService).
        // ─────────────────────────────────────────────────────────────────
        // ── GEAR TAB — modelled on the hero EquipmentPanel layout ────────
        // Big pet portrait + 3 slot rings flanking it, Atk/Def/HP stat
        // chips, Hat/Body/Trinket filter tabs, a rarity-framed item grid,
        // and an Equip All / Unequip All action row at the bottom. Body
        // is 1620 tall (card 1880, top 260 reserved for the Care/Gear tab
        // strip). All Y coords below are anchored to body top.
        private static void BuildGearTab(Transform body)
        {
            Sparq.Systems.PetService.Pet active = null;
            try { active = Sparq.Systems.PetService.Active(); } catch {}
            if (active == null)
            {
                var msg = MakeText(body, "NoPet",
                    "No active pet — pick one from Manage Pets.",
                    32, FontStyles.Italic, INK_SOFT);
                var mRT = msg.rectTransform;
                mRT.anchorMin = new Vector2(0, 1); mRT.anchorMax = new Vector2(1, 1);
                mRT.pivot = new Vector2(0.5f, 1);
                mRT.offsetMin = new Vector2(40, -360); mRT.offsetMax = new Vector2(-40, -260);
                msg.alignment = TextAlignmentOptions.Center;
                BuildBottomActionRow(body, new Vector2(0, -1480));
                return;
            }

            // ── Portrait container with the pet figure layered on a dark
            // backdrop, plus a LV pill anchored to the bottom.
            BuildGearPortrait(body, active);

            // ── 3 slot rings in a symmetric horizontal row beneath the
            // portrait (was an asymmetric 2-right/1-left flank that looked
            // off-balance). Order Hat · Body · Trinket reads left-to-right.
            BuildPetSlotRing(body, active, Sparq.Systems.PetService.Slot.Hat,     new Vector2(-205, -330));
            BuildPetSlotRing(body, active, Sparq.Systems.PetService.Slot.Body,    new Vector2(   0, -330));
            BuildPetSlotRing(body, active, Sparq.Systems.PetService.Slot.Trinket, new Vector2( 205, -330));

            // ── Stats strip (Atk / Def / HP chips with icons).
            BuildPetStatsRow(body, active, new Vector2(0, -545));

            // ── Filter strip — Hat / Body / Trinket. Tapping a tab
            // rebuilds the grid below for that slot type.
            BuildPetGearFilterStrip(body, new Vector2(0, -670));

            // ── Item grid — owned items for the selected slot.
            // Sits between the filter strip and the action row at the bottom.
            BuildPetGearGrid(body, active);

            // ── Equip All / Unequip All action pills pinned to the bottom.
            BuildPetGearActionButtons(body, active);
        }

        private static void BuildGearPortrait(Transform body, Sparq.Systems.PetService.Pet active)
        {
            var portrait = NewGO("GearPortrait", body, typeof(Image));
            var pRT = portrait.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.5f, 1); pRT.anchorMax = new Vector2(0.5f, 1);
            pRT.pivot = new Vector2(0.5f, 1);
            pRT.anchoredPosition = new Vector2(0, -12);
            // Slightly smaller so the slot row sits cleanly below it.
            pRT.sizeDelta = new Vector2(300, 300);
            var pImg = portrait.GetComponent<Image>();
            // Rounded sliced frame so the "stage" reads as an intentional card.
            var stageSp = LoadLayerLabSprite(BTN_CONVEX_GRAY);
            if (stageSp != null) { pImg.sprite = stageSp; pImg.type = Image.Type.Sliced; }
            pImg.color = new Color(0.10f, 0.08f, 0.22f, 0.85f);
            pImg.raycastTarget = false;

            // Pet figure on top of the backdrop. Slight inset so the dark
            // backdrop reads as a "stage" the pet stands on.
            var figGO = NewGO("Figure", portrait.transform, typeof(Image));
            var fRT = figGO.GetComponent<RectTransform>();
            fRT.anchorMin = new Vector2(0.5f, 0.5f); fRT.anchorMax = new Vector2(0.5f, 0.5f);
            fRT.pivot = new Vector2(0.5f, 0.5f);
            fRT.anchoredPosition = new Vector2(0, 22);
            fRT.sizeDelta = new Vector2(248, 248);
            var fImg = figGO.GetComponent<Image>();
            fImg.color = new Color(0, 0, 0, 0);    // transparent until sprite loads
            TryLoadPetSprite(fImg);
            fImg.raycastTarget = false;

            // LV pill at the bottom-center of the portrait box
            int lvl = 1; try { lvl = Mathf.Max(1, active.level); } catch {}
            var lbl = MakeText(portrait.transform, "Lv", $"LV.{lvl}", 36, FontStyles.Bold, CREAM);
            try { lbl.outlineWidth = 0.22f; lbl.outlineColor = new Color(0.05f, 0.03f, 0.10f); } catch {}
            var lRT = lbl.rectTransform;
            lRT.anchorMin = new Vector2(0, 0); lRT.anchorMax = new Vector2(1, 0);
            lRT.pivot = new Vector2(0.5f, 0);
            lRT.anchoredPosition = new Vector2(0, 10);
            lRT.sizeDelta = new Vector2(0, 50);
            lbl.alignment = TextAlignmentOptions.Center;
        }

        // ── Slot ring — 220×220 framed square that displays the currently
        // equipped item for that slot (or "+" hint when empty). Tap an
        // empty slot to filter the grid; tap an occupied slot to unequip.
        private static void BuildPetSlotRing(Transform body, Sparq.Systems.PetService.Pet pet,
            Sparq.Systems.PetService.Slot slot, Vector2 anchoredPos)
        {
            string equippedId = "";
            switch (slot)
            {
                case Sparq.Systems.PetService.Slot.Hat:     equippedId = pet.hatId   ?? ""; break;
                case Sparq.Systems.PetService.Slot.Body:    equippedId = pet.bodyId  ?? ""; break;
                case Sparq.Systems.PetService.Slot.Trinket: equippedId = pet.trinkId ?? ""; break;
            }
            var eq = !string.IsNullOrEmpty(equippedId)
                ? Sparq.Systems.PetService.FindItem(equippedId)
                : null;

            var go = NewGO("Slot_" + slot, body, typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = anchoredPos;
            // 186px with 205 spacing gives a clean ~20px gap between the three.
            rt.sizeDelta = new Vector2(186, 186);
            var bgImg = go.GetComponent<Image>();
            // Sliced rarity frame when occupied (tier-tinted), neutral grey when empty.
            var frame = LoadLayerLabSprite(GEAR_FRAMES_BY_TIER[eq != null ? ItemTier(eq) : 0]);
            if (frame != null) { bgImg.sprite = frame; bgImg.type = Image.Type.Sliced; bgImg.color = Color.white; }
            else bgImg.color = eq != null ? new Color(0.50f, 0.50f, 0.56f, 1f) : new Color(0.42f, 0.42f, 0.48f, 0.92f);
            bgImg.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = bgImg;
            btn.interactable = true;
            btn.onClick.AddListener(() => {
                if (eq != null)
                {
                    try { Sparq.Systems.PetService.Unequip(pet.instanceId, slot); } catch {}
                }
                else
                {
                    _gearFilter = slot;
                    _gearSelectedId = null;
                    BuildBody();   // rebuild gear tab
                }
            });

            // Slot-name caption at the bottom edge
            string capLabel = slot.ToString().ToUpper();
            var cap = MakeText(go.transform, "Cap", capLabel, 26, FontStyles.Bold, CREAM);
            try { cap.outlineWidth = 0.22f; cap.outlineColor = new Color(0, 0, 0, 0.95f); } catch {}
            var cRT = cap.rectTransform;
            cRT.anchorMin = new Vector2(0, 0); cRT.anchorMax = new Vector2(1, 0);
            cRT.pivot = new Vector2(0.5f, 0);
            cRT.anchoredPosition = new Vector2(0, 6);
            cRT.sizeDelta = new Vector2(0, 32);
            cap.alignment = TextAlignmentOptions.Center;

            if (eq == null)
            {
                // Empty-state "+" hint, hints to player they can fill this.
                var plus = MakeText(go.transform, "Plus", "+", 80, FontStyles.Bold,
                    new Color(1f, 1f, 1f, 0.45f));
                var prt = plus.rectTransform;
                prt.anchorMin = new Vector2(0, 0); prt.anchorMax = new Vector2(1, 1);
                prt.offsetMin = new Vector2(0, 30); prt.offsetMax = new Vector2(0, -8);
                plus.alignment = TextAlignmentOptions.Center;
                return;
            }

            // Equipped — slot icon tinted by item colour, plus a bonus chip.
            var sw = NewGO("Swatch", go.transform, typeof(Image));
            var swRT = sw.GetComponent<RectTransform>();
            swRT.anchorMin = new Vector2(0.5f, 0.5f); swRT.anchorMax = new Vector2(0.5f, 0.5f);
            swRT.pivot = new Vector2(0.5f, 0.5f);
            swRT.anchoredPosition = new Vector2(0, 12);
            swRT.sizeDelta = new Vector2(112, 112);
            var swImg = sw.GetComponent<Image>();
            var slotIcon = LoadLayerLabSprite(SlotIconPath(slot));
            if (slotIcon != null)
            {
                swImg.sprite = slotIcon;
                swImg.preserveAspect = true;
                swImg.color = eq.tint;
            }
            else swImg.color = eq.tint;
            swImg.raycastTarget = false;

            // Mini stat chip top-right ("A 5  H 12") — at-a-glance bonus
            string bonusStr = StatsAbbrev(eq);
            if (!string.IsNullOrEmpty(bonusStr))
            {
                var pip = NewGO("Bonus", go.transform, typeof(Image));
                var pRT = pip.GetComponent<RectTransform>();
                pRT.anchorMin = new Vector2(0, 1); pRT.anchorMax = new Vector2(1, 1);
                pRT.pivot = new Vector2(0.5f, 1);
                pRT.anchoredPosition = new Vector2(0, -6);
                pRT.sizeDelta = new Vector2(-10, 40);
                pip.GetComponent<Image>().color = new Color(0.06f, 0.05f, 0.14f, 0.92f);
                pip.GetComponent<Image>().raycastTarget = false;
                var pTxt = MakeText(pip.transform, "B", bonusStr, 24, FontStyles.Bold, GOLD);
                try { pTxt.outlineWidth = 0.20f; pTxt.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
                Stretch(pTxt.rectTransform); pTxt.alignment = TextAlignmentOptions.Center;
            }
        }

        // Compact "A 5  D 2  H 12" string from an item, omitting zero stats.
        private static string StatsAbbrev(Sparq.Systems.PetService.Item it)
        {
            if (it == null) return "";
            var parts = new List<string>(3);
            if (it.atk != 0) parts.Add($"A {it.atk}");
            if (it.def != 0) parts.Add($"D {it.def}");
            if (it.hp  != 0) parts.Add($"H {it.hp}");
            return string.Join("  ", parts);
        }

        // Map total stat budget → rarity-frame tier (0..3 = Gray/Blue/Purple/Gold).
        // Items have no real rarity, so we derive a visual tier from atk+def+hp.
        private static int ItemTier(Sparq.Systems.PetService.Item it)
        {
            if (it == null) return 0;
            int total = it.atk + it.def + it.hp;
            if (total >= 23) return 3;
            if (total >= 16) return 2;
            if (total >= 9)  return 1;
            return 0;
        }

        // ── Stats row (Atk / Def / HP chips with sword/shield/heart icons)
        private static void BuildPetStatsRow(Transform body, Sparq.Systems.PetService.Pet pet,
            Vector2 anchoredPos)
        {
            var stats = (atk: 0, def: 0, hp: 0);
            try { stats = Sparq.Systems.PetService.StatsOf(pet); } catch {}

            var row = NewGO("StatsRow", body, typeof(Image), typeof(HorizontalLayoutGroup));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(860, 110);
            // Beveled sliced frame instead of a flat rectangle so the stats
            // strip reads as an intentional panel.
            var rowImg = row.GetComponent<Image>();
            var rowSp = LoadLayerLabSprite(BAR_BG_SPRITE);
            if (rowSp != null) { rowImg.sprite = rowSp; rowImg.type = Image.Type.Sliced; }
            rowImg.color = new Color(0.16f, 0.16f, 0.21f, 1f);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.padding = new RectOffset(24, 24, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            BuildPetStatChip(row.transform, "Atk", stats.atk, GEAR_ATK_ICON, new Color(0.95f, 0.55f, 0.30f, 1f));
            BuildPetStatChip(row.transform, "Def", stats.def, GEAR_DEF_ICON, new Color(0.55f, 0.85f, 1.00f, 1f));
            BuildPetStatChip(row.transform, "HP",  stats.hp,  GEAR_HP_ICON,  new Color(0.95f, 0.40f, 0.45f, 1f));
        }

        private static void BuildPetStatChip(Transform parent, string label, int value,
            string iconPath, Color iconTint)
        {
            var go = NewGO("Stat_" + label, parent, typeof(Image));
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0);   // hit-area only; layout group sizes it

            var ico = NewGO("Icon", go.transform, typeof(Image));
            var iRT = ico.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0, 0.5f); iRT.anchorMax = new Vector2(0, 0.5f);
            iRT.pivot = new Vector2(0, 0.5f);
            iRT.anchoredPosition = new Vector2(10, 0);
            iRT.sizeDelta = new Vector2(60, 60);
            var iImg = ico.GetComponent<Image>();
            var sp = LoadLayerLabSprite(iconPath);
            if (sp != null) { iImg.sprite = sp; iImg.preserveAspect = true; }
            iImg.color = iconTint;
            iImg.raycastTarget = false;

            var lbl = MakeText(go.transform, "L", value.ToString(), 40, FontStyles.Bold, CREAM);
            try { lbl.outlineWidth = 0.22f; lbl.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var lRT = lbl.rectTransform;
            lRT.anchorMin = new Vector2(0, 0); lRT.anchorMax = new Vector2(1, 1);
            lRT.offsetMin = new Vector2(78, 0); lRT.offsetMax = new Vector2(-8, 0);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
        }

        // ── Filter strip: Hat / Body / Trinket. Selected tab is gold,
        // unselected is mid-grey with cream text — same hierarchy as the
        // hero EquipmentPanel's Weapon/Helm/Chest/Boots/Trinket tabs.
        private static void BuildPetGearFilterStrip(Transform body, Vector2 anchoredPos)
        {
            var strip = NewGO("FilterStrip", body, typeof(HorizontalLayoutGroup));
            var rt = strip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(860, 88);
            var hlg = strip.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            BuildPetFilterTab(strip.transform, "Hat",     Sparq.Systems.PetService.Slot.Hat);
            BuildPetFilterTab(strip.transform, "Body",    Sparq.Systems.PetService.Slot.Body);
            BuildPetFilterTab(strip.transform, "Trinket", Sparq.Systems.PetService.Slot.Trinket);
        }

        private static void BuildPetFilterTab(Transform parent, string label,
            Sparq.Systems.PetService.Slot slot)
        {
            var go = NewGO("Filter_" + label, parent, typeof(Image), typeof(Button));
            var img = go.GetComponent<Image>();
            bool active = (slot == _gearFilter);
            // Use the same beveled button sprite as the Care-tab action pills.
            var btnSp = LoadLayerLabSprite(BTN_CONVEX_GRAY);
            if (btnSp != null) { img.sprite = btnSp; img.type = Image.Type.Sliced; }
            // Per-slot accent: Trinket is yellow, Hat/Body keep the purple
            // (matches the Care/Gear tabs). Selected = full accent, unselected
            // = the accent dimmed toward grey so the active tab still stands out.
            Color accent = slot == Sparq.Systems.PetService.Slot.Trinket
                ? new Color(0.99f, 0.78f, 0.20f, 1f)   // yellow
                : new Color(0.55f, 0.45f, 0.95f, 1f);  // purple
            img.color = active
                ? accent
                : Color.Lerp(accent, new Color(0.40f, 0.40f, 0.46f, 1f), 0.62f);
            img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            btn.onClick.AddListener(() => {
                _gearFilter = slot;
                _gearSelectedId = null;
                BuildBody();
            });
            // White text on every tab for consistency (was dark-on-gold for the
            // selected tab, cream for the rest). Dark outline keeps it readable.
            var lbl = MakeText(go.transform, "L", label, 32, FontStyles.Bold, Color.white);
            try { lbl.outlineWidth = 0.24f; lbl.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
        }

        // ── Item grid — 4-column rarity-framed tiles. Vertical scroll so
        // a large inventory doesn't blow out the panel.
        private static void BuildPetGearGrid(Transform body, Sparq.Systems.PetService.Pet pet)
        {
            var scrollGO = NewGO("GearScroll", body, typeof(Image), typeof(ScrollRect));
            var srRT = scrollGO.GetComponent<RectTransform>();
            srRT.anchorMin = new Vector2(0, 0); srRT.anchorMax = new Vector2(1, 1);
            // Top below filter strip (-700 + 88 = -788) and bottom clears
            // the action row at the card floor (~140 from bottom).
            srRT.offsetMin = new Vector2(30, 150); srRT.offsetMax = new Vector2(-30, -800);
            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 30f;

            var vp = NewGO("VP", scrollGO.transform, typeof(Image), typeof(RectMask2D));
            Stretch(vp.GetComponent<RectTransform>());
            vp.GetComponent<Image>().color = new Color(0, 0, 0, 0);

            var content = NewGO("Content", vp.transform,
                typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            var ctRT = content.GetComponent<RectTransform>();
            ctRT.anchorMin = new Vector2(0, 1); ctRT.anchorMax = new Vector2(1, 1);
            ctRT.pivot = new Vector2(0.5f, 1);
            var glg = content.GetComponent<GridLayoutGroup>();
            // 3 columns of 270×270 — bigger tiles than the hero panel's
            // 4-col grid so the per-item name + stats actually render at a
            // readable size on a phone.
            glg.cellSize = new Vector2(270, 270);
            glg.spacing = new Vector2(16, 16);
            glg.padding = new RectOffset(12, 12, 12, 12);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 3;
            glg.childAlignment = TextAnchor.UpperCenter;
            var fit = content.GetComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vp.GetComponent<RectTransform>(); sr.content = ctRT;

            // Filter owned items to the active slot (Hat / Body / Trinket).
            List<string> owned = null;
            try { owned = Sparq.Systems.PetService.OwnedItems(); } catch { owned = new List<string>(); }
            var filtered = new List<Sparq.Systems.PetService.Item>();
            if (owned != null)
            {
                foreach (var id in owned)
                {
                    var it = Sparq.Systems.PetService.FindItem(id);
                    if (it == null) continue;
                    if (it.slot != _gearFilter) continue;
                    filtered.Add(it);
                }
            }

            if (filtered.Count == 0)
            {
                // Parent to the viewport (not the grid content) so it spans the
                // full width and centers — was being squeezed into a 270px cell
                // and wrapping awkwardly.
                var box = NewGO("EmptyBox", vp.transform, typeof(RectTransform));
                var boxRT = box.GetComponent<RectTransform>();
                boxRT.anchorMin = new Vector2(0, 1); boxRT.anchorMax = new Vector2(1, 1);
                boxRT.pivot = new Vector2(0.5f, 1);
                boxRT.anchoredPosition = new Vector2(0, -40);
                boxRT.sizeDelta = new Vector2(-40, 200);

                // Slot icon, dimmed, as a visual anchor.
                var ico = NewGO("EmptyIco", box.transform, typeof(Image));
                var iRT = ico.GetComponent<RectTransform>();
                iRT.anchorMin = new Vector2(0.5f, 1); iRT.anchorMax = new Vector2(0.5f, 1);
                iRT.pivot = new Vector2(0.5f, 1);
                iRT.anchoredPosition = new Vector2(0, 0);
                iRT.sizeDelta = new Vector2(96, 96);
                var iImg = ico.GetComponent<Image>();
                var sp = LoadLayerLabSprite(SlotIconPath(_gearFilter));
                if (sp != null) { iImg.sprite = sp; iImg.preserveAspect = true; }
                iImg.color = new Color(1f, 1f, 1f, 0.35f);
                iImg.raycastTarget = false;

                var empty = MakeText(box.transform, "Empty",
                    $"No {_gearFilter.ToString().ToLower()} gear yet.\nVisit the Shop to buy some.",
                    28, FontStyles.Bold, INK_SOFT);
                try { empty.outlineWidth = 0.18f; empty.outlineColor = new Color(0, 0, 0, 0.8f); } catch {}
                var eRT = empty.rectTransform;
                eRT.anchorMin = new Vector2(0, 1); eRT.anchorMax = new Vector2(1, 1);
                eRT.pivot = new Vector2(0.5f, 1);
                eRT.anchoredPosition = new Vector2(0, -108);
                eRT.sizeDelta = new Vector2(0, 90);
                empty.alignment = TextAlignmentOptions.Center;
                empty.textWrappingMode = TextWrappingModes.Normal;
                return;
            }

            foreach (var it in filtered)
                BuildPetGearTile(content.transform, pet, it);
        }

        // Single grid tile — rarity-framed background, swatch icon, name,
        // mini stat row, equipped-tick or up/down comparison chip.
        private static void BuildPetGearTile(Transform parent,
            Sparq.Systems.PetService.Pet pet, Sparq.Systems.PetService.Item it)
        {
            bool equipped = false;
            switch (it.slot)
            {
                case Sparq.Systems.PetService.Slot.Hat:     equipped = pet.hatId   == it.id; break;
                case Sparq.Systems.PetService.Slot.Body:    equipped = pet.bodyId  == it.id; break;
                case Sparq.Systems.PetService.Slot.Trinket: equipped = pet.trinkId == it.id; break;
            }
            bool selected = _gearSelectedId == it.id;

            var tile = NewGO("Tile_" + it.id, parent, typeof(Image), typeof(Button));
            var img = tile.GetComponent<Image>();
            int tier = ItemTier(it);
            var frame = LoadLayerLabSprite(GEAR_FRAMES_BY_TIER[tier]);
            if (frame != null)
            {
                img.sprite = frame; img.type = Image.Type.Sliced;
                img.color = selected ? GOLD : Color.white;
            }
            else img.color = new Color(0.30f, 0.30f, 0.36f, 1f);
            img.raycastTarget = true;
            var btn = tile.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            btn.onClick.AddListener(() => {
                if (equipped) return;
                try { Sparq.Systems.PetService.Equip(pet.instanceId, it.id); } catch {}
                _gearSelectedId = it.id;
            });

            // Swatch — slot icon (helmet / armor / ring) tinted by item color
            // so different items in the same slot are still visually distinct.
            // Scaled up to fill the bigger 270×270 tile.
            var sw = NewGO("Sw", tile.transform, typeof(Image));
            var swRT = sw.GetComponent<RectTransform>();
            swRT.anchorMin = new Vector2(0.5f, 1); swRT.anchorMax = new Vector2(0.5f, 1);
            swRT.pivot = new Vector2(0.5f, 1);
            swRT.anchoredPosition = new Vector2(0, -28);
            swRT.sizeDelta = new Vector2(140, 140);
            var swImg = sw.GetComponent<Image>();
            var slotIcon = LoadLayerLabSprite(SlotIconPath(it.slot));
            if (slotIcon != null)
            {
                swImg.sprite = slotIcon;
                swImg.preserveAspect = true;
                swImg.color = it.tint;
            }
            else swImg.color = it.tint;
            swImg.raycastTarget = false;

            // Item name — auto-shrinks 26–36pt so long names fit without truncation.
            var nm = MakeText(tile.transform, "N", it.name ?? it.id, 36, FontStyles.Bold, CREAM);
            try { nm.outlineWidth = 0.26f; nm.outlineColor = new Color(0, 0, 0, 0.95f); } catch {}
            var nRT = nm.rectTransform;
            nRT.anchorMin = new Vector2(0, 0); nRT.anchorMax = new Vector2(1, 0);
            nRT.pivot = new Vector2(0.5f, 0);
            nRT.anchoredPosition = new Vector2(0, 56);
            nRT.sizeDelta = new Vector2(-14, 48);
            nm.alignment = TextAlignmentOptions.Center;
            nm.enableAutoSizing = true;
            nm.fontSizeMin = 22;
            nm.fontSizeMax = 36;
            nm.textWrappingMode = TextWrappingModes.NoWrap;
            nm.overflowMode = TextOverflowModes.Ellipsis;

            // Stat line "A 5  D 2  H 12"
            var statsTxt = MakeText(tile.transform, "S",
                StatsAbbrev(it), 28, FontStyles.Bold, GOLD);
            try { statsTxt.outlineWidth = 0.24f; statsTxt.outlineColor = new Color(0, 0, 0, 0.95f); } catch {}
            var stRT = statsTxt.rectTransform;
            stRT.anchorMin = new Vector2(0, 0); stRT.anchorMax = new Vector2(1, 0);
            stRT.pivot = new Vector2(0.5f, 0);
            stRT.anchoredPosition = new Vector2(0, 16);
            stRT.sizeDelta = new Vector2(-14, 38);
            statsTxt.alignment = TextAlignmentOptions.Center;

            if (equipped)
            {
                var tick = NewGO("OK", tile.transform, typeof(Image));
                var tRT = tick.GetComponent<RectTransform>();
                tRT.anchorMin = new Vector2(1, 1); tRT.anchorMax = new Vector2(1, 1);
                tRT.pivot = new Vector2(1, 1);
                tRT.anchoredPosition = new Vector2(-10, -10);
                tRT.sizeDelta = new Vector2(72, 72);
                tick.GetComponent<Image>().color = new Color(0.40f, 0.85f, 0.55f, 1f);
                tick.GetComponent<Image>().raycastTarget = false;
                var l = MakeText(tick.transform, "L", "OK", 32, FontStyles.Bold, INK);
                Stretch(l.rectTransform); l.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                int cmp = ComparePetItem(pet, it);
                Color clr; string sym;
                if      (cmp > 0) { clr = new Color(0.40f, 0.85f, 0.55f, 1f); sym = "↑"; }
                else if (cmp < 0) { clr = new Color(0.95f, 0.40f, 0.45f, 1f); sym = "↓"; }
                else              { clr = new Color(0.55f, 0.55f, 0.60f, 1f); sym = "="; }
                var chip = NewGO("Cmp", tile.transform, typeof(Image));
                var cRT = chip.GetComponent<RectTransform>();
                cRT.anchorMin = new Vector2(1, 1); cRT.anchorMax = new Vector2(1, 1);
                cRT.pivot = new Vector2(1, 1);
                cRT.anchoredPosition = new Vector2(-10, -10);
                cRT.sizeDelta = new Vector2(76, 76);
                chip.GetComponent<Image>().color = clr;
                chip.GetComponent<Image>().raycastTarget = false;
                var l = MakeText(chip.transform, "L", sym, 50, FontStyles.Bold, INK);
                Stretch(l.rectTransform); l.alignment = TextAlignmentOptions.Center;
            }
        }

        // Returns >0 if `candidate` beats the currently-equipped item in its
        // slot, <0 if worse, 0 if equal — by total atk+def+hp.
        private static int ComparePetItem(Sparq.Systems.PetService.Pet pet,
            Sparq.Systems.PetService.Item candidate)
        {
            string equippedId = null;
            switch (candidate.slot)
            {
                case Sparq.Systems.PetService.Slot.Hat:     equippedId = pet.hatId;   break;
                case Sparq.Systems.PetService.Slot.Body:    equippedId = pet.bodyId;  break;
                case Sparq.Systems.PetService.Slot.Trinket: equippedId = pet.trinkId; break;
            }
            var eq = string.IsNullOrEmpty(equippedId)
                ? null
                : Sparq.Systems.PetService.FindItem(equippedId);
            int candTotal = candidate.atk + candidate.def + candidate.hp;
            int eqTotal   = eq != null ? (eq.atk + eq.def + eq.hp) : 0;
            return candTotal - eqTotal;
        }

        // ── Action row pinned to the card floor: Equip All / Unequip All.
        // Hero panel uses Equip All + Level Up — pet items don't level, so
        // the right pill becomes Unequip All (clears every slot in one tap).
        private static void BuildPetGearActionButtons(Transform body, Sparq.Systems.PetService.Pet pet)
        {
            var row = NewGO("GearActions", body, typeof(HorizontalLayoutGroup));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, -1500);
            rt.sizeDelta = new Vector2(860, 110);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 24;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true; hlg.childControlHeight = true;

            BuildPetGearPill(row.transform, "Equip All",
                new Color(0.25f, 0.65f, 1.00f, 1f), () => {        // bright blue
                    EquipBestForAllSlots(pet);
                    BuildBody();
                });
            BuildPetGearPill(row.transform, "Unequip All",
                new Color(1.00f, 0.45f, 0.75f, 1f), () => {        // pink
                    try { Sparq.Systems.PetService.Unequip(pet.instanceId, Sparq.Systems.PetService.Slot.Hat); } catch {}
                    try { Sparq.Systems.PetService.Unequip(pet.instanceId, Sparq.Systems.PetService.Slot.Body); } catch {}
                    try { Sparq.Systems.PetService.Unequip(pet.instanceId, Sparq.Systems.PetService.Slot.Trinket); } catch {}
                    BuildBody();
                });
        }

        private static void BuildPetGearPill(Transform parent, string label, Color bg, System.Action onClick)
        {
            var go = NewGO("Pill_" + label, parent, typeof(Image), typeof(Button));
            var img = go.GetComponent<Image>();
            var btnSp = LoadLayerLabSprite(BTN_CONVEX_GRAY);
            if (btnSp != null) { img.sprite = btnSp; img.type = Image.Type.Sliced; }
            img.color = bg; img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = true;
            // White label with a dark outline — consistent with the filter tabs.
            var lbl = MakeText(go.transform, "L", label, 38, FontStyles.Bold, Color.white);
            try { lbl.outlineWidth = 0.26f; lbl.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
            btn.onClick.AddListener(() => {
                try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
                onClick?.Invoke();
            });
        }

        // Walks all owned items, picks the highest-total per slot, equips them.
        private static void EquipBestForAllSlots(Sparq.Systems.PetService.Pet pet)
        {
            List<string> owned = null;
            try { owned = Sparq.Systems.PetService.OwnedItems(); } catch { owned = new List<string>(); }
            if (owned == null || owned.Count == 0) return;

            string bestHat = null, bestBody = null, bestTrinket = null;
            int bestHatTot = -1, bestBodyTot = -1, bestTrinketTot = -1;
            foreach (var id in owned)
            {
                var it = Sparq.Systems.PetService.FindItem(id);
                if (it == null) continue;
                int total = it.atk + it.def + it.hp;
                switch (it.slot)
                {
                    case Sparq.Systems.PetService.Slot.Hat:
                        if (total > bestHatTot) { bestHatTot = total; bestHat = id; }
                        break;
                    case Sparq.Systems.PetService.Slot.Body:
                        if (total > bestBodyTot) { bestBodyTot = total; bestBody = id; }
                        break;
                    case Sparq.Systems.PetService.Slot.Trinket:
                        if (total > bestTrinketTot) { bestTrinketTot = total; bestTrinket = id; }
                        break;
                }
            }
            try
            {
                if (bestHat     != null) Sparq.Systems.PetService.Equip(pet.instanceId, bestHat);
                if (bestBody    != null) Sparq.Systems.PetService.Equip(pet.instanceId, bestBody);
                if (bestTrinket != null) Sparq.Systems.PetService.Equip(pet.instanceId, bestTrinket);
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[PetCarePanel] EquipBest failed: {ex.Message}"); }
        }

        private static void BuildDeadBody(Transform body)
        {
            string petName = "Your pet";
            try { petName = Sparq.Core.SaveService.Data?.petName ?? "Your pet"; } catch {}

            var portrait = NewGO("Portrait", body, typeof(Image));
            var pRT = portrait.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.5f, 1); pRT.anchorMax = new Vector2(0.5f, 1);
            pRT.pivot = new Vector2(0.5f, 1);
            pRT.anchoredPosition = new Vector2(0, -100);
            pRT.sizeDelta = new Vector2(320, 320);
            portrait.GetComponent<Image>().color = new Color(0.25f, 0.20f, 0.20f, 1f);
            portrait.GetComponent<Image>().raycastTarget = false;
            var port = portrait.GetComponent<Image>();
            TryLoadPetSprite(port);
            port.color = new Color(0.35f, 0.35f, 0.40f, 1f);   // desaturate "passed on"

            var hdr = MakeText(body, "Hdr", petName + " has passed on…", 44, FontStyles.Bold, CREAM);
            var hRT = hdr.rectTransform;
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.offsetMin = new Vector2(40, -540); hRT.offsetMax = new Vector2(-40, -440);
            hdr.alignment = TextAlignmentOptions.Center;

            var sub = MakeText(body, "Sub",
                "Their needs went un-met for too long. Revive them, or pick a new companion from the roster.",
                30, FontStyles.Normal, INK_SOFT);
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0.5f, 1);
            sRT.offsetMin = new Vector2(60, -680); sRT.offsetMax = new Vector2(-60, -560);
            sub.alignment = TextAlignmentOptions.Center;
            sub.textWrappingMode = TextWrappingModes.Normal;

            int coins = 0; try { coins = Sparq.Core.SaveService.Data?.sparqCoins ?? 0; } catch {}
            bool canRevive = coins >= REVIVE_COST;

            BuildBigButton(body, $"REVIVE  ({REVIVE_COST} coins)", new Vector2(0, -780),
                canRevive ? new Color(0.40f, 0.85f, 0.55f, 1f) : new Color(0.55f, 0.55f, 0.60f, 1f),
                INK,
                canRevive ? (System.Action)(() => Sparq.Systems.PetService.Revive(REVIVE_COST)) : null);

            BuildBigButton(body, "Pick a New Pet", new Vector2(0, -940),
                new Color(0.30f, 0.30f, 0.35f, 1f), CREAM, () => {
                    Hide();
                    try { Sparq.UI.PetPanel.Show(); } catch {}
                });
        }

        // ── Need row — label + StatusBar-frame bar + beveled action pill
        private static void BuildNeedRow(Transform parent, Sparq.Systems.PetService.Need n,
            string label, string action, Color accent, string iconPath, float anchoredY)
        {
            int val = Sparq.Systems.PetService.NeedLevel(n);
            Color barColor = val >= 60 ? BAR_OK : val >= 30 ? BAR_WARN : BAR_DANGER;
            // % colour mirrors the bar tier — green/yellow/orange, but NEVER
            // red (that's reserved for the death warning) so a healthy meter
            // doesn't look alarming.
            Color pctColor = val >= 60 ? new Color(0.55f, 1.00f, 0.65f, 1f)
                            : val >= 30 ? new Color(1.00f, 0.90f, 0.40f, 1f)
                                        : new Color(1.00f, 0.65f, 0.30f, 1f);

            // Row label — "Hungry" on the left, big bold cream.
            var lbl = MakeText(parent, "Lbl_" + n, label, 34, FontStyles.Bold, CREAM);
            try { lbl.outlineWidth = 0.18f; lbl.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}
            var lRT = lbl.rectTransform;
            lRT.anchorMin = new Vector2(0, 1); lRT.anchorMax = new Vector2(1, 1);
            lRT.pivot = new Vector2(0, 1);
            lRT.offsetMin = new Vector2(48, anchoredY); lRT.offsetMax = new Vector2(-48, anchoredY + 40);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;

            // Percent text — placed on the SAME line as the label, right-aligned
            // above the bar so it never fights with the bar fill for legibility.
            // Big (38pt), tier-tinted, with a heavy outline so it reads on the
            // dark card without a background badge.
            var pctTxt = MakeText(parent, "PctTop_" + n, val + "%", 38, FontStyles.Bold, pctColor);
            try { pctTxt.outlineWidth = 0.28f; pctTxt.outlineColor = new Color(0, 0, 0, 0.95f); } catch {}
            var pRT2 = pctTxt.rectTransform;
            pRT2.anchorMin = new Vector2(0, 1); pRT2.anchorMax = new Vector2(1, 1);
            pRT2.pivot = new Vector2(1, 1);
            // Sit just left of the action pill so the bar has a clear horizontal lane.
            pRT2.offsetMin = new Vector2(48, anchoredY); pRT2.offsetMax = new Vector2(-280, anchoredY + 44);
            pctTxt.alignment = TextAlignmentOptions.MidlineRight;

            // Bar — sliced Layer Lab StatusBar sprite for a proper framed look.
            var bg = NewGO("Bg_" + n, parent, typeof(Image));
            var bgRT = bg.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 1); bgRT.anchorMax = new Vector2(1, 1);
            bgRT.pivot = new Vector2(0.5f, 1);
            bgRT.offsetMin = new Vector2(48, anchoredY - 90); bgRT.offsetMax = new Vector2(-280, anchoredY - 40);
            var bgImg = bg.GetComponent<Image>();
            var barSp = LoadLayerLabSprite(BAR_BG_SPRITE);
            if (barSp != null) { bgImg.sprite = barSp; bgImg.type = Image.Type.Sliced; bgImg.color = BAR_BG; }
            else bgImg.color = BAR_BG;
            bgImg.raycastTarget = false;

            var fg = NewGO("Fg_" + n, bg.transform, typeof(Image));
            var fgRT = fg.GetComponent<RectTransform>();
            fgRT.anchorMin = new Vector2(0, 0); fgRT.anchorMax = new Vector2(1, 1);
            fgRT.offsetMin = new Vector2(6, 6); fgRT.offsetMax = new Vector2(-6, -6);
            var fgImg = fg.GetComponent<Image>();
            if (barSp != null) { fgImg.sprite = barSp; fgImg.type = Image.Type.Sliced; }
            fgImg.color = barColor;
            fgImg.type = Image.Type.Filled;
            fgImg.fillMethod = Image.FillMethod.Horizontal;
            fgImg.fillAmount = Mathf.Clamp01(val / 100f);
            fgImg.raycastTarget = false;
            // (Percent text now lives in the row header above the bar — see pctTxt.)

            // Action pill (right side) — beveled Layer Lab Button_Convex
            // sprite tinted to the action's accent color, with an icon
            // inside next to the action text.
            var pill = NewGO("Pill_" + n, parent, typeof(Image), typeof(Button));
            var pRT = pill.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(1, 1); pRT.anchorMax = new Vector2(1, 1);
            pRT.pivot = new Vector2(1, 1);
            pRT.anchoredPosition = new Vector2(-40, anchoredY - 20);
            pRT.sizeDelta = new Vector2(220, 120);
            var img = pill.GetComponent<Image>();
            var btnSp = LoadLayerLabSprite(BTN_CONVEX_GRAY);
            if (btnSp != null) { img.sprite = btnSp; img.type = Image.Type.Sliced; img.color = accent; }
            else img.color = accent;
            img.raycastTarget = true;
            var btn = pill.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = val < 100;

            // Icon inside the pill (skip if no sprite path provided).
            if (!string.IsNullOrEmpty(iconPath))
            {
                var ico = NewGO("Ico", pill.transform, typeof(Image));
                var iRT = ico.GetComponent<RectTransform>();
                iRT.anchorMin = new Vector2(0, 0.5f); iRT.anchorMax = new Vector2(0, 0.5f);
                iRT.pivot = new Vector2(0, 0.5f);
                iRT.anchoredPosition = new Vector2(12, 0);
                iRT.sizeDelta = new Vector2(64, 64);
                var iImg = ico.GetComponent<Image>();
                var sp = LoadLayerLabSprite(iconPath);
                if (sp != null) { iImg.sprite = sp; iImg.preserveAspect = true; iImg.color = Color.white; }
                iImg.raycastTarget = false;
            }

            // Cream label with a heavy dark stroke — pops on every accent
            // (orange/blue/white/pink) where dark INK text was washing out.
            var bl = MakeText(pill.transform, "L", action, 38, FontStyles.Bold, CREAM);
            try { bl.outlineWidth = 0.32f; bl.outlineColor = new Color(0.05f, 0.05f, 0.08f, 1f); } catch {}
            var blRT = bl.rectTransform;
            blRT.anchorMin = new Vector2(string.IsNullOrEmpty(iconPath) ? 0 : 0.35f, 0);
            blRT.anchorMax = new Vector2(1, 1);
            blRT.offsetMin = new Vector2(4, 0); blRT.offsetMax = new Vector2(-12, 0);
            bl.alignment = TextAlignmentOptions.Center;
            // Auto-shrink for longer words like "BATHE" / "BRUSH" so they
            // always sit inside the pill without clipping.
            bl.enableAutoSizing = true;
            bl.fontSizeMin = 26;
            bl.fontSizeMax = 38;
            bl.textWrappingMode = TextWrappingModes.NoWrap;
            bl.overflowMode = TextOverflowModes.Ellipsis;

            btn.onClick.AddListener(() => {
                DoAction(n);
                // Floating "+N" feedback above the bar.
                int gain = n == Sparq.Systems.PetService.Need.Food    ? 30
                         : n == Sparq.Systems.PetService.Need.Hygiene ? 35
                         : n == Sparq.Systems.PetService.Need.Dental  ? 40
                                                                       : 30;
                Color floatColor = n == Sparq.Systems.PetService.Need.Social
                                   ? new Color(1f, 0.5f, 0.65f) : accent;
                FloatText(parent, "+" + gain, floatColor,
                    new Vector2(0, anchoredY - 40));
            });
        }

        private static void DoAction(Sparq.Systems.PetService.Need n)
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}
            try
            {
                switch (n)
                {
                    case Sparq.Systems.PetService.Need.Food:
                        // Auto-feed cheapest food the player owns; if none,
                        // route to roster/shop.
                        string feedId = PickCheapestOwnedFood();
                        if (!string.IsNullOrEmpty(feedId)) Sparq.Systems.PetService.Feed(feedId);
                        else { Hide(); Sparq.UI.PetPanel.Show(); }
                        break;
                    case Sparq.Systems.PetService.Need.Hygiene: Sparq.Systems.PetService.Bathe();     break;
                    case Sparq.Systems.PetService.Need.Dental:  Sparq.Systems.PetService.Brush();     break;
                    case Sparq.Systems.PetService.Need.Social:  Sparq.Systems.PetService.Socialize(); break;
                }
            }
            catch (System.Exception ex)
            { Debug.LogError($"[PetCarePanel] Care action failed: {ex.Message}"); }
        }

        private static string PickCheapestOwnedFood()
        {
            try
            {
                var counts = Sparq.Systems.PetService.FoodCounts();
                Sparq.Systems.PetService.Food best = null;
                foreach (var f in Sparq.Systems.PetService.FOODS)
                {
                    if (f == null) continue;
                    if (!counts.TryGetValue(f.id, out int n) || n <= 0) continue;
                    if (best == null || f.cost < best.cost) best = f;
                }
                return best?.id;
            }
            catch { return null; }
        }

        private static void TryLoadPetSprite(Image img)
        {
            try
            {
                var active = Sparq.Systems.PetService.Active();
                if (active == null) return;
                var spec = Sparq.Systems.PetService.FindSpecies(active.speciesId);
                if (spec == null || string.IsNullOrEmpty(spec.spritePath)) return;

                // Alpha-crop the source PNG so the pet fills its container
                // instead of getting drowned by transparent padding.
                Sprite sp = null;
                try { sp = Sparq.UI.HeroPortrait.LoadCropped(spec.spritePath); } catch {}
                if (sp == null) sp = LoadLayerLabSprite(spec.spritePath);   // fallback
                if (sp != null)
                {
                    img.sprite = sp;
                    img.preserveAspect = true;
                    img.color = Color.white;
                }
            }
            catch {}
        }

        // ─────────────────────────────────────────────────────────────────
        // PRIMITIVES
        // ─────────────────────────────────────────────────────────────────

        private static void BuildBigButton(Transform parent, string label, Vector2 anchoredPos,
            Color bg, Color textColor, System.Action onClick)
        {
            var go = NewGO("Btn_" + label, parent, typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(640, 120);
            var img = go.GetComponent<Image>();
            img.color = bg; img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img; btn.interactable = onClick != null;
            var lbl = MakeText(go.transform, "L", label, 32, FontStyles.Bold, textColor);
            Stretch(lbl.rectTransform); lbl.alignment = TextAlignmentOptions.Center;
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());
        }

        // ─────────────────────────────────────────────────────────────────
        // ANIMATION — idle bob + happy hop + floating action feedback
        // ─────────────────────────────────────────────────────────────────

        private static void EnsureRunner()
        {
            if (_runner != null) return;
            var go = new GameObject("PetCarePanel_Runner");
            if (_root != null) go.transform.SetParent(_root.transform, false);
            _runner = go.AddComponent<CareRunner>();
        }

        private static System.Collections.IEnumerator IdleBobCoroutine(RectTransform rt, Vector2 basePos)
        {
            const float amplitude = 10f;
            const float speed = 1.4f;
            float t = 0f;
            while (rt != null)
            {
                t += Time.unscaledDeltaTime * speed;
                rt.anchoredPosition = basePos + new Vector2(0, Mathf.Sin(t) * amplitude);
                yield return null;
            }
        }

        private static System.Collections.IEnumerator PetHopCoroutine(RectTransform rt)
        {
            if (rt == null) yield break;
            Vector2 start = rt.anchoredPosition;
            float duration = 0.30f;
            float t = 0f;
            while (t < duration && rt != null)
            {
                t += Time.unscaledDeltaTime;
                float h = Mathf.Sin((t / duration) * Mathf.PI) * 40f;
                rt.anchoredPosition = start + new Vector2(0, h);
                yield return null;
            }
            // Small social-meter bump as feedback for petting (clamped to 100).
            try
            {
                var d = Sparq.Core.SaveService.Data;
                if (d != null && d.petAlive)
                {
                    d.petSocial = Mathf.Min(100, d.petSocial + 3);
                    Sparq.Core.SaveService.Save();
                }
            }
            catch {}
        }

        // Small floating text that drifts up and fades — used for action
        // confirmation ("+35 ❤") and for the heart on pet-tap.
        private static void FloatText(Transform parent, string text, Color color, Vector2 startAnchoredPos)
        {
            EnsureRunner();
            if (_runner == null || parent == null) return;
            var go = new GameObject("Float", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = startAnchoredPos;
            rt.sizeDelta = new Vector2(160, 80);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text; tm.fontSize = 56; tm.fontStyle = FontStyles.Bold; tm.color = color;
            tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            try { tm.outlineWidth = 0.22f; tm.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}
            _runner.StartCoroutine(FloatTextCoroutine(rt, tm));
        }

        private static System.Collections.IEnumerator FloatTextCoroutine(RectTransform rt, TMP_Text tm)
        {
            float duration = 1.2f; float t = 0f;
            Vector2 start = rt.anchoredPosition;
            while (t < duration && rt != null && tm != null)
            {
                t += Time.unscaledDeltaTime;
                float p = t / duration;
                rt.anchoredPosition = start + new Vector2(0, p * 120f);
                var c = tm.color; c.a = 1f - p; tm.color = c;
                yield return null;
            }
            if (rt != null) UnityEngine.Object.Destroy(rt.gameObject);
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

        private static Sprite LoadLayerLabSprite(string path)
        {
#if UNITY_EDITOR
            // Editor-only: fix the sprite importer once so editor preview matches
            // runtime. Doesn't affect Player builds.
            try
            {
                var imp = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                if (imp != null && imp.textureType != UnityEditor.TextureImporterType.Sprite)
                { imp.textureType = UnityEditor.TextureImporterType.Sprite; imp.SaveAndReimport(); }
            }
            catch {}
#endif
            // Runs in both Editor + Player builds via SpriteLoader's Resources-first
            // path. Previously this whole method was #if UNITY_EDITOR which made all
            // PetCare/Equipment buttons render as white placeholder squares in APK.
            return Sparq.Core.SpriteLoader.Load(path);
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
