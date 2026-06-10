using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Swap the XP bar color by replacing the slider prefab.
    /// Preserves position, size, and rewires the XPBarDisplay controller.
    /// </summary>
    public static class SparqRecolorXPBar
    {
        private const string GREEN_PATH  = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_Component_Sliders/Slider_Border_Tapered_01_Green.prefab";
        private const string PURPLE_PATH = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_Component_Sliders/Slider_Border_Tapered_01_Purple.prefab";
        private const string BLUE_PATH   = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_Component_Sliders/Slider_Border_Tapered_01_Blue.prefab";
        private const string ORANGE_PATH = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_Component_Sliders/Slider_Border_Tapered_01_Orange.prefab";
        private const string MINT_PATH   = "Assets/Layer Lab/GUI Pro-FantasyHero/Prefabs/Prefabs_Component_Sliders/Slider_Border_Tapered_01_Mint.prefab";

        [MenuItem("Sparq/31. XP Bar → Green")]
        public static void Green() => Recolor(GREEN_PATH, "Green");
        [MenuItem("Sparq/31a. XP Bar → Purple")]
        public static void Purple() => Recolor(PURPLE_PATH, "Purple");
        [MenuItem("Sparq/31b. XP Bar → Blue")]
        public static void Blue() => Recolor(BLUE_PATH, "Blue");
        [MenuItem("Sparq/31c. XP Bar → Orange")]
        public static void Orange() => Recolor(ORANGE_PATH, "Orange");
        [MenuItem("Sparq/31d. XP Bar → Mint")]
        public static void Mint() => Recolor(MINT_PATH, "Mint");

        private static void Recolor(string prefabPath, string label)
        {
            var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (newPrefab == null)
            {
                EditorUtility.DisplayDialog("Sparq XP Bar",
                    $"Prefab not found:\n{prefabPath}", "OK");
                return;
            }

            // Find the existing FantasyXPBar
            var oldBar = GameObject.Find("FantasyXPBar");
            if (oldBar == null)
            {
                EditorUtility.DisplayDialog("Sparq XP Bar",
                    "No FantasyXPBar in scene. Run Sparq → 11 first.", "OK");
                return;
            }

            // Capture old transform + parent
            var rt = oldBar.GetComponent<RectTransform>();
            var parent = oldBar.transform.parent;
            var anchPos = rt.anchoredPosition;
            var sizeDelta = rt.sizeDelta;
            var scale = oldBar.transform.localScale;
            var anchMin = rt.anchorMin;
            var anchMax = rt.anchorMax;
            var pivot = rt.pivot;

            // Capture displayed level/xp text references inside the old bar
            string lvlText = "Lv.1";
            string xpText  = "0 / 100 XP";
            foreach (var tmp in oldBar.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (tmp == null) continue;
                if (tmp.text != null && tmp.text.StartsWith("Lv")) lvlText = tmp.text;
                else if (tmp.text != null && tmp.text.Contains("XP")) xpText = tmp.text;
            }

            // Instantiate the new colored slider as a sibling
            var newBar = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab, parent);
            newBar.name = "FantasyXPBar";
            var newRT = newBar.GetComponent<RectTransform>();
            newRT.anchorMin = anchMin;
            newRT.anchorMax = anchMax;
            newRT.pivot = pivot;
            newRT.anchoredPosition = anchPos;
            newRT.sizeDelta = sizeDelta;
            newBar.transform.localScale = scale;

            // Add the XPBarDisplay controller
            var ctrl = newBar.GetComponent<Sparq.UI.XPBarDisplay>();
            if (ctrl == null) ctrl = newBar.AddComponent<Sparq.UI.XPBarDisplay>();

            // Find the slider's fill image and the level/xp texts inside the new prefab
            var slider = newBar.GetComponent<UnityEngine.UI.Slider>() ?? newBar.GetComponentInChildren<UnityEngine.UI.Slider>(true);
            Image fillImg = null;
            if (slider != null && slider.fillRect != null)
                fillImg = slider.fillRect.GetComponent<Image>();

            TMPro.TMP_Text lvlTmp = null, xpTmp = null;
            foreach (var tmp in newBar.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (tmp == null) continue;
                if (lvlTmp == null && tmp.text != null && (tmp.text.StartsWith("Lv") || tmp.text.Length < 6))
                {
                    lvlTmp = tmp;
                    tmp.text = lvlText;
                }
                else if (xpTmp == null)
                {
                    xpTmp = tmp;
                    tmp.text = xpText;
                }
            }

            // Wire the controller's private fields via reflection
            var so = new SerializedObject(ctrl);
            if (fillImg != null) so.FindProperty("fillImage").objectReferenceValue = fillImg;
            if (lvlTmp != null) so.FindProperty("levelText").objectReferenceValue = lvlTmp;
            if (xpTmp  != null) so.FindProperty("xpText").objectReferenceValue   = xpTmp;
            so.ApplyModifiedProperties();

            // Remove the old bar
            Object.DestroyImmediate(oldBar);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[Sparq] XP bar swapped to {label}.");
            EditorUtility.DisplayDialog("Sparq XP Bar",
                $"✅ XP bar is now {label}.\n\n" +
                "Hit ▶ Play to see it in action.", "OK");
        }
    }
}
