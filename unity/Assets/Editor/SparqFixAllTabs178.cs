using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Sparq.UI;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 178: Comprehensive fixes for all 4 SocialPanel tabs.
    ///   • Chat: visible typed text, no empty bubbles
    ///   • Clan/Ranking/Profile: ScrollRect content gets ContentSizeFitter so scroll works
    ///   • Spelling: Serch → Search
    ///   • Ranking: slightly smaller scale
    ///   • Prefab buttons: tap-anywhere shows "Coming soon" toast (so taps aren't dead)
    /// </summary>
    public static class SparqFixAllTabs178
    {
        private static readonly Color GOLD       = new Color(1.00f, 0.78f, 0.22f);
        private static readonly Color CREAM      = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY  = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/178. Fix all tabs (scroll + text + spelling + buttons)")]
        public static void Apply()
        {
            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            int spellingFix = 0, scrollFix = 0, btnsWired = 0;
            var content = social.transform.Find("Content");
            if (content == null) { EditorUtility.DisplayDialog("Sparq", "Content missing.", "OK"); return; }

            // ───── 1. Live chat (Chat_Tab): rebuild bubbles cleanly so text is visible ─────
            var chatTab = content.Find("Chat_Tab");
            if (chatTab != null)
            {
                var live = chatTab.GetComponent<LiveChatTab>();
                FixLiveChatVisibility(chatTab, live);
            }

            // ───── 2. Prefab tabs: ScrollRect fix + spelling + button mock + scale ─────
            for (int i = 0; i < content.childCount; i++)
            {
                var c = content.GetChild(i);
                if (!c.name.EndsWith("_Tab") || c.name == "Chat_Tab") continue;

                // Spelling: Serch → Search
                foreach (var tmp in c.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    if (tmp.text != null && tmp.text.Equals("Serch", System.StringComparison.OrdinalIgnoreCase))
                    {
                        tmp.text = "Search";
                        spellingFix++;
                    }
                }

                // ScrollRect content sizing (force scroll to actually scroll)
                foreach (var sr in c.GetComponentsInChildren<ScrollRect>(true))
                {
                    if (sr == null || sr.content == null) continue;
                    var csf = sr.content.GetComponent<ContentSizeFitter>();
                    if (csf == null) csf = sr.content.gameObject.AddComponent<ContentSizeFitter>();
                    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                    if (sr.content.GetComponent<VerticalLayoutGroup>() == null)
                    {
                        var vlg = sr.content.gameObject.AddComponent<VerticalLayoutGroup>();
                        vlg.childForceExpandWidth = true;
                        vlg.childForceExpandHeight = false;
                        vlg.childControlHeight = false;
                        vlg.spacing = 6;
                    }
                    sr.movementType = ScrollRect.MovementType.Elastic;
                    sr.scrollSensitivity = 24;
                    scrollFix++;
                }

                // Wire all UI Buttons inside the prefab to a toast so taps aren't dead
                foreach (var btn in c.GetComponentsInChildren<Button>(true))
                {
                    if (btn == null) continue;
                    if (btn.onClick.GetPersistentEventCount() > 0) continue; // skip ones already wired

                    string label = "Action";
                    var lblTM = btn.GetComponentInChildren<TMP_Text>(true);
                    if (lblTM != null && !string.IsNullOrEmpty(lblTM.text)) label = lblTM.text;

                    btn.onClick.RemoveAllListeners();
                    var capLbl = label;
                    btn.onClick.AddListener(() => ShowToast(c, $"{capLbl} — coming soon"));
                    btnsWired++;
                }

                // Ranking: slight scale down
                if (c.name == "Ranking_Tab")
                {
                    var rt = c.GetComponent<RectTransform>();
                    if (rt != null) rt.localScale = new Vector3(0.85f, 0.85f, 1f);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Fixes applied:\n" +
                $"• Live chat text visibility cleaned\n" +
                $"• {scrollFix} ScrollRect(s) wired with ContentSizeFitter\n" +
                $"• {spellingFix} 'Serch' → 'Search'\n" +
                $"• {btnsWired} prefab button(s) now show a 'coming soon' toast\n" +
                "• Ranking scaled to 0.85×\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // ───────── Helpers ─────────
        private static void FixLiveChatVisibility(Transform chatTab, LiveChatTab live)
        {
            // Find the live chat input field, ensure typed text is visible
            var input = chatTab.GetComponentInChildren<TMP_InputField>(true);
            if (input != null && input.textComponent != null)
            {
                input.textComponent.color = CREAM;
                input.textComponent.fontSize = 22;
                if (input.textComponent.font == null)
                    input.textComponent.font = TMP_Settings.defaultFontAsset;
                input.textComponent.fontMaterial = input.textComponent.fontSharedMaterial;
                input.caretColor = GOLD;
                input.customCaretColor = true;
            }

            // Find the message list and rebuild any blank "You" bubbles so the text shows
            var msgList = chatTab.Find("Scroll/Viewport/MessageList");
            if (msgList == null) return;

            for (int i = msgList.childCount - 1; i >= 0; i--)
            {
                var row = msgList.GetChild(i);
                bool hasText = false;
                foreach (var tmp in row.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp != null && !string.IsNullOrEmpty(tmp.text) && tmp.name == "Body")
                        hasText = true;
                    if (tmp != null && tmp.font == null)
                        tmp.font = TMP_Settings.defaultFontAsset;
                }
                if (!hasText)
                {
                    Object.DestroyImmediate(row.gameObject);
                }
            }

            // Re-append "You" message we lost
            if (live != null)
            {
                live.AppendMessage("You", "count me in — gimme 5 min", true);
            }
        }

        private static void ShowToast(Transform anchor, string message)
        {
            var canvas = anchor.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            try
            {
                XPFloater.Spawn(canvas.transform,
                    anchor.position + new Vector3(0, 60, 0),
                    message, GOLD);
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
            } catch {}
        }
    }
}
