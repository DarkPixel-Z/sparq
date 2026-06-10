using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;

namespace Sparq.Editor
{
    /// <summary>
    /// Repairs the tap chain on Bear-Karu and attaches TapBattle.
    /// • Ensures Camera has Physics2DRaycaster
    /// • Ensures EventSystem exists
    /// • Removes broken old PetDisplay (had reference to old SVG renderer)
    /// • Adds a wide BoxCollider2D
    /// • Adds a Rigidbody2D (kinematic) — needed for some 2D click pipelines
    /// • Attaches TapBattle component
    /// </summary>
    public static class SparqWireTapBattle
    {
        [MenuItem("Sparq/33. Fix tap → ATTACK chain on Bear-Karu")]
        public static void Wire()
        {
            var karu = GameObject.Find("Karu");
            if (karu == null)
            {
                EditorUtility.DisplayDialog("Sparq", "No Karu in scene.", "OK");
                return;
            }

            // EventSystem
            var es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var esGO = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Debug.Log("[Sparq] Created EventSystem.");
            }

            // Camera + Physics2DRaycaster
            var cam = Camera.main;
            if (cam != null)
            {
                if (cam.GetComponent<Physics2DRaycaster>() == null)
                {
                    cam.gameObject.AddComponent<Physics2DRaycaster>();
                    Debug.Log("[Sparq] Added Physics2DRaycaster to Main Camera.");
                }
            }

            // Remove OLD PetDisplay (it referenced the old SVG sprites that don't exist on the bear)
            var oldPet = karu.GetComponent<Sparq.UI.PetDisplay>();
            if (oldPet != null) Object.DestroyImmediate(oldPet);

            // BoxCollider2D — generous size so taps land easily
            var col = karu.GetComponent<BoxCollider2D>();
            if (col == null) col = karu.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2.6f, 3.0f);
            col.offset = new Vector2(0f, 0.4f);
            col.isTrigger = false;

            // Kinematic Rigidbody2D — some Unity pipelines need this for OnMouseDown / Physics2DRaycaster
            var rb = karu.GetComponent<Rigidbody2D>();
            if (rb == null) rb = karu.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
            rb.gravityScale = 0f;

            // Attach TapBattle
            var tb = karu.GetComponent<Sparq.Systems.TapBattle>();
            if (tb == null) tb = karu.AddComponent<Sparq.Systems.TapBattle>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Sparq] Tap → Attack chain repaired.");
            EditorUtility.DisplayDialog("Sparq Tap Battle",
                "✅ Attack chain wired.\n\n" +
                "• Physics2DRaycaster on camera ✓\n" +
                "• EventSystem present ✓\n" +
                "• BoxCollider2D + Kinematic Rigidbody2D on Karu ✓\n" +
                "• Old broken PetDisplay removed ✓\n" +
                "• TapBattle attached ✓\n\n" +
                "Hit ▶ Play and tap Karu:\n" +
                "  • Bear face flips to ATTACK\n" +
                "  • Bear lunges right\n" +
                "  • Slash flashes on Volt\n" +
                "  • Volt's HP drains by 1\n" +
                "  • Hit SFX fires\n" +
                "  • Every 5 taps: +1 XP",
                "OK");
        }
    }
}
