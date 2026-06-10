using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;

namespace Sparq.Editor
{
    /// <summary>
    /// Swap the static Karu SVG for the modular Bear from 2D Animal Character Pack.
    /// Tints it red-panda orange. Transfers PetDisplay + breathing + tap collider.
    /// </summary>
    public static class SparqKaruSwap
    {
        private const string BEAR_PREFAB = "Assets/2D Animal Character Pack/Prefabs/BearCatOwl.prefab";

        // Red panda tint — warm orange/rust
        private static readonly Color RED_PANDA_TINT = new Color(1.0f, 0.55f, 0.35f, 1.0f);

        [MenuItem("Sparq/29. Replace Karu with modular Bear (red-panda tint)")]
        public static void Swap()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BEAR_PREFAB);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Sparq Karu",
                    $"Prefab not found at:\n{BEAR_PREFAB}", "OK");
                return;
            }

            var oldKaru = GameObject.Find("Karu");
            if (oldKaru == null)
            {
                EditorUtility.DisplayDialog("Sparq Karu",
                    "No Karu in scene. Run Sparq → 2 first.", "OK");
                return;
            }

            // Capture the old transform + components we want to migrate
            var oldPos = oldKaru.transform.position;
            var oldScale = oldKaru.transform.localScale;

            // Instantiate the bear prefab in place
            var bear = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            bear.name = "Karu";
            bear.transform.position = oldPos;
            // Bear prefabs are usually in world units of ~1 — scale to read big
            bear.transform.localScale = oldScale * 1.4f;

            // Tint every SpriteRenderer to red-panda orange (skip the face — keep that white so eyes/mouth stay clear)
            foreach (var sr in bear.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr == null) continue;
                if (sr.gameObject.name.ToLower().Contains("face"))
                {
                    sr.color = Color.white;
                }
                else
                {
                    sr.color = RED_PANDA_TINT;
                }
                sr.sortingOrder = 5; // same plane as the old Karu
            }

            // Hand off interactivity
            var petDisplay = bear.AddComponent<Sparq.UI.PetDisplay>();
            // Add a box collider sized to the bear so taps register
            var col = bear.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.6f, 1.8f);
            col.offset = new Vector2(0, 0.2f);

            // Idle breathing — main body
            bear.AddComponent<Sparq.Cinematic.IdleBreathing>();

            // Make sure the camera has a Physics2DRaycaster (so OnPointerClick fires)
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<Physics2DRaycaster>() == null)
                cam.gameObject.AddComponent<Physics2DRaycaster>();

            // Disable old Karu (don't destroy yet — let user verify before deleting)
            oldKaru.name = "Karu_OLD_SVG_disabled";
            oldKaru.SetActive(false);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Selection.activeObject = bear;
            Debug.Log($"[Sparq Karu] Bear-Karu instantiated, old SVG Karu disabled.");
            EditorUtility.DisplayDialog("Sparq Karu",
                "✅ Karu replaced with modular Bear.\n\n" +
                "• Tinted red-panda orange\n" +
                "• Breathing + tap detection wired\n" +
                "• Old SVG Karu hidden (named 'Karu_OLD_SVG_disabled')\n\n" +
                "Hit ▶ Play. Tap the bear → SFX + XP fire.\n\n" +
                "If you don't like it: re-enable 'Karu_OLD_SVG_disabled' in the Hierarchy and delete the new 'Karu'.", "OK");
        }

        [MenuItem("Sparq/29a. Set Karu emotion → Happy")]
        public static void EmotionHappy() => SetEmotion("Face-happy");
        [MenuItem("Sparq/29b. Set Karu emotion → Angry")]
        public static void EmotionAngry() => SetEmotion("Face-angry");
        [MenuItem("Sparq/29c. Set Karu emotion → Hurt")]
        public static void EmotionHurt() => SetEmotion("Face-hurt");
        [MenuItem("Sparq/29d. Set Karu emotion → Idle")]
        public static void EmotionIdle() => SetEmotion("Face-idle");

        private static void SetEmotion(string spriteName)
        {
            var karu = GameObject.Find("Karu");
            if (karu == null) return;

            // Find the Face sprite renderer
            SpriteRenderer faceSR = null;
            foreach (var sr in karu.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.gameObject.name.ToLower() == "face") { faceSR = sr; break; }
            }
            if (faceSR == null) return;

            // Bear sprite sheet has internal sub-sprites by name
            var allSubs = AssetDatabase.LoadAllAssetsAtPath("Assets/2D Animal Character Pack/Sprites/Characters/Bears/Bear.png");
            foreach (var o in allSubs)
            {
                if (o is Sprite sp && sp.name.Equals(spriteName, System.StringComparison.OrdinalIgnoreCase))
                {
                    faceSR.sprite = sp;
                    Debug.Log($"[Karu] Emotion → {spriteName}");
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    return;
                }
            }
            Debug.LogWarning($"[Karu] Sprite '{spriteName}' not found in Bear.png. Make sure it's imported as multi-sprite.");
        }
    }
}
