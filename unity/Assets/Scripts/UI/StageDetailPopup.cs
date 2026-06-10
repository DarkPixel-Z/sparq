using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Stage detail confirmation popup with WOW factor:
    /// • Big animated rival portrait in the center
    /// • Tier-colored glow aura pulsing behind portrait
    /// • Floating sparkle particles
    /// • Animated scale + slide-in entry
    /// • Sound on entry + button hover
    /// • CHALLENGE / BACK buttons
    /// </summary>
    public class StageDetailPopup : MonoBehaviour
    {
        public static void Show(int rivalIndex)
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var r = Sparq.Systems.RivalRoster.ROSTER[rivalIndex];

            // Tier color
            Color tierColor = r.tier switch {
                "mini"   => new Color(0.3f, 0.85f, 0.45f),
                "fodder" => new Color(0.95f, 0.78f, 0.25f),
                "elite"  => new Color(1.0f, 0.5f, 0.2f),
                "boss"   => new Color(0.95f, 0.25f, 0.30f),
                _        => Color.gray
            };

            // Root wrapper
            var root = new GameObject("StageDetailRoot",
                typeof(RectTransform), typeof(CanvasGroup),
                typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(canvas.transform, false);
            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var oc = root.GetComponent<Canvas>();
            oc.overrideSorting = true; oc.sortingOrder = 1500;
            root.transform.SetAsLastSibling();

            // Dimmer with click-to-back
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dim.transform.SetParent(root.transform, false);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0,0,0,0.85f);
            var dimBtn = dim.GetComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(() => { Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click); Object.Destroy(root); });

            // Card centered
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(root.transform, false);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot     = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(600, 820);
            var cardImg = card.GetComponent<Image>();
            cardImg.color = new Color(0.08f, 0.04f, 0.18f, 0.97f);

            // Tier-colored top accent bar
            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(card.transform, false);
            var art = accent.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0, 1); art.anchorMax = new Vector2(1, 1);
            art.pivot = new Vector2(0.5f, 1f);
            art.anchoredPosition = Vector2.zero;
            art.sizeDelta = new Vector2(0, 8);
            accent.GetComponent<Image>().color = tierColor;

            // STAGE label
            AddText(card.transform, "▼ STAGE ▼", 22, new Color(1f, 0.9f, 0.5f), FontStyles.Bold,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.96f), new Vector2(400, 40));

            // Stage number HUGE
            AddText(card.transform, $"{rivalIndex + 1}", 100, Color.white, FontStyles.Bold,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.88f), new Vector2(300, 100));

            // Rival name + title
            AddText(card.transform, r.name, 56, Color.white, FontStyles.Bold,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.78f), new Vector2(550, 70));
            AddText(card.transform, r.title, 28, new Color(0.85f, 0.7f, 1f), FontStyles.Italic,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.73f), new Vector2(550, 40));

            // GLOW ring behind portrait (pulses)
            var glow = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glow.transform.SetParent(card.transform, false);
            var grt = glow.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0.5f, 0.5f);
            grt.anchorMax = new Vector2(0.5f, 0.5f);
            grt.pivot = new Vector2(0.5f, 0.5f);
            grt.anchoredPosition = new Vector2(0, 60);
            grt.sizeDelta = new Vector2(360, 360);
            var glowImg = glow.GetComponent<Image>();
            glowImg.color = new Color(tierColor.r, tierColor.g, tierColor.b, 0.4f);
            glowImg.raycastTarget = false;
            glow.AddComponent<PulseScale>();

            // Portrait (rival sprite)
            var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(card.transform, false);
            var prt = portrait.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.anchoredPosition = new Vector2(0, 60);
            prt.sizeDelta = new Vector2(280, 280);
            var pImg = portrait.GetComponent<Image>();
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;
            LoadRivalPortrait(pImg, r);

            // Bouncing portrait idle
            portrait.AddComponent<BobAnimation>();

            // Tier chip
            var chipRect = AddText(card.transform, $"⚔  {r.tier.ToUpper()}  ⚔", 22, tierColor, FontStyles.Bold,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.34f), new Vector2(300, 40));

            // HP & Recommended Level row
            AddText(card.transform, $"<color=#FF7474>HP</color>  {r.baseHpXP}", 30, Color.white, FontStyles.Bold,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.27f), new Vector2(500, 50));
            AddText(card.transform, $"Recommended Level <color=#FFD86E>{r.minLevel}</color>", 22,
                    new Color(0.85f, 0.85f, 0.95f), FontStyles.Normal,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.22f), new Vector2(500, 36));

            // Rewards row
            int xpReward    = 40 + rivalIndex * 20;
            int coinsReward = 150 + rivalIndex * 75;
            AddText(card.transform, "── R E W A R D S ──", 18, new Color(0.55f, 0.55f, 0.7f), FontStyles.Normal,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.165f), new Vector2(500, 24));
            AddText(card.transform, $"+{xpReward} XP    •    +{coinsReward} 💰", 26,
                    new Color(1f, 0.85f, 0.4f), FontStyles.Bold,
                    TextAlignmentOptions.Center, new Vector2(0.5f, 0.115f), new Vector2(500, 40));

            // CHALLENGE button (big colored)
            var fightBtn = AddBigButton(card.transform, "⚔  C H A L L E N G E  ⚔",
                tierColor, Color.white, new Vector2(0.5f, 0.06f), new Vector2(500, 80));
            fightBtn.onClick.AddListener(() =>
            {
                var data = Sparq.Core.SaveService.Data;
                if (data == null) return;
                data.currentRivalIndex = rivalIndex;
                data.fitchXP = data.totalXP + r.baseHpXP;
                Sparq.Core.SaveService.Save();
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Whoosh);
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Crit);
                Debug.Log($"[Map] Now challenging {r.name} ({r.title}) — HP {r.baseHpXP}");
                Object.Destroy(root);
                if (PopupManager.Instance != null) PopupManager.Instance.Dismiss();
            });

            // Add floating sparkle particles around the portrait
            SpawnSparkles(card.transform, tierColor);

            // Entry animation: scale punch
            root.AddComponent<EntryAnimator>();

            // Entry sound
            Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin, pitch: 1.4f);
        }

        private static void LoadRivalPortrait(Image img, Sparq.Systems.RivalRoster.Rival r)
        {
            #if UNITY_EDITOR
            // Animated rival → use first idle frame
            if (!string.IsNullOrEmpty(r.folderName))
            {
                string dir = $"Assets/Fantasy Monster Pack 5 Handcrafted 2D Creatures/{r.folderName}/{r.animSubfolder}";
                if (System.IO.Directory.Exists(dir))
                {
                    var files = System.IO.Directory.GetFiles(dir, "*.png");
                    System.Array.Sort(files);
                    if (files.Length > 0)
                    {
                        string ap = files[0].Replace('\\','/');
                        int idx = ap.IndexOf("Assets/");
                        if (idx >= 0) ap = ap.Substring(idx);
                        var sp = Sparq.Core.SpriteLoader.Load(ap);
                        if (sp != null) { img.sprite = sp; return; }
                    }
                }
            }
            // Static path
            if (!string.IsNullOrEmpty(r.staticSpritePath))
            {
                var sp = Sparq.Core.SpriteLoader.Load(r.staticSpritePath);
                if (sp != null) { img.sprite = sp; return; }
            }
            #endif
            img.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }

        private static void SpawnSparkles(Transform parent, Color color)
        {
            for (int i = 0; i < 12; i++)
            {
                var sp = new GameObject($"Spark_{i}", typeof(RectTransform), typeof(Image));
                sp.transform.SetParent(parent, false);
                var srt = sp.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.5f, 0.5f); srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.pivot = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = new Vector2(Random.Range(-280f, 280f), Random.Range(-50f, 200f));
                srt.sizeDelta = new Vector2(8 + Random.value * 10, 8 + Random.value * 10);
                var img = sp.GetComponent<Image>();
                img.color = new Color(color.r, color.g, color.b, 0.8f);
                img.raycastTarget = false;
                sp.AddComponent<FloatSparkle>();
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────
        private static RectTransform AddText(Transform parent, string text, int size, Color color, FontStyles style,
                                             TextAlignmentOptions align, Vector2 anchor, Vector2 size2)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size2;
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = size;
            tm.color = color;
            tm.fontStyle = style;
            tm.alignment = align;
            tm.richText = true;
            tm.raycastTarget = false;
            return rt;
        }

        private static Button AddBigButton(Transform parent, string label, Color bg, Color fg, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor; rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = bg;

            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tm = lbl.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = 32;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = fg;
            tm.raycastTarget = false;

            return go.GetComponent<Button>();
        }

        // Animation helpers (private nested classes)
        private class PulseScale : MonoBehaviour
        {
            float t;
            void Update()
            {
                t += Time.deltaTime;
                float k = (Mathf.Sin(t * 2.5f) + 1f) * 0.5f;
                transform.localScale = Vector3.one * Mathf.Lerp(0.95f, 1.1f, k);
                var img = GetComponent<Image>();
                if (img != null) { var c = img.color; c.a = Mathf.Lerp(0.25f, 0.55f, k); img.color = c; }
            }
        }

        private class BobAnimation : MonoBehaviour
        {
            Vector3 basePos; float t;
            void Awake() { basePos = transform.localPosition; t = Random.value * 5f; }
            void Update()
            {
                t += Time.deltaTime;
                transform.localPosition = basePos + new Vector3(0, Mathf.Sin(t * 2f) * 6f, 0);
            }
        }

        private class FloatSparkle : MonoBehaviour
        {
            Vector3 basePos; float t; float speed; float drift;
            void Awake() { basePos = transform.localPosition; t = Random.value * 5f; speed = Random.Range(1.5f, 3f); drift = Random.Range(-15f, 15f); }
            void Update()
            {
                t += Time.deltaTime;
                transform.localPosition = basePos + new Vector3(Mathf.Sin(t * speed) * 8f, t * 30f - 50f, 0);
                var img = GetComponent<Image>();
                if (img != null) { var c = img.color; c.a = (Mathf.Sin(t * speed * 2f) + 1f) * 0.5f * 0.8f; img.color = c; }
                if (transform.localPosition.y - basePos.y > 250f) { transform.localPosition = basePos; t = 0; }
            }
        }

        private class EntryAnimator : MonoBehaviour
        {
            float t; CanvasGroup cg; Transform card;
            void Awake() { cg = GetComponent<CanvasGroup>(); card = transform.Find("Card"); if (cg != null) cg.alpha = 0; }
            void Update()
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.4f);
                if (cg != null) cg.alpha = k;
                if (card != null)
                {
                    float s = k < 0.7f ? Mathf.Lerp(0.4f, 1.1f, k / 0.7f) : Mathf.Lerp(1.1f, 1f, (k - 0.7f) / 0.3f);
                    card.localScale = Vector3.one * s;
                }
                if (k >= 1f) { Destroy(this); }
            }
        }
    }
}
