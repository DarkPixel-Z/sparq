using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Swap Volt's placeholder SVG for a real monster sprite
    /// (Darkness-Hellhound from the 2D Fantasy Monster Sprite Pack).
    /// </summary>
    public static class SparqMonsterSwap
    {
        private const string HELLHOUND_PATH =
            "Assets/2D Fantasy Monster Sprite Pack/Monsters/Hellhound/Darkness-Hellhound.png";

        [MenuItem("Sparq/14. Swap Volt Portrait → Darkness Hellhound")]
        public static void SwapVoltToHellhound()
        {
            // Load sprite
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HELLHOUND_PATH);
            if (sprite == null)
            {
                // The PNG might be imported as Default (texture), not Sprite. Force re-import as Sprite.
                var importer = AssetImporter.GetAtPath(HELLHOUND_PATH) as TextureImporter;
                if (importer == null)
                {
                    EditorUtility.DisplayDialog("Sparq Monsters",
                        "Couldn't find Darkness-Hellhound.png at:\n" + HELLHOUND_PATH, "OK");
                    return;
                }
                importer.textureType     = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode      = FilterMode.Bilinear;
                importer.mipmapEnabled   = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HELLHOUND_PATH);
            }

            if (sprite == null)
            {
                EditorUtility.DisplayDialog("Sparq Monsters",
                    "Re-imported but still couldn't load a Sprite from:\n" + HELLHOUND_PATH, "OK");
                return;
            }

            // Find VoltPortrait
            var portraitGO = GameObject.Find("VoltPortrait");
            if (portraitGO == null)
            {
                EditorUtility.DisplayDialog("Sparq Monsters",
                    "VoltPortrait not found. Run Sparq → 12 (Add Rival Card) first.", "OK");
                return;
            }

            var img = portraitGO.GetComponent<Image>();
            if (img == null)
            {
                EditorUtility.DisplayDialog("Sparq Monsters",
                    "VoltPortrait has no Image component.", "OK");
                return;
            }

            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = Color.white;

            // Bump size a touch so the monster reads better
            var rt = portraitGO.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(130, 130);
            }

            // Also rename the card's title to suit the new sprite (optional flavor)
            var titleGO = GameObject.Find("RivalTitle");
            if (titleGO != null)
            {
                var tmp = titleGO.GetComponent<TMP_Text>();
                if (tmp != null) tmp.text = "Shadow Hellhound";
            }

            EditorUtility.SetDirty(portraitGO);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Sparq Monsters] Volt portrait → Darkness-Hellhound swapped.");
            EditorUtility.DisplayDialog("Sparq Monsters",
                "✅ Volt is now wearing the Darkness-Hellhound skin.\n\n" +
                "• Portrait swapped to Darkness-Hellhound.png\n" +
                "• Title updated to 'Shadow Hellhound'\n" +
                "• Size bumped to 130×130\n\n" +
                "Hit ▶ Play to see him growl.", "OK");
        }
    }
}
