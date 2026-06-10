using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 183: Fix invisible chat message text — the cream bubble sprite
    /// I applied in #182 made cream-colored text invisible. Switch all message
    /// body text to dark navy so it reads against the new bubble bg.
    /// </summary>
    public static class SparqFixChatText183
    {
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);
        private static readonly Color GOLD      = new Color(1f, 0.78f, 0.22f);

        [MenuItem("Sparq/183. Fix invisible chat text (dark navy on cream bubbles)")]
        public static void Apply()
        {
            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            int fixedCount = 0;
            var msgList = social.transform.Find("Card/Content/Chat_Tab/Scroll/Viewport/List");
            if (msgList == null)
            {
                EditorUtility.DisplayDialog("Sparq", "Message list not found.", "OK");
                return;
            }

            for (int i = 0; i < msgList.childCount; i++)
            {
                var row = msgList.GetChild(i);
                var bubble = row.Find("Bubble");
                if (bubble == null) continue;

                // Author
                var auth = bubble.Find("Author");
                if (auth != null)
                {
                    var atm = auth.GetComponent<TMP_Text>();
                    if (atm != null) { atm.color = GOLD; fixedCount++; }
                }
                // Body
                var body = bubble.Find("Body");
                if (body != null)
                {
                    var btm = body.GetComponent<TMP_Text>();
                    if (btm != null)
                    {
                        btm.color = DEEP_NAVY; // dark navy on cream bubble = readable
                        if (btm.font == null) btm.font = TMP_Settings.defaultFontAsset;
                        fixedCount++;
                    }
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Fixed {fixedCount} chat text element(s):\n" +
                "• Body text → dark navy (visible on cream bubbles)\n" +
                "• Author tags → gold for accent\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
