// AllyRecruitPopup.cs — "Ally Recruited!" celebration.
//
// Fires from WorldExplorePanel.GrantBossReward when a new ally was unlocked
// (paired with the signature drop). Shows the ally's portrait + stats + a
// CTA to equip them right now.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class AllyRecruitPopup
    {
        private static readonly Color CARD_BG   = new Color(0.17f, 0.17f, 0.21f, 1f);
        private static readonly Color CREAM     = new Color(1f, 0.97f, 0.85f, 1f);
        private static readonly Color INK_SOFT  = new Color(0.80f, 0.82f, 0.90f, 1f);
        private static readonly Color GOLD      = new Color(0.99f, 0.80f, 0.30f, 1f);
        private static readonly Color BTN_GREEN = new Color(0.32f, 0.74f, 0.42f, 1f);
        private static readonly Color BTN_GRAY  = new Color(0.34f, 0.36f, 0.44f, 1f);

        private static GameObject _root;

        public static void Show(string allyId)
        {
            if (_root != null) return;
            var ally = Sparq.Systems.AllyRoster.ById(allyId);
            if (ally == null) return;
            EnsureEventSystem();

            int lvl = Sparq.Core.SaveService.Data?.level ?? 1;
            int hp  = ally.baseHp + lvl * ally.hpPerLevel;
            int atk = ally.baseAtk + lvl * ally.atkPerLevel;
            int def = ally.baseDef + lvl * ally.defPerLevel;
            Sprite portrait = LoadPortrait(ally.idleBase);

            _root = new GameObject("Sparq_AllyRecruitPopup",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Stretch(_root.GetComponent<RectTransform>());
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 16500;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort) maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 20;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            var dim = NewGO("Dim", _root.transform, typeof(Image));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.07f, 0.92f);

            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(820, 1080);
            card.GetComponent<Image>().color = CARD_BG;

            // Gold accent rule top
            var accent = NewGO("Accent", card.transform, typeof(Image));
            var aRT = accent.GetComponent<RectTransform>();
            aRT.anchorMin = new Vector2(0, 1); aRT.anchorMax = new Vector2(1, 1);
            aRT.pivot = new Vector2(0.5f, 1); aRT.anchoredPosition = Vector2.zero; aRT.sizeDelta = new Vector2(0, 6);
            accent.GetComponent<Image>().color = GOLD;
            accent.GetComponent<Image>().raycastTarget = false;

            // Title
            var title = MakeText(card.transform, "Title", "ALLY RECRUITED!", 50, FontStyles.Bold, GOLD);
            try { title.outlineWidth = 0.25f; title.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var tRT = title.rectTransform;
            tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.offsetMin = new Vector2(20, -110); tRT.offsetMax = new Vector2(-20, -34);
            title.alignment = TextAlignmentOptions.Center;

            // Portrait
            var port = NewGO("Portrait", card.transform, typeof(Image));
            var pRT = port.GetComponent<RectTransform>();
            pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
            pRT.pivot = new Vector2(0.5f, 0.5f);
            pRT.anchoredPosition = new Vector2(0, 130);
            pRT.sizeDelta = new Vector2(360, 360);
            var pImg = port.GetComponent<Image>();
            if (portrait != null) { pImg.sprite = portrait; pImg.preserveAspect = true; }
            else pImg.color = new Color(0.5f, 0.7f, 1f);
            pImg.raycastTarget = false;

            // Name + blurb
            var nameTxt = MakeText(card.transform, "Name", ally.name, 44, FontStyles.Bold, CREAM);
            try { nameTxt.outlineWidth = 0.22f; nameTxt.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var nRT = nameTxt.rectTransform;
            nRT.anchorMin = new Vector2(0, 1); nRT.anchorMax = new Vector2(1, 1);
            nRT.pivot = new Vector2(0.5f, 1);
            nRT.offsetMin = new Vector2(40, -680); nRT.offsetMax = new Vector2(-40, -610);
            nameTxt.alignment = TextAlignmentOptions.Center;

            var blurb = MakeText(card.transform, "Blurb", ally.blurb, 26, FontStyles.Normal, INK_SOFT);
            var bRT = blurb.rectTransform;
            bRT.anchorMin = new Vector2(0, 1); bRT.anchorMax = new Vector2(1, 1);
            bRT.pivot = new Vector2(0.5f, 1);
            bRT.offsetMin = new Vector2(56, -750); bRT.offsetMax = new Vector2(-56, -690);
            blurb.alignment = TextAlignmentOptions.Center;
            blurb.textWrappingMode = TextWrappingModes.Normal;

            // Stats line
            var stats = MakeText(card.transform, "Stats",
                $"<color=#FF8888>HP {hp}</color>   <color=#FFCB66>ATK {atk}</color>   <color=#88CCFF>DEF {def}</color>",
                28, FontStyles.Bold, CREAM);
            stats.richText = true;
            try { stats.outlineWidth = 0.20f; stats.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var stRT = stats.rectTransform;
            stRT.anchorMin = new Vector2(0, 1); stRT.anchorMax = new Vector2(1, 1);
            stRT.pivot = new Vector2(0.5f, 1);
            stRT.offsetMin = new Vector2(40, -830); stRT.offsetMax = new Vector2(-40, -770);
            stats.alignment = TextAlignmentOptions.Center;

            // Equip button (sets active + closes)
            var equip = NewGO("Equip", card.transform, typeof(Image), typeof(Button));
            var eRT = equip.GetComponent<RectTransform>();
            eRT.anchorMin = new Vector2(0.5f, 0); eRT.anchorMax = new Vector2(0.5f, 0);
            eRT.pivot = new Vector2(0.5f, 0);
            eRT.anchoredPosition = new Vector2(0, 160);
            eRT.sizeDelta = new Vector2(520, 120);
            equip.GetComponent<Image>().color = BTN_GREEN;
            var eBtn = equip.GetComponent<Button>(); eBtn.targetGraphic = equip.GetComponent<Image>();
            eBtn.onClick.AddListener(() =>
            {
                try { Sparq.Systems.AllyRoster.SetActive(ally.id); } catch {}
                Hide();
            });
            var eLbl = MakeText(equip.transform, "L", "Equip Now", 38, FontStyles.Bold, Color.white);
            try { eLbl.outlineWidth = 0.22f; eLbl.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}
            Stretch(eLbl.rectTransform); eLbl.alignment = TextAlignmentOptions.Center;

            // Maybe Later (just closes)
            var later = NewGO("Later", card.transform, typeof(Image), typeof(Button));
            var lRT = later.GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0.5f, 0); lRT.anchorMax = new Vector2(0.5f, 0);
            lRT.pivot = new Vector2(0.5f, 0);
            lRT.anchoredPosition = new Vector2(0, 36);
            lRT.sizeDelta = new Vector2(520, 110);
            later.GetComponent<Image>().color = BTN_GRAY;
            var lBtn = later.GetComponent<Button>(); lBtn.targetGraphic = later.GetComponent<Image>();
            lBtn.onClick.AddListener(Hide);
            var lLbl = MakeText(later.transform, "L", "Maybe Later", 32, FontStyles.Bold, Color.white);
            Stretch(lLbl.rectTransform); lLbl.alignment = TextAlignmentOptions.Center;

            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Victory); } catch {}
            Debug.Log($"[AllyRecruit] {ally.name} recruited.");
        }

        public static void Hide()
        {
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
        }

        // Load the first idle frame as a tight-cropped portrait sprite.
        private static Sprite LoadPortrait(string idleBase)
        {
#if UNITY_EDITOR
            try { return Sparq.UI.HeroPortrait.LoadCropped(idleBase + "000.png"); }
            catch { return null; }
#else
            return null;
#endif
        }

        // ── primitives ──
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

        private static TMP_Text MakeText(Transform parent, string name, string text, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text; tm.fontSize = size; tm.fontStyle = style; tm.color = color;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
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
