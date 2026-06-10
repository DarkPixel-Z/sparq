using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Sparq.Editor
{
    /// <summary>
    /// Menu 139: Stats card upgrade with real Layer Lab fantasy frames.
    /// • Stats card → BaseFrame_Border_Rectangle_H80 (Bg + Border + InnerBorder + Gradient)
    /// • Avatars → wrapped in BaseFrame_Border_Circle_H58
    /// • Gold pill slightly smaller
    /// </summary>
    public static class SparqStatsUpgrade139
    {
        private const string FH_FRAME = "Assets/Layer Lab/GUI Pro-FantasyHero/ResourcesData/Sptites/Components/Frame/";

        private static readonly Color CREAM     = new Color(1.00f, 0.95f, 0.82f);
        private static readonly Color DEEP_NAVY = new Color(0.10f, 0.08f, 0.18f);

        [MenuItem("Sparq/139. Stats card frame upgrade + smaller gold")]
        public static void Apply()
        {
            EnsureSprite(FH_FRAME + "BaseFrame_Border_Rectangle_H80_Bg.png");
            EnsureSprite(FH_FRAME + "BaseFrame_Border_Rectangle_H80_Border.png");
            EnsureSprite(FH_FRAME + "BaseFrame_Border_Rectangle_H80_InnerBorder.png");
            EnsureSprite(FH_FRAME + "BaseFrame_Border_Rectangle_H80_Gradient.png");
            EnsureSprite(FH_FRAME + "BaseFrame_Border_Circle_H106.png");

            ShrinkGoldPill();
            UpgradeStatsCardFrame();
            FrameAvatars();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                "✅ Stats card upgraded:\n\n" +
                "• Frame: real fantasy bordered rectangle (Bg + Border + InnerBorder + Gradient)\n" +
                "• Avatars wrapped in fantasy circle frame\n" +
                "• Gold pill slightly smaller\n\n" +
                "Hit ▶ Play.", "OK");
        }

        // ───────────────────── 1. Slight gold pill shrink ─────────────────────
        private static void ShrinkGoldPill()
        {
            var pill = GameObject.Find("GoldPill");
            if (pill == null) return;
            var le = pill.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth  = 102; // was 116
                le.preferredHeight = 44;  // was 48
            }
        }

        // ───────────────────── 2. Apply fantasy frame to stats card ─────────────────────
        private static void UpgradeStatsCardFrame()
        {
            var hud = GameObject.Find("PlayerHUD");
            if (hud == null) return;

            // Wipe any old frame layers we added before
            for (int i = hud.transform.childCount - 1; i >= 0; i--)
            {
                var c = hud.transform.GetChild(i);
                if (c.name == "FrameBg" || c.name == "FrameBorder"
                    || c.name == "FrameInnerBorder" || c.name == "FrameGradient")
                    Object.DestroyImmediate(c.gameObject);
            }

            // Replace HUD's primary background image with the fantasy frame Bg
            var hudImg = hud.GetComponent<Image>();
            if (hudImg == null) hudImg = hud.AddComponent<Image>();
            var bg = AssetDatabase.LoadAssetAtPath<Sprite>(FH_FRAME + "BaseFrame_Border_Rectangle_H80_Bg.png");
            if (bg != null) { hudImg.sprite = bg; hudImg.type = Image.Type.Sliced; hudImg.color = Color.white; }
            else hudImg.color = new Color(0.08f, 0.06f, 0.14f, 0.92f);
            hudImg.preserveAspect = false;

            // Add gradient layer (inserted as first child, behind contents but above the bg)
            AddOverlayLayer(hud.transform, "FrameGradient", FH_FRAME + "BaseFrame_Border_Rectangle_H80_Gradient.png", 0);
            // Border layer on top of contents (renders last so it's on top)
            // We'll add Border + InnerBorder as LAST siblings after content remains intact
            AddOverlayLayer(hud.transform, "FrameInnerBorder", FH_FRAME + "BaseFrame_Border_Rectangle_H80_InnerBorder.png", -1);
            AddOverlayLayer(hud.transform, "FrameBorder",      FH_FRAME + "BaseFrame_Border_Rectangle_H80_Border.png",      -1);
        }

        private static void AddOverlayLayer(Transform parent, string name, string spritePath, int siblingIndex)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) return;

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;

            if (siblingIndex >= 0) go.transform.SetSiblingIndex(siblingIndex);
            // siblingIndex == -1 → leaves it as last sibling (rendered on top)
        }

        // ───────────────────── 3. Avatars in fantasy circle frame ─────────────────────
        private static void FrameAvatars()
        {
            var circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FH_FRAME + "BaseFrame_Border_Circle_H106.png");
            if (circleSprite == null) return;

            FrameAvatar(GameObject.Find("PlayerHUD")?.transform.Find("KaruRow/AvatarBg"),  circleSprite);
            FrameAvatar(GameObject.Find("PlayerHUD")?.transform.Find("MochiRow/AvatarBg"), circleSprite);
        }

        private static void FrameAvatar(Transform avBg, Sprite frame)
        {
            if (avBg == null) return;

            // Replace AvatarBg's image with the circle frame
            var img = avBg.GetComponent<Image>();
            if (img == null) img = avBg.gameObject.AddComponent<Image>();
            img.sprite = frame;
            img.type   = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;

            // The actual avatar image (child) — inset so the frame border shows around it
            for (int i = 0; i < avBg.childCount; i++)
            {
                var child = avBg.GetChild(i);
                var childImg = child.GetComponent<Image>();
                if (childImg == null) continue;
                var rt = child.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                    rt.offsetMin = new Vector2(6, 6); rt.offsetMax = new Vector2(-6, -6);
                }
                childImg.preserveAspect = true;
            }
        }

        private static void EnsureSprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            bool changed = false;
            if (imp.textureType != TextureImporterType.Sprite)
            { imp.textureType = TextureImporterType.Sprite; changed = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single)
            { imp.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!imp.alphaIsTransparency)
            { imp.alphaIsTransparency = true; changed = true; }
            if (changed) imp.SaveAndReimport();
        }
    }
}
