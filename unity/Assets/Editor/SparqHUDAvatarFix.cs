using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqHUDAvatarFix
    {
        [MenuItem("Sparq/96. Fix HUD avatar (use chosen chibi)")]
        public static void Apply()
        {
            var hud = GameObject.Find("PlayerHUD");
            if (hud == null) return;

            // Disable the runtime KaruAvatarCapture (it was snapshotting old game state)
            var cap = hud.GetComponent<Sparq.UI.KaruAvatarCapture>();
            if (cap != null) Object.DestroyImmediate(cap);

            // Find the chibi sprite based on the chosen activePet
            var data = Sparq.Core.SaveService.Data;
            int chibiIdx = 1;
            if (data != null && !string.IsNullOrEmpty(data.activePet))
            {
                chibiIdx = data.activePet switch
                {
                    "kael" => 1,
                    "mira" => 22,
                    "rook" => 45,
                    "vex"  => 77,
                    "lyra" => 100,
                    _      => 1,
                };
            }
            string spritePath = $"Assets/Tancha_14/Chibi Characters Pack/Sprites/Chibi character_{chibiIdx}.png";

            var imp = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            // Find the Avatar Image inside HUD
            Image avatarImg = null;
            foreach (var img in hud.GetComponentsInChildren<Image>(true))
            {
                if (img != null && img.gameObject.name == "Avatar")
                {
                    avatarImg = img;
                    break;
                }
            }
            if (avatarImg == null)
            {
                EditorUtility.DisplayDialog("Sparq", "Avatar image not found in HUD.", "OK");
                return;
            }

            if (sprite != null)
            {
                avatarImg.sprite = sprite;
                avatarImg.color = Color.white;
                avatarImg.preserveAspect = true;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq",
                $"✅ HUD avatar updated.\n\n" +
                $"• Runtime snapshot disabled\n" +
                $"• Avatar = Chibi character #{chibiIdx} (matches your starter pick)\n\n" +
                "Hit ▶ Play.", "OK");
        }
    }
}
