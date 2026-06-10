using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 175: Professional pass on the SocialPanel.
    ///   • Lift ChatInputBar to its own override Canvas (sortingOrder 13000)
    ///     so the prefab can never block typing/clicks
    ///   • Disable the prefab's pretend "Text" placeholder bar at the bottom
    ///     so we don't have two stacked input UIs
    ///   • Tighten paddings, lock the close X above modal too
    /// </summary>
    public static class SparqProSocial175
    {
        private static readonly Color GOLD       = new Color(1.00f, 0.78f, 0.22f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f);
        private static readonly Color BAR_BG     = new Color(0.16f, 0.10f, 0.22f, 0.98f);

        [MenuItem("Sparq/175. Pro pass — working input + clean layout")]
        public static void Apply()
        {
            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            // 1. Disable the prefab's static "Layerlab AM 08:36 Text" pretend input row
            //    so we only have ONE input bar visible
            var content = social.transform.Find("Content");
            if (content != null)
            {
                foreach (var tmp in content.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    string s = tmp.text == null ? "" : tmp.text.Trim();
                    if (s == "Text" || s == "Enter Text..." || s.ToLower().Contains("type a message"))
                    {
                        // Hide the parent row — usually parent.parent is the "InputBar" cluster
                        var row = tmp.transform;
                        for (int up = 0; up < 3 && row.parent != null; up++) row = row.parent;
                        // Walk up to a sane "row" container — hide the immediate parent that has Image
                        var hide = tmp.transform.parent;
                        while (hide != null && hide.GetComponent<Image>() == null) hide = hide.parent;
                        if (hide != null && hide != content) hide.gameObject.SetActive(false);
                    }
                }
            }

            // 2. Lift our ChatInputBar to its own override Canvas
            var bar = social.transform.Find("ChatInputBar");
            if (bar != null)
            {
                Object.DestroyImmediate(bar.gameObject);
                bar = null;
            }

            var barGO = new GameObject("ChatInputBar",
                typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            barGO.transform.SetParent(social.transform, false);
            var brt = barGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.anchoredPosition = new Vector2(-130, 30);
            brt.sizeDelta = new Vector2(-300, 84);
            barGO.GetComponent<Image>().color = BAR_BG;

            // Override Canvas → guaranteed on top of prefab raycasters
            var oc = barGO.GetComponent<Canvas>();
            oc.overrideSorting = true;
            oc.sortingOrder = 13000;

            // Input field
            var fieldGO = new GameObject("Input", typeof(RectTransform), typeof(Image));
            fieldGO.transform.SetParent(barGO.transform, false);
            var frt = fieldGO.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0, 0.5f); frt.anchorMax = new Vector2(1, 0.5f);
            frt.pivot = new Vector2(0.5f, 0.5f);
            frt.anchoredPosition = new Vector2(-58, 0);
            frt.sizeDelta = new Vector2(-140, 60);
            var fImg = fieldGO.GetComponent<Image>();
            fImg.color = new Color(1f, 1f, 1f, 0.12f);
            fImg.raycastTarget = true;

            // Text component
            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(fieldGO.transform, false);
            var trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(18, 4); trt.offsetMax = new Vector2(-18, -4);
            var ttm = textGO.AddComponent<TextMeshProUGUI>();
            ttm.text = "";
            ttm.fontSize = 22;
            ttm.color = CREAM;
            ttm.alignment = TextAlignmentOptions.MidlineLeft;
            ttm.font = TMP_Settings.defaultFontAsset;

            // Placeholder
            var phGO = new GameObject("Placeholder", typeof(RectTransform));
            phGO.transform.SetParent(fieldGO.transform, false);
            var prt = phGO.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(18, 4); prt.offsetMax = new Vector2(-18, -4);
            var ptm = phGO.AddComponent<TextMeshProUGUI>();
            ptm.text = "type a message...";
            ptm.fontSize = 22;
            ptm.fontStyle = FontStyles.Italic;
            ptm.color = new Color(0.70f, 0.66f, 0.78f);
            ptm.alignment = TextAlignmentOptions.MidlineLeft;
            ptm.font = TMP_Settings.defaultFontAsset;

            // TMP_InputField
            var input = fieldGO.AddComponent<TMP_InputField>();
            input.textViewport = frt;
            input.textComponent = ttm;
            input.placeholder = ptm;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.targetGraphic = fImg;
            input.text = "";

            // Send button
            var sendGO = new GameObject("Send",
                typeof(RectTransform), typeof(Image), typeof(Button));
            sendGO.transform.SetParent(barGO.transform, false);
            var srt = sendGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(1, 0.5f); srt.anchorMax = new Vector2(1, 0.5f);
            srt.pivot = new Vector2(1, 0.5f);
            srt.anchoredPosition = new Vector2(-14, 0);
            srt.sizeDelta = new Vector2(108, 60);
            sendGO.GetComponent<Image>().color = GOLD;
            sendGO.GetComponent<Image>().raycastTarget = true;

            var sendLbl = new GameObject("Lbl", typeof(RectTransform));
            sendLbl.transform.SetParent(sendGO.transform, false);
            var slrt = sendLbl.GetComponent<RectTransform>();
            slrt.anchorMin = Vector2.zero; slrt.anchorMax = Vector2.one;
            slrt.offsetMin = Vector2.zero; slrt.offsetMax = Vector2.zero;
            var stm = sendLbl.AddComponent<TextMeshProUGUI>();
            stm.text = "Send";
            stm.fontSize = 24;
            stm.fontStyle = FontStyles.Bold;
            stm.color = DEEP_NAVY;
            stm.alignment = TextAlignmentOptions.Center;
            stm.font = TMP_Settings.defaultFontAsset;
            stm.raycastTarget = false;

            // 3. Also lift the close X to its own override canvas so it works above prefab too
            var close = social.transform.Find("Close");
            if (close != null)
            {
                var closeCanvas = close.GetComponent<Canvas>();
                if (closeCanvas == null) closeCanvas = close.gameObject.AddComponent<Canvas>();
                closeCanvas.overrideSorting = true;
                closeCanvas.sortingOrder = 13000;
                if (close.GetComponent<GraphicRaycaster>() == null)
                    close.gameObject.AddComponent<GraphicRaycaster>();
            }

            // 4. Also lift the tabs canvas so each tab click is above prefab
            var tabsT = social.transform.Find("Tabs");
            if (tabsT != null)
            {
                var tabsCanvas = tabsT.GetComponent<Canvas>();
                if (tabsCanvas == null) tabsCanvas = tabsT.gameObject.AddComponent<Canvas>();
                tabsCanvas.overrideSorting = true;
                tabsCanvas.sortingOrder = 13000;
                if (tabsT.GetComponent<GraphicRaycaster>() == null)
                    tabsT.gameObject.AddComponent<GraphicRaycaster>();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Pro pass:\n" +
                "• ChatInputBar → own override Canvas at sortingOrder 13000 → typing/clicks work\n" +
                "• Tabs + Close X → own override canvases at 13000 → clickable above prefab\n" +
                "• Prefab's pretend 'Text' bar disabled\n" +
                "• Cleaner spacing\n\n" +
                "Hit ▶ Play, click input box, type. Tap Send to fire click.", "OK");
        }
    }
}
