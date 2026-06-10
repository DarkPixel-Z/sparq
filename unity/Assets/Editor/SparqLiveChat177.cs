using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 177: Replace the static Chat prefab tab with a real working live chat
    /// (header + scrollable message list + working input + Send appends bubble).
    /// Other 3 tabs (Clan/Ranking/Profile) keep their prefabs.
    /// </summary>
    public static class SparqLiveChat177
    {
        private static readonly Color GOLD       = new Color(1.00f, 0.78f, 0.22f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f);
        private static readonly Color CARD_BG    = new Color(0.10f, 0.06f, 0.18f, 0.96f);
        private static readonly Color INPUT_BG   = new Color(1f, 1f, 1f, 0.12f);

        [MenuItem("Sparq/177. Replace Chat tab with REAL working chat")]
        public static void Apply()
        {
            // Strip old standalone ChatInputBar from #175
            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            var oldBar = social.transform.Find("ChatInputBar");
            if (oldBar != null) Object.DestroyImmediate(oldBar.gameObject);

            // Find content area
            var content = social.transform.Find("Content");
            if (content == null) { EditorUtility.DisplayDialog("Sparq", "Content area missing.", "OK"); return; }

            // Find the existing Chat_Tab prefab and replace it
            Transform oldChat = null;
            for (int i = 0; i < content.childCount; i++)
            {
                var c = content.GetChild(i);
                if (c.name == "Chat_Tab") { oldChat = c; break; }
            }
            int chatSibling = oldChat != null ? oldChat.GetSiblingIndex() : -1;
            if (oldChat != null) Object.DestroyImmediate(oldChat.gameObject);

            // Build the live chat root that replaces it
            var live = BuildLiveChat(content, chatSibling);

            // Re-wire TabGroup to use live as the Chat tab content
            var tg = social.GetComponent<TabGroup>();
            if (tg != null)
            {
                var so = new SerializedObject(tg);
                var tabsArr = so.FindProperty("tabs");
                if (tabsArr != null && tabsArr.arraySize > 0)
                {
                    var entry = tabsArr.GetArrayElementAtIndex(0);
                    entry.FindPropertyRelative("content").objectReferenceValue = live;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Live chat wired:\n" +
                "• Old static Chat prefab removed\n" +
                "• Replaced with a real working chat:\n" +
                "  · Header + subtitle\n" +
                "  · Scrollable message list (5 mock messages)\n" +
                "  · Working text input + Send\n" +
                "  · Send appends new bubble + auto-scrolls\n\n" +
                "Hit ▶ Play, tap WORLD → Chat → type → Send.", "OK");
        }

        private static GameObject BuildLiveChat(Transform parent, int siblingIndex)
        {
            var root = new GameObject("Chat_Tab", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            if (siblingIndex >= 0) root.transform.SetSiblingIndex(siblingIndex);

            var rrt = root.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = CARD_BG;

            // Header
            var hdr = new GameObject("Header", typeof(RectTransform), typeof(Image));
            hdr.transform.SetParent(root.transform, false);
            var hrt = hdr.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1);
            hrt.pivot = new Vector2(0.5f, 1);
            hrt.sizeDelta = new Vector2(0, 60);
            hdr.GetComponent<Image>().color = new Color(GOLD.r, GOLD.g, GOLD.b, 0.18f);
            MakeText(hdr.transform, "T", "GLOBAL CHAT  ·  1,284 online",
                20, FontStyles.Bold, GOLD,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero)
                .alignment = TextAlignmentOptions.Center;

            // ScrollRect
            var scrollGO = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scrollGO.transform.SetParent(root.transform, false);
            var srt = scrollGO.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0); srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(0, 96); srt.offsetMax = new Vector2(0, -64);
            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.20f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vrt = viewport.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var contentList = new GameObject("MessageList",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentList.transform.SetParent(viewport.transform, false);
            var clrt = contentList.GetComponent<RectTransform>();
            clrt.anchorMin = new Vector2(0, 1); clrt.anchorMax = new Vector2(1, 1);
            clrt.pivot = new Vector2(0.5f, 1);
            clrt.anchoredPosition = Vector2.zero;
            clrt.sizeDelta = new Vector2(0, 0);
            var vlg = contentList.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 8;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = false;
            var csf = contentList.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var sr = scrollGO.GetComponent<ScrollRect>();
            sr.viewport = vrt;
            sr.content = clrt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 24;
            sr.movementType = ScrollRect.MovementType.Elastic;

            // Input bar (bottom 80px)
            var inputBar = new GameObject("InputBar", typeof(RectTransform), typeof(Image));
            inputBar.transform.SetParent(root.transform, false);
            var ibrt = inputBar.GetComponent<RectTransform>();
            ibrt.anchorMin = new Vector2(0, 0); ibrt.anchorMax = new Vector2(1, 0);
            ibrt.pivot = new Vector2(0.5f, 0);
            ibrt.sizeDelta = new Vector2(0, 80);
            inputBar.GetComponent<Image>().color = new Color(0, 0, 0, 0.30f);

            // Input field
            var fieldGO = new GameObject("Input", typeof(RectTransform), typeof(Image));
            fieldGO.transform.SetParent(inputBar.transform, false);
            var frt = fieldGO.GetComponent<RectTransform>();
            frt.anchorMin = new Vector2(0, 0.5f); frt.anchorMax = new Vector2(1, 0.5f);
            frt.pivot = new Vector2(0.5f, 0.5f);
            frt.anchoredPosition = new Vector2(-58, 0);
            frt.sizeDelta = new Vector2(-150, 56);
            var fImg = fieldGO.GetComponent<Image>();
            fImg.color = INPUT_BG;
            fImg.raycastTarget = true;

            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(fieldGO.transform, false);
            var trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(16, 4); trt.offsetMax = new Vector2(-16, -4);
            var ttm = textGO.AddComponent<TextMeshProUGUI>();
            ttm.text = "";
            ttm.fontSize = 20;
            ttm.color = CREAM;                  // visible typed text!
            ttm.alignment = TextAlignmentOptions.MidlineLeft;
            ttm.font = TMP_Settings.defaultFontAsset;

            var phGO = new GameObject("Placeholder", typeof(RectTransform));
            phGO.transform.SetParent(fieldGO.transform, false);
            var prt = phGO.GetComponent<RectTransform>();
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(16, 4); prt.offsetMax = new Vector2(-16, -4);
            var ptm = phGO.AddComponent<TextMeshProUGUI>();
            ptm.text = "type a message...";
            ptm.fontSize = 20;
            ptm.fontStyle = FontStyles.Italic;
            ptm.color = new Color(0.70f, 0.66f, 0.78f);
            ptm.alignment = TextAlignmentOptions.MidlineLeft;
            ptm.font = TMP_Settings.defaultFontAsset;

            var input = fieldGO.AddComponent<TMP_InputField>();
            input.textViewport = frt;
            input.textComponent = ttm;
            input.placeholder = ptm;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.targetGraphic = fImg;

            // Send button
            var sendGO = new GameObject("Send", typeof(RectTransform), typeof(Image), typeof(Button));
            sendGO.transform.SetParent(inputBar.transform, false);
            var srtb = sendGO.GetComponent<RectTransform>();
            srtb.anchorMin = new Vector2(1, 0.5f); srtb.anchorMax = new Vector2(1, 0.5f);
            srtb.pivot = new Vector2(1, 0.5f);
            srtb.anchoredPosition = new Vector2(-12, 0);
            srtb.sizeDelta = new Vector2(116, 56);
            sendGO.GetComponent<Image>().color = GOLD;
            MakeText(sendGO.transform, "Lbl", "Send",
                22, FontStyles.Bold, DEEP_NAVY,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero)
                .alignment = TextAlignmentOptions.Center;

            // LiveChatTab controller
            var live = root.AddComponent<LiveChatTab>();
            var liveSO = new SerializedObject(live);
            liveSO.FindProperty("input").objectReferenceValue       = input;
            liveSO.FindProperty("sendBtn").objectReferenceValue     = sendGO.GetComponent<Button>();
            liveSO.FindProperty("messageList").objectReferenceValue = clrt;
            liveSO.FindProperty("scrollRect").objectReferenceValue  = sr;
            liveSO.FindProperty("font").objectReferenceValue        = TMP_Settings.defaultFontAsset;
            liveSO.ApplyModifiedPropertiesWithoutUndo();

            // Pre-fill 5 mock messages
            BuildMockMessage(clrt, "Aria",  "anyone running the trial today?", false);
            BuildMockMessage(clrt, "Bram",  "i'm in. need 1 more for the boss", false);
            BuildMockMessage(clrt, "You",   "count me in — gimme 5 min", true);
            BuildMockMessage(clrt, "Dax",   "finally hit Lv.9!!", false);
            BuildMockMessage(clrt, "Aria",  "let's go!! meeting at the portal", false);

            return root;
        }

        private static void BuildMockMessage(Transform parent, string author, string text, bool fromMe)
        {
            // Reuse the same bubble layout as runtime-appended messages
            // (we duplicate here so initial mocks render before LiveChatTab.Start)
            var row = new GameObject($"Msg_{author}", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 60;

            var bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            bubble.transform.SetParent(row.transform, false);
            var brt = bubble.GetComponent<RectTransform>();
            float w = Mathf.Min(560f, 100f + text.Length * 11f);
            brt.anchorMin = new Vector2(fromMe ? 1 : 0, 0); brt.anchorMax = new Vector2(fromMe ? 1 : 0, 1);
            brt.pivot     = new Vector2(fromMe ? 1 : 0, 0.5f);
            brt.anchoredPosition = new Vector2(fromMe ? -16 : 16, 0);
            brt.sizeDelta = new Vector2(w, -8);
            bubble.GetComponent<Image>().color = fromMe ? GOLD : new Color(0.22f, 0.16f, 0.34f);

            MakeText(bubble.transform, "Author", author, 11, FontStyles.Bold,
                fromMe ? new Color(0.3f, 0.2f, 0.05f) : GOLD,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -2), new Vector2(-16, 14))
                .alignment = TextAlignmentOptions.MidlineLeft;
            var b = MakeText(bubble.transform, "Body", text, 16, FontStyles.Normal,
                fromMe ? DEEP_NAVY : CREAM,
                new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, -8), new Vector2(-24, -16));
            b.alignment = TextAlignmentOptions.MidlineLeft;
            b.textWrappingMode = TextWrappingModes.Normal;
        }

        private static TMP_Text MakeText(Transform parent, string name, string text,
            float size, FontStyles style, Color color,
            Vector2 amin, Vector2 amax, Vector2 anch, Vector2 sd)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.pivot = new Vector2((amin.x + amax.x) * 0.5f, (amin.y + amax.y) * 0.5f);
            rt.anchoredPosition = anch;
            rt.sizeDelta = sd;
            var tm = go.AddComponent<TextMeshProUGUI>();
            tm.text = text;
            tm.fontSize = size;
            tm.fontStyle = style;
            tm.color = color;
            tm.alignment = TextAlignmentOptions.Center;
            tm.font = TMP_Settings.defaultFontAsset;
            tm.raycastTarget = false;
            return tm;
        }
    }
}
