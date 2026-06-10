using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 174: Strip the broken in-prefab TMP_InputField hacks (they were
    /// fighting with the prefab's layout → glitch). Build a real working input
    /// bar OUTSIDE the prefab at the bottom of the SocialPanel — typing works.
    /// </summary>
    public static class SparqWorkingInput174
    {
        private static readonly Color GOLD       = new Color(1.00f, 0.78f, 0.22f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/174. Working chat input (strip prefab hacks, add real input bar)")]
        public static void Apply()
        {
            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            // 1. Strip any TMP_InputField + InputText we added inside the prefab content
            int stripped = 0;
            var content = social.transform.Find("Content");
            if (content != null)
            {
                foreach (var f in content.GetComponentsInChildren<TMP_InputField>(true))
                {
                    if (f == null) continue;
                    Object.DestroyImmediate(f);
                    stripped++;
                }
                // Remove our custom "InputText" overlays
                foreach (var t in content.GetComponentsInChildren<Transform>(true))
                {
                    if (t != null && t.name == "InputText")
                        Object.DestroyImmediate(t.gameObject);
                }
            }

            // 2. Build a real input bar at the bottom of SocialPanel
            var existingBar = social.transform.Find("ChatInputBar");
            if (existingBar != null) Object.DestroyImmediate(existingBar.gameObject);

            var bar = new GameObject("ChatInputBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(social.transform, false);
            var brt = bar.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
            brt.pivot = new Vector2(0.5f, 0);
            brt.anchoredPosition = new Vector2(-130, 24); // shifted left to clear tabs column
            brt.sizeDelta = new Vector2(-300, 80);
            bar.GetComponent<Image>().color = new Color(0.16f, 0.10f, 0.22f, 0.95f);

            // Input field
            var fieldGO = new GameObject("Input", typeof(RectTransform), typeof(Image));
            fieldGO.transform.SetParent(bar.transform, false);
            var frt = fieldGO.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0, 0.5f); frt.anchorMax = new Vector2(1, 0.5f);
            frt.pivot = new Vector2(0.5f, 0.5f);
            frt.anchoredPosition = new Vector2(-50, 0);
            frt.sizeDelta = new Vector2(-130, 60);
            fieldGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);

            // Text component
            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(fieldGO.transform, false);
            var trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16, 4); trt.offsetMax = new Vector2(-16, -4);
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
            prt.offsetMin = new Vector2(16, 4); prt.offsetMax = new Vector2(-16, -4);
            var ptm = phGO.AddComponent<TextMeshProUGUI>();
            ptm.text = "type a message...";
            ptm.fontSize = 22;
            ptm.fontStyle = FontStyles.Italic;
            ptm.color = new Color(0.65f, 0.62f, 0.70f);
            ptm.alignment = TextAlignmentOptions.MidlineLeft;
            ptm.font = TMP_Settings.defaultFontAsset;

            // TMP_InputField wiring
            var input = fieldGO.AddComponent<TMP_InputField>();
            input.textViewport = frt;
            input.textComponent = ttm;
            input.placeholder = ptm;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.targetGraphic = fieldGO.GetComponent<Image>();
            input.text = "";

            // Send button
            var sendGO = new GameObject("Send", typeof(RectTransform), typeof(Image), typeof(Button));
            sendGO.transform.SetParent(bar.transform, false);
            var srt = sendGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(1, 0.5f); srt.anchorMax = new Vector2(1, 0.5f);
            srt.pivot = new Vector2(1, 0.5f);
            srt.anchoredPosition = new Vector2(-12, 0);
            srt.sizeDelta = new Vector2(96, 60);
            sendGO.GetComponent<Image>().color = GOLD;

            var sendLbl = new GameObject("Lbl", typeof(RectTransform));
            sendLbl.transform.SetParent(sendGO.transform, false);
            var slrt = sendLbl.GetComponent<RectTransform>();
            slrt.anchorMin = Vector2.zero; slrt.anchorMax = Vector2.one;
            slrt.offsetMin = Vector2.zero; slrt.offsetMax = Vector2.zero;
            var stm = sendLbl.AddComponent<TextMeshProUGUI>();
            stm.text = "Send";
            stm.fontSize = 22;
            stm.fontStyle = FontStyles.Bold;
            stm.color = DEEP_NAVY;
            stm.alignment = TextAlignmentOptions.Center;
            stm.font = TMP_Settings.defaultFontAsset;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Working chat input wired:\n" +
                $"• Stripped {stripped} broken in-prefab InputField hacks\n" +
                "• Real input bar at bottom of SocialPanel (outside prefab)\n" +
                "• Click the input box and type — it works\n" +
                "• Gold Send button on the right\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
