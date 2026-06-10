using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqRecolorSky
    {
        [MenuItem("Sparq/42. Sky → Lighter Purple")]
        public static void Light() => Recolor(
            top:    new Color(0.65f, 0.45f, 0.85f), // soft lavender
            middle: new Color(0.55f, 0.35f, 0.75f), // lilac
            bottom: new Color(0.30f, 0.18f, 0.50f)  // dusk plum
        );

        [MenuItem("Sparq/42a. Sky → Original Deep Purple")]
        public static void Deep() => Recolor(
            top:    new Color(0.32f, 0.12f, 0.45f),
            middle: new Color(0.18f, 0.08f, 0.35f),
            bottom: new Color(0.06f, 0.04f, 0.18f)
        );

        [MenuItem("Sparq/42b. Sky → Twilight Pink")]
        public static void Pink() => Recolor(
            top:    new Color(0.95f, 0.65f, 0.85f), // soft pink
            middle: new Color(0.70f, 0.45f, 0.80f),
            bottom: new Color(0.40f, 0.20f, 0.50f)
        );

        [MenuItem("Sparq/42c. Sky → Sunset Magenta")]
        public static void Sunset() => Recolor(
            top:    new Color(0.75f, 0.35f, 0.70f),
            middle: new Color(0.85f, 0.50f, 0.45f),
            bottom: new Color(0.50f, 0.20f, 0.40f)
        );

        private static void Recolor(Color top, Color middle, Color bottom)
        {
            var sky = GameObject.Find("SkyGradient");
            if (sky == null)
            {
                EditorUtility.DisplayDialog("Sparq Sky",
                    "SkyGradient not found. Run Sparq → 15 (Cinematic Scene) first.", "OK");
                return;
            }
            var img = sky.GetComponent<RawImage>();
            if (img == null) return;

            img.texture = MakeVerticalGradient(top, middle, bottom);

            // Also re-tint the camera background so PPv2 fadeouts use it
            if (Camera.main != null)
                Camera.main.backgroundColor = bottom;

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Sparq Sky",
                "✅ Sky recolored.\n\nHit ▶ Play to see it.", "OK");
        }

        private static Texture2D MakeVerticalGradient(Color top, Color middle, Color bottom)
        {
            const int h = 256;
            var tex = new Texture2D(1, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color c = (t < 0.5f)
                    ? Color.Lerp(bottom, middle, t * 2f)
                    : Color.Lerp(middle, top, (t - 0.5f) * 2f);
                tex.SetPixel(0, y, c);
            }
            tex.Apply();
            return tex;
        }
    }
}
