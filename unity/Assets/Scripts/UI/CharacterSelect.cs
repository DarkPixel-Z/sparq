using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// First-launch character select. 5 fantasy starters:
    /// Bear, Cat, Owl, Wisp, Bat. User picks → activePet saved → home loads.
    /// </summary>
    public static class CharacterSelect
    {
        private struct Starter
        {
            public string id;
            public string name;
            public string title;
            public string flavor;
            public string spritePath;
            public Color cardColor;
            public int atkBonus;
            public int defBonus;
            public int spdBonus;
        }

        private const string CHIBI_DIR = "Assets/Tancha_14/Chibi Characters Pack/Sprites/";

        // 5 hand-picked chibi heroes from the 160-pack. These IDs map to specific sprites
        // and feel distinct (different hairstyles, classes).
        private static readonly Starter[] STARTERS = new Starter[]
        {
            new Starter {
                id = "kael", name = "Kael", title = "Knight of Dawn",
                flavor = "Steady. Loyal. Heavy armor.",
                spritePath = CHIBI_DIR + "Chibi character_1.png",
                cardColor = new Color(0.95f, 0.65f, 0.35f),
                atkBonus = 3, defBonus = 4, spdBonus = 0,
            },
            new Starter {
                id = "mira", name = "Mira", title = "Arcane Scholar",
                flavor = "Sharp mind. Reads spells faster than you read texts.",
                spritePath = CHIBI_DIR + "Chibi character_22.png",
                cardColor = new Color(0.65f, 0.45f, 1f),
                atkBonus = 4, defBonus = 1, spdBonus = 2,
            },
            new Starter {
                id = "rook", name = "Rook", title = "Forest Ranger",
                flavor = "Quiet step. Sharp eye. Calm presence.",
                spritePath = CHIBI_DIR + "Chibi character_45.png",
                cardColor = new Color(0.45f, 0.85f, 0.55f),
                atkBonus = 2, defBonus = 2, spdBonus = 4,
            },
            new Starter {
                id = "vex",  name = "Vex",  title = "Shadow Duelist",
                flavor = "Vanishes when bored. Reappears when amused.",
                spritePath = CHIBI_DIR + "Chibi character_77.png",
                cardColor = new Color(0.75f, 0.40f, 0.85f),
                atkBonus = 5, defBonus = 0, spdBonus = 3,
            },
            new Starter {
                id = "lyra", name = "Lyra", title = "Sun Cleric",
                flavor = "Warm. Grounded. Heals the room.",
                spritePath = CHIBI_DIR + "Chibi character_100.png",
                cardColor = new Color(1f, 0.85f, 0.40f),
                atkBonus = 1, defBonus = 4, spdBonus = 1,
            },
        };

        public static void Show()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var root = new GameObject("CharSelectRoot",
                typeof(RectTransform), typeof(CanvasGroup),
                typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(canvas.transform, false);
            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var oc = root.GetComponent<Canvas>();
            oc.overrideSorting = true; oc.sortingOrder = 2000;
            root.transform.SetAsLastSibling();

            // Solid dark backdrop
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(root.transform, false);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0.05f, 0.02f, 0.12f, 0.95f);

            // Title
            AddText(root.transform, "CHOOSE YOUR COMPANION", 36,
                new Color(1f, 0.85f, 0.4f), FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.92f), new Vector2(700, 60));
            AddText(root.transform, "Pick your starter pet — they grow with you.", 18,
                new Color(1, 1, 1, 0.75f), FontStyles.Italic,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.86f), new Vector2(700, 30));

            // Horizontal row of 5 cards
            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(root.transform, false);
            var rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0.5f, 0.5f);
            rowRT.anchorMax = new Vector2(0.5f, 0.5f);
            rowRT.pivot = new Vector2(0.5f, 0.5f);
            rowRT.anchoredPosition = new Vector2(0, 0);
            rowRT.sizeDelta = new Vector2(900, 460);
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.padding = new RectOffset(8, 8, 8, 8);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            foreach (var s in STARTERS)
            {
                BuildCard(row.transform, s, root);
            }
        }

        private static void BuildCard(Transform parent, Starter s, GameObject root)
        {
            var card = new GameObject($"Card_{s.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(parent, false);
            var le = card.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 440;

            var bg = card.GetComponent<Image>();
            bg.color = new Color(0.15f, 0.08f, 0.25f, 0.95f);

            // Tier color top stripe
            var stripe = new GameObject("Stripe", typeof(RectTransform), typeof(Image));
            stripe.transform.SetParent(card.transform, false);
            var stRT = stripe.GetComponent<RectTransform>();
            stRT.anchorMin = new Vector2(0, 1); stRT.anchorMax = new Vector2(1, 1);
            stRT.pivot = new Vector2(0.5f, 1f);
            stRT.anchoredPosition = Vector2.zero;
            stRT.sizeDelta = new Vector2(0, 8);
            stripe.GetComponent<Image>().color = s.cardColor;

            // Portrait
            var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(card.transform, false);
            var pRT = portrait.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.5f, 0.65f); pRT.anchorMax = new Vector2(0.5f, 0.65f);
            pRT.pivot = new Vector2(0.5f, 0.5f);
            pRT.anchoredPosition = Vector2.zero;
            pRT.sizeDelta = new Vector2(140, 140);
            var pImg = portrait.GetComponent<Image>();
            pImg.preserveAspect = true;
            #if UNITY_EDITOR
            if (!string.IsNullOrEmpty(s.spritePath))
            {
                var allSubs = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(s.spritePath);
                foreach (var o in allSubs)
                {
                    if (o is Sprite sp)
                    {
                        // Prefer the "Body" or main sprite
                        if (sp.name.Contains("Body") || sp.name == System.IO.Path.GetFileNameWithoutExtension(s.spritePath))
                        { pImg.sprite = sp; break; }
                    }
                }
                if (pImg.sprite == null)
                {
                    var single = Sparq.Core.SpriteLoader.Load(s.spritePath);
                    if (single != null) pImg.sprite = single;
                }
            }
            #endif
            // Tint slightly toward card color
            pImg.color = pImg.sprite != null ? Color.white : s.cardColor;

            // Name
            AddTextLayout(card.transform, s.name, 28, Color.white, FontStyles.Bold,
                TextAlignmentOptions.Center, 36, 0.45f);
            // Title
            AddTextLayout(card.transform, s.title, 16, new Color(0.9f, 0.85f, 1f, 0.9f), FontStyles.Italic,
                TextAlignmentOptions.Center, 24, 0.38f);
            // Flavor
            AddTextLayout(card.transform, s.flavor, 12, new Color(1, 1, 1, 0.7f), FontStyles.Normal,
                TextAlignmentOptions.Center, 36, 0.30f);
            // Stats line
            AddTextLayout(card.transform,
                $"<color=#FF7474>ATK</color> +{s.atkBonus}   <color=#74FF8E>DEF</color> +{s.defBonus}   <color=#74D9FF>SPD</color> +{s.spdBonus}",
                14, Color.white, FontStyles.Bold, TextAlignmentOptions.Center, 24, 0.20f);

            // Choose button
            var pickBtn = AddButtonLayout(card.transform, "CHOOSE", s.cardColor, 0.10f);
            pickBtn.onClick.AddListener(() =>
            {
                var data = Sparq.Core.SaveService.Data;
                if (data != null)
                {
                    data.activePet = s.id;
                    data.petName = s.name;
                    Sparq.Core.SaveService.Save();
                    Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.LevelUp);
                }
                Object.Destroy(root);
            });

            // Hover bounce
            card.AddComponent<CardHover>();
        }

        private static void AddText(Transform parent, string text, int size, Color color, FontStyles style,
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
        }

        // Anchor inside a single card (anchor is normalized)
        private static void AddTextLayout(Transform parent, string text, int size, Color color, FontStyles style,
                                          TextAlignmentOptions align, float height, float yAnchor)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, yAnchor);
            rt.anchorMax = new Vector2(1, yAnchor);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, height);
            rt.offsetMin = new Vector2(8, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-8, rt.offsetMax.y);
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = size;
            tm.color = color;
            tm.fontStyle = style;
            tm.alignment = align;
            tm.richText = true;
            tm.raycastTarget = false;
            tm.textWrappingMode = TextWrappingModes.Normal;
        }

        private static Button AddButtonLayout(Transform parent, string label, Color color, float yAnchor)
        {
            var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, yAnchor);
            rt.anchorMax = new Vector2(1, yAnchor);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0, 50);
            rt.offsetMin = new Vector2(12, rt.offsetMin.y);
            rt.offsetMax = new Vector2(-12, rt.offsetMax.y);
            go.GetComponent<Image>().color = color;

            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tm = lbl.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = 18;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = new Color(0.05f, 0.02f, 0.12f);
            tm.raycastTarget = false;

            return go.GetComponent<Button>();
        }

        private class CardHover : MonoBehaviour
        {
            float t; Vector3 baseScale;
            void Awake() { baseScale = transform.localScale; t = Random.value * 5f; }
            void Update()
            {
                t += Time.deltaTime;
                float s = 1f + Mathf.Sin(t * 1.6f) * 0.015f;
                transform.localScale = baseScale * s;
            }
        }
    }
}
