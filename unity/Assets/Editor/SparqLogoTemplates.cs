using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Try each of the 12 PSD Logo Template PNGs as the home logo to preview style.
    /// User picks the one they like, then opens that PSD in Photoshop to type "Sparq".
    /// </summary>
    public static class SparqLogoTemplates
    {
        private const string PNG_DIR = "Assets/PSD Logo Templates/PNG/";

        [MenuItem("Sparq/70. Logo template → 01 Mutants (chunky comic)")]   public static void T01() => Apply("01_Mutants.png");
        [MenuItem("Sparq/70a. Logo template → 02 SpeedKart (racing bold)")] public static void T02() => Apply("02_SpeedKart.png");
        [MenuItem("Sparq/70b. Logo template → 03 PuzzlePieces (playful)")]  public static void T03() => Apply("03_PuzzlePieces.png");
        [MenuItem("Sparq/70c. Logo template → 04 PirateShip (adventure)")]  public static void T04() => Apply("04_PirateShip.png");
        [MenuItem("Sparq/70d. Logo template → 05 RetroArcade (80s neon)")]  public static void T05() => Apply("05_RetroArcade.png");
        [MenuItem("Sparq/70e. Logo template → 06 SuperBaseball (sport)")]   public static void T06() => Apply("06_SuperBaseball.png");
        [MenuItem("Sparq/70f. Logo template → 07 FinalEpisode (cinematic)")] public static void T07() => Apply("07_FinalEpisode.png");
        [MenuItem("Sparq/70g. Logo template → 08 KungFusion (martial)")]    public static void T08() => Apply("08_KungFusion.png");
        [MenuItem("Sparq/70h. Logo template → 09 KingsGuard (medieval)")]   public static void T09() => Apply("09_KingsGuard.png");
        [MenuItem("Sparq/70i. Logo template → 10 RoboticRevolt (sci-fi)")]  public static void T10() => Apply("10_RoboticRevolt.png");
        [MenuItem("Sparq/70j. Logo template → 11 Bedtime (cozy/cute)")]     public static void T11() => Apply("11_Bedtime.png");
        [MenuItem("Sparq/70k. Logo template → 12 ComicBook (POW! style)")]  public static void T12() => Apply("12_ComicBook.png");

        // Also: when user has a custom Sparq.png, this menu wires it
        [MenuItem("Sparq/71. Logo from custom PNG (Assets/Art/Sparq/sparq-logo.png)")]
        public static void Custom() => ApplyPath("Assets/Art/Sparq/sparq-logo.png");

        // ── Color tints for the Comic Book logo ──────────────────────────────
        [MenuItem("Sparq/72. ComicBook logo → Hot Pink")]
        public static void TintPink() => Tint(new Color(1f, 0.45f, 0.78f));

        [MenuItem("Sparq/72a. ComicBook logo → Electric Cyan")]
        public static void TintCyan() => Tint(new Color(0.40f, 0.95f, 1f));

        [MenuItem("Sparq/72b. ComicBook logo → Mint Green")]
        public static void TintMint() => Tint(new Color(0.45f, 1f, 0.65f));

        [MenuItem("Sparq/72c. ComicBook logo → Sunset Orange")]
        public static void TintOrange() => Tint(new Color(1f, 0.55f, 0.20f));

        [MenuItem("Sparq/72d. ComicBook logo → Royal Purple")]
        public static void TintPurple() => Tint(new Color(0.75f, 0.40f, 1f));

        [MenuItem("Sparq/72e. ComicBook logo → Crimson")]
        public static void TintCrimson() => Tint(new Color(1f, 0.35f, 0.40f));

        [MenuItem("Sparq/72f. ComicBook logo → Pure White (no tint)")]
        public static void TintWhite() => Tint(Color.white);

        private static void Tint(Color c)
        {
            var go = GameObject.Find("GameTitle");
            if (go == null)
            {
                EditorUtility.DisplayDialog("Sparq Logo",
                    "GameTitle not found. Run a 70.x template menu first.", "OK");
                return;
            }
            var img = go.GetComponent<Image>();
            if (img == null) return;
            img.color = c;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq Logo", "✅ Tinted.\n\nHit ▶ Play.", "OK");
        }

        private static void Apply(string fileName) => ApplyPath(PNG_DIR + fileName);

        private static void ApplyPath(string path)
        {
            // Ensure imported as Sprite
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single))
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                EditorUtility.DisplayDialog("Sparq Logo",
                    $"Couldn't load sprite at:\n{path}", "OK");
                return;
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Wipe old logo
            var old = GameObject.Find("GameTitle");
            if (old != null) Object.DestroyImmediate(old);

            // Build new — single Image with the logo PNG
            var go = new GameObject("GameTitle", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(20f, -16f);
            rt.sizeDelta = new Vector2(290, 110);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Logo",
                $"✅ Logo set to template:\n{System.IO.Path.GetFileName(path)}\n\n" +
                "This is a STYLE PREVIEW — text says the template's name, not 'Sparq'.\n\n" +
                "When you pick the style you want:\n" +
                "1. Open the matching PSD in Photoshop\n" +
                "2. Edit the text layer to say 'Sparq'\n" +
                "3. Export PNG to: Assets/Art/Sparq/sparq-logo.png\n" +
                "4. Run Sparq → 71 to wire it in", "OK");
        }
    }
}
