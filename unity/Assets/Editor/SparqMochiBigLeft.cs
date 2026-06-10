using UnityEngine;
using UnityEditor;

namespace Sparq.Editor
{
    public static class SparqMochiBigLeft
    {
        [MenuItem("Sparq/113. Mochi → BIG + LEFT past hero (force scale)")]
        public static void Apply()
        {
            var mochi = GameObject.Find("Mochi");
            if (mochi == null)
            {
                EditorUtility.DisplayDialog("Sparq", "Mochi not in scene.", "OK");
                return;
            }

            var karu = GameObject.Find("Karu");

            // Reset any parent that might be scaling mochi weird
            if (mochi.transform.parent != null && mochi.transform.parent.gameObject.name != "Home")
            {
                Debug.Log($"[Mochi Fix] Was parented to {mochi.transform.parent.name}, unparenting.");
                mochi.transform.SetParent(null);
            }

            // FORCE the scale large — using lossyScale check
            mochi.transform.localScale = Vector3.one * 2.0f;

            // Position to LEFT of Karu (Karu is at -3.0 typically)
            float karuX = karu != null ? karu.transform.position.x : 0f;
            mochi.transform.position = new Vector3(karuX - 2.5f, -1.0f, 0f);

            // Verify the sprite is rendering
            var sr = mochi.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogWarning("[Mochi Fix] No SpriteRenderer — adding one.");
                sr = mochi.AddComponent<SpriteRenderer>();
            }
            // If no sprite, load mochi.svg as fallback
            if (sr.sprite == null)
            {
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sparq/mochi.svg");
                if (sp == null)
                {
                    foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Sparq/mochi.svg"))
                    {
                        if (o is Sprite spr) { sp = spr; break; }
                    }
                }
                if (sp != null) sr.sprite = sp;
            }

            sr.sortingOrder = 50; // SAME as Karu (in front so visible)
            sr.color = Color.white;
            sr.enabled = true;

            // Make sure it's active in hierarchy
            mochi.SetActive(true);

            Debug.Log($"[Mochi Fix] Pos={mochi.transform.position}, Scale={mochi.transform.localScale}, " +
                      $"LossyScale={mochi.transform.lossyScale}, Sprite={(sr.sprite != null ? sr.sprite.name : "NULL")}");

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq Mochi",
                $"✅ Mochi forced:\n\n" +
                $"• Position: ({mochi.transform.position.x:F1}, {mochi.transform.position.y:F1})\n" +
                $"• Local scale: 2.0 (lossy: {mochi.transform.lossyScale.x:F2})\n" +
                $"• Sprite: {(sr.sprite != null ? sr.sprite.name : "MISSING")}\n" +
                $"• Sorting order: 50\n\n" +
                "Hit ▶ Play. If still small, check Console for the [Mochi Fix] log.", "OK");
        }
    }
}
