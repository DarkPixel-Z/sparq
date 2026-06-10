using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqFantasyPet
    {
        private const string MONSTER_DIR = "Assets/2D Fantasy Monster Sprite Pack/Monsters/";

        [MenuItem("Sparq/116. Pet → Magic Wisp (right of hero)")]
        public static void Wisp() => Apply(MONSTER_DIR + "Wisp/Magic-Wisp.png", "Wisp", 0.85f);

        [MenuItem("Sparq/116a. Pet → Playful Pup")]
        public static void Pup() => Apply(MONSTER_DIR + "Pup/Playful-Pup.png", "Pup", 0.95f);

        [MenuItem("Sparq/116b. Pet → Arctic Pup (icy)")]
        public static void Arctic() => Apply(MONSTER_DIR + "Pup/Arctic-Pup.png", "Arctic Pup", 0.95f);

        [MenuItem("Sparq/116c. Pet → Hellhound Pup (fire)")]
        public static void Hellhound() => Apply(MONSTER_DIR + "Pup/Hellhound-Pup.png", "Lil Hellhound", 0.95f);

        [MenuItem("Sparq/116d. Pet → Aquatic Wisp")]
        public static void Aquatic() => Apply(MONSTER_DIR + "Wisp/Aquatic-Wisp.png", "Aquatic Wisp", 0.85f);

        [MenuItem("Sparq/116e. Pet → Flare Wisp")]
        public static void Flare() => Apply(MONSTER_DIR + "Wisp/Flare-Wisp.png", "Flare Wisp", 0.85f);

        [MenuItem("Sparq/116f. Pet → Chick")]
        public static void Chick() => Apply(MONSTER_DIR + "Chick/Chick.png", "Chick", 0.85f);

        [MenuItem("Sparq/116g. Pet → Mouse")]
        public static void Mouse() => Apply(MONSTER_DIR + "Mouse/Mouse.png", "Mouse", 0.85f);

        private static void Apply(string spritePath, string petName, float scale)
        {
            // Force sprite import
            var imp = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                EditorUtility.DisplayDialog("Sparq",
                    $"Sprite not found:\n{spritePath}", "OK");
                return;
            }

            // Replace Mochi's sprite + reposition to RIGHT of Karu
            var mochi = GameObject.Find("Mochi");
            if (mochi == null)
            {
                mochi = new GameObject("Mochi");
                mochi.AddComponent<SpriteRenderer>();
            }
            var sr = mochi.GetComponent<SpriteRenderer>();
            if (sr == null) sr = mochi.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingOrder = 49;
            sr.enabled = true;

            // Remove any pixel-art-PNG-specific UISpriteAnimator if present (for animated monsters earlier)
            var animator = mochi.GetComponent<Sparq.UI.UISpriteAnimator>();
            if (animator != null) Object.DestroyImmediate(animator);

            // Position to the RIGHT of Karu
            var karu = GameObject.Find("Karu");
            float karuX = karu != null ? karu.transform.position.x : -3.0f;
            mochi.transform.position = new Vector3(karuX + 2.5f, -1.8f, 0f);
            mochi.transform.localScale = Vector3.one * scale;

            // Update Mochi name in the HUD strip if present
            var hud = GameObject.Find("PlayerHUD");
            if (hud != null)
            {
                var nameGO = hud.transform.Find("MochiRow/Name");
                if (nameGO != null)
                {
                    var tm = nameGO.GetComponent<TMPro.TMP_Text>();
                    if (tm != null) tm.text = petName;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Fantasy Pet",
                $"✅ Pet swapped to {petName}.\n\n" +
                $"• Sprite: {System.IO.Path.GetFileName(spritePath)}\n" +
                $"• Position: right of Karu (x={karuX + 2.5f:F1}, y=-1.8)\n" +
                $"• Scale: {scale}\n\n" +
                "Hit ▶ Play.\n\nNot the right pet? Try other 116a-116g menus.", "OK");
        }
    }
}
