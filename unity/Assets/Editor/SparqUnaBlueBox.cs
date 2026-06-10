using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqUnaBlueBox
    {
        [MenuItem("Sparq/103. Una over BLUE help box")]
        public static void Apply()
        {
            string unaPath = "Assets/Art/Sparq/una-mage.png";
            var imp = AssetImporter.GetAtPath(unaPath) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            var unaSprite = AssetDatabase.LoadAssetAtPath<Sprite>(unaPath);

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Wipe ALL existing help icons
            var allHelps = GameObject.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var rt in allHelps)
            {
                if (rt != null && rt.gameObject.name == "HelpIcon")
                    Object.DestroyImmediate(rt.gameObject);
            }

            // Build fresh: BLUE box → Una on top → "?" badge
            var help = new GameObject("HelpIcon", typeof(RectTransform), typeof(Image), typeof(Button));
            help.transform.SetParent(canvas.transform, false);
            var hrt = help.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0f);
            hrt.anchorMax = new Vector2(0f, 0f);
            hrt.pivot = new Vector2(0f, 0f);
            hrt.anchoredPosition = new Vector2(14f, 96f);
            hrt.sizeDelta = new Vector2(110, 110);

            // Blue background box
            var bg = help.GetComponent<Image>();
            bg.color = new Color(0.30f, 0.55f, 0.95f, 0.95f);  // mid blue

            // Una sprite ON TOP of the blue (single child Image)
            if (unaSprite != null)
            {
                var unaGO = new GameObject("Una", typeof(RectTransform), typeof(Image));
                unaGO.transform.SetParent(help.transform, false);
                var urt = unaGO.GetComponent<RectTransform>();
                urt.anchorMin = Vector2.zero; urt.anchorMax = Vector2.one;
                urt.offsetMin = new Vector2(2, 2); urt.offsetMax = new Vector2(-2, -2);
                var uimg = unaGO.GetComponent<Image>();
                uimg.sprite = unaSprite;
                uimg.preserveAspect = true;
                uimg.color = Color.white;
                uimg.raycastTarget = false;
            }

            // Tap = open help
            var btn = help.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() =>
            {
                Sparq.Audio.SoundManager.Play(Sparq.Audio.SoundManager.Sfx.Click);
                Sparq.UI.HelpPopup.Show();
            });

            // "?" badge in top-right
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

            help.AddComponent<Pulse>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Una over blue help box.\n\n" +
                "• Blue background plate (110×110)\n" +
                "• Una mage sprite ON TOP of the blue\n" +
                "• Yellow '?' badge top-right\n" +
                "• Pulse animation\n\n" +
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
