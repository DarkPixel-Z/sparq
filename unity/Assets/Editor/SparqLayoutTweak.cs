using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqLayoutTweak
    {
        [MenuItem("Sparq/38c. Tighten quest box (avoid covering tree)")]
        public static void TightenQuests()
        {
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql == null)
            {
                EditorUtility.DisplayDialog("Sparq", "Quest list not found.", "OK");
                return;
            }
            var qrt = ql.GetComponent<RectTransform>();
            qrt.anchorMin = new Vector2(1f, 1f);
            qrt.anchorMax = new Vector2(1f, 1f);
            qrt.pivot     = new Vector2(1f, 1f);
            // Tuck closer to rival card, smaller footprint
            qrt.anchoredPosition = new Vector2(-12f, -240f);
            qrt.sizeDelta = new Vector2(380, 280);

            // Slight transparency on the quest BG so the forest peeks through
            var bgImg = ql.GetComponent<UnityEngine.UI.Image>();
            if (bgImg != null)
            {
                var c = bgImg.color;
                c.a = Mathf.Min(c.a, 0.78f);
                bgImg.color = c;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq",
                "✅ Quest box tightened.\n\n" +
                "• Tucked closer under rival card\n" +
                "• Slightly smaller (380×280)\n" +
                "• 78% opacity — forest peeks through\n\n" +
                "Hit ▶ Play.", "OK");
        }

        [MenuItem("Sparq/38b. Move quests to top-RIGHT (under rival card)")]
        public static void TweakRight()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var nav = GameObject.Find("HomeNavButtons");
            if (nav != null)
            {
                var rt = nav.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot     = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(14f, -180f);
            }

            // Quest list → top-right, under the rival card
            var ql = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (ql != null)
            {
                var qrt = ql.GetComponent<RectTransform>();
                qrt.anchorMin = new Vector2(1f, 1f);
                qrt.anchorMax = new Vector2(1f, 1f);
                qrt.pivot     = new Vector2(1f, 1f);
                qrt.anchoredPosition = new Vector2(-20f, -260f); // sit just below rival card
                qrt.sizeDelta = new Vector2(420, 320);
            }

            // Karu — center-bottom hero pose
            var karu = GameObject.Find("Karu");
            if (karu != null && karu.activeSelf)
            {
                karu.transform.position = new Vector3(0f, -0.6f, 0f);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq Layout",
                "✅ Quests now top-RIGHT, under the rival card.\n\n" +
                "• Nav (MAP/SHOP/BAG): top-left\n" +
                "• Quests: top-right\n" +
                "• Rival card: above the quests\n" +
                "• Karu: center-bottom hero shot\n\n" +
                "Hit ▶ Play.", "OK");
        }

        [MenuItem("Sparq/38. Tweak layout (quests + nav up)")]
        public static void Tweak()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Sparq Layout", "No Canvas in scene.", "OK");
                return;
            }

            // Push HomeNavButtons column up-left (was middle-left, y=120)
            var nav = GameObject.Find("HomeNavButtons");
            if (nav != null)
            {
                var rt = nav.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot     = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(14f, -180f); // top-left, just below HUD area
                }
            }

            // Push quest list up (keeping it visible but off Karu's body)
            var questListObj = Object.FindAnyObjectByType<Sparq.UI.QuestListUI>();
            if (questListObj != null)
            {
                var qrt = questListObj.GetComponent<RectTransform>();
                if (qrt != null)
                {
                    qrt.anchorMin = new Vector2(0.5f, 1f);
                    qrt.anchorMax = new Vector2(0.5f, 1f);
                    qrt.pivot     = new Vector2(0.5f, 1f);
                    qrt.anchoredPosition = new Vector2(-80f, -200f); // top-center, slightly left
                    qrt.sizeDelta = new Vector2(430, 330);
                }
            }

            // Move Karu down a bit so he's center-bottom (hero shot)
            var karu = GameObject.Find("Karu");
            if (karu != null && karu.activeSelf)
            {
                karu.transform.position = new Vector3(0f, -1.2f, 0f);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Layout",
                "✅ Layout tweaked.\n\n" +
                "• Nav buttons (MAP/SHOP/BAG) → top-left corner\n" +
                "• Quest list → top-center (slightly left)\n" +
                "• Karu → center-bottom (hero pose in forest)\n\n" +
                "Hit ▶ Play. You should see more of the forest + Karu now.", "OK");
        }
    }
}
