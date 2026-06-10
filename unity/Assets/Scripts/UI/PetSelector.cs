using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Pet selection popup. Lets the player choose between Karu (Bear) and
    /// alternate pets (Batty Buddies for now, more to come).
    /// </summary>
    public static class PetSelector
    {
        private struct PetOption
        {
            public string id;
            public string displayName;
            public string flavor;
            public Color  cardColor;
        }

        private static readonly PetOption[] PETS = new[]
        {
            new PetOption { id="bear",  displayName="Karu",   flavor="Cozy red panda. Bobs gently. Loyal to a fault.", cardColor = new Color(1f, 0.55f, 0.35f) },
            new PetOption { id="batty", displayName="Batty",  flavor="Tiny chaos bat. Coming soon.",                  cardColor = new Color(0.5f, 0.35f, 0.85f) },
            new PetOption { id="cat",   displayName="Mochi",  flavor="Sleepy stargazer. Coming soon.",                cardColor = new Color(0.7f, 0.55f, 0.85f) },
        };

        public static void Show()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var root = new GameObject("PetSelectorRoot",
                typeof(RectTransform), typeof(CanvasGroup),
                typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(canvas.transform, false);
            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var oc = root.GetComponent<Canvas>();
            oc.overrideSorting = true; oc.sortingOrder = 1700;
            root.transform.SetAsLastSibling();

            // Dim
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dim.transform.SetParent(root.transform, false);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0,0,0,0.8f);
            var dimBtn = dim.GetComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.onClick.AddListener(() => Object.Destroy(root));

            // Card
            var card = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            card.transform.SetParent(root.transform, false);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot     = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(540, 600);
            card.GetComponent<Image>().color = new Color(0.10f, 0.05f, 0.20f, 0.97f);
            var vlg = card.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(28, 28, 28, 28);
            vlg.spacing = 14;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // Title
            AddText(card.transform, "🐾 Choose Your Companion", 32, Color.white, FontStyles.Bold,
                    TextAlignmentOptions.Center, 50);

            string activeId = Sparq.Core.SaveService.Data?.activePet ?? "bear";

            // Build option buttons
            foreach (var p in PETS)
            {
                BuildOption(card.transform, p, activeId == p.id, () =>
                {
                    var data = Sparq.Core.SaveService.Data;
                    if (data == null) return;
                    if (p.id == "batty" || p.id == "cat")
                    {
                        Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                        // not yet unlocked — show notice
                        FlashLockedNotice(card.transform, p.displayName);
                        return;
                    }
                    data.activePet = p.id;
                    Sparq.Core.SaveService.Save();
                    Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Coin);
                    Object.Destroy(root);
                });
            }

            // Close
            var closeBtn = AddButton(card.transform, "Close", new Color(0.4f, 0.3f, 0.5f));
            closeBtn.onClick.AddListener(() => { Object.Destroy(root); });
        }

        private static void BuildOption(Transform parent, PetOption p, bool isActive, UnityEngine.Events.UnityAction onPick)
        {
            var go = new GameObject($"Pet_{p.id}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 110;
            le.flexibleWidth = 1;

            var img = go.GetComponent<Image>();
            img.color = isActive ? p.cardColor : new Color(p.cardColor.r * 0.5f, p.cardColor.g * 0.5f, p.cardColor.b * 0.5f, 0.85f);

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(onPick);

            // Text inside (name + flavor)
            var inner = new GameObject("Inner", typeof(RectTransform), typeof(VerticalLayoutGroup));
            inner.transform.SetParent(go.transform, false);
            var irt = inner.GetComponent<RectTransform>();
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(20, 8); irt.offsetMax = new Vector2(-20, -8);
            var ivlg = inner.GetComponent<VerticalLayoutGroup>();
            ivlg.spacing = 4;
            ivlg.childForceExpandWidth = true;
            ivlg.childAlignment = TextAnchor.MiddleLeft;

            string activeBadge = isActive ? "  ✓ ACTIVE" : (p.id == "bear" ? "" : "  🔒 LOCKED");
            AddText(inner.transform, $"{p.displayName}{activeBadge}", 26, Color.white, FontStyles.Bold,
                    TextAlignmentOptions.Left, 32);
            AddText(inner.transform, p.flavor, 16, new Color(1, 1, 1, 0.85f), FontStyles.Italic,
                    TextAlignmentOptions.Left, 24);
        }

        private static void FlashLockedNotice(Transform parent, string petName)
        {
            // Spawn a quick floater above the card
            var canvas = parent.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                XPFloater.Spawn(canvas.transform,
                    parent.position + new Vector3(0, 200, 0),
                    $"🔒 {petName} unlocks at Lv.10",
                    new Color(1f, 0.6f, 0.3f));
            }
        }

        private static void AddText(Transform parent, string text, int size, Color color, FontStyles style,
                                    TextAlignmentOptions align, float height)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = size;
            tm.color = color;
            tm.fontStyle = style;
            tm.alignment = align;
            tm.raycastTarget = false;
        }

        private static Button AddButton(Transform parent, string label, Color color)
        {
            var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            le.flexibleWidth = 1;
            go.GetComponent<Image>().color = color;
            var btn = go.GetComponent<Button>();

            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tm = lbl.AddComponent<TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = 24;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = Color.white;
            tm.raycastTarget = false;

            return btn;
        }
    }
}
