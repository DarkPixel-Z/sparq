using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    public static class SparqWispHUDFix
    {
        [MenuItem("Sparq/120. Fix HUD: Wisp image + matching row sizes")]
        public static void Apply()
        {
            string wispPath = "Assets/2D Fantasy Monster Sprite Pack/Monsters/Wisp/Magic-Wisp.png";

            // Force wisp sprite import
            var imp = AssetImporter.GetAtPath(wispPath) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            var wispSprite = AssetDatabase.LoadAssetAtPath<Sprite>(wispPath);

            var hud = GameObject.Find("PlayerHUD");
            if (hud == null) return;

            // Replace the avatar in MochiRow with Wisp sprite
            var mochiRow = hud.transform.Find("MochiRow");
            if (mochiRow != null && wispSprite != null)
            {
                foreach (var img in mochiRow.GetComponentsInChildren<Image>(true))
                {
                    if (img != null && img.gameObject.name == "Avatar")
                    {
                        img.sprite = wispSprite;
                        img.color = Color.white;
                        img.preserveAspect = true;
                        break;
                    }
                }
                // Update name text to "Wisp"
                var name = mochiRow.Find("Name");
                if (name != null)
                {
                    var tm = name.GetComponent<TMP_Text>();
                    if (tm != null) tm.text = "Wisp";
                }
            }

            // Now force BOTH rows to same exact dimensions (Karu and Wisp/Mochi)
            ForceRow(hud.transform.Find("KaruRow"));
            ForceRow(hud.transform.Find("MochiRow"));

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ HUD fixed:\n\n" +
                "• Wisp image (Magic-Wisp.png) in place of axolotl\n" +
                "• Wisp name displayed\n" +
                "• Both rows forced to same height + same avatar size\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void ForceRow(Transform row)
        {
            if (row == null) return;
            var rt = row.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = new Vector2(-16, 90);

            // Avatar BG → 70×70
            var avBg = row.Find("AvatarBg");
            if (avBg != null)
            {
                var arrt = avBg.GetComponent<RectTransform>();
                arrt.anchoredPosition = new Vector2(8, 0);
                arrt.sizeDelta = new Vector2(70, 70);
            }

            // Name → 24pt
            foreach (var name in row.GetComponentsInChildren<Transform>(true))
            {
                if (name == null) continue;
                if (name.name == "Name")
                {
                    var nrt = name.GetComponent<RectTransform>();
                    if (nrt != null)
                    {
                        nrt.anchoredPosition = new Vector2(86, 18);
                        nrt.sizeDelta = new Vector2(220, 32);
                    }
                    foreach (var tm in name.GetComponentsInChildren<TMP_Text>(true))
                    {
                        tm.fontSize = 24;
                        tm.fontStyle = FontStyles.Bold;
                    }
                }
                if (name.name == "Level")
                {
                    var lrt = name.GetComponent<RectTransform>();
                    if (lrt != null)
                    {
                        lrt.anchoredPosition = new Vector2(86, -16);
                        lrt.sizeDelta = new Vector2(60, 24);
                    }
                }
                if (name.name == "Subtitle")
                {
                    var srt = name.GetComponent<RectTransform>();
                    if (srt != null)
                    {
                        srt.anchoredPosition = new Vector2(156, -16);
                        srt.sizeDelta = new Vector2(220, 22);
                    }
                }
            }
        }
    }
}
