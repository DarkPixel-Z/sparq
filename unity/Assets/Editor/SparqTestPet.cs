using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sparq.Editor
{
    /// <summary>Menu 204: Test the Pet popup + wire the PETS top-nav button to open it.</summary>
    public static class SparqTestPet
    {
        [MenuItem("Sparq/204. Open Pet Panel")]
        public static void OpenPet()
        {
            if (!Application.isPlaying)
            { EditorUtility.DisplayDialog("Sparq", "Hit ▶ Play first.", "OK"); return; }
            Sparq.UI.PetPanel.Show();
        }

        [MenuItem("Sparq/204b. Wire PETS top button → Pet Panel")]
        public static void WirePetsBtn()
        {
            var bar = GameObject.Find("HomeNavButtons");
            if (bar == null)
            {
                EditorUtility.DisplayDialog("Sparq", "HomeNavButtons not found.", "OK"); return;
            }
            var pets = bar.transform.Find("PetsBtn");
            if (pets == null)
            {
                EditorUtility.DisplayDialog("Sparq", "PetsBtn not found in HomeNavButtons.", "OK"); return;
            }
            var btn = pets.GetComponent<Button>();
            if (btn == null) { EditorUtility.DisplayDialog("Sparq", "PetsBtn has no Button.", "OK"); return; }
            // Add the PetPanel.Show listener (won't disturb existing listeners)
            btn.onClick.AddListener(Sparq.UI.PetPanel.Show);

            if (!Application.isPlaying)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Sparq", "✅ PETS button wired to open Pet Panel.", "OK");
        }
    }

}
