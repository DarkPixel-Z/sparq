// AfkRewardPopup.cs — "Welcome Back!" idle-rewards claim dialog.
//
// Shown when the explore (idle) map opens and the squad has accrued offline
// earnings (see AfkRewardService). Charcoal card matching Store/Pet/Guild,
// with a treasure-chest hero icon, the time-away duration, the coin + XP
// rewards, and a big green CLAIM button.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Sparq.UI
{
    public static class AfkRewardPopup
    {
        private static readonly Color CARD_BG  = new Color(0.17f, 0.17f, 0.21f, 1f);
        private static readonly Color CREAM     = new Color(1f, 0.97f, 0.85f, 1f);
        private static readonly Color INK       = new Color(0.11f, 0.13f, 0.16f, 1f);
        private static readonly Color INK_SOFT  = new Color(0.78f, 0.80f, 0.88f, 1f);
        private static readonly Color GOLD      = new Color(0.99f, 0.80f, 0.30f, 1f);
        private static readonly Color XP_BLUE   = new Color(0.45f, 0.78f, 1.00f, 1f);
        private static readonly Color BUY_GREEN = new Color(0.32f, 0.74f, 0.42f, 1f);
        private static readonly Color TILE_BG   = new Color(0.10f, 0.10f, 0.13f, 0.95f);

        private const string DIR = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Icon_ItemIcons/128/";
        private const string ICON_CHEST = DIR + "ItemIcon_Chest_Gold.png";
        private const string ICON_COIN  = DIR + "ItemIcon_Coin_Gold.png";
        private const string ICON_STAR  = DIR + "ItemIcon_Star.png";
        private const string BTN_CONVEX = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Button/Button_Convex_Rectangle_01_Gray.png";

        private static GameObject _root;

        public static void Show()
        {
            if (_root != null) return;
            EnsureEventSystem();

            var pending = Sparq.Systems.AfkRewardService.Pending();
            float secs = Sparq.Systems.AfkRewardService.ElapsedSeconds();

            _root = new GameObject("Sparq_AfkRewardPopup",
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
            crt.sizeDelta = new Vector2(820, 980);
            var cardImg = card.GetComponent<Image>();
            var convex = LoadSprite(BTN_CONVEX);
            if (convex != null) { cardImg.sprite = convex; cardImg.type = Image.Type.Sliced; }
            cardImg.color = CARD_BG;

            // Chest icon
            var ico = NewGO("Chest", card.transform, typeof(Image));
            var iRT = ico.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0.5f, 1); iRT.anchorMax = new Vector2(0.5f, 1);
            iRT.pivot = new Vector2(0.5f, 1);
            iRT.anchoredPosition = new Vector2(0, -50);
            iRT.sizeDelta = new Vector2(220, 220);
            var iImg = ico.GetComponent<Image>();
            var chestSp = LoadSprite(ICON_CHEST);
            if (chestSp != null) { iImg.sprite = chestSp; iImg.preserveAspect = true; }
            else iImg.color = GOLD;
            iImg.raycastTarget = false;

            // Title
            var title = MakeText(card.transform, "Title", "WELCOME BACK!", 52, FontStyles.Bold, GOLD);
            try { title.outlineWidth = 0.22f; title.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var tRT = title.rectTransform;
            tRT.anchorMin = new Vector2(0, 1); tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0.5f, 1);
            tRT.offsetMin = new Vector2(20, -360); tRT.offsetMax = new Vector2(-20, -290);
            title.alignment = TextAlignmentOptions.Center;

            // Sub-line: time away
            var sub = MakeText(card.transform, "Sub",
                $"Your squad kept fighting for <color=#FFCB4D>{Sparq.Systems.AfkRewardService.FormatDuration(secs)}</color>"
                + (pending.capped ? "  (max)" : ""),
                30, FontStyles.Normal, INK_SOFT);
            sub.richText = true;
            var sRT = sub.rectTransform;
            sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0.5f, 1);
            sRT.offsetMin = new Vector2(30, -420); sRT.offsetMax = new Vector2(-30, -362);
            sub.alignment = TextAlignmentOptions.Center;
            sub.textWrappingMode = TextWrappingModes.Normal;

            // Reward rows
            BuildRewardRow(card.transform, ICON_COIN, $"+{pending.coins:N0}", "Coins", GOLD,   -460);
            BuildRewardRow(card.transform, ICON_STAR, $"+{pending.xp:N0}",    "XP",    XP_BLUE, -610);

            // Vitality bonus note — self-care today boosted these idle earnings.
            int vitBonus = 0;
            try { vitBonus = Sparq.Systems.AfkRewardService.VitalityBonusPercent(); } catch {}
            if (vitBonus > 0)
            {
                var vitNote = MakeText(card.transform, "VitNote",
                    $"<color=#7CFC8A>Vitality +{vitBonus}% boost included</color>",
                    28, FontStyles.Bold, CREAM);
                vitNote.richText = true;
                var vRT = vitNote.rectTransform;
                vRT.anchorMin = new Vector2(0, 1); vRT.anchorMax = new Vector2(1, 1);
                vRT.pivot = new Vector2(0.5f, 1);
                vRT.offsetMin = new Vector2(30, -752); vRT.offsetMax = new Vector2(-30, -700);
                vitNote.alignment = TextAlignmentOptions.Center;
                try { vitNote.outlineWidth = 0.2f; vitNote.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}
            }

            // CLAIM button (1×) — left half of the bottom row.
            var claim = NewGO("Claim", card.transform, typeof(Image), typeof(Button));
            var clRT = claim.GetComponent<RectTransform>();
            clRT.anchorMin = new Vector2(0, 0); clRT.anchorMax = new Vector2(0, 0);
            clRT.pivot = new Vector2(0, 0);
            clRT.anchoredPosition = new Vector2(40, 50);
            clRT.sizeDelta = new Vector2(350, 130);
            var clImg = claim.GetComponent<Image>();
            if (convex != null) { clImg.sprite = convex; clImg.type = Image.Type.Sliced; }
            clImg.color = BUY_GREEN;
            var clBtn = claim.GetComponent<Button>();
            clBtn.targetGraphic = clImg;
            var clLbl = MakeText(claim.transform, "L", "CLAIM", 42, FontStyles.Bold, Color.white);
            try { clLbl.outlineWidth = 0.24f; clLbl.outlineColor = new Color(0, 0, 0, 0.85f); } catch {}
            Stretch(clLbl.rectTransform); clLbl.alignment = TextAlignmentOptions.Center;
            clBtn.onClick.AddListener(() => OnClaim(1));

            // WATCH AD ×2 button — right half. Doubles the reward.
            var x2 = NewGO("WatchAd", card.transform, typeof(Image), typeof(Button));
            var x2RT = x2.GetComponent<RectTransform>();
            x2RT.anchorMin = new Vector2(1, 0); x2RT.anchorMax = new Vector2(1, 0);
            x2RT.pivot = new Vector2(1, 0);
            x2RT.anchoredPosition = new Vector2(-40, 50);
            x2RT.sizeDelta = new Vector2(350, 130);
            var x2Img = x2.GetComponent<Image>();
            if (convex != null) { x2Img.sprite = convex; x2Img.type = Image.Type.Sliced; }
            x2Img.color = new Color(1.00f, 0.62f, 0.18f, 1f);   // orange = bonus/ad
            var x2Btn = x2.GetComponent<Button>();
            x2Btn.targetGraphic = x2Img;
            var x2Lbl = MakeText(x2.transform, "L", "WATCH AD\n×2", 30, FontStyles.Bold, INK);
            try { x2Lbl.outlineWidth = 0.18f; x2Lbl.outlineColor = new Color(1f, 0.95f, 0.8f, 0.6f); } catch {}
            Stretch(x2Lbl.rectTransform); x2Lbl.alignment = TextAlignmentOptions.Center;
            x2Btn.onClick.AddListener(OnWatchAd);

            Debug.Log($"[AfkRewardPopup] Shown — coins={pending.coins} xp={pending.xp} away={secs:F0}s");
        }

        private static void BuildRewardRow(Transform card, string iconPath, string amount, string label, Color amtColor, float y)
        {
            var row = NewGO("Reward_" + label, card, typeof(Image));
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(640, 130);
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
            iRT.sizeDelta = new Vector2(90, 90);
            var iImg = ico.GetComponent<Image>();
            var sp = LoadSprite(iconPath);
            if (sp != null) { iImg.sprite = sp; iImg.preserveAspect = true; }
            else iImg.color = amtColor;
            iImg.raycastTarget = false;

            var amt = MakeText(row.transform, "Amt", amount, 46, FontStyles.Bold, amtColor);
            try { amt.outlineWidth = 0.2f; amt.outlineColor = new Color(0, 0, 0, 0.9f); } catch {}
            var aRT = amt.rectTransform;
            aRT.anchorMin = new Vector2(0, 0); aRT.anchorMax = new Vector2(1, 1);
            aRT.offsetMin = new Vector2(130, 0); aRT.offsetMax = new Vector2(-30, 0);
            amt.alignment = TextAlignmentOptions.MidlineLeft;

            var lbl = MakeText(row.transform, "Lbl", label, 28, FontStyles.Bold, INK_SOFT);
            var lRT = lbl.rectTransform;
            lRT.anchorMin = new Vector2(1, 0); lRT.anchorMax = new Vector2(1, 1);
            lRT.pivot = new Vector2(1, 0.5f);
            lRT.anchoredPosition = new Vector2(-28, 0);
            lRT.sizeDelta = new Vector2(120, 60);
            lbl.alignment = TextAlignmentOptions.MidlineRight;
        }

        private static void OnClaim(int multiplier)
        {
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin); } catch {}
            var got = Sparq.Systems.AfkRewardService.Claim(multiplier);
            Debug.Log($"[AfkRewardPopup] Claimed ×{multiplier} coins={got.coins} xp={got.xp} levels={got.levelsGained}");
            Hide();
        }

        // Simulated rewarded ad → grant double. No ad SDK yet, so we briefly
        // disable the card and show a "playing ad…" overlay, then claim ×2.
        private static bool _adPlaying;
        private static void OnWatchAd()
        {
            if (_adPlaying || _root == null) return;
            _adPlaying = true;
            try { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); } catch {}

            // Full-card "ad" overlay so the doubling reads as an intentional reward.
            var overlay = NewGO("AdOverlay", _root.transform, typeof(Image));
            Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.05f, 0.96f);
            var msg = MakeText(overlay.transform, "M", "Playing ad…", 48, FontStyles.Bold, CREAM);
            Stretch(msg.rectTransform); msg.alignment = TextAlignmentOptions.Center;

            var runner = overlay.AddComponent<AdRunner>();
            runner.StartCoroutine(AdThenDouble(overlay));
        }

        private class AdRunner : MonoBehaviour {}

        private static System.Collections.IEnumerator AdThenDouble(GameObject overlay)
        {
            yield return new WaitForSecondsRealtime(1.2f);
            _adPlaying = false;
            OnClaim(2);   // grants ×2 then hides the whole popup (overlay included)
        }

        public static void Hide()
        {
            _adPlaying = false;
            if (_root != null) { UnityEngine.Object.Destroy(_root); _root = null; }
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
