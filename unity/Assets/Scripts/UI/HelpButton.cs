using UnityEngine;

namespace Sparq.UI
{
    /// <summary>
    /// Una the axolotl now serves as the HELP / TUTORIAL button.
    /// Tap her → tutorial popup explains the game's basics.
    /// </summary>
    public class HelpButton : MonoBehaviour
    {
        private void OnMouseDown()
        {
            Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
            HelpPopup.Show();
        }
    }

    public static class HelpPopup
    {
        public static void Show()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var root = new GameObject("HelpRoot",
                typeof(RectTransform), typeof(CanvasGroup),
                typeof(Canvas), typeof(UnityEngine.UI.GraphicRaycaster));
            root.transform.SetParent(canvas.transform, false);
            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var oc = root.GetComponent<Canvas>();
            oc.overrideSorting = true; oc.sortingOrder = 1900;
            root.transform.SetAsLastSibling();

            // Dim
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            dim.transform.SetParent(root.transform, false);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<UnityEngine.UI.Image>().color = new Color(0,0,0,0.85f);
            var dimBtn = dim.GetComponent<UnityEngine.UI.Button>();
            dimBtn.transition = UnityEngine.UI.Selectable.Transition.None;
            dimBtn.onClick.AddListener(() => Object.Destroy(root));

            // Card
            var card = new GameObject("Card", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.VerticalLayoutGroup));
            card.transform.SetParent(root.transform, false);
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot     = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(560, 720);
            card.GetComponent<UnityEngine.UI.Image>().color = new Color(0.10f, 0.05f, 0.20f, 0.97f);
            var vlg = card.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            vlg.padding = new RectOffset(28, 28, 28, 28);
            vlg.spacing = 12;
            vlg.childForceExpandWidth = true;
            vlg.childAlignment = TextAnchor.UpperCenter;

            AddText(card.transform, "HELP", 32, new Color(1f, 0.85f, 0.4f), TMPro.FontStyles.Bold,
                    TMPro.TextAlignmentOptions.Center, 50);
            AddText(card.transform, "Hi, I'm Una. Here's how Sparq works:",
                    16, new Color(1, 1, 1, 0.85f), TMPro.FontStyles.Italic, TMPro.TextAlignmentOptions.Center, 30);

            string[] tips = {
                "QUESTS: Tap the green checkboxes top-right to complete real-life tasks. +XP per quest.",
                "+ ADD QUEST: Add custom tasks like 'Pay electric bill' or 'Schedule dentist'.",
                "MAP: Choose your rival monster. Each tier (mini → boss) gets harder.",
                "TAP KARU: Each tap deals damage to your current rival. Every 7th tap is a CRIT.",
                "DEFEAT: When rival HP hits 0, you get coins + XP + next monster appears.",
                "LOOT: Tap chests, butterflies, runes scattered in the forest for surprise rewards.",
                "DAILY BONUS: Login each day for the 7-day reward carousel.",
                "STREAK: Complete a quest each day to grow your streak fire.",
                "PETS: Choose your companion (Karu the Bear is your starter).",
                "WORLD: Chat + guilds with other players (coming soon).",
            };

            foreach (var tip in tips)
            {
                AddText(card.transform, "• " + tip, 14, new Color(0.95f, 0.95f, 1f, 0.9f),
                        TMPro.FontStyles.Normal, TMPro.TextAlignmentOptions.Left, 50);
            }

            // Close
            var btn = AddButton(card.transform, "Got it!", new Color(0.3f, 0.85f, 0.45f));
            btn.onClick.AddListener(() => Object.Destroy(root));
        }

        private static void AddText(Transform parent, string text, int size, Color color, TMPro.FontStyles style,
                                    TMPro.TextAlignmentOptions align, float height)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;
            var tm = go.AddComponent<TMPro.TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = size;
            tm.color = color;
            tm.fontStyle = style;
            tm.alignment = align;
            tm.raycastTarget = false;
        }

        private static UnityEngine.UI.Button AddButton(Transform parent, string label, Color color)
        {
            var go = new GameObject("Btn", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<UnityEngine.UI.LayoutElement>();
            le.preferredHeight = 60;
            le.flexibleWidth = 1;
            go.GetComponent<UnityEngine.UI.Image>().color = color;
            var btn = go.GetComponent<UnityEngine.UI.Button>();

            var lbl = new GameObject("Label", typeof(RectTransform));
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tm = lbl.AddComponent<TMPro.TextMeshProUGUI>();
            tm.text = label;
            tm.fontSize = 24;
            tm.fontStyle = TMPro.FontStyles.Bold;
            tm.alignment = TMPro.TextAlignmentOptions.Center;
            tm.color = Color.white;
            tm.raycastTarget = false;

            return btn;
        }
    }
}
