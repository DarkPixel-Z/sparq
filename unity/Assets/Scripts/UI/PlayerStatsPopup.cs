using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sparq.UI
{
    /// <summary>
    /// Tap the Player HUD → opens this popup with full stats:
    /// ATK / DEF / SPD, plus Quests done, Streak, Total XP.
    /// </summary>
    public static class PlayerStatsPopup
    {
        public static void Show()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var data = Sparq.Core.SaveService.Data;
            if (data == null) return;

            var root = new GameObject("PlayerStatsRoot",
                typeof(RectTransform), typeof(CanvasGroup),
                typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(canvas.transform, false);
            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            var oc = root.GetComponent<Canvas>();
            oc.overrideSorting = true; oc.sortingOrder = 1750;
            root.transform.SetAsLastSibling();

            // Dim
            var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dim.transform.SetParent(root.transform, false);
            var drt = dim.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero; drt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0,0,0,0.85f);
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
            crt.sizeDelta = new Vector2(520, 600);
            card.GetComponent<Image>().color = new Color(0.10f, 0.05f, 0.20f, 0.97f);
            var vlg = card.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(28, 28, 28, 28);
            vlg.spacing = 12;
            vlg.childForceExpandWidth = true;
            vlg.childAlignment = TextAnchor.UpperCenter;

            string petName = string.IsNullOrEmpty(data.petName) ? "Karu" : data.petName;
            int atk = 5 + data.level * 2;
            int def = 3 + data.level;
            int spd = 5 + data.level;

            AddText(card.transform, petName, 36, Color.white, FontStyles.Bold,
                    TextAlignmentOptions.Center, 50);
            AddText(card.transform, $"Lv.{data.level}", 22, new Color(1f, 0.85f, 0.35f),
                    FontStyles.Bold, TextAlignmentOptions.Center, 32);

            AddDivider(card.transform);

            // Combat stats
            AddText(card.transform, "COMBAT", 18, new Color(1f, 0.55f, 0.4f), FontStyles.Bold,
                    TextAlignmentOptions.Left, 28);
            AddRow(card.transform, "ATK  Attack",  atk.ToString());
            AddRow(card.transform, "DEF  Defense", def.ToString());
            AddRow(card.transform, "SPD  Speed",   spd.ToString());

            AddDivider(card.transform);

            // Progress stats
            AddText(card.transform, "PROGRESS", 18, new Color(0.5f, 0.85f, 1f), FontStyles.Bold,
                    TextAlignmentOptions.Left, 28);
            AddRow(card.transform, "Quests Done",   data.totalTasksDone.ToString());
            AddRow(card.transform, "Current Streak", data.streak.ToString());
            AddRow(card.transform, "Longest Streak", data.longestStreak.ToString());
            AddRow(card.transform, "Total XP",       data.totalXP.ToString());
            AddRow(card.transform, "Coins",          data.sparqCoins.ToString());

            // Spacer
            var sp = new GameObject("Spacer", typeof(RectTransform));
            sp.transform.SetParent(card.transform, false);
            var sple = sp.AddComponent<LayoutElement>();
            sple.preferredHeight = 8;

            // Close
            var btn = AddButton(card.transform, "Close", new Color(0.4f, 0.3f, 0.5f));
            btn.onClick.AddListener(() => Object.Destroy(root));
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

        private static void AddRow(Transform parent, string label, string value)
        {
            var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(parent, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 32;
            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.childForceExpandWidth = true;

            var lblGO = new GameObject("Label", typeof(RectTransform));
            lblGO.transform.SetParent(row.transform, false);
            var lblTM = lblGO.AddComponent<TextMeshProUGUI>();
            lblTM.text = label;
            lblTM.fontSize = 18;
            lblTM.color = new Color(1, 1, 1, 0.85f);
            lblTM.alignment = TextAlignmentOptions.Left;

            var valGO = new GameObject("Value", typeof(RectTransform));
            valGO.transform.SetParent(row.transform, false);
            var valTM = valGO.AddComponent<TextMeshProUGUI>();
            valTM.text = value;
            valTM.fontSize = 20;
            valTM.fontStyle = FontStyles.Bold;
            valTM.color = new Color(1f, 0.92f, 0.4f);
            valTM.alignment = TextAlignmentOptions.Right;
        }

        private static void AddDivider(Transform parent)
        {
            var go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 2;
            le.flexibleWidth = 1;
            go.GetComponent<Image>().color = new Color(1,1,1,0.15f);
        }

        private static Button AddButton(Transform parent, string label, Color color)
        {
            var go = new GameObject("Btn", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 56;
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
            tm.fontSize = 22;
            tm.fontStyle = FontStyles.Bold;
            tm.alignment = TextAlignmentOptions.Center;
            tm.color = Color.white;
            return btn;
        }
    }
}
