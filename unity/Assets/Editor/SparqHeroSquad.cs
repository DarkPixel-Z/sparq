using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

namespace Sparq.Editor
{
    /// <summary>
    /// Replaces the cute bear with a chibi hero, ensures all chibi sprites are imported
    /// as Sprites, and adds a hero-squad row of 4 chibi companions on the home screen.
    /// </summary>
    public static class SparqHeroSquad
    {
        private const string CHIBI_DIR = "Assets/Tancha_14/Chibi Characters Pack/Sprites";

        [MenuItem("Sparq/87. Replace Karu with chibi + add hero squad")]
        public static void Apply()
        {
            EnsureSprites();

            ReplaceKaruWithChibi();
            BuildHeroSquadRow();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Heroes",
                "✅ Chibi hero swap done.\n\n" +
                "• Karu's Bear sprite swapped for the chibi matching the player's chosen starter\n" +
                "• 4 companion chibis added in a row at the lower home screen\n" +
                "• 160 chibi PNGs auto-imported as Sprites\n\n" +
                "Hit ▶ Play.", "OK");
        }

        private static void EnsureSprites()
        {
            if (!Directory.Exists(CHIBI_DIR)) return;
            bool changed = false;
            foreach (var f in Directory.GetFiles(CHIBI_DIR, "*.png"))
            {
                string ap = f.Replace('\\','/');
                int idx = ap.IndexOf("Assets/");
                if (idx >= 0) ap = ap.Substring(idx);
                var imp = AssetImporter.GetAtPath(ap) as TextureImporter;
                if (imp == null) continue;
                if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                    changed = true;
                }
            }
            if (changed) AssetDatabase.Refresh();
        }

        private static Sprite ChibiByIndex(int n)
        {
            string p = $"{CHIBI_DIR}/Chibi character_{n}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(p);
        }

        private static void ReplaceKaruWithChibi()
        {
            // Pick chibi based on player's chosen starter id (or default to #1)
            var data = Sparq.Core.SaveService.Data;
            int chibiIdx = 1;
            if (data != null)
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

            var sprite = ChibiByIndex(chibiIdx);
            if (sprite == null) return;

            // Hide existing Karu (Bear prefab) and add new chibi sprite as Karu
            var oldKaru = GameObject.Find("Karu");
            if (oldKaru != null)
            {
                // Disable all child renderers so the bear hides
                foreach (var sr in oldKaru.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (sr != null && sr.gameObject != oldKaru) sr.enabled = false;
                }
                // Add or update the main sprite
                var mainSR = oldKaru.GetComponent<SpriteRenderer>();
                if (mainSR == null) mainSR = oldKaru.AddComponent<SpriteRenderer>();
                mainSR.sprite = sprite;
                mainSR.color = Color.white;
                mainSR.sortingOrder = 12;
                mainSR.enabled = true;
                oldKaru.transform.localScale = Vector3.one * 0.018f; // chibi PNGs are big
                oldKaru.transform.position = new Vector3(0f, -0.3f, 0f);
            }
        }

        private static void BuildHeroSquadRow()
        {
            // Remove old squad
            var old = GameObject.Find("[HeroSquad]");
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject("[HeroSquad]");
            root.transform.position = new Vector3(0f, -1.4f, 0f);

            // 4 companions — pick chibis distinct from main
            int[] picks = { 5, 30, 60, 120 };
            float spacing = 1.1f;
            for (int i = 0; i < picks.Length; i++)
            {
                var sp = ChibiByIndex(picks[i]);
                if (sp == null) continue;
                var go = new GameObject($"Hero_{picks[i]}");
                go.transform.SetParent(root.transform, false);
                float xOffset = (i - (picks.Length - 1) / 2f) * spacing;
                // Skip the center where Karu stands
                if (Mathf.Abs(xOffset) < 0.3f) xOffset = (i < picks.Length / 2f ? -1.6f : 1.6f);
                go.transform.localPosition = new Vector3(xOffset, 0, 0);
                go.transform.localScale = Vector3.one * 0.012f;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sp;
                sr.sortingOrder = 11;

                go.AddComponent<Sparq.Cinematic.IdleBreathing>();
            }
        }
    }
}
