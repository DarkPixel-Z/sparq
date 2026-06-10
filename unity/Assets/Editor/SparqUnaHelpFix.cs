using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqUnaHelpFix
    {
        [MenuItem("Sparq/101. REBUILD help icon with Una mage visible")]
        public static void Apply()
        {
            string unaPath = "Assets/Art/Sparq/una-mage.png";

            // Force re-import as Sprite
            var imp = AssetImporter.GetAtPath(unaPath) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.maxTextureSize = 2048;
                imp.SaveAndReimport();
            }
            var unaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(unaPath);
            if (unaSprite == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    "una-mage.png not found at:\n" + unaPath +
                    "\n\nMake sure file is at Assets/Art/Sparq/una-mage.png", "OK");
                return;
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Wipe old help icon completely
            var old = GameObject.Find("HelpIcon");
            if (old != null) Object.DestroyImmediate(old);

            // Build fresh: Una as the MAIN visual (no opaque background covering her)
            var help = new GameObject("HelpIcon", typeof(RectTransform), typeof(Image), typeof(Button));
            help.transform.SetParent(canvas.transform, false);
            var rt = help.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(14f, 96f);
            rt.sizeDelta = new Vector2(110, 110);

            // The MAIN image IS Una herself (no separate child needed)
            var img = help.GetComponent<Image>();
            img.sprite = unaSprite;
            img.preserveAspect = true;
            img.color = Color.white;

            // Wire button to open help popup
            var btn = help.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                Sparq.UI.HelpPopup.Show();
            });

            // Small "?" badge in TOP-RIGHT corner so it's clearly a help button
            var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(help.transform, false);
            var brt = badge.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(1f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(4f, 4f);
            brt.sizeDelta = new Vector2(34, 34);
            badge.GetComponent<Image>().color = new Color(1f, 0.85f, 0.35f, 0.95f);
            badge.GetComponent<Image>().raycastTarget = false;

            var qGO = new GameObject("Q", typeof(RectTransform));
            qGO.transform.SetParent(badge.transform, false);
            var qrt = qGO.GetComponent<RectTransform>();
            qrt.anchorMin = Vector2.zero; qrt.anchorMax = Vector2.one;
            qrt.offsetMin = Vector2.zero; qrt.offsetMax = Vector2.zero;
            var qtm = qGO.AddComponent<TextMeshProUGUI>();
            qtm.text = "?";
            qtm.fontSize = 24;
            qtm.fontStyle = FontStyles.Bold;
            qtm.alignment = TextAlignmentOptions.Center;
            qtm.color = new Color(0.05f, 0.02f, 0.10f);
            qtm.raycastTarget = false;

            // Subtle pulse
            help.AddComponent<Pulse>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Help icon rebuilt:\n\n" +
                "• Una mage IS the main image (no covering plate)\n" +
                "• 110×110 — bigger so the chibi reads\n" +
                "• Yellow '?' badge in TOP-RIGHT corner\n" +
                "• Tap → Help popup\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private class Pulse : MonoBehaviour
        {
            float t;
            void Update()
            {
                t += Time.deltaTime;
                float s = 1f + Mathf.Sin(t * 2f) * 0.05f;
                transform.localScale = new Vector3(s, s, 1);
            }
        }
    }
}
