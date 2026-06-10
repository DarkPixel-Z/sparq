using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 184: Sharpen chat text + center button labels.
    ///   • Bigger font sizes (blurry usually = too small for atlas)
    ///   • Solid black text color (max contrast on cream bubbles)
    ///   • Force scale = 1 on everything (fractional scale = blur)
    ///   • Center all button labels properly
    /// </summary>
    public static class SparqSharpenChat184
    {
        [MenuItem("Sparq/184. Sharpen chat text + center buttons")]
        public static void Apply()
        {
            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            // 1. Force unit scale on every RectTransform inside SocialPanel
            int scaleFix = 0;
            foreach (var rt in social.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt == null) continue;
                if (rt.localScale != Vector3.one)
                {
                    rt.localScale = Vector3.one;
                    scaleFix++;
                }
            }

            // 2. Sharpen + recolor chat message text
            var msgList = social.transform.Find("Card/Content/Chat_Tab/Scroll/Viewport/List");
            int textFix = 0;
            if (msgList != null)
            {
                for (int i = 0; i < msgList.childCount; i++)
                {
                    var row = msgList.GetChild(i);
                    var bubble = row.Find("Bubble");
                    if (bubble == null) continue;

                    // Make bubble big enough for bigger text
                    var brt = bubble.GetComponent<RectTransform>();
                    var le  = row.GetComponent<LayoutElement>();
                    if (le != null) le.preferredHeight = 78;

                    var auth = bubble.Find("Author");
                    if (auth != null)
                    {
                        var atm = auth.GetComponent<TMP_Text>();
                        if (atm != null)
                        {
                            atm.color = new Color(0.55f, 0.30f, 0.05f);  // dark amber, readable
                            atm.fontSize = 16;                            // was 12 (blurry small)
                            atm.fontStyle = FontStyles.Bold;
                            if (atm.font == null) atm.font = TMP_Settings.defaultFontAsset;
                            var art = auth.GetComponent<RectTransform>();
                            if (art != null)
                            {
                                art.anchoredPosition = new Vector2(0, -4);
                                art.sizeDelta = new Vector2(-16, 22);
                            }
                            textFix++;
                        }
                    }

                    var body = bubble.Find("Body");
                    if (body != null)
                    {
                        var btm = body.GetComponent<TMP_Text>();
                        if (btm != null)
                        {
                            btm.color = Color.black;     // solid black = max contrast
                            btm.fontSize = 22;            // bigger so it doesn't render blurry
                            btm.fontStyle = FontStyles.Normal;
                            if (btm.font == null) btm.font = TMP_Settings.defaultFontAsset;
                            var bdrt = body.GetComponent<RectTransform>();
                            if (bdrt != null)
                            {
                                bdrt.anchoredPosition = new Vector2(0, -8);
                                bdrt.sizeDelta = new Vector2(-24, -28);
                            }
                            textFix++;
                        }
                    }
                }
            }

            // 3. Center every Button label (Lbl child) properly
            int btnFix = 0;
            foreach (var btn in social.GetComponentsInChildren<Button>(true))
            {
                if (btn == null) continue;
                var lbl = btn.transform.Find("Lbl");
                if (lbl == null) continue;
                var lrt = lbl.GetComponent<RectTransform>();
                if (lrt != null)
                {
                    lrt.anchorMin = Vector2.zero;
                    lrt.anchorMax = Vector2.one;
                    lrt.pivot = new Vector2(0.5f, 0.5f);
                    lrt.offsetMin = Vector2.zero;
                    lrt.offsetMax = Vector2.zero;
                    lrt.localScale = Vector3.one;
                }
                foreach (var tm in lbl.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.alignment = TextAlignmentOptions.Center;
                    if (tm.font == null) tm.font = TMP_Settings.defaultFontAsset;
                }
                btnFix++;
            }

            // 4. Update typed-text color too (input field)
            var input = social.transform.Find("Card/Content/Chat_Tab/InputBar/Input")?.GetComponent<TMP_InputField>();
            if (input != null && input.textComponent != null)
            {
                input.textComponent.color = Color.black;
                input.textComponent.fontSize = 22;
                if (input.textComponent.font == null) input.textComponent.font = TMP_Settings.defaultFontAsset;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ Sharpened:\n• {scaleFix} RectTransform scales reset to 1 (fractional scale = blur)\n• {textFix} chat text element(s) → black/22pt body, dark amber 16pt author\n• {btnFix} button label(s) centered properly\n• Typed input text → black 22pt\n\nHit ▶ Play.", "OK");
        }
    }
}
