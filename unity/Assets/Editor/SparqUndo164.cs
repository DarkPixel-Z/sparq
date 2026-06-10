using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 165: Undo #164 — strip the Button_01 sprites off SocialPanel tabs.
    /// Returns them to flat colored rectangles, leaves home top buttons alone.
    /// After this, re-run #135 → #143 → #155 to rebuild home top buttons if
    /// they look wrong.
    /// </summary>
    public static class SparqUndo164
    {
        private static readonly Color GOLD     = new Color(1f, 0.82f, 0.32f);
        private static readonly Color CREAM    = new Color(1f, 0.95f, 0.82f);
        private static readonly Color INACT_BG = new Color(1f, 1f, 1f, 0.10f);
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/165. Undo 164 — restore flat SocialPanel tabs")]
        public static void Apply()
        {
            GameObject social = null;
            foreach (var go in Object.FindObjectsByType<GameObject>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go != null && go.name == "SocialPanel") { social = go; break; }
            }
            if (social == null) { EditorUtility.DisplayDialog("Sparq", "SocialPanel not found.", "OK"); return; }

            // Remove the sprite-swap component so it stops touching things
            foreach (var s in social.GetComponents<Sparq.UI.TabSpriteSwap>())
                Object.DestroyImmediate(s);

            var tabs = social.transform.Find("Tabs");
            if (tabs == null) return;

            for (int i = 0; i < tabs.childCount; i++)
            {
                var tab = tabs.GetChild(i);
                var img = tab.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = null;
                    img.type = Image.Type.Simple;
                    img.color = (i == 0) ? GOLD : INACT_BG;
                }
                foreach (var tm in tab.GetComponentsInChildren<TMP_Text>(true))
                {
                    tm.fontSize = 22;
                    tm.fontStyle = FontStyles.Bold;
                    tm.color = (i == 0) ? DEEP_NAVY : CREAM;
                    tm.outlineWidth = 0;
                }
            }

            // Restore TabGroup colors to original
            var tg = social.GetComponent<Sparq.UI.TabGroup>();
            if (tg != null)
            {
                var so = new SerializedObject(tg);
                so.FindProperty("activeBg").colorValue   = GOLD;
                so.FindProperty("inactiveBg").colorValue = INACT_BG;
                so.FindProperty("activeFg").colorValue   = DEEP_NAVY;
                so.FindProperty("inactiveFg").colorValue = CREAM;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Reset tab row height
            var trt = tabs.GetComponent<RectTransform>();
            if (trt != null) trt.sizeDelta = new Vector2(trt.sizeDelta.x, 76);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ #164 undone.\n\n" +
                "• SocialPanel tabs back to flat color\n" +
                "• Home top buttons untouched by this script\n\n" +
                "If home top buttons still look wrong, re-run:\n" +
                "  Sparq → 135 (top fantasy buttons)\n" +
                "  Sparq → 143 (add WORLD button)\n" +
                "  Sparq → 155 (Earth icon for WORLD)\n" +
                "  Sparq → 158 (rewire WORLD → SocialPanel)", "OK");
        }
    }
}
