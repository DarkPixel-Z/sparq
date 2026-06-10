// DailyBonusPopup.cs — procedural "Daily Reward" claim dialog.
//
// The retention hook: open the app each day, claim an escalating reward, keep
// the 7-day cycle going. Built procedurally (matching AfkRewardPopup) so it
// never depends on an unassigned prefab — it always shows and looks consistent.
//
// Driven by DailyBonusManager (date logic + reward grant). Show via:
//   DailyBonusPopup.Show(dayToShow, onClaim)

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class DailyBonusPopup
    {
        private static readonly Color CARD_BG   = new Color(0.17f, 0.17f, 0.21f, 1f);
        private static readonly Color CREAM     = new Color(1f, 0.97f, 0.85f, 1f);
        private static readonly Color INK       = new Color(0.11f, 0.13f, 0.16f, 1f);
        private static readonly Color INK_SOFT  = new Color(0.78f, 0.80f, 0.88f, 1f);
        private static readonly Color GOLD      = new Color(0.99f, 0.80f, 0.30f, 1f);
        private static readonly Color XP_BLUE   = new Color(0.45f, 0.78f, 1.00f, 1f);
        private static readonly Color BUY_GREEN = new Color(0.32f, 0.74f, 0.42f, 1f);
        private static readonly Color TILE_BG   = new Color(0.10f, 0.10f, 0.13f, 0.95f);
        private static readonly Color PIP_OFF   = new Color(0.30f, 0.32f, 0.40f, 1f);

        private const string DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/128/";
        private const string ICON_CHEST = DIR + "ItemIcon_Chest_Gold.png";
        // Coin/star live in the Casual icon pack (the ItemIcons folder is chests only).
        private const string ICON_COIN  = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Coin01_Gold.png";
        private const string ICON_STAR  = "Assets/Layer Lab/2D Icons-CasualIconPack/Icons/128/Icon_Resources_Star01_Gold.png";
        private const string ICON_EGG   = "Assets/2D Fantasy Monster Sprite Pack/Monsters/Egg/Mystery-Egg.png";
        private const string BTN_CONVEX = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Convex_Rectangle_01_Gray.png";

        private static GameObject _root;
        private static System.Action<int> _onClaim;
        private static int _day;

        public static void Show(int dayToShow, System.Action<int> onClaim)
        {
            if (_root != null) return;
            EnsureEventSystem();
            _onClaim = onClaim;
            _day = Mathf.Clamp(dayToShow, 1, 7);

            int coins = DailyCoins(_day);
            int xp    = DailyXp(_day);
            bool isDay7 = _day == 7;

            _root = new GameObject("Sparq_DailyBonusPopup",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Stretch(_root.GetComponent<RectTransform>());
            var canv = _root.GetComponent<Canvas>();
            canv.renderMode = RenderMode.ScreenSpaceOverlay;
            int maxSort = 16000;
            foreach (var other in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (other != null && other.gameObject != _root && other.sortingOrder > maxSort) maxSort = other.sortingOrder;
            canv.sortingOrder = maxSort + 20;
            var cs = _root.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.matchWidthOrHeight = 0.5f;

            var dim = NewGO("Dim", _root.transform, typeof(Image));
            Stretch(dim.GetComponent<RectTransform>());
            dim.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.09f, 0.88f);

            // Card
            var card = NewGO("Card", _root.transform, typeof(Image));
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f); crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(840, 1020);
            var cardImg = card.GetComponent<Image>();
            var convex = LoadSprite(BTN_CONVEX);
            if (convex != null) { cardImg.sprite = convex; cardImg.type = Image.Type.Sliced; }
            cardImg.color = CARD_BG;

            // Chest icon
            var ico = NewGO("Chest", card.transform, typeof(Image));
            var iRT = ico.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0.5f, 1); iRT.anchorMax = new Vector2(0.5f, 1);
            iRT.pivot = new Vector2(0.5f, 1);
            iRT.anchoredPosition = new Vector2(0, -44);
            iRT.sizeDelta = new Vector2(200, 200);
            var iImg = ico.GetComponent<Image>();
            var chestSp = LoadSprite(ICON_CHEST);
            if (chestSp != null) { iImg.sprite = chestSp; iImg.preserveAspect = true; }
            else iImg.color = GOLD;
            iImg.raycastTarget = false;

            // Title
            var title = MakeText(card.transform, "Title", "DAILY REWARD", 52, FontStyles.Bold, GOLD);
            try { title.outlineWidth = 0.22f; title.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var tRT = title.rectTransform;
            tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.offsetMin = new Vector2(20, -330); tRT.offsetMax = new Vector2(-20, -262);
            title.alignment = TextAlignmentOptions.Center;

            // Sub-line: day of streak
            var sub = MakeText(card.transform, "Sub", $"Day <color=#FFCB4D>{_day}</color> of 7", 32, FontStyles.Normal, INK_SOFT);
            sub.richText = true;
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0.5f, 1);
            sRT.offsetMin = new Vector2(30, -392); sRT.offsetMax = new Vector2(-30, -334);
            sub.alignment = TextAlignmentOptions.Center;

            // 7-day progress pips.
            BuildPips(card.transform, _day, -440);

            // Reward rows.
            BuildRewardRow(card.transform, ICON_COIN, $"+{coins:N0}", "Coins", GOLD,   -520);
            BuildRewardRow(card.transform, ICON_STAR, $"+{xp:N0}",    "XP",    XP_BLUE, -650);
            if (isDay7)
                BuildRewardRow(card.transform, ICON_EGG, "+1", "Mystery Egg", new Color(0.85f, 0.6f, 1f), -780);

            // CLAIM button.
            var claim = NewGO("Claim", card.transform, typeof(Image), typeof(Button));
            var clRT = claim.GetComponent<RectTransform>();
            clRT.anchorMin = new Vector2(0.5f, 0); clRT.anchorMax = new Vector2(0.5f, 0);
            clRT.pivot = new Vector2(0.5f, 0);
            clRT.anchoredPosition = new Vector2(0, 48);
            clRT.sizeDelta = new Vector2(520, 130);
            var clImg = claim.GetComponent<Image>();
            if (convex != null) { clImg.sprite = convex; clImg.type = Image.Type.Sliced; }
            clImg.color = BUY_GREEN;
            var clBtn = claim.GetComponent<Button>();
            clBtn.targetGraphic = clImg;
            var clLbl = MakeText(claim.transform, "L", "CLAIM", 44, FontStyles.Bold, Color.white);
            try { clLbl.outlineWidth = 0.24f; clLbl.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}
            Stretch(clLbl.rectTransform); clLbl.alignment = TextAlignmentOptions.Center;
            clBtn.onClick.AddListener(OnClaimTapped);

            Debug.Log($"[DailyBonusPopup] Shown — day {_day}, +{coins} coins +{xp} XP{(isDay7 ? " +egg" : "")}.");
        }

        private static void BuildPips(Transform card, int day, float y)
        {
            var holder = NewGO("Pips", card, typeof(RectTransform));
            var hRT = holder.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0.5f, 1); hRT.anchorMax = new Vector2(0.5f, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.anchoredPosition = new Vector2(0, y);
            hRT.sizeDelta = new Vector2(640, 40);

            float step = 88f;
            float startX = -(step * 6) / 2f;
            var circ = LoadSprite("Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Border_Circle_H67_White_Bg.png");
            for (int i = 1; i <= 7; i++)
            {
                var pip = NewGO($"Pip{i}", holder.transform, typeof(Image));
                var pRT = pip.GetComponent<RectTransform>();
                pRT.anchorMin = pRT.anchorMax = new Vector2(0.5f, 0.5f);
                pRT.pivot = new Vector2(0.5f, 0.5f);
                pRT.anchoredPosition = new Vector2(startX + (i - 1) * step, 0);
                pRT.sizeDelta = new Vector2(i == day ? 40 : 30, i == day ? 40 : 30);
                var pImg = pip.GetComponent<Image>();
                if (circ != null) pImg.sprite = circ;
                pImg.color = i < day ? GOLD : (i == day ? GOLD : PIP_OFF);
                if (i < day) { var c = pImg.color; c.a = 0.55f; pImg.color = c; }  // past days dimmed
                pImg.raycastTarget = false;
            }
        }

        private static void BuildRewardRow(Transform card, string iconPath, string amount, string label, Color amtColor, float y)
        {
            var row = NewGO("Reward_" + label, card, typeof(Image));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(640, 116);
            var img = row.GetComponent<Image>();
            var convex = LoadSprite(BTN_CONVEX);
            if (convex != null) { img.sprite = convex; img.type = Image.Type.Sliced; }
            img.color = TILE_BG;
            img.raycastTarget = false;

            var ico = NewGO("Ico", row.transform, typeof(Image));
            var iRT = ico.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0, 0.5f); iRT.anchorMax = new Vector2(0, 0.5f);
            iRT.pivot = new Vector2(0, 0.5f);
            iRT.anchoredPosition = new Vector2(24, 0);
            iRT.sizeDelta = new Vector2(80, 80);
            var iImg = ico.GetComponent<Image>();
            var sp = LoadSprite(iconPath);
            if (sp != null) { iImg.sprite = sp; iImg.preserveAspect = true; }
            else iImg.color = amtColor;
            iImg.raycastTarget = false;

            var amt = MakeText(row.transform, "Amt", amount, 44, FontStyles.Bold, amtColor);
            try { amt.outlineWidth = 0.2f; amt.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var aRT = amt.rectTransform;
            aRT.anchorMin = new Vector2(0, 0); aRT.anchorMax = new Vector2(1, 1);
            aRT.offsetMin = new Vector2(120, 0); aRT.offsetMax = new Vector2(-30, 0);
            amt.alignment = TextAlignmentOptions.MidlineLeft;

            var lbl = MakeText(row.transform, "Lbl", label, 28, FontStyles.Bold, INK_SOFT);
            var lRT = lbl.rectTransform;
            lRT.anchorMin = new Vector2(1, 0); lRT.anchorMax = new Vector2(1, 1);
            lRT.pivot = new Vector2(1, 0.5f);
            lRT.anchoredPosition = new Vector2(-28, 0);
            lRT.sizeDelta = new Vector2(220, 60);
            lbl.alignment = TextAlignmentOptions.MidlineRight;
        }

        private static void OnClaimTapped()
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin); } catch {}
            var cb = _onClaim; int d = _day;
            Hide();
            cb?.Invoke(d);
        }

        public static void Hide()
        {
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
            _onClaim = null;
        }

        // Reward tables (mirror DailyBonusManager; kept here so the popup can
        // render amounts without a hard dependency on the manager instance).
        private static int DailyCoins(int day)
        {
            var arr = Sparq.Systems.DailyBonusManager.DAILY_COINS;
            return arr[Mathf.Clamp(day - 1, 0, arr.Length - 1)];
        }
        private static int DailyXp(int day)
        {
            var arr = Sparq.Systems.DailyBonusManager.DAILY_XP;
            return arr[Mathf.Clamp(day - 1, 0, arr.Length - 1)];
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
